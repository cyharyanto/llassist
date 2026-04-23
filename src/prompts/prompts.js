// Prompts (spec: Prompts appendix)
// {{$variable}} placeholders substituted by caller.
// Output schema appended as JSON template.
// Default: temperature 0, maxTokens 4096.

export const INGESTION_PROMPT = `You are a bibliographic data parser. Extract structured metadata from the following raw bibliographic record.

Rules:
- Extract: title, authors (normalized to "Last, First M." format), year, venue, identifiers (DOI, arXiv, etc.), abstract, keywords (author-supplied and controlled/index terms separately).
- Preserve any additional fields in sourceFields as key-value pairs.
- NEVER invent identifiers, URLs, or abstracts not present in the input.
- If the record is malformed or fields are ambiguous, set parseQuality to "degraded" and explain in parseNote.
- If a field is not present in the input, set it to null.

Respond with ONLY a JSON object matching this schema:
{
  "title": "string",
  "authors": ["Last, First M."],
  "year": null,
  "venue": null,
  "abstract": null,
  "refs": {
    "doi": null, "arxivId": null, "openAlexId": null,
    "scopusEid": null, "pubmedId": null, "url": null
  },
  "keywords": {
    "author": [],
    "controlled": []
  },
  "citationCount": null,
  "sourceFields": {},
  "parseQuality": "ok",
  "parseNote": null
}`;

export const SCREENING_PHASE1_PROMPT = `You are a research content analyzer. Extract key semantic features from the following academic paper metadata.

Input:
{{$content}}

Extract and respond with ONLY a JSON object:
{
  "topics": ["main research topics covered"],
  "entities": ["specific technologies, methods, frameworks, datasets mentioned"],
  "keywords": ["additional relevant terms for classification"]
}`;

export const SCREENING_PHASE2_PROMPT = `You are a systematic review screener. Assess whether the following paper is relevant to a specific research question.

Paper metadata and key semantics:
{{$content}}

Content type: {{$contentType}}

Research Question: {{$question}}

Definitions and scope:
{{$definitions}}

Assess the paper's relevance and potential contribution to this research question.

- relevanceScore (0-1): How directly does the paper address this research question? 0 = completely unrelated, 1 = directly answers the question.
- contributionScore (0-1): If relevant, how significant is the paper's contribution to understanding this question? 0 = no contribution, 1 = major contribution.
- recommendation: "include", "exclude", or "uncertain"

Provide detailed reasoning BEFORE scoring. Think step-by-step about what the paper covers and how it relates to the research question.

Respond with ONLY a JSON object:
{
  "reasoning": "Step-by-step analysis of relevance...",
  "relevanceScore": 0.0,
  "contributionScore": 0.0,
  "recommendation": "exclude"
}`;

// --- Prompt substitution ---

export function substitutePrompt(template, variables) {
  let result = template;
  for (const [key, value] of Object.entries(variables)) {
    result = result.replaceAll(`{{$${key}}}`, value ?? '');
  }
  return result;
}
