// Storage interface (spec: Architecture — first of five interfaces)
// persistence of all entities with compare-and-swap versioning, scoped by projectId.
// Named operations — no ad-hoc .filter() in UI code.

import {
  dbPut, dbGet, dbGetAll, dbDelete, dbPutVersioned,
  dbPutBatch, dbClear, dbCount, clearProjectData, openDB,
} from '../storage/idb.js';
import { initTransient, TRANSIENT_DEFAULTS } from '../types/entities.js';

// --- Generic CRUD ---

/**
 * Put an entity (unversioned — for immutable entities like Protocol, Assessment).
 */
export async function put(storeName, entity) {
  return dbPut(storeName, entity);
}

/**
 * Put with compare-and-swap (versioned entities — Article, Note, Graph, GraphNode, etc.).
 * @param {string} storeName
 * @param {Object} entity - must have `id` and current field values
 * @param {number} expectedVersion - the version the caller believes is current
 * @returns {Object} the saved entity with incremented version
 * @throws {VersionConflictError} if version mismatch
 */
export async function putVersioned(storeName, entity, expectedVersion) {
  return dbPutVersioned(storeName, entity, expectedVersion);
}

/**
 * Get a single entity by id. Returns null if not found.
 */
export async function get(storeName, id) {
  return dbGet(storeName, id);
}

/**
 * Get all entities in a store, optionally filtered by index.
 */
export async function getAll(storeName, indexName, indexValue) {
  return dbGetAll(storeName, indexName, indexValue);
}

/**
 * Delete an entity by id.
 */
export async function del(storeName, id) {
  return dbDelete(storeName, id);
}

// --- Project-scoped queries ---

export async function getFolders(projectId) {
  return getAll('folders', 'by-project', projectId);
}

export async function getChildFolders(parentFolderId) {
  return getAll('folders', 'by-parent', parentFolderId);
}

export async function getArticles(projectId) {
  return getAll('articles', 'by-project', projectId);
}

export async function getArticlesByFolder(folderId) {
  return getAll('articles', 'by-folder', folderId);
}

export async function getArticlesByImport(importId) {
  return getAll('articles', 'by-import', importId);
}

export async function getNotes(projectId) {
  return getAll('notes', 'by-project', projectId);
}

export async function getNotesByFolder(folderId) {
  return getAll('notes', 'by-folder', folderId);
}

export async function getReports(projectId) {
  return getAll('reports', 'by-project', projectId);
}

export async function getImports(projectId) {
  return getAll('imports', 'by-project', projectId);
}

export async function getProtocols(projectId) {
  return getAll('protocols', 'by-project', projectId);
}

export async function getGraphs(projectId) {
  return getAll('graphs', 'by-project', projectId);
}

export async function getGraphByReport(reportId) {
  const results = await getAll('graphs', 'by-report', reportId);
  return results[0] || null;
}

export async function getGraphNodes(graphId) {
  const nodes = await getAll('graphNodes', 'by-graph', graphId);
  // spec: processing nodes saved as "running" corrected to "interrupted" on load
  return nodes.map(n => {
    const transientKey = n.kind === 'processing' ? 'graphNode' : null;
    const defaults = transientKey ? TRANSIENT_DEFAULTS[transientKey] : {};
    const entity = initTransient(n, defaults);
    if (entity.kind === 'processing' && entity.status === 'running') {
      entity.status = 'interrupted';
    }
    return entity;
  });
}

export async function getLayerResults(projectId) {
  return getAll('layerResults', 'by-project', projectId);
}

export async function getLayerResultByNode(processingNodeId) {
  const results = await getAll('layerResults', 'by-processing-node', processingNodeId);
  return results[0] || null;
}

export async function getAssessments(layerResultId) {
  return getAll('assessments', 'by-layer-result', layerResultId);
}

export async function getAssessmentsByArticle(articleId) {
  return getAll('assessments', 'by-article', articleId);
}

export async function getExtractionRecords(layerResultId) {
  return getAll('extractionRecords', 'by-layer-result', layerResultId);
}

export async function getAdjudications(projectId) {
  return getAll('adjudications', 'by-project', projectId);
}

