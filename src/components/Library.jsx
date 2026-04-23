import { useRef, useState } from 'preact/hooks';
import * as state from '../state/store.js';
import { exportBibTeX, exportRIS } from '../interfaces/integration.js';

export function Library() {
  return (
    <div class="library">
      <FolderSidebar />
      <ItemList />
      <DetailPane />
    </div>
  );
}

function FolderSidebar() {
  const nameRef = useRef(null);
  const handleAddFolder = async () => {
    const name = nameRef.current?.value?.trim();
    if (!name) return;
    await state.addFolder(name, state.selectedFolderId.value);
    nameRef.current.value = '';
  };

  const tree = state.folderTree.value;

  return (
    <aside class="library-sidebar">
      <div class="sidebar-header"><h2 class="sidebar-title">Folders</h2></div>
      <div class="sidebar-tree">
        <button class={`folder-item ${state.selectedFolderId.value === null ? 'active' : ''}`}
          onClick={() => { state.selectedFolderId.value = null; }}>/ Root</button>
        <FolderChildren parentId="__root__" tree={tree} depth={0} />
      </div>
      <div class="sidebar-add">
        <input ref={nameRef} type="text" placeholder="New folder..." class="sidebar-input"
          onKeyDown={e => e.key === 'Enter' && handleAddFolder()} />
        <button class="btn btn-sm" onClick={handleAddFolder}>+</button>
      </div>
    </aside>
  );
}

function FolderChildren({ parentId, tree, depth }) {
  const children = tree.get(parentId) || [];
  if (!children.length) return null;
  return (
    <div class="folder-children" style={{ paddingLeft: `${(depth + 1) * 12}px` }}>
      {children.map(f => (
        <div key={f.id}>
          <button class={`folder-item ${state.selectedFolderId.value === f.id ? 'active' : ''}`}
            onClick={() => { state.selectedFolderId.value = f.id; }}>{f.name}</button>
          <FolderChildren parentId={f.id} tree={tree} depth={depth + 1} />
        </div>
      ))}
    </div>
  );
}

