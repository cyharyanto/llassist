// Project interface (spec: Share)
// .llassist file = zip with canonical JSON serialization.
// save → download. open → upload + deserialize into IDB.

import JSZip from 'jszip';
import * as storage from './storage.js';

const SCHEMA_VERSION = '0.1.0';

// --- Canonical serialization (spec: Share) ---
// Keys sorted lexicographically (Unicode codepoint).
// Numbers as shortest unambiguous IEEE 754.
// Minimal string escaping. UTF-8 without BOM. LF line endings.

function canonicalJSON(obj) {
  return JSON.stringify(sortKeys(obj));
}

// Recursively sort object keys (spec: lexicographic Unicode codepoint order).
// Arrays preserve element order; objects get sorted keys.
function sortKeys(val) {
  if (val === null || typeof val !== 'object') return val;
  if (Array.isArray(val)) return val.map(sortKeys);
  const sorted = {};
  for (const k of Object.keys(val).sort()) {
    sorted[k] = sortKeys(val[k]);
  }
  return sorted;
}

function toJSONL(entities) {
  return entities
    .slice()
    .sort((a, b) => a.id < b.id ? -1 : a.id > b.id ? 1 : 0)
    .map(e => canonicalJSON(e))
    .join('\n') + '\n';
}

function fromJSONL(text) {
  if (!text || !text.trim()) return [];
  return text.trim().split('\n').map(line => JSON.parse(line));
}

// --- Save ---

export async function saveProject(projectId) {
  const data = await storage.serializeProjectData(projectId);
  const zip = new JSZip();

  const manifest = {
    schemaVersion: SCHEMA_VERSION,
    projectId: data.project.id,
    projectName: data.project.name,
    projectCreatedAt: data.project.createdAt,
    savedAt: new Date().toISOString(),
    entityCounts: {
      folders: data.folders.length,
      articles: data.articles.length,
      notes: data.notes.length,
      reports: data.reports.length,
      imports: data.imports.length,
      protocols: data.protocols.length,
      graphs: data.graphs.length,
      graphNodes: data.graphNodes.length,
      layerResults: data.layerResults.length,
      assessments: data.assessments.length,
      extractionRecords: data.extractionRecords.length,
      dedupDecisions: data.dedupDecisions.length,
      adjudications: data.adjudications.length,
      disagreements: data.disagreements.length,
      codes: data.codes.length,
      themes: data.themes.length,
      findings: data.findings.length,
    },
  };

  zip.file('manifest.json', canonicalJSON(manifest) + '\n');
  zip.file('config.json', canonicalJSON(data.project.config || {}) + '\n');

  // Library
  zip.file('library/folders.json', canonicalJSON(data.folders) + '\n');
  zip.file('library/articles.jsonl', toJSONL(data.articles));
  zip.file('library/notes.jsonl', toJSONL(data.notes));
  zip.file('library/reports.json', canonicalJSON(data.reports) + '\n');
  zip.file('library/layer-results.jsonl', toJSONL(data.layerResults));

  // Graphs
  for (const graph of data.graphs) {
    const graphNodes = data.graphNodes.filter(n => n.graphId === graph.id);
    const graphData = { ...graph, _nodes: graphNodes };
    zip.file(`graphs/${graph.id}.json`, canonicalJSON(graphData) + '\n');
  }

  // Processing
  zip.file('processing/assessments.jsonl', toJSONL(data.assessments));
  zip.file('processing/extraction-records.jsonl', toJSONL(data.extractionRecords));
  zip.file('processing/dedup-decisions.jsonl', toJSONL(data.dedupDecisions));
  zip.file('processing/adjudications.jsonl', toJSONL(data.adjudications));
  zip.file('processing/disagreements.jsonl', toJSONL(data.disagreements));

  // Synthesis
  zip.file('synthesis/codes.json', canonicalJSON(data.codes) + '\n');
  zip.file('synthesis/themes.json', canonicalJSON(data.themes) + '\n');
  zip.file('synthesis/findings.json', canonicalJSON(data.findings) + '\n');

  // Protocols & imports
  zip.file('protocols/protocols.json', canonicalJSON(data.protocols) + '\n');
  zip.file('imports/imports.jsonl', toJSONL(data.imports));

  const blob = await zip.generateAsync({
    type: 'blob',
    compression: 'DEFLATE',
    compressionOptions: { level: 6 },
  });

  storage.markClean();
  return blob;
}

/**
 * Trigger browser download of the project file.
 */
