import { useState, useEffect } from 'preact/hooks';
import * as state from '../state/store.js';

export function ConfigPanel() {
  const proj = state.currentProject.value;
  const providers = proj?.config?.providers || {};
  const providerIds = Object.keys(providers);

  return (
    <div class="config-panel">
      <div class="config-inner">
        <h2 class="config-title">LLM Providers</h2>
        <p class="config-note">Configure at least one provider to import and screen articles.</p>

        {providerIds.map(id => (
          <ProviderCard key={id} providerId={id} config={providers[id]} />
        ))}

        <AddProvider />
      </div>
    </div>
  );
}

function ProviderCard({ providerId, config }) {
  const [apiKey, setApiKey] = useState('');
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    state.getApiKey(providerId).then(k => { if (k) setApiKey(k); });
  }, [providerId]);

  const handleSaveKey = async () => {
    await state.setApiKey(providerId, apiKey);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  return (
    <div class="provider-card">
      <div class="provider-header">
        <span class="provider-label">{providerId}</span>
        <span class="provider-type">{config.type}</span>
      </div>
      <div class="provider-detail">
        <span>Model: {config.defaultModel}</span>
        {config.baseUrl && config.baseUrl !== 'https://api.openai.com/v1' && (
          <span> · URL: {config.baseUrl}</span>
        )}
      </div>
      <div class="provider-key-row">
        <input
          type="password"
          value={apiKey}
          onInput={e => setApiKey(e.target.value)}
          placeholder="API key..."
          class="provider-key-input"
        />
        <button class="btn btn-sm" onClick={handleSaveKey}>
          {saved ? '✓' : 'Save key'}
        </button>
      </div>
    </div>
  );
}

function AddProvider() {
  const [label, setLabel] = useState('');
  const [type, setType] = useState('openai');
  const [baseUrl, setBaseUrl] = useState('https://api.openai.com/v1');
  const [model, setModel] = useState('gpt-4o-mini');
  const [inputPrice, setInputPrice] = useState('0.15');
  const [outputPrice, setOutputPrice] = useState('0.60');
  const [expanded, setExpanded] = useState(false);

  const handleAdd = async () => {
    if (!label.trim()) return;
    await state.updateProviderConfig(label.trim(), {
      type,
      baseUrl: baseUrl.trim(),
      defaultModel: model.trim(),
      concurrency: 1,
      pricing: { [model.trim()]: { input: parseFloat(inputPrice), output: parseFloat(outputPrice) } },
    });
    setLabel('');
    setExpanded(false);
  };

  if (!expanded) {
    return (
      <button class="btn btn-secondary add-provider-btn" onClick={() => setExpanded(true)}>
        + Add provider
      </button>
    );
  }

  return (
    <div class="add-provider-form">
      <div class="form-row">
        <label>Label</label>
        <input value={label} onInput={e => setLabel(e.target.value)} placeholder="e.g. my-openai" class="form-input" />
      </div>
      <div class="form-row">
        <label>Type</label>
        <select value={type} onChange={e => {
          setType(e.target.value);
          if (e.target.value === 'openai') setBaseUrl('https://api.openai.com/v1');
          else if (e.target.value === 'ollama') setBaseUrl('http://localhost:11434/v1');
        }} class="form-input">
          <option value="openai">OpenAI / compatible</option>
          <option value="ollama">Ollama</option>
        </select>
      </div>
      <div class="form-row">
        <label>Base URL</label>
        <input value={baseUrl} onInput={e => setBaseUrl(e.target.value)} class="form-input" />
      </div>
      <div class="form-row">
        <label>Model</label>
        <input value={model} onInput={e => setModel(e.target.value)} class="form-input" />
      </div>
      <div class="form-row form-row-split">
        <div>
          <label>Input $/M tokens</label>
          <input type="number" step="0.01" value={inputPrice} onInput={e => setInputPrice(e.target.value)} class="form-input" />
        </div>
        <div>
          <label>Output $/M tokens</label>
          <input type="number" step="0.01" value={outputPrice} onInput={e => setOutputPrice(e.target.value)} class="form-input" />
        </div>
      </div>
      <div class="form-actions">
        <button class="btn btn-primary" onClick={handleAdd}>Add</button>
        <button class="btn btn-secondary" onClick={() => setExpanded(false)}>Cancel</button>
      </div>
    </div>
  );
}
