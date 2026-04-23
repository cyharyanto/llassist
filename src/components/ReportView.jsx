import { useState } from 'preact/hooks';
import * as state from '../state/store.js';

export function ReportView() {
  const sections = state.reportSections.value;
  const proj = state.currentProject.value;
  const providerIds = Object.keys(proj?.config?.providers || {});
  const [providerId, setProviderId] = useState(providerIds[0] || '');

  if (!providerIds.length) {
    return (
      <div class="report-view">
        <p class="config-note">Configure a provider first (Settings tab).</p>
      </div>
    );
  }

  const handleAdd = async () => {
    await state.addSection('');
  };

  return (
    <div class="report-view">
      <div class="report-header">
        <h2>Report</h2>
        <div class="report-actions">
          <select value={providerId} onChange={e => setProviderId(e.target.value)} class="form-input form-input-sm">
            {providerIds.map(id => <option key={id} value={id}>{id}</option>)}
          </select>
          <button class="btn btn-sm" onClick={handleAdd}>+ Add Section</button>
        </div>
      </div>

      <div class="sections-list">
        {sections.length === 0 && (
          <p class="config-note">No sections yet. Add a section and write a prompt to render.</p>
        )}
        {sections.map(section => (
          <SectionEditor
            key={section.id}
            section={section}
            providerId={providerId}
          />
        ))}
      </div>
    </div>
  );
}

function SectionEditor({ section, providerId }) {
  const [prompt, setPrompt] = useState(section.sourcePrompt || '');
  const [editing, setEditing] = useState(!section.renderedOutput);
  const isRendering = state.renderingSection.value === section.id;

  const handleSavePrompt = async () => {
    if (prompt !== section.sourcePrompt) {
      await state.updateSectionPrompt(section.id, prompt);
    }
  };

  const handleRender = async () => {
    await handleSavePrompt();
    try {
      await state.renderSectionNode(section.id, providerId);
    } catch (err) {
      state.pushError('terminal', err.message);
    }
  };

  const handleDelete = async () => {
    if (confirm('Delete this section?')) {
      await state.deleteSection(section.id);
    }
  };

  return (
    <div class={`section-editor ${section.isStale ? 'stale' : ''}`}>
      <div class="section-toolbar">
        <span class="section-ordinal">§{section.ordinal}</span>
        {section.isStale && <span class="stale-badge">stale</span>}
        <div class="section-toolbar-right">
          <button class="btn btn-sm" onClick={() => setEditing(!editing)}>
            {editing ? 'Preview' : 'Edit'}
          </button>
          <button class="btn btn-sm btn-primary" onClick={handleRender} disabled={isRendering}>
            {isRendering ? 'Rendering...' : 'Render'}
          </button>
          <button class="btn-icon" onClick={handleDelete} title="Delete section">×</button>
        </div>
      </div>

      {editing && (
        <textarea
          class="section-prompt"
          value={prompt}
          onInput={e => setPrompt(e.target.value)}
          onBlur={handleSavePrompt}
          placeholder="Write your section prompt. Use natural language to reference data, e.g.: Summarize the field coverage across all research questions. Include the number of relevant and contributing papers per RQ."
          rows={5}
        />
      )}

      {section.renderedOutput && (
        <div class="section-rendered" dangerouslySetInnerHTML={{ __html: markdownToHtml(section.renderedOutput) }} />
      )}

      {!section.renderedOutput && !editing && (
        <p class="config-note">No rendered output yet. Write a prompt and click Render.</p>
      )}
    </div>
  );
}

// Minimal markdown → HTML (just enough for rendered sections)
function markdownToHtml(md) {
  if (!md) return '';
  return md
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    // Headers
    .replace(/^### (.+)$/gm, '<h4>$1</h4>')
    .replace(/^## (.+)$/gm, '<h3>$1</h3>')
    .replace(/^# (.+)$/gm, '<h2>$1</h2>')
    // Bold + italic
    .replace(/\*\*\*(.+?)\*\*\*/g, '<strong><em>$1</em></strong>')
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.+?)\*/g, '<em>$1</em>')
    // Lists
    .replace(/^- (.+)$/gm, '<li>$1</li>')
    .replace(/(<li>.*<\/li>\n?)+/g, '<ul>$&</ul>')
    // Paragraphs
    .replace(/\n\n/g, '</p><p>')
    .replace(/^(?!<[hul])/, '<p>')
    .replace(/(?<![>])$/, '</p>')
    // Line breaks
    .replace(/\n/g, '<br/>');
}
