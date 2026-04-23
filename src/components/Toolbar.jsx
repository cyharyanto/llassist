import * as state from '../state/store.js';
import { useSignal } from '@preact/signals';
import { useEffect } from 'preact/hooks';
import { isDirty, onDirtyChange } from '../interfaces/storage.js';

export function Toolbar() {
  const dirty = useSignal(isDirty());

  useEffect(() => {
    return onDirtyChange((d) => { dirty.value = d; });
  }, []);

  const proj = state.currentProject.value;
  if (!proj) return null;

  const handleSave = async () => {
    try { await state.saveCurrentProject(); }
    catch (err) { state.pushError('operational', `Save failed: ${err.message}`); }
  };

  const handleClose = async () => {
    if (dirty.value && !confirm('Unsaved changes will be lost. Save first?')) return;
    await state.closeProject();
  };

  const v = state.view.value;
  const tab = (id, label) => (
    <button class={`toolbar-tab ${v === id ? 'active' : ''}`} onClick={() => state.view.value = id}>
      {label}
    </button>
  );

  return (
    <header class="toolbar">
      <div class="toolbar-left">
        <button class="toolbar-btn" onClick={() => state.view.value = 'home'} title="All projects">←</button>
        <span class="toolbar-project-name">
          {proj.name}
          {dirty.value && <span class="dirty-dot"> •</span>}
        </span>
      </div>
      <nav class="toolbar-nav">
        {tab('library', 'Library')}
        {tab('analyze', 'Analyze')}
        {tab('report', 'Report')}
        {tab('config', 'Settings')}
      </nav>
      <div class="toolbar-right">
        <button class="toolbar-btn" onClick={handleSave} title="Ctrl+S">Save</button>
        <button class="toolbar-btn toolbar-btn-close" onClick={handleClose}>Close</button>
      </div>
    </header>
  );
}
