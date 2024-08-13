# LLAssist

LLAssist is a tool for processing and analyzing research articles using Natural Language Processing (NLP) techniques and Large Language Models (LLMs). Paper: https://doi.org/10.48550/arXiv.2407.13993

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
```
dotnet run <input_csv_file> <research_questions_file>
```

Where:
- `<input_csv_file>` is the path to the CSV file containing the articles
- `<research_questions_file>` is the path to a text file containing the research questions (one per line)

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
