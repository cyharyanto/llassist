# LLAssist

LLAssist is a tool for processing and analyzing research articles using Natural Language Processing (NLP) techniques and Large Language Models (LLMs).

Note:
- The paper with title [LLAssist: Simple Tools for Automating Literature Review Using Large Language Models](https://doi.org/10.48550/arXiv.2407.13993) uses [commit versions prior to `07caad7`](https://github.com/cyharyanto/llassist/tree/07caad7d954f9e64933ffa5aa34d0b745006feea), specifically [commit `3bf51a6`](https://github.com/cyharyanto/llassist/tree/3bf51a695b945e07c77eaa0a323c9aa3e57372bd).

## Features

- Read articles from CSV files
- Extract key semantics (topics, entities, keywords) from article titles and abstracts
- Estimate relevance of articles to research questions
- Generate embeddings for keywords
- Output results in both JSON and CSV formats

## Components

### Program.cs

The main entry point of the application. It orchestrates the process of:
1. Reading articles from a CSV file
2. Processing each article to extract semantics and estimate relevance
3. Writing results incrementally to a CSV file
4. Generating a final JSON output

### Services

#### NLPService

Handles Natural Language Processing tasks:
- Extracting key semantics from text
- Estimating relevance of content to research questions
- Generating embeddings for keywords

#### LLMService

Manages connections to various Large Language Models:
- Ollama Gemma 2 (local)
- GPT-3.5 Turbo (OpenAI)
- GPT-4 (OpenAI)
- Text Embedding model (OpenAI)

#### ArticleService

Handles file I/O operations:
- Reading articles from CSV files
- Writing articles to JSON files
- Writing results to CSV files

## Usage

### Console Mode

```
dotnet run --project llassist.AppConsole <input_csv_file> <research_questions_file>
```

Where:
- `<input_csv_file>` is the path to the CSV file containing the articles
- `<research_questions_file>` is the path to a text file containing the research questions (one per line)

### Web Application

Run docker compose in the root directory
```
docker-compose up -d
```

Run DB migrations in ApiService dir
```
dotnet ef database update
```


## Output

The program generates two output files:
1. A JSON file (`<input_filename>-result.json`) containing all processed articles with their semantics and relevance scores
2. A CSV file (`<input_filename>-result.csv`) with the same information in a tabular format

## Dependencies

- Microsoft.SemanticKernel
- CsvHelper
- Microsoft.Extensions.Logging

## Notes

- The program uses a local Ollama instance for the Gemma 2 model. Ensure it's running on `http://localhost:11434` before executing the program.
- OpenAI API key is required for GPT models and embeddings. Set it in the `LLMService` constructor.

## Disclaimer

This tool is for research purposes. Ensure you have the necessary rights and permissions to process and analyze the articles.

## Licensing
1. This project is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
2. This project is a fork of llassist, which was originally MIT licensed. The original MIT-licensed code has been incorporated into this project. As per the terms of the MIT license, we have included the original MIT license text and copyright notice in our NOTICE file.
3. All modifications and additions to the original code, as well as the project as a whole, are licensed under AGPL-3.0.
4. For full license text and attribution details, please see the LICENSE and NOTICE files.
