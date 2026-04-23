// Application state (spec: "DOM is a projection")
// State lives in signals. DOM renders from state. DOM events dispatch to state.
// The state layer never reads the DOM.

import { signal, computed } from '@preact/signals';
import * as storage from '../interfaces/storage.js';
import * as projectIO from '../interfaces/project.js';
import * as llmInterface from '../interfaces/llm.js';
import { createProject, createFolder, createNote, createProtocol, createGraph, createGraphNode, createReport, createLayerResult } from '../types/entities.js';
import { runImport } from '../engines/import.js';
import { runScreening } from '../engines/screening.js';
import { renderSection } from '../engines/render.js';
import { deriveAssessmentStats, deriveRQDistribution, deriveAssessmentStatus } from '../utils/threshold.js';

// === Core state ===

export const currentProjectId = signal(null);
export const currentProject = signal(null);
export const projects = signal([]);
export const folders = signal([]);
export const articles = signal([]);
export const notes = signal([]);
export const reports = signal([]);
export const imports = signal([]);
export const protocols = signal([]);
export const assessments = signal([]);
export const layerResults = signal([]);

// UI state
export const selectedFolderId = signal(null);
export const selectedItemId = signal(null);
export const selectedItemType = signal(null);
export const view = signal('home');
export const errors = signal([]);

// Pipeline state
export const importProgress = signal(null);
export const screeningProgress = signal(null);
export const activeThreshold = signal(0.5);
export const activeProtocol = signal(null);
export const activeLayerResult = signal(null);
export const activeReport = signal(null);
export const reportSections = signal([]);
export const renderingSection = signal(null);

// === Initialize ===

export async function initialize() {
  projects.value = await storage.listProjects();
}

// === Project lifecycle ===

export async function newProject(name) {
  const proj = createProject({ name });
  await storage.put('projects', proj);
  storage.markDirty();
  await openProjectById(proj.id);
  return proj;
}

export async function openProjectById(projectId) {
  const proj = await storage.get('projects', projectId);
  if (!proj) throw new Error(`Project ${projectId} not found`);
  currentProjectId.value = projectId;
  currentProject.value = proj;
  view.value = 'library';
  await refreshAll();
  projects.value = await storage.listProjects();
}

export async function openProjectFile(file) {
  const proj = await projectIO.openProject(file);
  await openProjectById(proj.id);
  return proj;
}

export async function saveCurrentProject() {
  const pid = currentProjectId.value;
  if (!pid) return;
  const blob = await projectIO.saveProject(pid);
  const proj = currentProject.value;
  const filename = `${proj.name.replace(/[^a-zA-Z0-9_-]/g, '_')}.llassist`;
  projectIO.downloadBlob(blob, filename);
}

export async function closeProject() {
  const pid = currentProjectId.value;
  if (!pid) return;
  await storage.removeProject(pid);
  currentProjectId.value = null;
  currentProject.value = null;
  folders.value = []; articles.value = []; notes.value = [];
  reports.value = []; imports.value = []; protocols.value = [];
  assessments.value = []; layerResults.value = [];
  activeProtocol.value = null; activeLayerResult.value = null;
  activeReport.value = null; reportSections.value = [];
  importProgress.value = null; screeningProgress.value = null;
  view.value = 'home';
  projects.value = await storage.listProjects();
}

// === Refresh ===

export async function refreshAll() {
  const pid = currentProjectId.value;
  if (!pid) return;
  folders.value = await storage.getFolders(pid);
  articles.value = await storage.getArticles(pid);
  notes.value = await storage.getNotes(pid);
  reports.value = await storage.getReports(pid);
  imports.value = await storage.getImports(pid);
  protocols.value = await storage.getProtocols(pid);
  layerResults.value = await storage.getLayerResults(pid);

  if (protocols.value.length && !activeProtocol.value) {
    activeProtocol.value = protocols.value[protocols.value.length - 1];
  }
  if (layerResults.value.length && !activeLayerResult.value) {
    activeLayerResult.value = layerResults.value[layerResults.value.length - 1];
    await refreshAssessments();
  }
  if (reports.value.length && !activeReport.value) {
    activeReport.value = reports.value[reports.value.length - 1];
    await refreshReportSections();
  }
}

export async function refreshAssessments() {
  const lr = activeLayerResult.value;
  if (!lr) { assessments.value = []; return; }
  assessments.value = await storage.getAssessments(lr.id);
}

