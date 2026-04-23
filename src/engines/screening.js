// Screening engine (spec: Analyze/Screen)
// Two LLM calls per article — Chain-of-Thought by construction.
// Phase 1: extract key semantics (topics, entities, keywords).
// Phase 2: estimate relevance per RQ using semantics as context.
// Progress is idempotent: writing the Assessment IS the progress.

import * as llm from '../interfaces/llm.js';
import * as storage from '../interfaces/storage.js';
import { createAssessment, createLayerResult } from '../types/entities.js';
import { SCREENING_PHASE1_PROMPT, SCREENING_PHASE2_PROMPT, substitutePrompt } from '../prompts/prompts.js';

const DEFAULT_DELAY_MS = 500;

/**
 * Run screening on a set of articles.
 * @param {Object} params
 * @param {string} params.projectId
 * @param {Object} params.protocol - Protocol entity
 * @param {string[]} params.articleIds - target set
 * @param {Object} params.llmConfig - resolved LLMConfig
 * @param {string} params.layerResultId - existing LayerResult or null to create
 * @param {string} params.graphId
 * @param {string} params.processingNodeId
 * @param {number} [params.delayMs]
 * @param {Object} [params.budget]
 * @param {Object} params.observer - { onProgress, onComplete, onError, onPause }
 * @param {AbortSignal} [params.signal]
 */
export async function runScreening({
  projectId, protocol, articleIds, llmConfig,
  layerResultId, graphId, processingNodeId,
  delayMs = DEFAULT_DELAY_MS, budget = {},
  observer = {}, signal,
}) {
  // Create or reuse LayerResult
  let layerResult;
  if (layerResultId) {
    layerResult = await storage.get('layerResults', layerResultId);
  }
  if (!layerResult) {
    layerResult = createLayerResult({ projectId, graphId, processingNodeId });
    await storage.put('layerResults', layerResult);
    // Persist layerResultId on processing node so resume can discover it
    const pNode = await storage.get('graphNodes', processingNodeId);
    if (pNode && !pNode.layerResultId) {
      pNode.layerResultId = layerResult.id;
      await storage.put('graphNodes', pNode);
    }
    storage.markDirty();
  }

  // Get unprocessed articles (idempotent resume)
  const unprocessed = await storage.getUnprocessedArticles(layerResult.id, articleIds);
  let processedCount = articleIds.length - unprocessed.length;
  let totalCost = 0;

  for (const articleId of unprocessed) {
    if (signal?.aborted) {
      observer.onPause?.('cancelled');
      return layerResult;
    }

    if (budget.maxCost && totalCost >= budget.maxCost) {
      observer.onPause?.('budget-exceeded');
      return layerResult;
    }

    const article = await storage.get('articles', articleId);
    if (!article) continue;

    try {
      const assessment = await screenArticle(article, protocol, llmConfig, layerResult.id);
      await storage.put('assessments', assessment);
      storage.markDirty();

      totalCost += assessment.cost;
      processedCount++;
      observer.onProgress?.(processedCount, articleIds.length, article);

    } catch (err) {
      observer.onError?.(err, article);
      // Continue on error — skip this article
      processedCount++;
      continue;
    }

    if (delayMs > 0) {
      await new Promise(r => setTimeout(r, delayMs));
    }
  }

  observer.onComplete?.(layerResult);
  return layerResult;
}

const SCREENING_SYSTEM = 'You are a systematic review assistant. Respond with ONLY valid JSON matching the requested schema. No markdown, no explanation outside the JSON.';

async function screenArticle(article, protocol, llmConfig, layerResultId) {
  const contentStr = `Title: ${article.title || '(no title)'}\nAbstract: ${article.abstract || '(no abstract)'}`;

  // Phase 1: Key Semantics — prompt template becomes user message
  const phase1UserMsg = substitutePrompt(
    protocol.prompts?.semantics || SCREENING_PHASE1_PROMPT,
    { content: contentStr }
  );

  const phase1Result = await llm.complete(llmConfig, SCREENING_SYSTEM, phase1UserMsg, true);
  let keySemantics;
  try {
    keySemantics = JSON.parse(phase1Result.content);
  } catch {
    keySemantics = { topics: [], entities: [], keywords: [] };
  }

  // Phase 2: Relevance per RQ
  const perRQ = [];
  let totalInput = phase1Result.tokenUsage.input;
  let totalOutput = phase1Result.tokenUsage.output;
  let totalCost = phase1Result.cost;

  const enrichedContent = `${contentStr}\n\nKey Semantics:\nTopics: ${(keySemantics.topics || []).join(', ')}\nEntities: ${(keySemantics.entities || []).join(', ')}\nKeywords: ${(keySemantics.keywords || []).join(', ')}`;

  for (const rq of protocol.researchQuestions) {
    const phase2UserMsg = substitutePrompt(
      protocol.prompts?.screening || SCREENING_PHASE2_PROMPT,
      {
        content: enrichedContent,
        contentType: 'abstract',
        question: rq.text,
        definitions: (rq.definitions || []).join('\n'),
      }
    );

    const phase2Result = await llm.complete(llmConfig, SCREENING_SYSTEM, phase2UserMsg, true);
    totalInput += phase2Result.tokenUsage.input;
    totalOutput += phase2Result.tokenUsage.output;
    totalCost += phase2Result.cost;

    let rqResult;
    try {
      rqResult = JSON.parse(phase2Result.content);
    } catch {
      rqResult = {
        reasoning: 'Failed to parse LLM response',
        relevanceScore: 0,
        contributionScore: 0,
        recommendation: 'exclude',
      };
    }

    perRQ.push({
      rqId: rq.id,
      reasoning: rqResult.reasoning || '',
      relevanceScore: Math.max(0, Math.min(1, rqResult.relevanceScore || 0)),
      contributionScore: Math.max(0, Math.min(1, rqResult.contributionScore || 0)),
      recommendation: rqResult.recommendation || 'exclude',
    });
  }

  return createAssessment({
    layerResultId,
    protocolId: protocol.id,
    articleId: article.id,
    keySemantics,
    perRQ,
    tokenUsage: { input: totalInput, output: totalOutput },
    cost: totalCost,
  });
}
