// Layers interface (spec: Architecture — third of five interfaces)
// Processing node lifecycle: start, pause, resume, cancel, observe.
// Implementation deferred to Phase 6.

/**
 * @typedef {Object} LayerObserver
 * @property {Function} onProgress - (processed, total, currentArticle) => void
 * @property {Function} onComplete - (layerResult) => void
 * @property {Function} onError - (error, article) => void
 * @property {Function} onPause - (reason) => void
 */

/**
 * Start processing a node.
 * @param {string} graphId
 * @param {string} nodeId
 * @param {LayerObserver} observer
 */
export async function start(graphId, nodeId, observer) {
  throw new Error('Layers interface not yet implemented (Phase 6)');
}

/**
 * Pause a running node. Current in-flight call finishes.
 */
export async function pause(nodeId, reason = 'user-requested') {
  throw new Error('Layers interface not yet implemented (Phase 6)');
}

/**
 * Resume a paused/interrupted node.
 */
export async function resume(nodeId, observer) {
  throw new Error('Layers interface not yet implemented (Phase 6)');
}

/**
 * Cancel a node. Clears the queue, in-flight call finishes.
 */
export async function cancel(nodeId) {
  throw new Error('Layers interface not yet implemented (Phase 6)');
}