async function refreshReportSections() {
  const report = activeReport.value;
  if (!report?.graphId) { reportSections.value = []; return; }
  const nodes = await storage.getGraphNodes(report.graphId);
  reportSections.value = nodes
    .filter(n => n.kind === 'section')
    .sort((a, b) => (a.ordinal || 0) - (b.ordinal || 0));
}

// === Library ===

export async function addFolder(name, parentFolderId = null) {
  const pid = currentProjectId.value;
  const folder = createFolder({ projectId: pid, parentFolderId, name });
  await storage.put('folders', folder);
  storage.markDirty();
  await refreshAll();
  return folder;
}

export async function addNote(name, folderId = null) {
  const pid = currentProjectId.value;
  const note = createNote({ projectId: pid, folderId, name });
  await storage.put('notes', note);
  storage.markDirty();
  await refreshAll();
  return note;
}

export async function updateNote(note, expectedVersion) {
  const updated = await storage.putVersioned('notes', note, expectedVersion);
  storage.markDirty();
  await refreshAll();
  return updated;
}

export async function deleteItem(storeName, id) {
  await storage.del(storeName, id);
  storage.markDirty();
  await refreshAll();
}

export const folderTree = computed(() => {
  const all = folders.value;
  const byParent = new Map();
  for (const f of all) {
    const parent = f.parentFolderId || '__root__';
    if (!byParent.has(parent)) byParent.set(parent, []);
    byParent.get(parent).push(f);
  }
  return byParent;
});

export const currentFolderItems = computed(() => {
  const fid = selectedFolderId.value;
  return {
    articles: articles.value.filter(a => (a.folderId || null) === fid),
    notes: notes.value.filter(n => (n.folderId || null) === fid),
    reports: reports.value.filter(r => (r.folderId || null) === fid),
  };
});

// === Errors ===

export function pushError(type, message) {
  errors.value = [...errors.value, {
    id: crypto.randomUUID(), type, message, timestamp: new Date().toISOString(),
  }];
}

export function clearError(errorId) {
  errors.value = errors.value.filter(e => e.id !== errorId);
}

// === Provider config + API keys ===

export async function updateProviderConfig(providerId, config) {
  const proj = currentProject.value;
  if (!proj) return;
  const updated = {
    ...proj,
    config: { ...proj.config, providers: { ...(proj.config?.providers || {}), [providerId]: config } },
  };
  await storage.put('projects', updated);
  currentProject.value = updated;
  storage.markDirty();
}

export async function setApiKey(providerId, apiKey) {
  await storage.setApiKey(providerId, apiKey);
}

export async function getApiKey(providerId) {
  return storage.getApiKey(providerId);
}

export async function resolveLLMConfig(providerId) {
  const proj = currentProject.value;
  const providers = proj?.config?.providers || {};
  return llmInterface.resolveConfig(providers, providerId, storage.getApiKey);
}

// === Import ===

let importAbort = null;

export async function startImport(file, folderId, providerId) {
  const pid = currentProjectId.value;
  const llmConfig = await resolveLLMConfig(providerId);
  llmConfig._providerId = providerId;

  const content = await file.text();
  importAbort = new AbortController();
  importProgress.value = { processed: 0, total: 0, status: 'starting', currentTitle: '' };

  try {
    await runImport({
      projectId: pid, folderId, fileName: file.name, fileContent: content,
      llmConfig, signal: importAbort.signal,
      observer: {
        onProgress: (processed, total, article) => {
          importProgress.value = { processed, total, status: 'running', currentTitle: article.title };
          storage.getArticles(pid).then(a => { articles.value = a; });
        },
        onComplete: () => {
          importProgress.value = { ...importProgress.value, status: 'completed' };
          refreshAll();
        },
        onError: (err, unitIndex) => {
          pushError('retryable', `Import unit ${unitIndex}: ${err.message}`);
        },
        onPause: (reason) => {
          importProgress.value = { ...importProgress.value, status: `paused: ${reason}` };
        },
      },
    });
  } catch (err) {
    importProgress.value = { ...importProgress.value, status: 'failed' };
    pushError('terminal', `Import failed: ${err.message}`);
  }
}

export function cancelImport() { importAbort?.abort(); }

// === Protocol ===

