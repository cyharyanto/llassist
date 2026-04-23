# LLAssist — Software Requirements Specification

Implementation tracking. Each row is a discrete requirement. Status: `[x]` = implemented, `[ ]` = not yet.

Remarks note known gaps between spec and implementation where the checkbox is checked.

---

## Architecture

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| A-01 | Client-side SPA; no server-side component | [x] | |
| A-02 | Served over HTTP (GitHub Pages production, local static server dev) | [x] | |
| A-03 | `start.sh` / `start.bat` fallback chain: Vite → python → npx serve | [x] | |
| A-04 | Browser requirements: IndexedDB, Fetch API, `crypto.randomUUID()` | [x] | |
| A-05 | IndexedDB as working memory; `.llassist` file as durable portable state | [x] | |
| A-06 | IDB `onupgradeneeded` with `DB_VERSION` integer; one object store per entity | [x] | |
| A-07 | Thin promise wrappers (`dbPut`, `dbGet`, `dbGetAll`, `dbDelete`); no library | [x] | |
| A-08 | Auto-save to IDB every 30s when dirty (crash recovery) | [x] | Timer runs but callback is no-op; writes already go to IDB on every mutation. The 30s timer is structurally present but functionally redundant. |
| A-09 | Ctrl+S triggers `.llassist` file download | [x] | |
| A-10 | `beforeunload` warns if dirty | [x] | |
| A-11 | All entity IDs: `crypto.randomUUID()` | [x] | |
| A-12 | DOM is a projection; state layer → DOM; DOM events → state; state layer never reads DOM | [x] | |
| A-13 | Transient fields (`_` prefix) stripped before persistence, defaults on load | [x] | |
| A-14 | Five interfaces only: storage, llm, layers, integration, project | [x] | |
| A-15 | Every mutable entity carries version; writes carry expected version; mismatch → rejection | [x] | CAS enforced on section nodes (prompt update, render write) and processing node status transitions. Other mutable writes (provider config, graph creation, report creation) use plain put — acceptable for single-tab SPA but not fully disciplined. |
| A-16 | Typed failures: terminal, retryable, informational, operational | [x] | |

## Entities — Organize

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| O-01 | `Project { id, name, schemaVersion, createdAt, updatedAt }` | [x] | |
| O-02 | `Folder { id, projectId, parentFolderId, name, createdAt, updatedAt }` | [x] | |
| O-03 | `Note { id, projectId, folderId, name, content, tags[], createdAt, updatedAt, version }` | [x] | |
| O-04 | Project-level Trash; deleted items excluded from Library and Graph | [ ] | Current deletes are hard deletes. Invariant IN-03 is not enforced. |
| O-05 | Trash: items referenced by Graph nodes cannot be trashed until reference removed | [ ] | |
| O-06 | Emptying Trash requires confirmation | [ ] | |

## Import

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| I-01 | Import is a Library operation (no Report/Graph required) | [x] | |
| I-02 | File splitting: CSV (header + one row per unit), BibTeX (split on `@`), RIS (split on `TY/ER`), PDF (one unit) | [x] | CSV parser is RFC 4180-aware (handles quoted multiline fields). |
| I-03 | LLM semantic parsing per unit → canonical `Article` | [x] | |
| I-04 | `Article` entity with all spec fields (refs, keywords.author/controlled, sourceFields, parseQuality, parseNote) | [x] | |
| I-05 | LLM must not invent identifiers/URLs/abstracts; malformed → `parseQuality: "degraded"` | [x] | Enforced by prompt; not structurally verified post-response. |
| I-06 | `Import` entity tracks operation (fileName, fileHash, totalUnits, status, budget, actualCost) | [x] | |
| I-07 | Idempotent resume: `getUnprocessedUnits(importId, totalUnits) → integer[]` | [x] | Resume finds existing incomplete Import by fileHash; reuses identity. |
| I-08 | Budget enforcement on actuals; pause on breach | [x] | |
| I-09 | Sequential processing with configurable inter-call delay (default 500ms) | [x] | |
| I-10 | Cost estimation displayed as range before commit | [ ] | `estimateCost()` function exists but not wired to UI. |
| I-11 | Processing status visible in Library (abstract present, title-only, degraded flagged) | [x] | |

