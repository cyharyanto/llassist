using llassist.Models;
using llassist.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace llassist;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2) // Adjusted to expect 2 arguments
        {
            Console.WriteLine("Usage: dotnet run <input_csv_file> <research_questions_file>");
            return;
        }

        string inputFile = args[0];
        string researchQuestionsFile = args[1]; // The second argument is the path to the research questions file

        // Read research questions from the file
        var researchQuestions = File.ReadAllLines(researchQuestionsFile);

        string baseOutputFile = Path.GetFileNameWithoutExtension(inputFile);
        string jsonOutputFile = $"{baseOutputFile}-result.json";
        string csvOutputFile = $"{baseOutputFile}-result.csv";

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        ILogger<NLPService> logger = loggerFactory.CreateLogger<NLPService>();
        var llmService = new LLMService("nothing_to_see_here");
        var nlpService = new NLPService(llmService, logger);

        var articles = ArticleService.ReadArticlesFromCsv(inputFile);
        var csvWriter = ArticleService.BeginCsvWriting(csvOutputFile, researchQuestions);

        for (int i = 0; i < articles.Count; i++)
        {
            var article = articles[i];
            double progressPercentage = (i / (double)articles.Count) * 100;
            Console.WriteLine($"Processing article {i + 1} out of {articles.Count} ({progressPercentage:F2}%) ...");
            Console.WriteLine($"Article: {article.Title}");

            var keySemantics = await nlpService.ExtractKeySemantics($"Title: {article.Title}\n Abstract: {article.Abstract}");
            article.KeySemantics = keySemantics;
            Console.WriteLine($"Topics: {string.Join(", ", keySemantics.Topics)}");
            Console.WriteLine($"Entities: {string.Join(", ", keySemantics.Entities)}");
            Console.WriteLine($"Keywords: {string.Join(", ", keySemantics.Keywords)}");

            bool mustRead = false;
            article.Relevances = new Relevance[researchQuestions.Length];
            for (int j = 0; j < researchQuestions.Length; j++)
            {
                var researchQuestion = researchQuestions[j];
                var relevance = await nlpService.EstimateRevelance($"Title: {article.Title}\n Abstract: {article.Abstract} \n Metadata: {JsonSerializer.Serialize(keySemantics)}", "abstract", researchQuestion);
                article.Relevances[j] = relevance;
                Console.WriteLine($"RQ-{j+1} -- IR:{relevance.IsRelevant} RS:{relevance.RelevanceScore} IC:{relevance.IsContributing} CS:{relevance.ContributionScore}");
                Console.WriteLine($"\t>> RR: {relevance.RelevanceReason.Substring(0, Math.Min(120, relevance.RelevanceReason.Length))}...");
                Console.WriteLine($"\t>> CR: {relevance.ContributionReason.Substring(0, Math.Min(120, relevance.ContributionReason.Length))}...");
                mustRead = mustRead || relevance.IsRelevant || relevance.IsContributing;
            }
            article.MustRead = mustRead;
            Console.WriteLine($"Decision: {(mustRead ? "Must Read" : "Skip")}\n");

            // Incremental CSV writing
            ArticleService.WriteArticleToCsv(csvWriter, article);
        }

        // Finalize CSV writing
        ArticleService.EndCsvWriting(csvWriter);

        // JSON output
        ArticleService.WriteArticlesToJson(articles, jsonOutputFile);

        Console.WriteLine($"Successfully extracted {articles.Count} articles to {jsonOutputFile} and {csvOutputFile}");
        Console.WriteLine($"Processed {researchQuestions.Length} research questions.");
    }
}
