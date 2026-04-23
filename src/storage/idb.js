// IDB implementation (spec: Architecture)
// DB_VERSION integer, onupgradeneeded migration.
// One object store per entity type.
// Thin promise wrappers — no library.
// Compare-and-swap on versioned writes.

import { stripTransient, initTransient, TRANSIENT_DEFAULTS } from '../types/entities.js';
import { VersionConflictError } from '../utils/errors.js';

const DB_NAME = 'llassist';
const DB_VERSION = 1;

// Store definitions: { name, keyPath, indexes: [{ name, keyPath, options }] }
const STORES = [
  { name: 'projects', keyPath: 'id', indexes: [] },
  { name: 'folders', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
    { name: 'by-parent', keyPath: 'parentFolderId' },
  ]},
  { name: 'articles', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
    { name: 'by-folder', keyPath: 'folderId' },
    { name: 'by-import', keyPath: 'importId' },
    // Compound index for idempotent resume: importId + importUnitIndex
    { name: 'by-import-unit', keyPath: ['importId', 'importUnitIndex'] },
  ]},
  { name: 'notes', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
    { name: 'by-folder', keyPath: 'folderId' },
  ]},
  { name: 'reports', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
  ]},
  { name: 'imports', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
  ]},
  { name: 'protocols', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
  ]},
  { name: 'graphs', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
    { name: 'by-report', keyPath: 'reportId' },
  ]},
  { name: 'graphNodes', keyPath: 'id', indexes: [
    { name: 'by-graph', keyPath: 'graphId' },
  ]},
  { name: 'layerResults', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
    { name: 'by-graph', keyPath: 'graphId' },
    { name: 'by-processing-node', keyPath: 'processingNodeId' },
  ]},
  { name: 'assessments', keyPath: 'id', indexes: [
    { name: 'by-layer-result', keyPath: 'layerResultId' },
    { name: 'by-article', keyPath: 'articleId' },
    { name: 'by-layer-article', keyPath: ['layerResultId', 'articleId'] },
  ]},
  { name: 'extractionRecords', keyPath: 'id', indexes: [
    { name: 'by-layer-result', keyPath: 'layerResultId' },
    { name: 'by-article', keyPath: 'articleId' },
  ]},
  { name: 'dedupDecisions', keyPath: 'id', indexes: [
    { name: 'by-layer-result', keyPath: 'layerResultId' },
  ]},
  { name: 'adjudications', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
    { name: 'by-article', keyPath: 'articleId' },
  ]},
  { name: 'disagreements', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
    { name: 'by-article', keyPath: 'articleId' },
  ]},
  { name: 'codes', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
  ]},
  { name: 'themes', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
  ]},
  { name: 'findings', keyPath: 'id', indexes: [
    { name: 'by-project', keyPath: 'projectId' },
  ]},
  // KeyStore — per-installation, never in project file
  { name: 'keyStore', keyPath: 'providerId', indexes: [] },
];

let dbInstance = null;

export function openDB() {
  if (dbInstance) return Promise.resolve(dbInstance);

  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VERSION);

    req.onupgradeneeded = (e) => {
      const db = e.target.result;
      for (const storeDef of STORES) {
        if (!db.objectStoreNames.contains(storeDef.name)) {
          const store = db.createObjectStore(storeDef.name, { keyPath: storeDef.keyPath });
          for (const idx of storeDef.indexes) {
            store.createIndex(idx.name, idx.keyPath, idx.options || {});
          }
        }
      }
    };

    req.onsuccess = () => {
      dbInstance = req.result;
      resolve(dbInstance);
    };

    req.onerror = () => reject(req.error);
  });
}

// --- Promise wrappers ---

export async function dbPut(storeName, entity) {
  const db = await openDB();
  const clean = stripTransient(entity);
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readwrite');
    const store = tx.objectStore(storeName);
    const req = store.put(clean);
    req.onsuccess = () => resolve(entity);
    req.onerror = () => reject(req.error);
  });
}

export async function dbGet(storeName, id) {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readonly');
    const store = tx.objectStore(storeName);
    const req = store.get(id);
    req.onsuccess = () => resolve(req.result || null);
    req.onerror = () => reject(req.error);
  });
}

export async function dbGetAll(storeName, indexName, indexValue) {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readonly');
    const store = tx.objectStore(storeName);
    let req;
    if (indexName && indexValue !== undefined) {
      const index = store.index(indexName);
      req = index.getAll(indexValue);
    } else {
      req = store.getAll();
    }
    req.onsuccess = () => resolve(req.result || []);
    req.onerror = () => reject(req.error);
  });
}

export async function dbDelete(storeName, id) {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readwrite');
    const store = tx.objectStore(storeName);
    const req = store.delete(id);
    req.onsuccess = () => resolve();
    req.onerror = () => reject(req.error);
  });
}