function ItemList() {
  const items = state.currentFolderItems.value;
  const noteRef = useRef(null);
  const importRef = useRef(null);
  const [showImport, setShowImport] = useState(false);
  const [importFile, setImportFile] = useState(null);
  const progress = state.importProgress.value;

  const handleAddNote = async () => {
    const name = noteRef.current?.value?.trim();
    if (!name) return;
    await state.addNote(name, state.selectedFolderId.value);
    noteRef.current.value = '';
  };

  const handleFileSelected = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setImportFile(file);
    setShowImport(true);
    e.target.value = '';
  };

  const handleExport = (format) => {
    const arts = items.articles.length > 0 ? items.articles : state.articles.value;
    if (!arts.length) return;
    const content = format === 'bibtex' ? exportBibTeX(arts) : exportRIS(arts);
    const ext = format === 'bibtex' ? '.bib' : '.ris';
    const blob = new Blob([content], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `export${ext}`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const selectItem = (id, type) => {
    state.selectedItemId.value = id;
    state.selectedItemType.value = type;
  };

  return (
    <div class="library-items">
      <div class="items-header">
        <h2 class="items-title">
          {state.selectedFolderId.value
            ? state.folders.value.find(f => f.id === state.selectedFolderId.value)?.name
            : 'Root'}
          <span class="items-count"> ({items.articles.length + items.notes.length})</span>
        </h2>
        <div class="items-actions">
          <input ref={noteRef} type="text" placeholder="New note..." class="items-input"
            onKeyDown={e => e.key === 'Enter' && handleAddNote()} />
          <button class="btn btn-sm" onClick={handleAddNote}>+ Note</button>
          <button class="btn btn-sm" onClick={() => importRef.current?.click()}>Import</button>
          <button class="btn btn-sm" onClick={() => handleExport('bibtex')} title="Export BibTeX">BibTeX↓</button>
          <button class="btn btn-sm" onClick={() => handleExport('ris')} title="Export RIS">RIS↓</button>
          <input ref={importRef} type="file" accept=".csv,.bib,.ris,.tsv" class="hidden" onChange={handleFileSelected} />
        </div>
      </div>

      {showImport && importFile && (
        <ImportDialog file={importFile} onClose={() => { setShowImport(false); setImportFile(null); }} />
      )}

      {progress && (
        <div class="import-progress-bar">
          <div class="progress-bar" style={{ width: `${progress.total ? (progress.processed / progress.total) * 100 : 0}%` }} />
          <span class="progress-text">{progress.status}: {progress.processed}/{progress.total}</span>
          {progress.status === 'running' && (
            <button class="btn-icon" onClick={state.cancelImport}>×</button>
          )}
        </div>
      )}

      <div class="items-list">
        {items.articles.length === 0 && items.notes.length === 0 && items.reports.length === 0 && (
          <p class="items-empty">Empty folder. Add notes or import articles.</p>
        )}
        {items.articles.map(a => (
          <button key={a.id}
            class={`item-row ${state.selectedItemId.value === a.id ? 'selected' : ''} ${a.parseQuality === 'degraded' ? 'degraded' : ''}`}
            onClick={() => selectItem(a.id, 'article')}>
            <span class="item-icon">A</span>
            <span class="item-label">{a.title || '(untitled)'}</span>
            <span class="item-meta">{a.year || ''}</span>
          </button>
        ))}
        {items.notes.map(n => (
          <button key={n.id} class={`item-row ${state.selectedItemId.value === n.id ? 'selected' : ''}`}
            onClick={() => selectItem(n.id, 'note')}>
            <span class="item-icon">N</span>
            <span class="item-label">{n.name}</span>
          </button>
        ))}
        {items.reports.map(r => (
          <button key={r.id} class={`item-row ${state.selectedItemId.value === r.id ? 'selected' : ''}`}
            onClick={() => selectItem(r.id, 'report')}>
            <span class="item-icon">R</span>
            <span class="item-label">{r.name}</span>
          </button>
        ))}
      </div>
    </div>
  );
}

function ImportDialog({ file, onClose }) {
  const proj = state.currentProject.value;
  const providerIds = Object.keys(proj?.config?.providers || {});
  const [providerId, setProviderId] = useState(providerIds[0] || '');

  if (!providerIds.length) {
    return (
      <div class="import-dialog">
        <p>Configure a provider first (Settings tab).</p>
        <button class="btn btn-sm" onClick={onClose}>Close</button>
      </div>
    );
  }

  const handleStart = async () => {
    onClose();
    try {
      await state.startImport(file, state.selectedFolderId.value, providerId);
    } catch (err) {
      state.pushError('terminal', err.message);
    }
  };

  return (
    <div class="import-dialog">
      <p>Import <strong>{file.name}</strong></p>
      <div class="form-row">
        <label>Provider</label>
        <select value={providerId} onChange={e => setProviderId(e.target.value)} class="form-input">
          {providerIds.map(id => <option key={id} value={id}>{id}</option>)}
        </select>
      </div>
      <div class="form-actions">
        <button class="btn btn-primary" onClick={handleStart}>Start Import</button>
        <button class="btn btn-secondary" onClick={onClose}>Cancel</button>
      </div>
    </div>
  );
}

function DetailPane() {
  const id = state.selectedItemId.value;
  const type = state.selectedItemType.value;
  if (!id) return <div class="library-detail"><p class="detail-empty">Select an item to view details</p></div>;
  if (type === 'article') return <ArticleDetail id={id} />;
  if (type === 'note') return <NoteDetail id={id} />;
  return <div class="library-detail"><p class="detail-empty">Detail view for {type}</p></div>;
}

function ArticleDetail({ id }) {
  const article = state.articles.value.find(a => a.id === id);
  if (!article) return <div class="library-detail"><p>Article not found</p></div>;
  return (
    <div class="library-detail">
      <h3 class="detail-title">{article.title || '(untitled)'}</h3>
      <div class="detail-meta">
        <span>{article.authors?.join('; ')}</span>
        {article.year && <span> · {article.year}</span>}
        {article.venue && <span> · {article.venue}</span>}
      </div>
      {article.refs?.doi && <div class="detail-field"><label>DOI</label><span>{article.refs.doi}</span></div>}
      {article.abstract && <div class="detail-field"><label>Abstract</label><p class="detail-abstract">{article.abstract}</p></div>}
      {article.keywords?.author?.length > 0 && <div class="detail-field"><label>Keywords</label><span>{article.keywords.author.join(', ')}</span></div>}
      {article.parseQuality === 'degraded' && <div class="detail-warning">Parse quality: degraded — {article.parseNote}</div>}
    </div>
  );
}

function NoteDetail({ id }) {
  const note = state.notes.value.find(n => n.id === id);
  if (!note) return <div class="library-detail"><p>Note not found</p></div>;
  const handleBlur = async (e) => {
    const content = e.target.value;
    if (content === note.content) return;
    try { await state.updateNote({ ...note, content }, note.version); }
    catch (err) { state.pushError('operational', `Save note failed: ${err.message}`); }
  };
  return (
    <div class="library-detail">
      <h3 class="detail-title">{note.name}</h3>
      <textarea class="detail-editor" value={note.content} onBlur={handleBlur} placeholder="Write your note..." />
    </div>
  );
}
