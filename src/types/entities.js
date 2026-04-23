// Entity factories — spec shapes with defaults.
// Mutable entities carry `version: 1` on creation.
// Transient fields prefixed `_` — stripped before persistence, defaults on load.

const now = () => new Date().toISOString();
const uuid = () => crypto.randomUUID();

// --- Project ---

export function createProject({ name }) {
  const ts = now();
  return {
    id: uuid(),
    name,
    schemaVersion: '0.1.0',
    config: createProjectConfig(),
    createdAt: ts,
    updatedAt: ts,
  };
}

export function createProjectConfig(overrides = {}) {
  return {
    providers: {},
    ...overrides,
  };
}

// --- Library ---

export function createFolder({ projectId, parentFolderId = null, name }) {
  const ts = now();
  return {
    id: uuid(), projectId, parentFolderId, name,
    createdAt: ts, updatedAt: ts,
  };
}

export function createArticle({
  projectId, folderId, importId = null, importUnitIndex = null,
  refs = {}, title = '', authors = [], year = null, venue = null,
  abstract = null, keywords = { author: [], controlled: [] },
  citationCount = null, sourceFields = {},
  parseQuality = 'ok', parseNote = null,
}) {
  const ts = now();
  return {
    id: uuid(), projectId, folderId,
    importId, importUnitIndex,
    refs: { doi: null, arxivId: null, openAlexId: null, scopusEid: null, pubmedId: null, url: null, ...refs },
    title, authors, year, venue, abstract,
    keywords: { author: keywords.author || [], controlled: keywords.controlled || [] },
    citationCount, sourceFields,
    parseQuality, parseNote,
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

export function createNote({ projectId, folderId, name, content = '', tags = [] }) {
  const ts = now();
  return {
    id: uuid(), projectId, folderId, name, content, tags,
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

// --- Import ---

export function createImport({
  projectId, targetFolderId, fileName, fileHash,
  totalUnits, providerId, providerType, model, prompt,
  temperature = 0, maxTokens = 4096,
  budget = { maxCost: null, maxDuration: null, maxArticles: null },
}) {
  const ts = now();
  return {
    id: uuid(), projectId, targetFolderId,
    fileName, fileHash, totalUnits,
    providerId, providerType, model, prompt,
    temperature, maxTokens,
    status: 'running',
    budget, actualCost: 0,
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

// --- Protocol ---

export function createProtocol({
  projectId, versionLabel = '1',
  supersedes = null,
  providerId, providerType, model, temperature = 0,
  prompts = { semantics: null, screening: null, extraction: null },
  researchQuestions = [], threshold = 0.5,
  seedExamples = [], extractionSchema = null,
  embeddingModel = null,
}) {
  return {
    id: uuid(), projectId, versionLabel,
    supersedes,
    providerId, providerType, model, temperature,
    prompts, researchQuestions, threshold,
    seedExamples, extractionSchema,
    embeddingModel,
    createdAt: now(),
    // immutable once processing starts — no updatedAt, no version
  };
}

// --- Graph ---

export function createGraph({ projectId, reportId }) {
  const ts = now();
  return {
    id: uuid(), projectId, reportId,
    edges: [], // { fromNodeId, toNodeId }[]
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

export function createGraphNode({
  graphId, kind, // "source-ref" | "processing" | "section"
  // source-ref fields
  libraryItemId = null, libraryItemType = null,
  // processing fields
  protocolId = null, processingKind = null, subset = null,
  // section fields
  ordinal = null, sourcePrompt = null,
}) {
  const ts = now();
  return {
    id: uuid(), graphId, kind,
    // source-ref
    libraryItemId, libraryItemType,
    // processing
    protocolId, processingKind, subset,
    status: kind === 'processing' ? 'pending' : null,
    pauseReason: null,
    budget: null, actualCost: null, actualDuration: null,
    layerResultId: null,
    // section
    ordinal, sourcePrompt,
    renderedOutput: null, isStale: true,
    renderFingerprint: null,
    citedItemIds: [], predecessors: [], history: [],
    // common
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

// --- Report ---

export function createReport({ projectId, folderId = null, name, graphId = null, renderProviderId = null, renderModel = null }) {
  const ts = now();
  return {
    id: uuid(), projectId, folderId, name,
    graphId, renderProviderId, renderModel,
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

// --- Layer Results ---

export function createLayerResult({ projectId, folderId = null, graphId, processingNodeId }) {
  const ts = now();
  return {
    id: uuid(), projectId, folderId, graphId, processingNodeId,
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

export function createAssessment({
  layerResultId, protocolId, articleId,
  keySemantics = { topics: [], entities: [], keywords: [] },
  perRQ = [], tokenUsage = { input: 0, output: 0 }, cost = 0,
}) {
  return {
    id: uuid(), layerResultId, protocolId, articleId,
    keySemantics, perRQ, tokenUsage, cost,
    authorityClass: 'machine-interpretation',
    createdAt: now(),
    // immutable — no version
  };
}

export function createExtractionRecord({
  layerResultId, protocolId, articleId,
  fields = {}, citations = [], reasoning = '',
  tokenUsage = { input: 0, output: 0 }, cost = 0,
}) {
  return {
    id: uuid(), layerResultId, protocolId, articleId,
    fields, citations, reasoning, tokenUsage, cost,
    authorityClass: 'machine-interpretation',
    createdAt: now(),
  };
}

export function createDedupDecision({
  layerResultId, articleIdA, articleIdB,
  similarity, decision, reasoning, survivorId = null,
  tokenUsage = { input: 0, output: 0 }, cost = 0,
}) {
  return {
    id: uuid(), layerResultId,
    articleIdA, articleIdB, similarity,
    decision, reasoning, survivorId,
    tokenUsage, cost, createdAt: now(),
  };
}

export function createAdjudication({
  projectId, articleId, layerResultId, rqId,
  reviewerHandle, verdict, note = null,
}) {
  return {
    id: uuid(), projectId, articleId, layerResultId, rqId,
    reviewerHandle, verdict, note,
    authorityClass: 'researcher-confirmed',
    createdAt: now(),
  };
}

export function createDisagreement({
  projectId, articleId, rqId,
  layerResultIdA, layerResultIdB, adjudicationId = null,
}) {
  return {
    id: uuid(), projectId, articleId, rqId,
    layerResultIdA, layerResultIdB, adjudicationId,
    createdAt: now(),
  };
}

// --- Synthesis ---

export function createCode({
  projectId, label, parentId = null, evidenceRefs = [],
}) {
  const ts = now();
  return {
    id: uuid(), projectId, label, parentId, evidenceRefs,
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

export function createTheme({ projectId, label, codeIds = [] }) {
  return {
    id: uuid(), projectId, label, codeIds,
    createdAt: now(), version: 1,
  };
}

export function createFinding({ projectId, claimText, codeIds }) {
  if (!codeIds || codeIds.length === 0) {
    throw new Error('Finding requires at least one Code (spec: provenance is law)');
  }
  const ts = now();
  return {
    id: uuid(), projectId, claimText, codeIds,
    createdAt: ts, updatedAt: ts, version: 1,
  };
}

// --- Transient field handling ---

const TRANSIENT_PREFIX = '_';

export function stripTransient(entity) {
  const clean = {};
  for (const [k, v] of Object.entries(entity)) {
    if (!k.startsWith(TRANSIENT_PREFIX)) clean[k] = v;
  }
  return clean;
}

export function initTransient(entity, defaults = {}) {
  return { ...defaults, ...entity };
}

// Default transient fields per entity kind
export const TRANSIENT_DEFAULTS = {
  graphNode: { _isProcessing: false, _error: null },
  import: { _isProcessing: false, _error: null },
};
