// Render engine (spec: Write)
// Sends sourcePrompt to LLM with tool definitions.
// LLM calls tools to query data model, receives results, produces final markdown.
// Max 10 iterations. Tools are Graph-scoped.

import * as llm from '../interfaces/llm.js';
import * as storage from '../interfaces/storage.js';
import { deriveAssessmentStats, deriveRQDistribution } from '../utils/threshold.js';
import { evaluateFilter } from '../utils/filter.js';

const RENDER_SYSTEM_PROMPT = `You are a research report section writer. You have access to tools that query a systematic review dataset. Use them to gather data, then write a well-structured section in markdown.

When the user provides a section prompt, use the available tools to fetch relevant data (article counts, assessment statistics, research question distributions, article details, etc.), then synthesize the information into a coherent, well-written section.

Write in an academic but clear style. Include specific numbers and statistics from the data. Do not fabricate any data — only use what the tools return.`;

/**
 * Render a section node.
 * @param {Object} params
 * @param {Object} params.sectionNode - GraphNode with kind='section'
 * @param {Object} params.graph - Graph entity
 * @param {string} params.projectId
 * @param {Object} params.llmConfig
 * @param {number} params.threshold - active threshold for derivation
 * @returns {Promise<{ renderedOutput: string, citedItemIds: { itemId: string, isExternal: boolean }[], tokenUsage: Object, cost: number }>}
 */
export async function renderSection({ sectionNode, graph, projectId, llmConfig, threshold }) {
  if (!sectionNode.sourcePrompt) {
    return { renderedOutput: '', citedItemIds: [], tokenUsage: { input: 0, output: 0 }, cost: 0 };
  }

  // Map<itemId, isExternal> — spec: citedItemIds carries { itemId, isExternal } per item
  const citedItems = new Map();

  // Collect data context — the LLM needs to know what IDs to pass to tools
  const context = await buildDataContext(graph, projectId);

  const systemPrompt = RENDER_SYSTEM_PROMPT + '\n\n' + context;

  // Build tools with handlers bound to the project context
  const tools = buildTools({ graph, projectId, threshold, citedItems });

  const messages = [
    { role: 'user', content: sectionNode.sourcePrompt },
  ];

  const result = await llm.completeWithTools(
    llmConfig,
    systemPrompt,
    messages,
    tools,
    10,
  );

  return {
    renderedOutput: result.content,
    citedItemIds: Array.from(citedItems.entries()).map(([itemId, isExternal]) => ({ itemId, isExternal })),
    tokenUsage: result.tokenUsage,
    cost: result.cost,
  };
}

async function buildDataContext(graph, projectId) {
  const articles = await storage.getArticles(projectId);
  const nodes = await storage.getGraphNodes(graph.id);
  const processingNodes = nodes.filter(n => n.kind === 'processing');

  const lines = [`Available data context:`, `- Total articles in library: ${articles.length}`];

  for (const pn of processingNodes) {
    const lr = await storage.getLayerResultByNode(pn.id);
    if (!lr) continue;
    const assessmentCount = (await storage.getAssessments(lr.id)).length;
    const protocol = pn.protocolId ? await storage.get('protocols', pn.protocolId) : null;
    const rqs = protocol?.researchQuestions || [];

    lines.push(`- Screening layer: layerResultId="${lr.id}", protocolId="${pn.protocolId}", status=${pn.status}, ${assessmentCount} assessments`);
    rqs.forEach((rq, i) => {
      lines.push(`  RQ${i + 1} (id="${rq.id}"): ${rq.text}`);
    });
  }

  lines.push('', 'Use these IDs when calling tools like get-assessment-stats, get-rq-distribution, etc.');
  return lines.join('\n');
}