export function downloadBlob(blob, filename) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

// --- Open ---

export async function openProject(file) {
  const zip = await JSZip.loadAsync(file);

  const manifestText = await zip.file('manifest.json')?.async('string');
  if (!manifestText) throw new Error('Invalid .llassist file: missing manifest.json');
  const manifest = JSON.parse(manifestText);

  // Schema version compatibility (spec: strict at major, auto at minor)
  const [major] = manifest.schemaVersion.split('.');
  const [currentMajor] = SCHEMA_VERSION.split('.');
  if (major !== currentMajor) {
    throw new Error(`Incompatible schema version: ${manifest.schemaVersion} (current: ${SCHEMA_VERSION})`);
  }

  const configText = await zip.file('config.json')?.async('string');
  const config = configText ? JSON.parse(configText) : {};

  // Parse library
  const folders = JSON.parse(await readZipFile(zip, 'library/folders.json', '[]'));
  const articles = fromJSONL(await readZipFile(zip, 'library/articles.jsonl', ''));
  const notes = fromJSONL(await readZipFile(zip, 'library/notes.jsonl', ''));
  const reports = JSON.parse(await readZipFile(zip, 'library/reports.json', '[]'));
  const layerResults = fromJSONL(await readZipFile(zip, 'library/layer-results.jsonl', ''));

  // Parse graphs — nodes stored inline in each graph file
  const graphs = [];
  const graphNodes = [];
  const graphsDir = zip.folder('graphs');
  if (graphsDir) {
    const graphFiles = [];
    graphsDir.forEach((path, entry) => {
      if (path.endsWith('.json')) graphFiles.push(entry);
    });
    for (const entry of graphFiles) {
      const graphData = JSON.parse(await entry.async('string'));
      const nodes = graphData._nodes || [];
      delete graphData._nodes;
      graphs.push(graphData);
      graphNodes.push(...nodes);
    }
  }

  // Parse processing
  const assessments = fromJSONL(await readZipFile(zip, 'processing/assessments.jsonl', ''));
  const extractionRecords = fromJSONL(await readZipFile(zip, 'processing/extraction-records.jsonl', ''));
  const dedupDecisions = fromJSONL(await readZipFile(zip, 'processing/dedup-decisions.jsonl', ''));
  const adjudications = fromJSONL(await readZipFile(zip, 'processing/adjudications.jsonl', ''));
  const disagreements = fromJSONL(await readZipFile(zip, 'processing/disagreements.jsonl', ''));

  // Parse synthesis
  const codes = JSON.parse(await readZipFile(zip, 'synthesis/codes.json', '[]'));
  const themes = JSON.parse(await readZipFile(zip, 'synthesis/themes.json', '[]'));
  const findings = JSON.parse(await readZipFile(zip, 'synthesis/findings.json', '[]'));

  // Parse protocols & imports
  const protocols = JSON.parse(await readZipFile(zip, 'protocols/protocols.json', '[]'));
  const imports = fromJSONL(await readZipFile(zip, 'imports/imports.jsonl', ''));

  // Reconstruct project entity — timestamps preserved exactly (spec: Conformance)
  const project = {
    id: manifest.projectId,
    name: manifest.projectName,
    schemaVersion: manifest.schemaVersion,
    config,
    createdAt: manifest.projectCreatedAt || manifest.savedAt,
    updatedAt: manifest.savedAt,
  };

  // Load into IDB
  await storage.loadProjectData(manifest.projectId, {
    project, folders, articles, notes, reports, imports, protocols,
    graphs, graphNodes, layerResults, assessments, extractionRecords,
    dedupDecisions, adjudications, disagreements, codes, themes, findings,
  });

  storage.markClean();
  return project;
}

async function readZipFile(zip, path, defaultValue) {
  const entry = zip.file(path);
  if (!entry) return defaultValue;
  return entry.async('string');
}

// --- Diff (spec: project.diff(a, b)) ---

export async function diffProjects(blobA, blobB) {
  // Deferred to later phase — structure exists, implementation pending
  throw new Error('project.diff not yet implemented');
}

// --- Merge (spec: project.merge) ---

export async function mergeProject(blob, targetProjectId, strategy = 'keep-both') {
  // Deferred to later phase
  throw new Error('project.merge not yet implemented');
}

// --- Export subset (spec: project.exportSubset) ---

export async function exportSubset(reportId) {
  // Deferred to later phase
  throw new Error('project.exportSubset not yet implemented');
}