## Analyze — Graph & Protocol

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| G-01 | `Graph { id, projectId, reportId, edges[], createdAt, updatedAt, version }` | [x] | |
| G-02 | `GraphNode { id, graphId, kind }` — three kinds: source-ref, processing, section | [x] | |
| G-03 | Edges on Graph entity; `graph.edges.filter(e => e.toNodeId === nodeId)` for sources | [x] | |
| G-04 | Visual DAG editor (React Flow + dagre) with drag-from-Library | [ ] | Graph auto-created by screening; no visual editor yet. |
| G-05 | Cycle detection: BFS within Graph + transitive Report reference check | [ ] | |
| G-06 | `Protocol` entity with all spec fields (supersedes, prompts, researchQuestions, threshold, seedExamples, extractionSchema, embeddingModel) | [x] | |
| G-07 | Protocol immutable once processing starts; edits produce new version with `supersedes` link | [x] | |
| G-08 | `SubsetDefinition { type: "all" \| "explicit" \| "filter" }` | [x] | |
| G-09 | `FilterExpression` with logical combinators (and/or/not) and leaf ops (eq/gt/lt/gte/lte/in/contains) | [x] | |
| G-10 | Graph Engine: resolve target set from edges → outputs → subset | [ ] | Screening currently targets all project articles, not Graph-resolved set. |

## Analyze — Screening

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| S-01 | Two-phase screening: Phase 1 key semantics, Phase 2 relevance per RQ | [x] | |
| S-02 | `Assessment` entity (keySemantics, perRQ, tokenUsage, cost, authorityClass) | [x] | |
| S-03 | `LayerResult { id, projectId, folderId, graphId, processingNodeId }` | [x] | |
| S-04 | Idempotent resume: `getUnprocessedArticles(processingNodeId) → Article[]` | [x] | Resume finds existing incomplete processing node by protocolId; reuses identity and layerResult. |
| S-05 | Processing nodes saved as `"running"` corrected to `"interrupted"` on project open | [x] | |
| S-06 | Threshold derivation: `isRelevant = relevanceScore >= activeThreshold`; `mustRead = any(isRelevant \|\| isContributing)` | [x] | |
| S-07 | Active threshold (UI slider) drives all derivation; zero network calls on change | [x] | Protocol also stores a threshold field which is not exposed in UI and not used for derivation. The active threshold signal is the sole live authority. |
| S-08 | Per-RQ distribution tables: relevant, contributing, ratio, underserved identification | [x] | |
| S-09 | Temporal distribution (by year) | [ ] | |

## Analyze — Extraction

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| E-01 | Full-text upload (transient; PDF.js extracts text client-side; PDF bytes discarded) | [ ] | |
| E-02 | `ExtractionRecord` entity (fields, citations, reasoning, tokenUsage, cost) | [x] | Entity factory exists; extraction engine not wired. |

## Analyze — Dedup

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| D-01 | Phase 1: vector similarity via provider embedding endpoint | [ ] | |
| D-02 | Phase 2: LLM pair comparison → merge/discard/distinct | [ ] | |
| D-03 | `DedupDecision` entity (articleIdA, articleIdB, similarity, decision, reasoning, survivorId) | [x] | Entity factory exists; dedup engine not wired. |

