import { useState } from 'preact/hooks';
import * as state from '../state/store.js';

export function AnalyzeView() {
  const hasArticles = state.articles.value.length > 0;

  if (!hasArticles) {
    return (
      <div class="analyze-empty">
        <p>Import articles first (Library → Import), then return here to screen them.</p>
      </div>
    );
  }

  return (
    <div class="analyze-view">
      <div class="analyze-left">
        <ProtocolEditor />
        <ScreeningControl />
      </div>
      <div class="analyze-right">
        <ResultsPanel />
      </div>
    </div>
  );
}

// --- Protocol Editor ---

function ProtocolEditor() {
  const protocol = state.activeProtocol.value;
  const proj = state.currentProject.value;
  const providerIds = Object.keys(proj?.config?.providers || {});
  const [rqs, setRqs] = useState(protocol?.researchQuestions || [{ id: crypto.randomUUID(), text: '', definitions: [] }]);
  const [providerId, setProviderId] = useState(protocol?.providerId || providerIds[0] || '');
  const [threshold, setThreshold] = useState(protocol?.threshold || 0.5);

  if (!providerIds.length) {
    return (
      <div class="protocol-editor section-box">
        <h3>Protocol</h3>
        <p class="config-note">Configure a provider first (Settings tab).</p>
      </div>
    );
  }

  const addRQ = () => {
    setRqs([...rqs, { id: crypto.randomUUID(), text: '', definitions: [] }]);
  };

  const updateRQ = (idx, text) => {
    const updated = [...rqs];
    updated[idx] = { ...updated[idx], text };
    setRqs(updated);
  };

  const removeRQ = (idx) => {
    setRqs(rqs.filter((_, i) => i !== idx));
  };

  const handleSave = async () => {
    const validRQs = rqs.filter(r => r.text.trim());
    if (!validRQs.length) return;
    const provider = proj?.config?.providers?.[providerId];
    await state.createAndSetProtocol({
      researchQuestions: validRQs,
      providerId,
      model: provider?.defaultModel,
      threshold,
    });
  };

  const isLocked = protocol && state.screeningProgress.value?.status === 'running';

  return (
    <div class="protocol-editor section-box">
      <h3>Protocol</h3>

      <div class="form-row">
        <label>Provider</label>
        <select value={providerId} onChange={e => setProviderId(e.target.value)} class="form-input" disabled={isLocked}>
          {providerIds.map(id => <option key={id} value={id}>{id}</option>)}
        </select>
      </div>

      <div class="rq-list">
        <label>Research Questions</label>
        {rqs.map((rq, i) => (
          <div key={rq.id} class="rq-row">
            <span class="rq-num">RQ{i + 1}</span>
            <input
              value={rq.text}
              onInput={e => updateRQ(i, e.target.value)}
              placeholder="e.g. What methods are used for..."
              class="form-input rq-input"
              disabled={isLocked}
            />
            {rqs.length > 1 && !isLocked && (
              <button class="btn-icon" onClick={() => removeRQ(i)}>×</button>
            )}
          </div>
        ))}
        {!isLocked && (
          <button class="btn btn-sm" onClick={addRQ}>+ Add RQ</button>
        )}
      </div>

      {!isLocked && (
        <button class="btn btn-primary" onClick={handleSave} style="margin-top:8px">
          {protocol ? 'Update Protocol (new version)' : 'Create Protocol'}
        </button>
      )}

      {protocol && (
        <div class="protocol-status">
          Protocol {protocol.versionLabel} · {protocol.researchQuestions.length} RQs · {protocol.model}
        </div>
      )}
    </div>
  );
}

// --- Screening Control ---

function ScreeningControl() {
  const protocol = state.activeProtocol.value;
  const progress = state.screeningProgress.value;
  const articleCount = state.articles.value.length;

  if (!protocol) return null;

  const handleStart = async () => {
    try {
      await state.startScreening(protocol.providerId);
    } catch (err) {
      state.pushError('terminal', err.message);
    }
  };

  const isRunning = progress?.status === 'running';
  const isDone = progress?.status === 'completed' || state.assessments.value.length > 0;

  return (
    <div class="screening-control section-box">
      <h3>Screening</h3>
      <p class="meta-text">{articleCount} articles · {protocol.researchQuestions.length} RQs · {protocol.researchQuestions.length + 1} LLM calls/article</p>

      {!isRunning && !isDone && (
        <button class="btn btn-primary" onClick={handleStart}>
          Start Screening
        </button>
      )}

      {isRunning && (
        <div class="progress-bar-container">
          <div class="progress-bar" style={{ width: `${(progress.processed / progress.total) * 100}%` }} />
          <span class="progress-text">{progress.processed}/{progress.total}</span>
          <button class="btn btn-sm" onClick={state.cancelScreening}>Cancel</button>
        </div>
      )}

      {progress?.currentTitle && isRunning && (
        <p class="progress-current">{progress.currentTitle}</p>
      )}

      {isDone && !isRunning && (
        <p class="meta-text done-text">✓ {state.assessments.value.length} articles screened</p>
      )}
    </div>
  );
}