export async function createAndSetProtocol({ researchQuestions, providerId, model, threshold = 0.5 }) {
  const pid = currentProjectId.value;
  const proj = currentProject.value;
  const provider = proj?.config?.providers?.[providerId];
  const prior = activeProtocol.value;

  const protocol = createProtocol({
    projectId: pid, providerId,
    supersedes: prior?.id || null,
    providerType: provider?.type || 'openai',
    model: model || provider?.defaultModel || '',
    researchQuestions, threshold,
  });

  await storage.put('protocols', protocol);
  storage.markDirty();
  activeProtocol.value = protocol;
  protocols.value = await storage.getProtocols(pid);
  return protocol;
}

// === Screening ===

let screeningAbort = null;

export async function startScreening(providerId) {
  const pid = currentProjectId.value;
  const protocol = activeProtocol.value;
  if (!protocol) throw new Error('No active protocol');

  const llmConfig = await resolveLLMConfig(providerId || protocol.providerId);
  const articleIds = articles.value.map(a => a.id);
  if (!articleIds.length) throw new Error('No articles to screen');

  // Ensure Report + Graph exist
  let report = activeReport.value;
  let graph;
  if (!report) {
    graph = createGraph({ projectId: pid, reportId: null });
    await storage.put('graphs', graph);
    report = createReport({ projectId: pid, name: 'Analysis', graphId: graph.id, renderProviderId: protocol.providerId, renderModel: protocol.model });
    await storage.put('reports', report);
    graph.reportId = report.id;
    await storage.put('graphs', graph);
    storage.markDirty();
    activeReport.value = report;
  } else {
    graph = await storage.getGraphByReport(report.id);
    if (!graph) {
      graph = createGraph({ projectId: pid, reportId: report.id });
      await storage.put('graphs', graph);
      report = { ...report, graphId: graph.id };
      await storage.put('reports', report);
      activeReport.value = report;
    }
  }

  // Find existing incomplete screening node for this protocol, or create new
  const existingNodes = await storage.getGraphNodes(graph.id);
  let processingNode = existingNodes.find(
    n => n.kind === 'processing' && n.protocolId === protocol.id
      && n.processingKind === 'screening'
      && (n.status === 'interrupted' || n.status === 'paused' || n.status === 'running')
  );

  let existingLayerResultId = null;
  if (processingNode) {
    // Resume existing node
    existingLayerResultId = processingNode.layerResultId;
    processingNode.status = 'running';
    processingNode.updatedAt = new Date().toISOString();
    await storage.put('graphNodes', processingNode);
  } else {
    processingNode = createGraphNode({
      graphId: graph.id, kind: 'processing',
      protocolId: protocol.id, processingKind: 'screening',
      subset: { type: 'all' },
    });
    processingNode.status = 'running';
    await storage.put('graphNodes', processingNode);
  }

  screeningAbort = new AbortController();
  screeningProgress.value = { processed: 0, total: articleIds.length, status: 'running', currentTitle: '' };

  try {
    const layerResult = await runScreening({
      projectId: pid, protocol, articleIds, llmConfig,
      layerResultId: existingLayerResultId,
      graphId: graph.id, processingNodeId: processingNode.id,
      signal: screeningAbort.signal,
      observer: {
        onProgress: async (processed, total, article) => {
          screeningProgress.value = { processed, total, status: 'running', currentTitle: article.title };
          // Refresh assessments periodically for live results view
          if (processed % 10 === 0 || processed === total) {
            const lr = await storage.getLayerResultByNode(processingNode.id);
            if (lr) {
              activeLayerResult.value = lr;
              assessments.value = await storage.getAssessments(lr.id);
            }
          }
        },
        onComplete: async (lr) => {
          screeningProgress.value = { ...screeningProgress.value, status: 'completed' };
          activeLayerResult.value = lr;
          const freshNode = await storage.get('graphNodes', processingNode.id);
          if (freshNode) {
            await storage.putVersioned('graphNodes', { ...freshNode, status: 'completed' }, freshNode.version);
          }
          await refreshAll();
          await refreshAssessments();
        },
        onError: (err, article) => {
          pushError('retryable', `Screening ${article?.title}: ${err.message}`);
        },
        onPause: (reason) => {
          screeningProgress.value = { ...screeningProgress.value, status: `paused: ${reason}` };
        },
      },
    });
    activeLayerResult.value = layerResult;
    layerResults.value = await storage.getLayerResults(pid);
  } catch (err) {
    screeningProgress.value = { ...screeningProgress.value, status: 'failed' };
    pushError('terminal', `Screening failed: ${err.message}`);
    const freshNode = await storage.get('graphNodes', processingNode.id);
    if (freshNode) {
      await storage.putVersioned('graphNodes', { ...freshNode, status: 'failed' }, freshNode.version);
    }
  }
}