## Analyze — Validation & Override

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| V-01 | Sampling processing node: random, stratified by year, stratified by score band | [ ] | |
| V-02 | Human labeling + inter-reviewer agreement (Cohen's κ, precision, recall) | [ ] | |
| V-03 | `Adjudication` entity (researcher override, authorityClass: "researcher-confirmed") | [x] | Entity factory exists; override UI not wired. |
| V-04 | `Disagreement` entity (first-class record; exports surface disagreements) | [x] | Entity factory exists; disagreement tracking not wired. |

## Analyze — Synthesis

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| Y-01 | `Code { id, projectId, label, parentId, evidenceRefs[] }` | [x] | Entity factory exists; synthesis UI not wired. |
| Y-02 | `Theme { id, projectId, label, codeIds[] }` | [x] | Entity factory exists. |
| Y-03 | `Finding { id, projectId, claimText, codeIds[] (non-empty) }` | [x] | Entity factory exists; enforces non-empty codeIds. |
| Y-04 | Authority classes: system-observation, researcher-confirmed, protocol-derived, machine-interpretation | [x] | |
| Y-05 | LLM-proposed candidate Codes from ExtractionRecords → machine-interpretation until confirmed | [ ] | |
| Y-06 | Synthesis UI (code/theme/finding CRUD, evidence attachment) | [ ] | |

## Analyze — Focus

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| F-01 | `getArticlesNearThreshold(layerResultId, threshold, margin)` | [x] | |
| F-02 | Attention routing: near-threshold first, then disagreements, then degraded | [ ] | |

## Write

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| W-01 | `Report { id, projectId, folderId, name, graphId, renderProviderId, renderModel }` | [x] | |
| W-02 | Section nodes ordered by `ordinal`; `sourcePrompt` is researcher text | [x] | |
| W-03 | Render Engine: multi-turn tool-use loop, max 10 iterations | [x] | |
| W-04 | Seven rendering tools: query-corpus, get-assessment-stats, get-layer-summary, get-article-detail, list-codes, get-finding, search-library | [x] | `get-finding` not implemented (returns error); others functional. |
| W-05 | Tool access Graph-scoped; search-library results outside Graph flagged `isExternal: true` in `citedItemIds` | [x] | Graph scoping partially enforced: `query-corpus` queries all project articles, not only Graph-reachable ones. `search-library` correctly flags as external. True Graph-scoped filtering requires Graph Engine (G-10). |
| W-06 | `citedItemIds: { itemId, isExternal }[]` populated from tool-call log | [x] | |
| W-07 | `renderFingerprint`: hash of (sourcePrompt + LayerResult versions + Protocol versions + Adjudication versions + Code/Theme/Finding versions) | [ ] | |
| W-08 | `isStale` boolean is cache; fingerprint is truth | [ ] | `isStale` set manually on prompt edit; fingerprint-based staleness not implemented. |
| W-09 | Staleness propagation: forward BFS up DAG on any node output change | [ ] | |
| W-10 | "Render all stale" sequential with inter-call delay; retain prior output on failure | [ ] | |
| W-11 | Section split: two new sections, original → both predecessors | [ ] | |
| W-12 | Section merge: one new section, both originals → predecessors; undo moves merged to Trash | [ ] | |
| W-13 | NLE guarantee: researcher text never overwritten; editing marks render stale | [x] | |
| W-14 | Section history preserved (prior sourcePrompt + renderedOutput + timestamp) | [x] | |
| W-15 | Drilldown: section → citedItemIds → articles; article → assessments; code → evidence → articles | [ ] | Storage queries exist (`getItemsReferencingArticle`, `getArticleAssessments`); UI not wired. |

## Share

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| P-01 | `.llassist` file = ZIP with canonical JSON | [x] | |
| P-02 | File structure: manifest.json, config.json, library/, graphs/, processing/, synthesis/, protocols/, imports/ | [x] | |
| P-03 | Canonical serialization: keys sorted lexicographically, JSONL ordered by ID, UTF-8, LF, trailing LF | [x] | |
| P-04 | Round-trip: `save(open(save(P))) == save(P)` (savedAt excluded) | [x] | |
| P-05 | No API keys, PDF bytes, full-text extracts, or verbatim spans >15 words in project file | [x] | |
| P-06 | Schema compatibility: strict at major, automatic at minor, transparent at patch | [x] | Major version check on open; minor/patch migration not exercised yet. |
| P-07 | `project.merge(blob, targetProjectId, strategy)` — take-mine/take-theirs/keep-both | [ ] | |
| P-08 | `project.diff(a, b)` — field-level diffs aligned by ID | [ ] | |
| P-09 | `project.exportSubset(reportId)` — one Report with transitive dependencies | [ ] | |
| P-10 | BibTeX export: deterministic, no LLM; ISSN → `@article`, ISBN/conference → `@inproceedings` | [x] | |
| P-11 | RIS export: deterministic | [x] | |
| P-12 | CSV export: deterministic | [x] | |

## Configure

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| C-01 | `ProjectConfig.providers`: Record of { type, baseUrl, defaultModel, concurrency, pricing } | [x] | |
| C-02 | `KeyStore`: per-installation IDB store; never in project file | [x] | |
| C-03 | Provider resolution: Protocol.providerId → ProjectConfig → KeyStore | [x] | |
| C-04 | Adapter: OpenAI (`Authorization: Bearer`) | [x] | |
| C-05 | Adapter: Anthropic (`x-api-key`, `anthropic-version`, `anthropic-dangerous-direct-browser-access`) | [ ] | |
| C-06 | Adapter: Gemini (`?key=` query parameter) | [ ] | |
| C-07 | Adapter: Ollama (no auth; OpenAI-compat mode) | [x] | Uses OpenAI adapter with Ollama base URL. |
| C-08 | Base URL override for proxies, Azure, vLLM, LMStudio | [x] | |
| C-09 | Cost estimation: pricing × estimated tokens × count, displayed as range | [ ] | `estimateCost()` exists; UI not wired. |
| C-10 | Cost tracking: tokenUsage + cost on every result; running sum on processing node/import | [x] | |
| C-11 | ProjectConfig migration on open: spread loaded values over hardcoded defaults | [ ] | |

## Prompts

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| PR-01 | `{{$variable}}` placeholder substitution | [x] | |
| PR-02 | Output schema appended as JSON template | [x] | Inline in prompt text, not separate schema object. |
| PR-03 | Provider-specific JSON-mode enforcement | [x] | OpenAI `response_format: { type: "json_object" }`. |
| PR-04 | Default: temperature 0, maxTokens 4096 | [x] | |
| PR-05 | Ingestion prompt: extract canonical fields, never invent, mark degraded | [x] | |
| PR-06 | Screening Phase 1: key semantics extraction | [x] | |
| PR-07 | Screening Phase 2: per-RQ relevance with reasoning before scoring | [x] | |
| PR-08 | Rendering tools: seven tool definitions with specified signatures | [x] | `get-finding` stub only. |

## Invariants

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| IN-01 | Reasoning is source; scores are derived (inclusion recomputed live from active threshold) | [x] | |
| IN-02 | Human judgment authoritative (overrides are Adjudications, not edits) | [x] | Entity structure enforced; UI not wired. |
| IN-03 | No operation destructive (delete → Trash; protocol edit → new version) | [ ] | `deleteItem` and `deleteSection` are hard deletes. Protocol edit → new version is enforced. |
| IN-04 | Provenance is law (every artifact traces to inputs; orphan claims impossible) | [x] | Finding requires non-empty codeIds; Assessment links to Protocol + Article + LayerResult. |
| IN-05 | Analytical state portable (project file carries everything; no PDFs/keys/full-text) | [x] | |
| IN-06 | Messy in, clean out (LLM processes input; deterministic code produces output) | [x] | |
| IN-07 | Graph is acyclic (including transitive Report references) | [ ] | Cycle detection not implemented. |
| IN-08 | DOM is projection (state layer is truth) | [x] | |

## Conformance

| ID | Requirement | Status | Remark |
|----|-------------|--------|--------|
| CF-01 | Round-trip byte-equality: `save(open(save(P))) == save(P)` | [x] | |
| CF-02 | Threshold change: zero network activity, all derived views update within one animation frame | [x] | |
| CF-03 | Editing RQ → new Protocol + processing node; prior results intact | [x] | |
| CF-04 | No API keys, PDF bytes, or verbatim spans >15 words in project file | [x] | |
| CF-05 | Interrupted processing resumes by processing only entities lacking results | [x] | |
| CF-06 | 10,000 articles: load, filter, browse without UI unresponsive | [ ] | No virtual scrolling; not tested at scale. |
| CF-07 | CSV ingestion → canonical Articles; re-ingestion with same config → structurally equivalent | [x] | |
| CF-08 | BibTeX/RIS export parseable by standard reference managers | [x] | |
| CF-09 | Section prompt edit → stale; re-render reflects current state; history preserved | [x] | |
| CF-10 | Concurrent writes → version rejection, never silent overwrite | [x] | CAS on critical paths (section nodes, processing node status). Other mutable writes use plain put. |
| CF-11 | DOM rebuild from state → identical application state | [x] | |
| CF-12 | `.llassist` file cross-browser/OS compatible | [x] | |
