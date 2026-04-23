# LLAssist

Research authoring environment for systematic literature reviews. Import bibliographic data, screen articles against research questions using LLMs, explore field-level gaps, and render report sections from live analysis.

Runs entirely in the browser. No server required.

## Versions

| Version | Stack | Branch/Tag |
|---------|-------|------------|
| **Current** (v2 — browser SPA) | Preact, Vite, IndexedDB | [`main`](https://github.com/ZipThought/llassist) |
| **Legacy** (v0.1 — .NET server) | C#, Blazor, PostgreSQL, Docker | [`archive/dotnet-v0.1`](https://github.com/ZipThought/llassist/tree/archive/dotnet-v0.1) |

> The v0.1 .NET version is archived and no longer actively developed. See the [v0.1 README](https://github.com/ZipThought/llassist/tree/archive/dotnet-v0.1#readme) for setup and usage of that version.
>
> The paper [LLAssist: Simple Tools for Automating Literature Review Using Large Language Models](https://doi.org/10.48550/arXiv.2407.13993) refers to the v0.1 .NET codebase.

## Quick Start

```bash
npm install
npm run dev
```

Open `http://localhost:5173`.

## Usage

### 1. Create a Project

Launch LLAssist → enter a project name → **New Project**.

### 2. Configure a Provider

**Settings** tab → **+ Add provider**.

| Field | Example |
|-------|---------|
| Label | `my-openai` |
| Type | OpenAI / compatible |
| Base URL | `https://api.openai.com/v1` |
| Model | `gpt-4o-mini` |
| Input $/M tokens | `0.15` |
| Output $/M tokens | `0.60` |

Enter your API key and click **Save key**. The key stays in your browser (IndexedDB) and is never included in project files.

For Ollama: set type to Ollama, base URL to `http://localhost:11434/v1`, no API key needed.

### 3. Import Articles

**Library** tab → **Import** → select a `.csv`, `.bib`, or `.ris` file → choose provider → **Start Import**.

Articles appear in the Library as they are processed. Each record is sent to the LLM for semantic parsing into a canonical format.

### 4. Define Protocol & Screen

**Analyze** tab → add research questions → select provider → **Create Protocol** → **Start Screening**.

Each article is screened in two phases:
- Phase 1: extract key semantics (topics, entities, keywords)
- Phase 2: estimate relevance and contribution per research question

### 5. Explore Results

After screening completes, the results panel shows:
- **Threshold slider** — adjust inclusion threshold; all counts update instantly with zero network calls
- **Stats** — total, relevant, contributing, must-read counts
- **Per-RQ distribution** — which questions are underserved
- **Article list** — sorted by relevance, click to drill into per-RQ reasoning

### 6. Render Report Sections

**Report** tab → **+ Add Section** → write a prompt describing what the section should contain → **Render**.

The LLM has access to tools that query your screening data (assessment stats, article details, RQ distributions). It calls these tools, gathers data, and produces a markdown section.

Example prompt:
> Summarize the distribution of relevant papers across all research questions. Identify which questions are underserved and discuss temporal trends.

### 7. Save & Share

**Ctrl+S** (or **Save** button) downloads a `.llassist` file containing the entire project: articles, assessments, protocols, rendered sections — everything except API keys and PDF content.

Open the file in any browser running LLAssist to resume work.

### 8. Export

**Library** tab → **BibTeX↓** or **RIS↓** exports articles in the current folder (or all articles if the folder is empty) as standard bibliographic formats.

## Hosting

### Local Development

```bash
npm install
npm run dev
```

### Local Production Preview

```bash
npm run build
npm run preview
```

Or without Node.js:

```bash
npm run build
cd dist && python3 -m http.server 8080
```

The `start.sh` and `start.bat` scripts automate this fallback chain.

### GitHub Pages

1. Push the repository to GitHub
2. Go to **Settings → Pages → Source** → select **GitHub Actions**
3. Push to `main` — the included workflow (`.github/workflows/deploy.yml`) builds and deploys automatically

The site will be available at `https://<username>.github.io/<repo-name>/`.

## Browser Requirements

Chrome 90+, Firefox 90+, Edge 90+, Safari 15+. Requires IndexedDB, Fetch API, and `crypto.randomUUID()`.

## Project File Format

`.llassist` files are ZIP archives containing JSON/JSONL with canonical serialization (sorted keys, deterministic output). See `SPECIFICATION.md` for the full file structure.

## License

MIT