export function cancelScreening() { screeningAbort?.abort(); }

// === Results (derived, zero network calls) ===

export const assessmentStats = computed(() => {
  const a = assessments.value;
  const t = activeThreshold.value;
  if (!a.length) return null;
  return deriveAssessmentStats(a, t);
});

export const rqDistribution = computed(() => {
  const a = assessments.value;
  const t = activeThreshold.value;
  const p = activeProtocol.value;
  if (!a.length || !p) return [];
  return deriveRQDistribution(a, t, p.researchQuestions);
});

export const articleRelevance = computed(() => {
  const assmts = assessments.value;
  const t = activeThreshold.value;
  const arts = articles.value;
  if (!assmts.length) return [];

  const byArticle = new Map();
  for (const a of assmts) byArticle.set(a.articleId, a);

  return arts.map(article => {
    const assessment = byArticle.get(article.id);
    if (!assessment) return { article, status: null };
    return { article, assessment, status: deriveAssessmentStatus(assessment, t) };
  }).sort((a, b) => {
    if (a.status?.mustRead && !b.status?.mustRead) return -1;
    if (!a.status?.mustRead && b.status?.mustRead) return 1;
    const sA = a.assessment ? Math.max(...a.assessment.perRQ.map(r => r.relevanceScore)) : -1;
    const sB = b.assessment ? Math.max(...b.assessment.perRQ.map(r => r.relevanceScore)) : -1;
    return sB - sA;
  });
});

// === Report + sections ===

export async function createReportIfNeeded() {
  const pid = currentProjectId.value;
  let report = activeReport.value;
  if (report) return report;
  const graph = createGraph({ projectId: pid, reportId: null });
  await storage.put('graphs', graph);
  report = createReport({ projectId: pid, name: 'Report', graphId: graph.id });
  await storage.put('reports', report);
  graph.reportId = report.id;
  await storage.put('graphs', graph);
  storage.markDirty();
  activeReport.value = report;
  reports.value = await storage.getReports(pid);
  return report;
}

export async function addSection(sourcePrompt = '') {
  const report = await createReportIfNeeded();
  const sections = reportSections.value;
  const ordinal = sections.length > 0 ? Math.max(...sections.map(s => s.ordinal || 0)) + 1 : 1;
  const node = createGraphNode({ graphId: report.graphId, kind: 'section', ordinal, sourcePrompt });
  await storage.put('graphNodes', node);
  storage.markDirty();
  await refreshReportSections();
  return node;
}

export async function updateSectionPrompt(sectionId, sourcePrompt) {
  const node = await storage.get('graphNodes', sectionId);
  if (!node) return;
  const updated = {
    ...node, sourcePrompt, isStale: true,
    history: [...(node.history || []), { sourcePrompt: node.sourcePrompt, renderedOutput: node.renderedOutput, timestamp: new Date().toISOString() }],
  };
  await storage.putVersioned('graphNodes', updated, node.version);
  storage.markDirty();
  await refreshReportSections();
}

export async function renderSectionNode(sectionId, providerId) {
  const pid = currentProjectId.value;
  const node = await storage.get('graphNodes', sectionId);
  if (!node) return;
  const expectedVersion = node.version;
  const report = activeReport.value;
  const graph = await storage.getGraphByReport(report.id);
  const llmConfig = await resolveLLMConfig(providerId);
  renderingSection.value = sectionId;
  try {
    const result = await renderSection({ sectionNode: node, graph, projectId: pid, llmConfig, threshold: activeThreshold.value });
    const updated = { ...node, renderedOutput: result.renderedOutput, citedItemIds: result.citedItemIds, isStale: false };
    await storage.putVersioned('graphNodes', updated, expectedVersion);
    storage.markDirty();
    await refreshReportSections();
  } catch (err) {
    if (err.constructor?.name === 'VersionConflictError') {
      pushError('informational', 'Section was modified during rendering. Re-render to update.');
    } else {
      pushError('terminal', `Render failed: ${err.message}`);
    }
  } finally {
    renderingSection.value = null;
  }
}

export async function deleteSection(sectionId) {
  await storage.del('graphNodes', sectionId);
  storage.markDirty();
  await refreshReportSections();
}