export async function dbClear(storeName) {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readwrite');
    const store = tx.objectStore(storeName);
    const req = store.clear();
    req.onsuccess = () => resolve();
    req.onerror = () => reject(req.error);
  });
}

// Count by index value — used for progress/stats
export async function dbCount(storeName, indexName, indexValue) {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readonly');
    const store = tx.objectStore(storeName);
    let req;
    if (indexName && indexValue !== undefined) {
      const index = store.index(indexName);
      req = index.count(indexValue);
    } else {
      req = store.count();
    }
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

// --- Compare-and-swap versioned write ---
// Spec: every write carries expected version; mismatch → rejection with current state

export async function dbPutVersioned(storeName, entity, expectedVersion) {
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readwrite');
    const store = tx.objectStore(storeName);

    // Read current to verify version
    const getReq = store.get(entity.id);
    getReq.onsuccess = () => {
      const current = getReq.result;

      if (current && expectedVersion !== undefined && current.version !== expectedVersion) {
        reject(new VersionConflictError(storeName, entity.id, expectedVersion, current));
        return;
      }

      const updated = {
        ...stripTransient(entity),
        version: (current?.version || 0) + 1,
        updatedAt: new Date().toISOString(),
      };

      const putReq = store.put(updated);
      putReq.onsuccess = () => resolve(updated);
      putReq.onerror = () => reject(putReq.error);
    };
    getReq.onerror = () => reject(getReq.error);
  });
}

// --- Bulk operations for project load/clear ---

const PROJECT_SCOPED_STORES = STORES
  .filter(s => s.name !== 'projects' && s.name !== 'keyStore')
  .map(s => s.name);

/**
 * Clear all data for a projectId across all stores.
 * IDB does not cascade deletes — transitive stores (graphNodes by graph,
 * assessments by layerResult) must be cleared explicitly.
 */
export async function clearProjectData(projectId) {
  const db = await openDB();

  // Phase 1: collect parent IDs needed to clear transitive stores
  const graphIds = await getKeysByIndex(db, 'graphs', 'by-project', projectId);
  const layerResultIds = await getKeysByIndex(db, 'layerResults', 'by-project', projectId);

  // Phase 2: clear transitive stores (no by-project index)
  for (const graphId of graphIds) {
    await deleteByIndex(db, 'graphNodes', 'by-graph', graphId);
  }
  for (const lrId of layerResultIds) {
    await deleteByIndex(db, 'assessments', 'by-layer-result', lrId);
    await deleteByIndex(db, 'extractionRecords', 'by-layer-result', lrId);
    await deleteByIndex(db, 'dedupDecisions', 'by-layer-result', lrId);
  }

  // Phase 3: clear project-indexed stores
  for (const storeName of PROJECT_SCOPED_STORES) {
    const tx = db.transaction(storeName, 'readwrite');
    const store = tx.objectStore(storeName);
    if (store.indexNames.contains('by-project')) {
      await deleteByIndexInTx(store, 'by-project', projectId);
    }
    await new Promise((resolve, reject) => {
      tx.oncomplete = resolve;
      tx.onerror = () => reject(tx.error);
    });
  }
}

async function getKeysByIndex(db, storeName, indexName, value) {
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readonly');
    const index = tx.objectStore(storeName).index(indexName);
    const req = index.getAllKeys(value);
    req.onsuccess = () => resolve(req.result || []);
    req.onerror = () => reject(req.error);
  });
}

async function deleteByIndex(db, storeName, indexName, value) {
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readwrite');
    const store = tx.objectStore(storeName);
    const index = store.index(indexName);
    const req = index.getAllKeys(value);
    req.onsuccess = () => {
      for (const key of req.result) store.delete(key);
      tx.oncomplete = resolve;
      tx.onerror = () => reject(tx.error);
    };
    req.onerror = () => reject(req.error);
  });
}

function deleteByIndexInTx(store, indexName, value) {
  return new Promise((resolve, reject) => {
    const index = store.index(indexName);
    const req = index.getAllKeys(value);
    req.onsuccess = () => {
      for (const key of req.result) store.delete(key);
      resolve();
    };
    req.onerror = () => reject(req.error);
  });
}

/**
 * Bulk put — used during project file deserialization.
 * No version checking (fresh load replaces all).
 */
export async function dbPutBatch(storeName, entities) {
  // Filter entities missing their keyPath value — defensive against corrupt serialization
  const valid = entities.filter(e => e && typeof e === 'object' && e.id != null);
  if (!valid.length) return;
  const db = await openDB();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(storeName, 'readwrite');
    const store = tx.objectStore(storeName);
    for (const entity of valid) {
      store.put(stripTransient(entity));
    }
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
}

export { STORES, PROJECT_SCOPED_STORES };
