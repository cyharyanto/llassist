// Import engine (spec: Import)
// Two stages: integration.splitFile (deterministic) → LLM ingest (semantic parsing).
// Progress is idempotent: writing the Article IS the progress.
// Sequential with configurable inter-call delay. Budget enforcement on actuals.

import { splitFile, detectFormat } from '../interfaces/integration.js';
import * as llm from '../interfaces/llm.js';
import * as storage from '../interfaces/storage.js';
import { createArticle, createImport } from '../types/entities.js';
import { INGESTION_PROMPT } from '../prompts/prompts.js';
import { RetryableError, TerminalError } from '../utils/errors.js';

const DEFAULT_DELAY_MS = 500;

/**
 * Run an import operation.
 * @param {Object} params
 * @param {string} params.projectId
 * @param {string} params.folderId - target folder
 * @param {string} params.fileName
 * @param {string} params.fileContent - raw file text
 * @param {Object} params.llmConfig - resolved LLMConfig
 * @param {Object} [params.budget] - { maxCost, maxArticles }
 * @param {number} [params.delayMs] - inter-call delay
 * @param {Object} params.observer - { onProgress, onComplete, onError, onPause }
 * @param {AbortSignal} [params.signal] - for cancellation
 */
export async function runImport({
  projectId, folderId, fileName, fileContent, llmConfig,
  budget = {}, delayMs = DEFAULT_DELAY_MS,
  observer = {}, signal,
}) {
  const format = detectFormat(fileName);
  if (!format) throw new TerminalError(`Unsupported file format: ${fileName}`);

  const units = splitFile(fileContent, format);
  if (!units.length) throw new TerminalError('No records found in file');

  const fileHash = await hashContent(fileContent);

  // Resume existing incomplete import for same file, or create new
  const existingImports = await storage.getImports(projectId);
  let importRecord = existingImports.find(
    i => i.fileHash === fileHash && i.status !== 'completed'
  );

  if (importRecord) {
    // Resume — update status
    importRecord.status = 'running';
    importRecord.updatedAt = new Date().toISOString();
    await storage.put('imports', importRecord);
  } else {
    importRecord = createImport({
      projectId, targetFolderId: folderId,
      fileName, fileHash,
      totalUnits: units.length,
      providerId: llmConfig._providerId || 'default',
      providerType: llmConfig.type,
      model: llmConfig.model,
      prompt: INGESTION_PROMPT,
      temperature: llmConfig.temperature,
      maxTokens: llmConfig.maxTokens,
      budget,
    });
    await storage.put('imports', importRecord);
  }
  storage.markDirty();

  // Get unprocessed units (idempotent resume)
  const unprocessed = await storage.getUnprocessedUnits(importRecord.id, units.length);

  let processedCount = units.length - unprocessed.length;

  for (const unitIndex of unprocessed) {
    // Check cancellation
    if (signal?.aborted) {
      await updateImportStatus(importRecord.id, 'paused');
      observer.onPause?.('cancelled');
      return importRecord;
    }

    // Budget check
    if (budget.maxCost && importRecord.actualCost >= budget.maxCost) {
      await updateImportStatus(importRecord.id, 'paused');
      observer.onPause?.('budget-exceeded');
      return importRecord;
    }
    if (budget.maxArticles && processedCount >= budget.maxArticles) {
      await updateImportStatus(importRecord.id, 'paused');
      observer.onPause?.('article-limit');
      return importRecord;
    }

    try {
      const unit = units[unitIndex];
      const result = await llm.complete(
        llmConfig,
        INGESTION_PROMPT,
        unit,
        true, // request JSON
      );

      let parsed;
      try {
        parsed = JSON.parse(result.content);
      } catch {
        // LLM returned non-JSON — mark degraded
        parsed = {
          title: `Parse error (unit ${unitIndex})`,
          authors: [], year: null, venue: null, abstract: null,
          refs: {}, keywords: { author: [], controlled: [] },
          citationCount: null, sourceFields: { rawResponse: result.content },
          parseQuality: 'degraded',
          parseNote: 'LLM returned non-JSON response',
        };
      }

      const article = createArticle({
        projectId, folderId,
        importId: importRecord.id,
        importUnitIndex: unitIndex,
        title: parsed.title || '',
        authors: parsed.authors || [],
        year: parsed.year,
        venue: parsed.venue,
        abstract: parsed.abstract,
        refs: parsed.refs || {},
        keywords: parsed.keywords || { author: [], controlled: [] },
        citationCount: parsed.citationCount,
        sourceFields: parsed.sourceFields || {},
        parseQuality: parsed.parseQuality || 'ok',
        parseNote: parsed.parseNote,
      });

      await storage.put('articles', article);

      // Update cost tracking
      importRecord.actualCost += result.cost;
      await storage.put('imports', importRecord);
      storage.markDirty();

      processedCount++;
      observer.onProgress?.(processedCount, units.length, article);

    } catch (err) {
      observer.onError?.(err, unitIndex);
      // Retryable — skip this unit, continue
      if (err instanceof RetryableError) continue;
      // Terminal — pause
      await updateImportStatus(importRecord.id, 'failed');
      throw err;
    }

    // Inter-call delay
    if (delayMs > 0) {
      await new Promise(r => setTimeout(r, delayMs));
    }
  }

  await updateImportStatus(importRecord.id, 'completed');
  observer.onComplete?.(importRecord);
  return importRecord;
}

async function updateImportStatus(importId, status) {
  const record = await storage.get('imports', importId);
  if (record) {
    record.status = status;
    record.updatedAt = new Date().toISOString();
    await storage.put('imports', record);
  }
}

async function hashContent(content) {
  const encoder = new TextEncoder();
  const data = encoder.encode(content);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}

/**
 * Estimate cost before starting import.
 * @param {number} unitCount
 * @param {Object} pricing - { input, output } per million tokens
 * @param {number} avgInputTokens - estimated tokens per unit
 * @param {number} avgOutputTokens - estimated output tokens
 * @returns {{ low: number, high: number }}
 */
export function estimateCost(unitCount, pricing, avgInputTokens = 300, avgOutputTokens = 400) {
  const perUnit = ((avgInputTokens / 1_000_000) * pricing.input) +
                  ((avgOutputTokens / 1_000_000) * pricing.output);
  return {
    low: perUnit * unitCount * 0.8,
    high: perUnit * unitCount * 1.3,
  };
}
