// Integration interface (spec: Architecture — fourth of five interfaces)
// BibTeX/CSV/RIS deterministic export; import orchestration.
// Export: deterministic, no LLM. Import: orchestrated, uses LLM.
// Export implemented in Phase 3. Import orchestration in Phase 4.

// --- Import: file splitting (deterministic) ---

/**
 * Split a file into units for LLM ingestion.
 * CSV: header + one row per unit.
 * BibTeX: split on @ markers.
 * RIS: split on TY/ER markers.
 * PDF: one unit per file.
 *
 * @param {string} content - raw file text
 * @param {string} format - 'csv' | 'bibtex' | 'ris' | 'pdf'
 * @returns {string[]} array of text units
 */
export function splitFile(content, format) {
  switch (format) {
    case 'csv': return splitCSV(content);
    case 'bibtex': return splitBibTeX(content);
    case 'ris': return splitRIS(content);
    case 'pdf': return [content]; // one unit
    default: throw new Error(`Unknown import format: ${format}`);
  }
}

function splitCSV(content) {
  // RFC 4180-aware row splitting. Quoted fields may contain newlines, commas, and escaped quotes ("").
  const rows = parseCSVRows(content);
  if (rows.length < 2) return [];
  const header = rows[0];
  const units = [];
  for (let i = 1; i < rows.length; i++) {
    const row = rows[i].trim();
    if (!row) continue;
    // Always header + one row (spec: Import)
    units.push(header + '\n' + row);
  }
  return units;
}

/**
 * Split CSV content into raw row strings, respecting quoted fields.
 * Does not parse fields — only identifies row boundaries.
 */
function parseCSVRows(content) {
  const rows = [];
  let current = '';
  let inQuotes = false;

  for (let i = 0; i < content.length; i++) {
    const ch = content[i];

    if (inQuotes) {
      if (ch === '"') {
        // Peek ahead: "" is escaped quote, otherwise end of quoted field
        if (i + 1 < content.length && content[i + 1] === '"') {
          current += '""';
          i++; // skip next quote
        } else {
          inQuotes = false;
          current += ch;
        }
      } else {
        current += ch;
      }
    } else {
      if (ch === '"') {
        inQuotes = true;
        current += ch;
      } else if (ch === '\n') {
        rows.push(current);
        current = '';
      } else if (ch === '\r') {
        // Skip \r; \n will follow
      } else {
        current += ch;
      }
    }
  }

  if (current.trim()) rows.push(current);
  return rows;
}

function splitBibTeX(content) {
  // Split on @ markers (spec: Import)
  const entries = [];
  const parts = content.split(/(?=@\w+\{)/);
  for (const part of parts) {
    const trimmed = part.trim();
    if (trimmed && trimmed.startsWith('@')) {
      entries.push(trimmed);
    }
  }
  return entries;
}

function splitRIS(content) {
  // Split on TY/ER markers (spec: Import)
  const entries = [];
  const lines = content.split('\n');
  let current = [];
  let inEntry = false;

  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith('TY  -')) {
      inEntry = true;
      current = [trimmed];
    } else if (trimmed.startsWith('ER  -') && inEntry) {
      current.push(trimmed);
      entries.push(current.join('\n'));
      current = [];
      inEntry = false;
    } else if (inEntry) {
      current.push(trimmed);
    }
  }

  return entries;
}

/**
 * Detect import format from file extension.
 */
export function detectFormat(fileName) {
  const ext = fileName.toLowerCase().split('.').pop();
  if (ext === 'csv' || ext === 'tsv') return 'csv';
  if (ext === 'bib' || ext === 'bibtex') return 'bibtex';
  if (ext === 'ris') return 'ris';
  if (ext === 'pdf') return 'pdf';
  return null;
}

// --- Export: deterministic, no LLM ---

/**
 * Export articles as BibTeX.
 * Entry type inferred from metadata: ISSN → @article, ISBN/conference → @inproceedings.
 * Author format: Last, First M.
 * Absent fields omitted.
 * @param {Array} articles
 * @returns {string} valid BibTeX
 */
export function exportBibTeX(articles) {
  return articles.map(a => articleToBibTeX(a)).join('\n\n') + '\n';
}

