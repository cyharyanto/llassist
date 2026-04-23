// OpenAI-compatible adapter (spec: Configure)
// Auth: Authorization: Bearer ${apiKey}
// Covers OpenAI, Azure OpenAI, vLLM, LMStudio, Ollama with OpenAI-compat.

/**
 * @param {import('../interfaces/llm.js').LLMConfig} config
 * @param {string} systemPrompt
 * @param {string} userContent
 * @param {Object} [outputSchema] - if provided, appended as instruction
 * @returns {Promise<import('../interfaces/llm.js').LLMResponse>}
 */
export async function complete(config, systemPrompt, userContent, outputSchema = null) {
  const messages = [
    { role: 'system', content: systemPrompt },
    { role: 'user', content: userContent },
  ];

  const body = {
    model: config.model,
    messages,
    temperature: config.temperature ?? 0,
    max_tokens: config.maxTokens ?? 4096,
  };

  // JSON mode if output schema provided
  if (outputSchema) {
    body.response_format = { type: 'json_object' };
  }

  const response = await fetchOpenAI(config, '/chat/completions', body);
  const choice = response.choices?.[0];
  const content = choice?.message?.content || '';
  const usage = response.usage || {};

  return {
    content,
    tokenUsage: {
      input: usage.prompt_tokens || 0,
      output: usage.completion_tokens || 0,
    },
    cost: computeCost(usage, config.pricing),
  };
}

/**
 * Multi-turn tool-use completion for section rendering.
 */
export async function completeWithTools(config, systemPrompt, messages, tools, maxIterations = 10) {
  const openaiTools = tools.map(t => ({
    type: 'function',
    function: {
      name: t.name,
      description: t.description,
      parameters: t.parameters,
    },
  }));

  let conversationMessages = [
    { role: 'system', content: systemPrompt },
    ...messages,
  ];

  let totalUsage = { input: 0, output: 0 };
  let totalCost = 0;
  let finalContent = '';
  const allToolCalls = [];

  for (let i = 0; i < maxIterations; i++) {
    const body = {
      model: config.model,
      messages: conversationMessages,
      temperature: config.temperature ?? 0,
      max_tokens: config.maxTokens ?? 4096,
      tools: openaiTools,
    };

    const response = await fetchOpenAI(config, '/chat/completions', body);
    const choice = response.choices?.[0];
    const usage = response.usage || {};

    totalUsage.input += usage.prompt_tokens || 0;
    totalUsage.output += usage.completion_tokens || 0;
    totalCost += computeCost(usage, config.pricing);

    const message = choice?.message;
    if (!message) break;

    // Add assistant message to conversation
    conversationMessages.push(message);

    // If no tool calls, we're done — extract final text
    if (!message.tool_calls?.length) {
      finalContent = message.content || '';
      break;
    }

    // Process tool calls
    for (const tc of message.tool_calls) {
      allToolCalls.push({
        name: tc.function.name,
        arguments: JSON.parse(tc.function.arguments || '{}'),
      });

      // Find the tool handler and execute
      const tool = tools.find(t => t.name === tc.function.name);
      let result;
      if (tool?.handler) {
        try {
          result = await tool.handler(JSON.parse(tc.function.arguments || '{}'));
        } catch (err) {
          result = { error: err.message };
        }
      } else {
        result = { error: `Unknown tool: ${tc.function.name}` };
      }

      conversationMessages.push({
        role: 'tool',
        tool_call_id: tc.id,
        content: JSON.stringify(result),
      });
    }
  }

  return {
    content: finalContent,
    toolCalls: allToolCalls,
    tokenUsage: totalUsage,
    cost: totalCost,
  };
}

// --- Internal ---

async function fetchOpenAI(config, path, body) {
  const baseUrl = (config.baseUrl || 'https://api.openai.com/v1').replace(/\/+$/, '');
  const url = `${baseUrl}${path}`;

  const headers = {
    'Content-Type': 'application/json',
  };

  if (config.apiKey) {
    headers['Authorization'] = `Bearer ${config.apiKey}`;
  }

  const resp = await fetch(url, {
    method: 'POST',
    headers,
    body: JSON.stringify(body),
  });

  if (!resp.ok) {
    const errorText = await resp.text().catch(() => '');
    throw new Error(`OpenAI API error ${resp.status}: ${errorText}`);
  }

  return resp.json();
}

function computeCost(usage, pricing) {
  if (!pricing) return 0;
  const inputCost = ((usage.prompt_tokens || 0) / 1_000_000) * (pricing.input || 0);
  const outputCost = ((usage.completion_tokens || 0) / 1_000_000) * (pricing.output || 0);
  return inputCost + outputCost;
}