// --- Results Panel ---

function ResultsPanel() {
  const stats = state.assessmentStats.value;
  const dist = state.rqDistribution.value;
  const artRel = state.articleRelevance.value;
  const threshold = state.activeThreshold.value;
  const [selectedArticle, setSelectedArticle] = useState(null);

  if (!stats) {
    return (
      <div class="results-panel">
        <p class="config-note">Run screening to see results.</p>
      </div>
    );
  }

  return (
    <div class="results-panel">
      <div class="threshold-control">
        <label>Threshold: {threshold.toFixed(2)}</label>
        <input
          type="range" min="0" max="1" step="0.01"
          value={threshold}
          onInput={e => { state.activeThreshold.value = parseFloat(e.target.value); }}
          class="threshold-slider"
        />
      </div>

      <div class="stats-row">
        <div class="stat-box">
          <span class="stat-num">{stats.total}</span>
          <span class="stat-label">Total</span>
        </div>
        <div class="stat-box stat-relevant">
          <span class="stat-num">{stats.relevant}</span>
          <span class="stat-label">Relevant</span>
        </div>
        <div class="stat-box stat-contributing">
          <span class="stat-num">{stats.contributing}</span>
          <span class="stat-label">Contributing</span>
        </div>
        <div class="stat-box stat-mustread">
          <span class="stat-num">{stats.mustRead}</span>
          <span class="stat-label">Must Read</span>
        </div>
      </div>

      {dist.length > 0 && (
        <div class="rq-dist-table">
          <h4>Per-RQ Distribution</h4>
          <table>
            <thead>
              <tr><th>RQ</th><th>Relevant</th><th>Contributing</th><th>Ratio</th></tr>
            </thead>
            <tbody>
              {dist.map(d => (
                <tr key={d.rqId} class={d.relevant === 0 ? 'rq-underserved' : ''}>
                  <td class="rq-text-cell" title={d.rqText}>{d.rqText.substring(0, 60)}{d.rqText.length > 60 ? '...' : ''}</td>
                  <td>{d.relevant}</td>
                  <td>{d.contributing}</td>
                  <td>{d.ratio.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div class="article-relevance-list">
        <h4>Articles ({artRel.length})</h4>
        <div class="article-rel-scroll">
          {artRel.map(({ article, assessment, status }) => (
            <button
              key={article.id}
              class={`article-rel-row ${status?.mustRead ? 'must-read' : ''} ${selectedArticle === article.id ? 'selected' : ''}`}
              onClick={() => setSelectedArticle(selectedArticle === article.id ? null : article.id)}
            >
              <span class={`rel-indicator ${status?.mustRead ? 'rel-yes' : status ? 'rel-no' : 'rel-pending'}`} />
              <span class="rel-title">{article.title}</span>
              {assessment && (
                <span class="rel-score">
                  {Math.max(...assessment.perRQ.map(r => r.relevanceScore)).toFixed(2)}
                </span>
              )}
            </button>
          ))}
        </div>
      </div>

      {selectedArticle && <ArticleAssessmentDetail articleId={selectedArticle} />}
    </div>
  );
}

function ArticleAssessmentDetail({ articleId }) {
  const artRel = state.articleRelevance.value;
  const entry = artRel.find(a => a.article.id === articleId);
  if (!entry?.assessment) return null;

  const { article, assessment } = entry;

  return (
    <div class="assessment-detail">
      <h4>{article.title}</h4>
      <p class="meta-text">{article.authors?.join('; ')} · {article.year}</p>

      {assessment.keySemantics && (
        <div class="semantics-block">
          <span class="detail-label">Topics:</span> {assessment.keySemantics.topics?.join(', ')}
          <br />
          <span class="detail-label">Entities:</span> {assessment.keySemantics.entities?.join(', ')}
        </div>
      )}

      <div class="per-rq-detail">
        {assessment.perRQ.map(rq => {
          const protocol = state.activeProtocol.value;
          const rqDef = protocol?.researchQuestions.find(r => r.id === rq.rqId);
          return (
            <div key={rq.rqId} class="rq-detail-block">
              <div class="rq-detail-header">
                <span class="rq-detail-name">{rqDef?.text || rq.rqId}</span>
                <span class="rq-detail-scores">
                  rel: {rq.relevanceScore.toFixed(2)} · contrib: {rq.contributionScore.toFixed(2)}
                </span>
              </div>
              <p class="rq-reasoning">{rq.reasoning}</p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