function articleToBibTeX(article) {
  const type = inferBibTeXType(article);
  const key = generateCiteKey(article);
  const fields = [];

  if (article.title) fields.push(`  title = {${escapeBibTeX(article.title)}}`);
  if (article.authors?.length) {
    fields.push(`  author = {${article.authors.join(' and ')}}`);
  }
  if (article.year) fields.push(`  year = {${article.year}}`);
  if (article.venue) {
    if (type === 'article') fields.push(`  journal = {${escapeBibTeX(article.venue)}}`);
    else fields.push(`  booktitle = {${escapeBibTeX(article.venue)}}`);
  }
  if (article.refs?.doi) fields.push(`  doi = {${article.refs.doi}}`);
  if (article.refs?.url) fields.push(`  url = {${article.refs.url}}`);
  if (article.abstract) fields.push(`  abstract = {${escapeBibTeX(article.abstract)}}`);
  if (article.keywords?.author?.length) {
    fields.push(`  keywords = {${article.keywords.author.join(', ')}}`);
  }

  // sourceFields pass-through: volume, number, pages, publisher, issn, isbn
  for (const key of ['volume', 'number', 'pages', 'publisher', 'issn', 'isbn']) {
    if (article.sourceFields?.[key]) {
      fields.push(`  ${key} = {${escapeBibTeX(article.sourceFields[key])}}`);
    }
  }

  return `@${type}{${key},\n${fields.join(',\n')}\n}`;
}

function inferBibTeXType(article) {
  // spec: ISSN → @article, ISBN or conference metadata → @inproceedings
  if (article.sourceFields?.issn) return 'article';
  if (article.sourceFields?.isbn) return 'inproceedings';
  if (article.sourceFields?.conference) return 'inproceedings';
  if (article.sourceFields?.booktitle) return 'inproceedings';
  // Default: if venue looks like a journal name (heuristic), article
  return 'article';
}

function generateCiteKey(article) {
  const author = (article.authors?.[0] || 'unknown').split(',')[0].replace(/\s+/g, '');
  const year = article.year || 'nd';
  const word = (article.title || 'untitled').split(/\s+/)[0].toLowerCase().replace(/[^a-z]/g, '');
  return `${author}${year}${word}`;
}

function escapeBibTeX(str) {
  return str.replace(/[{}]/g, '');
}

/**
 * Export articles as RIS.
 * @param {Array} articles
 * @returns {string} valid RIS
 */
export function exportRIS(articles) {
  return articles.map(a => articleToRIS(a)).join('\n') + '\n';
}

function articleToRIS(article) {
  const lines = [];
  const type = article.sourceFields?.isbn || article.sourceFields?.conference ? 'CPAPER' : 'JOUR';
  lines.push(`TY  - ${type}`);
  if (article.title) lines.push(`TI  - ${article.title}`);
  for (const author of (article.authors || [])) {
    lines.push(`AU  - ${author}`);
  }
  if (article.year) lines.push(`PY  - ${article.year}`);
  if (article.venue) {
    if (type === 'JOUR') lines.push(`JO  - ${article.venue}`);
    else lines.push(`T2  - ${article.venue}`);
  }
  if (article.refs?.doi) lines.push(`DO  - ${article.refs.doi}`);
  if (article.refs?.url) lines.push(`UR  - ${article.refs.url}`);
  if (article.abstract) lines.push(`AB  - ${article.abstract}`);
  for (const kw of (article.keywords?.author || [])) {
    lines.push(`KW  - ${kw}`);
  }
  for (const key of ['volume', 'number', 'pages', 'publisher', 'issn', 'isbn']) {
    if (article.sourceFields?.[key]) {
      const tag = { volume: 'VL', number: 'IS', pages: 'SP', publisher: 'PB', issn: 'SN', isbn: 'SN' }[key];
      lines.push(`${tag}  - ${article.sourceFields[key]}`);
    }
  }
  lines.push('ER  -');
  return lines.join('\n');
}

/**
 * Export articles as CSV.
 * @param {Array} articles
 * @returns {string} CSV text
 */
export function exportCSV(articles) {
  const headers = ['title', 'authors', 'year', 'venue', 'doi', 'abstract', 'keywords'];
  const rows = articles.map(a => [
    csvEscape(a.title || ''),
    csvEscape((a.authors || []).join('; ')),
    a.year || '',
    csvEscape(a.venue || ''),
    a.refs?.doi || '',
    csvEscape(a.abstract || ''),
    csvEscape((a.keywords?.author || []).join('; ')),
  ].join(','));
  return headers.join(',') + '\n' + rows.join('\n') + '\n';
}

function csvEscape(str) {
  if (str.includes(',') || str.includes('"') || str.includes('\n')) {
    return '"' + str.replace(/"/g, '""') + '"';
  }
  return str;
}