export async function getAdjudicationsByArticle(articleId) {
  return getAll('adjudications', 'by-article', articleId);
}

export async function getDisagreements(projectId) {
  return getAll('disagreements', 'by-project', projectId);
}

export async function getCodes(projectId) {
  return getAll('codes', 'by-project', projectId);
}

export async function getThemes(projectId) {
  return getAll('themes', 'by-project', projectId);
}

export async function getFindings(projectId) {
  return getAll('findings', 'by-project', projectId);
}

// --- Specialized queries ---

/**
 * Idempotent resume for imports (spec: Import).
 * Returns unit indexes in 0..totalUnits-1 that lack a corresponding Article.
 */
export async function getUnprocessedUnits(importId, totalUnits) {
  const articles = await getArticlesByImport(importId);
  const processed = new Set(articles.map(a => a.importUnitIndex));
  const unprocessed = [];
  for (let i = 0; i < totalUnits; i++) {
    if (!processed.has(i)) unprocessed.push(i);
  }
  return unprocessed;
}

/**
 * Idempotent resume for screening (spec: Analyze/Screen).
 * Returns articles in the target set that lack an Assessment for this layer result.
 */
export async function getUnprocessedArticles(layerResultId, targetArticleIds) {
  const assessments = await getAssessments(layerResultId);
  const assessed = new Set(assessments.map(a => a.articleId));
  return targetArticleIds.filter(id => !assessed.has(id));
}

/**
 * Attention routing (spec: Focus).
 * Articles near the threshold — high-value review targets.
 */
export async function getArticlesNearThreshold(layerResultId, threshold, margin = 0.1) {
  const assessments = await getAssessments(layerResultId);
  const nearThreshold = assessments.filter(a =>
    a.perRQ.some(rq => {
      const dist = Math.abs(rq.relevanceScore - threshold);
      return dist <= margin;
    })
  );
  // Sort by closest to threshold
  nearThreshold.sort((a, b) => {
    const distA = Math.min(...a.perRQ.map(rq => Math.abs(rq.relevanceScore - threshold)));
    const distB = Math.min(...b.perRQ.map(rq => Math.abs(rq.relevanceScore - threshold)));
    return distA - distB;
  });
  return nearThreshold.map(a => a.articleId);
}

/**
 * Article assessments across all layers (spec: Write/drilldown).
 */
export async function getArticleAssessments(articleId) {
  return getAll('assessments', 'by-article', articleId);
}

/**
 * Items referencing an article — sections citing it, codes referencing it (spec: Write/drilldown).
 */
export async function getItemsReferencingArticle(projectId, articleId) {
  const refs = { sections: [], codes: [], adjudications: [] };

  // Sections citing this article
  const graphs = await getGraphs(projectId);
  for (const graph of graphs) {
    const nodes = await getGraphNodes(graph.id);
    for (const node of nodes) {
      if (node.kind === 'section' && node.citedItemIds?.some(c => c.itemId === articleId)) {
        refs.sections.push(node);
      }
    }
  }

  // Codes referencing this article
  const codes = await getCodes(projectId);
  for (const code of codes) {
    if (code.evidenceRefs?.some(r => r.entityId === articleId)) {
      refs.codes.push(code);
    }
  }

  // Adjudications for this article
  refs.adjudications = await getAdjudicationsByArticle(articleId);

  return refs;
}

// --- KeyStore (per-installation, never in project file) ---

export async function getApiKey(providerId) {
  const result = await dbGet('keyStore', providerId);
  return result?.apiKey || null;
}

export async function setApiKey(providerId, apiKey) {
  return dbPut('keyStore', { providerId, apiKey });
}

export async function deleteApiKey(providerId) {
  return dbDelete('keyStore', providerId);
}

export async function getAllApiKeys() {
  return dbGetAll('keyStore');
}

// --- Project lifecycle ---

/**
 * Load project data into IDB from deserialized project file contents.
 * Replaces all data for the given projectId.
 */
