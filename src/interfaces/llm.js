// LLM interface (spec: Architecture — second of five interfaces)
// Provider-agnostic screening, extraction, rendering, ingestion calls.
// Adapters construct fetch calls with provider-specific auth.
// Implementation deferred to Phase 4 — contract defined here.

/**
 * @typedef {Object} LLMResponse
 * @property {string} content - model output text
 * @property {Object} tokenUsage - { input: number, output: number }
 * @property {number} cost - computed from tokenUsage × pricing
 */

/**
 * @typedef {Object} LLMConfig
 * @property {string} type - openai | anthropic | gemini | ollama
 * @property {string} baseUrl
 * @property {string} model
 * @property {string} apiKey
 * @property {number} temperature
 * @property {number} maxTokens
 * @property {Object} pricing - { input: number, output: number } per million tokens
 */

/**
 * Call an LLM with a prompt and expect structured JSON output.
 * @param {LLMConfig} config
 * @param {string} systemPrompt
 * @param {string} userContent
 * @param {Object} [outputSchema] - JSON schema for structured output
 * @returns {Promise<LLMResponse>}
 */
export async function complete(config, systemPrompt, userContent, outputSchema = null) {
  const adapter = getAdapter(config.type);
  return adapter.complete(config, systemPrompt, userContent, outputSchema);
}

/**
 * Call an LLM with tool definitions (multi-turn, for section rendering).
 * @param {LLMConfig} config
 * @param {string} systemPrompt
 * @param {Array} messages - conversation history
 * @param {Array} tools - tool definitions
 * @param {number} maxIterations
 * @returns {Promise<{ content: string, toolCalls: Array, tokenUsage: Object, cost: number }>}
 */
export async function completeWithTools(config, systemPrompt, messages, tools, maxIterations = 10) {
  const adapter = getAdapter(config.type);
  return adapter.completeWithTools(config, systemPrompt, messages, tools, maxIterations);
}

// --- Adapters ---

const adapters = {};

export function registerAdapter(type, adapter) {
  adapters[type] = adapter;
}

function getAdapter(type) {
  const adapter = adapters[type];
  if (!adapter) {
    throw new Error(`No LLM adapter registered for type: ${type}. Configure a provider first.`);
  }
  return adapter;
}

// Auto-register built-in adapters
import * as openaiAdapter from '../llm/openai.js';
registerAdapter('openai', openaiAdapter);
// Ollama with OpenAI-compat mode uses the same adapter
registerAdapter('ollama', openaiAdapter);

/**
 * Resolve LLM config from ProjectConfig + KeyStore.
 * @param {Object} projectConfig - ProjectConfig.providers
 * @param {string} providerId - label key
 * @param {Function} getApiKey - async (providerId) => string|null
 * @returns {Promise<LLMConfig>}
 */
export async function resolveConfig(projectConfig, providerId, getApiKey) {
  const provider = projectConfig[providerId];
  if (!provider) {
    throw new Error(`Provider "${providerId}" not found in project config.`);
  }

  const apiKey = await getApiKey(providerId);
  // Ollama needs no key (spec: Configure)
  if (!apiKey && provider.type !== 'ollama') {
    throw new Error(`No API key for provider "${providerId}". Supply key to enable processing.`);
  }

  return {
    type: provider.type,
    baseUrl: provider.baseUrl,
    model: provider.defaultModel,
    apiKey: apiKey || '',
    temperature: 0,
    maxTokens: 4096,
    pricing: provider.pricing?.[provider.defaultModel] || { input: 0, output: 0 },
  };
}
