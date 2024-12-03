using System.Text.Json;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using llassist.Common.Models;
using llassist.Common.Mappers;
using llassist.Common.ViewModels;
using System.Globalization;
using llassist.ApiService.Repositories.Specifications;
using llassist.Common;

namespace llassist.ApiService.Services;

public class ArticleService
{
    private readonly ICRUDRepository<Ulid, Article, ArticleSearchSpec> _articleRepository;

    public ArticleService(ICRUDRepository<Ulid, Article, ArticleSearchSpec> articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public static IList<Article> ReadArticlesFromCsv(TextReader reader, Ulid projectId)
    {
        var csvConfig = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null
        };

        using var csv = new CsvReader(reader, csvConfig);

        var articles = new List<Article>();
        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var article = new Article
            {
                Authors = csv.GetField("Authors") ?? string.Empty,
                Year = int.TryParse(csv.GetField("Year") ?? csv.GetField("Publication Year"), out int year) ? year : 0,
                Title = csv.GetField("Title") ?? csv.GetField("Document Title") ?? string.Empty,
                DOI = csv.GetField("DOI") ?? string.Empty,
                Link = csv.GetField("Link") ?? csv.GetField("PDF Link") ?? string.Empty,
                Abstract = csv.GetField("Abstract") ?? string.Empty,
                ProjectId = projectId
            };
            articles.Add(article);
        }

        return articles;
    }

    public static void WriteArticlesToJson(IList<Article> articles, string filePath)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string jsonString = JsonSerializer.Serialize(articles, options);
        File.WriteAllText(filePath, jsonString);
    }

    public static StreamWriter BeginCsvWriting(string filePath, string[] RQs)
    {
        var csvConfig = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = ",", // Set the delimiter to a comma
            Escape = '"', // Set the escape character to a double quote
            Encoding = Encoding.UTF8, // Set the encoding to UTF-8
            HasHeaderRecord = true, // Indicate that the CSV file has a header record
        };

        var writer = new StreamWriter(filePath);
        var csv = new CsvWriter(writer, csvConfig);

        // Write the header
        csv.WriteField("Authors");
        csv.WriteField("Year");
        csv.WriteField("Title");
        csv.WriteField("DOI");
        csv.WriteField("Link");
        csv.WriteField("Abstract");
        csv.WriteField("Topics");
        csv.WriteField("Entities");
        csv.WriteField("Keywords");
        csv.WriteField("MustRead");
        for (int i = 0; i < RQs.Length; i++)
        {
            csv.WriteField($"RQ{i + 1}_Q");
            csv.WriteField($"RQ{i + 1}_RI");
            csv.WriteField($"RQ{i + 1}_CI");
            csv.WriteField($"RQ{i + 1}_RS");
            csv.WriteField($"RQ{i + 1}_CS");
            csv.WriteField($"RQ{i + 1}_RR");
            csv.WriteField($"RQ{i + 1}_CR");
        }
        csv.NextRecord();

        return writer; // Return the StreamWriter for further writing
    }

    public static void WriteArticleToCsv(StreamWriter writer, Article article)
    {
        var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture));
        csv.WriteField(article.Authors);
        csv.WriteField(article.Year);
        csv.WriteField(article.Title);
        csv.WriteField(article.DOI);
        csv.WriteField(article.Link);
        csv.WriteField(article.Abstract);
        csv.WriteField(string.Join("; ", article.ArticleKeySemantics.Where(t => t.Type == ArticleKeySemantic.TypeTopic)));
        csv.WriteField(string.Join("; ", article.ArticleKeySemantics.Where(t => t.Type == ArticleKeySemantic.TypeEntity)));
        csv.WriteField(string.Join("; ", article.ArticleKeySemantics.Where(t => t.Type == ArticleKeySemantic.TypeKeyword)));
        csv.WriteField(article.MustRead);

        foreach (var relevance in article.ArticleRelevances)
        {
            csv.WriteField(relevance.Question);
            csv.WriteField(relevance.IsRelevant);
            csv.WriteField(relevance.IsContributing);
            csv.WriteField(relevance.RelevanceScore);
            csv.WriteField(relevance.ContributionScore);
            csv.WriteField(relevance.RelevanceReason);
            csv.WriteField(relevance.ContributionReason);
        }

        csv.NextRecord();
        writer.Flush();
    }

    public static void EndCsvWriting(StreamWriter writer)
    {
        writer.Close();
    }

    // ...

    public static byte[] WriteProcessToCsv(ProcessResultViewModel processedResults)
    {
        var csvConfig = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = ",", // Set the delimiter to a comma
            Escape = '"', // Set the escape character to a double quote
            Encoding = Encoding.UTF8, // Set the encoding to UTF-8
            HasHeaderRecord = true, // Indicate that the CSV file has a header record
        };
        using var memoryStream = new MemoryStream();

        using (var streamWriter = new StreamWriter(memoryStream))
        {
            using var csvWriter = new CsvWriter(streamWriter, csvConfig);

            // Write the header
            csvWriter.WriteField("Title");
            csvWriter.WriteField("Authors");
            csvWriter.WriteField("Year");
            csvWriter.WriteField("Abstract");
            csvWriter.WriteField("MustRead");
            csvWriter.WriteField("Question");
            csvWriter.WriteField("RelevanceScore");
            csvWriter.WriteField("IsRelevant");
            csvWriter.NextRecord();

            foreach (var article in processedResults.ProcessedArticles)
            {
                foreach (var relevance in article.Relevances)
                {
                    csvWriter.WriteField(article.Title);
                    csvWriter.WriteField(article.Authors);
                    csvWriter.WriteField(article.Year);
                    csvWriter.WriteField(article.Abstract);
                    csvWriter.WriteField(article.MustRead);
                    csvWriter.WriteField(relevance.Question);
                    csvWriter.WriteField(relevance.RelevanceScore);
                    csvWriter.WriteField(relevance.IsRelevant);
                    csvWriter.NextRecord();
                }
            }
        }

        return memoryStream.ToArray();
    }

    public async Task<bool> DeleteArticleAsync(Ulid articleId)
    {
        return await _articleRepository.DeleteAsync(articleId);
    }
}