function buildTools({ graph, projectId, threshold, citedItems }) {
  return [
    {
      name: 'query-corpus',
      description: 'Query articles reachable through the Graph. Returns articles matching a filter.',
      parameters: {
        type: 'object',
        properties: {
          filter: {
            type: 'object',
            description: 'FilterExpression: { field, op (eq/gt/lt/gte/lte/in/contains), value } or { and/or: [...] } or { not: {...} }. Fields: title, year, venue, authors, abstract, keywords.author, keywords.controlled.',
          },
          limit: { type: 'number', description: 'Max results to return. Default 20.' },
        },
      },
      handler: async ({ filter, limit = 20 }) => {
        const articles = await storage.getArticles(projectId);
        let filtered = filter ? articles.filter(a => evaluateFilter(a, filter)) : articles;
        filtered = filtered.slice(0, limit);
        filtered.forEach(a => citedItems.set(a.id, false));
        return {
          articles: filtered.map(a => ({
            id: a.id, title: a.title, authors: a.authors, year: a.year,
            venue: a.venue, abstract: a.abstract?.substring(0, 200),
          })),
          total: filtered.length,
        };
      },
    },
    {
      name: 'get-assessment-stats',
      description: 'Get screening statistics: total, relevant, contributing, mustRead counts. Optionally filter by research question.',
      parameters: {
        type: 'object',
        properties: {
          layerResultId: { type: 'string', description: 'LayerResult ID to query.' },
          rqId: { type: 'string', description: 'Optional: filter to specific research question ID.' },
        },
        required: ['layerResultId'],
      },
      handler: async ({ layerResultId, rqId }) => {
        const assessments = await storage.getAssessments(layerResultId);
        const stats = deriveAssessmentStats(assessments, threshold, rqId);

        // Year distribution
        const byYear = new Map();
        for (const a of assessments) {
          const article = await storage.get('articles', a.articleId);
          const yr = article?.year || 'unknown';
          if (!byYear.has(yr)) byYear.set(yr, { year: yr, total: 0, relevant: 0 });
          const entry = byYear.get(yr);
          entry.total++;
          const isRel = rqId
            ? a.perRQ.find(r => r.rqId === rqId)?.relevanceScore >= threshold
            : a.perRQ.some(r => r.relevanceScore >= threshold);
          if (isRel) entry.relevant++;
        }

        return {
          ...stats,
          byYear: Array.from(byYear.values()).sort((a, b) => (a.year > b.year ? 1 : -1)),
        };
      },
    },
    {
      name: 'get-rq-distribution',
      description: 'Get per-research-question distribution table showing which questions are underserved.',
      parameters: {
        type: 'object',
        properties: {
          layerResultId: { type: 'string', description: 'LayerResult ID.' },
          protocolId: { type: 'string', description: 'Protocol ID for RQ definitions.' },
        },
        required: ['layerResultId', 'protocolId'],
      },
      handler: async ({ layerResultId, protocolId }) => {
        const assessments = await storage.getAssessments(layerResultId);
        const protocol = await storage.get('protocols', protocolId);
        if (!protocol) return { error: 'Protocol not found' };
        return deriveRQDistribution(assessments, threshold, protocol.researchQuestions);
      },
    },
    {
      name: 'get-article-detail',
      description: 'Get full details of a specific article including all assessments.',
      parameters: {
        type: 'object',
        properties: {
          articleId: { type: 'string', description: 'Article ID.' },
        },
        required: ['articleId'],
      },
      handler: async ({ articleId }) => {
        const article = await storage.get('articles', articleId);
        if (!article) return { error: 'Article not found' };
        citedItems.set(articleId, false);
        const assessments = await storage.getArticleAssessments(articleId);
        return { article, assessments };
      },
    },
    {
      name: 'get-layer-summary',
      description: 'Get summary of a processing layer: status, progress, cost.',
      parameters: {
        type: 'object',
        properties: {
          layerResultId: { type: 'string', description: 'LayerResult ID.' },
        },
        required: ['layerResultId'],
      },
      handler: async ({ layerResultId }) => {
        const lr = await storage.get('layerResults', layerResultId);
        if (!lr) return { error: 'LayerResult not found' };
        const assessments = await storage.getAssessments(layerResultId);
        return {
          layerResultId: lr.id,
          processingNodeId: lr.processingNodeId,
          completed: assessments.length,
          totalCost: assessments.reduce((s, a) => s + (a.cost || 0), 0),
        };
      },
    },
    {
      name: 'list-codes',
      description: 'List all synthesis codes and themes.',
      parameters: { type: 'object', properties: {} },
      handler: async () => {
        const codes = await storage.getCodes(projectId);
        const themes = await storage.getThemes(projectId);
        return { codes, themes };
      },
    },
    {
      name: 'search-library',
      description: 'Search the full library by keyword. Results outside the Graph are flagged as external.',
      parameters: {
        type: 'object',
        properties: {
          query: { type: 'string', description: 'Search query.' },
          limit: { type: 'number', description: 'Max results. Default 10.' },
        },
        required: ['query'],
      },
      handler: async ({ query, limit = 10 }) => {
        const articles = await storage.getArticles(projectId);
        const q = query.toLowerCase();
        const matches = articles
          .filter(a =>
            (a.title || '').toLowerCase().includes(q) ||
            (a.abstract || '').toLowerCase().includes(q) ||
            (a.keywords?.author || []).some(k => k.toLowerCase().includes(q))
          )
          .slice(0, limit);
        // search-library is library-wide; results not already graph-scoped are external
        matches.forEach(a => {
          if (!citedItems.has(a.id)) citedItems.set(a.id, true);
        });
        return {
          results: matches.map(a => ({
            id: a.id, title: a.title, year: a.year, venue: a.venue,
          })),
        };
      },
    },
  ];
}
