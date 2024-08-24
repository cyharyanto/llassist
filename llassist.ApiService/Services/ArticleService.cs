using System.Text.Json;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using llassist.Common.Models;

namespace llassist.ApiService.Services;

public class ArticleService
{
    public static IList<Article> ReadArticlesFromCsv(StreamReader reader)
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
            };
            articles.Add(article);
        }

        return articles;
    }

    public static void WriteArticlesToJson(List<Article> articles, string filePath)
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
        csv.WriteField(string.Join("; ", article.KeySemantics.Topics));
        csv.WriteField(string.Join("; ", article.KeySemantics.Entities));
        csv.WriteField(string.Join("; ", article.KeySemantics.Keywords));
        csv.WriteField(article.MustRead);
        for (int i = 0; i < article.Relevances.Length; i++)
        {
            csv.WriteField(article.Relevances[i].Question);
            csv.WriteField(article.Relevances[i].IsRelevant);
            csv.WriteField(article.Relevances[i].IsContributing);
            csv.WriteField(article.Relevances[i].RelevanceScore);
            csv.WriteField(article.Relevances[i].ContributionScore);
            csv.WriteField(article.Relevances[i].RelevanceReason);
            csv.WriteField(article.Relevances[i].ContributionReason);
        }
        csv.NextRecord();
        writer.Flush();
    }

    public static void EndCsvWriting(StreamWriter writer)
    {
        writer.Close();
    }
}