export async function loadProjectData(projectId, data) {
  // Clear existing project data first
  await clearProjectData(projectId);

  // Bulk insert each entity type
  if (data.project) await dbPut('projects', data.project);
  if (data.folders) await dbPutBatch('folders', data.folders);
  if (data.articles) await dbPutBatch('articles', data.articles);
  if (data.notes) await dbPutBatch('notes', data.notes);
  if (data.reports) await dbPutBatch('reports', data.reports);
  if (data.imports) await dbPutBatch('imports', data.imports);
  if (data.protocols) await dbPutBatch('protocols', data.protocols);
  if (data.graphs) await dbPutBatch('graphs', data.graphs);
  if (data.graphNodes) await dbPutBatch('graphNodes', data.graphNodes);
  if (data.layerResults) await dbPutBatch('layerResults', data.layerResults);
  if (data.assessments) await dbPutBatch('assessments', data.assessments);
  if (data.extractionRecords) await dbPutBatch('extractionRecords', data.extractionRecords);
  if (data.dedupDecisions) await dbPutBatch('dedupDecisions', data.dedupDecisions);
  if (data.adjudications) await dbPutBatch('adjudications', data.adjudications);
  if (data.disagreements) await dbPutBatch('disagreements', data.disagreements);
  if (data.codes) await dbPutBatch('codes', data.codes);
  if (data.themes) await dbPutBatch('themes', data.themes);
  if (data.findings) await dbPutBatch('findings', data.findings);
}

/**
 * Serialize all project data from IDB for saving.
 * Returns the structured data object (not the zip — that's the project interface's job).
 */
export async function serializeProjectData(projectId) {
  const project = await dbGet('projects', projectId);
  if (!project) throw new Error(`Project ${projectId} not found in IDB`);

  return {
    project,
    folders: await getFolders(projectId),
    articles: await getArticles(projectId),
    notes: await getNotes(projectId),
    reports: await getReports(projectId),
    imports: await getImports(projectId),
    protocols: await getProtocols(projectId),
    graphs: await getGraphs(projectId),
    graphNodes: await collectGraphNodes(projectId),
    layerResults: await getLayerResults(projectId),
    assessments: await collectAssessments(projectId),
    extractionRecords: await collectExtractionRecords(projectId),
    dedupDecisions: await collectDedupDecisions(projectId),
    adjudications: await getAdjudications(projectId),
    disagreements: await getDisagreements(projectId),
    codes: await getCodes(projectId),
    themes: await getThemes(projectId),
    findings: await getFindings(projectId),
  };
}

// Collect child entities that are indexed by parent, not projectId directly
async function collectGraphNodes(projectId) {
  const graphs = await getGraphs(projectId);
  const nodes = [];
  for (const g of graphs) {
    nodes.push(...await getGraphNodes(g.id));
  }
  return nodes;
}

async function collectAssessments(projectId) {
  const layerResults = await getLayerResults(projectId);
  const all = [];
  for (const lr of layerResults) {
    all.push(...await getAssessments(lr.id));
  }
  return all;
}

async function collectExtractionRecords(projectId) {
  const layerResults = await getLayerResults(projectId);
  const all = [];
  for (const lr of layerResults) {
    all.push(...await getExtractionRecords(lr.id));
  }
  return all;
}

async function collectDedupDecisions(projectId) {
  const layerResults = await getLayerResults(projectId);
  const all = [];
  for (const lr of layerResults) {
    all.push(...await getAll('dedupDecisions', 'by-layer-result', lr.id));
  }
  return all;
}

/**
 * Remove all data for a project from IDB (spec: "close project" removes projectId data).
 */
export async function removeProject(projectId) {
  await clearProjectData(projectId);
  await dbDelete('projects', projectId);
}

/**
 * List all projects currently in IDB.
 */
export async function listProjects() {
  return dbGetAll('projects');
}

// --- Dirty tracking ---

let _dirty = false;
let _dirtyListeners = [];

export function isDirty() { return _dirty; }

export function markDirty() {
  _dirty = true;
  _dirtyListeners.forEach(fn => fn(true));
}

export function markClean() {
  _dirty = false;
  _dirtyListeners.forEach(fn => fn(false));
}

export function onDirtyChange(fn) {
  _dirtyListeners.push(fn);
  return () => { _dirtyListeners = _dirtyListeners.filter(f => f !== fn); };
}
