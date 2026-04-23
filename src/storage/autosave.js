// Auto-save to IDB every 30s when dirty (spec: Architecture).
// This is crash recovery — not file persistence.
// Ctrl+S triggers file download. beforeunload warns if dirty.

import { isDirty } from '../interfaces/storage.js';

const AUTOSAVE_INTERVAL_MS = 30_000;
let timer = null;

/**
 * Start auto-save timer.
 * @param {Function} onSave - called when auto-save triggers; should persist to IDB
 */
export function startAutoSave(onSave) {
  stopAutoSave();
  timer = setInterval(() => {
    if (isDirty()) {
      onSave();
    }
  }, AUTOSAVE_INTERVAL_MS);

  // beforeunload warns if dirty
  window.addEventListener('beforeunload', handleBeforeUnload);
}

export function stopAutoSave() {
  if (timer) {
    clearInterval(timer);
    timer = null;
  }
  window.removeEventListener('beforeunload', handleBeforeUnload);
}

function handleBeforeUnload(e) {
  if (isDirty()) {
    e.preventDefault();
    // spec: beforeunload warns if dirty
    e.returnValue = 'You have unsaved changes. Save your project before leaving.';
    return e.returnValue;
  }
}
