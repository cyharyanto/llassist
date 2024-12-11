using DotNetWorkQueue;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using llassist.Common;
using llassist.Common.Models;
using llassist.ApiService.Repositories;
using llassist.ApiService.Repositories.Specifications;

namespace llassist.ApiService.Services.Document;

public class AnthropicDocumentProcessor : IDocumentProcessingService
{
    private readonly IProducerQueue<DocumentProcessingTask> _producerQueue;
    private readonly ICRUDRepository<Ulid, DocumentProcessingJob, BaseSearchSpec> _jobRepository;
    private readonly ILogger<AnthropicDocumentProcessor> _logger;
    private readonly HttpClient _httpClient;

    public AnthropicDocumentProcessor(
        IProducerQueue<DocumentProcessingTask> producerQueue,
        ICRUDRepository<Ulid, DocumentProcessingJob, BaseSearchSpec> jobRepository,
        ILogger<AnthropicDocumentProcessor> logger)
    {
        _producerQueue = producerQueue;
        _jobRepository = jobRepository;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.DefaultRequestHeaders.Add("anthropic-beta", "pdfs-2024-09-25,prompt-caching-2024-07-31");
    }

    public async Task<DocumentProcessingJob> CreateProcessingJobAsync(Stream documentStream, string fileName, string contentType)
    {
        var base64Content = await ConvertToBase64(documentStream);
        
        var job = new DocumentProcessingJob
        {
            Id = Ulid.NewUlid(),
            FileName = fileName,
            ContentType = contentType,
            Base64Content = base64Content,
            TotalTasks = 3 // METADATA, SUMMARY, CHUNKING
        };

        await _jobRepository.CreateAsync(job);

        // Create and queue initial tasks
        var tasks = new[]
        {
            new DocumentProcessingTask { Id = Ulid.NewUlid(), JobId = job.Id, Type = DocumentTaskType.METADATA },
            new DocumentProcessingTask { Id = Ulid.NewUlid(), JobId = job.Id, Type = DocumentTaskType.SUMMARY },
            new DocumentProcessingTask { Id = Ulid.NewUlid(), JobId = job.Id, Type = DocumentTaskType.CHUNKING }
        };

        foreach (var task in tasks)
        {
            await _producerQueue.SendAsync(task);
        }

        return job;
    }

    public async Task ExecuteTaskAsync(DocumentProcessingTask task)
    {
        var job = await _jobRepository.ReadAsync(task.JobId);
        if (job == null) return;

        try
        {
            switch (task.Type)
            {
                case DocumentTaskType.METADATA:
                    await ExecuteMetadataTask(job);
                    break;
                case DocumentTaskType.SUMMARY:
                    await ExecuteSummaryTask(job);
                    break;
                case DocumentTaskType.CHUNKING:
                    await ExecuteChunkingTask(job);
                    break;
                case DocumentTaskType.FINALIZATION:
                    await ExecuteFinalizationTask(job);
                    break;
            }

            job.CompletedTasks++;
            await _jobRepository.UpdateAsync(job);
            
            await CheckAndCreateFinalizationTask(job);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process task {taskId} for job {jobId}", task.Id, job.Id);
            job.Status = ProcessingStatus.Failed;
            await _jobRepository.UpdateAsync(job);
        }
    }

    private async Task CheckAndCreateFinalizationTask(DocumentProcessingJob job)
    {
        if (job.CompletedTasks == job.TotalTasks)
        {
            await _producerQueue.SendAsync(new DocumentProcessingTask 
            { 
                Id = Ulid.NewUlid(),
                JobId = job.Id,
                Type = DocumentTaskType.FINALIZATION
            });
        }
    }

    private async Task ExecuteMetadataTask(DocumentProcessingJob job)
    {
        var response = await SendAnthropicRequest(CreateMetadataPrompt(job));
        var metadata = await ParseMetadataResponse(response);
        job.Parameters["metadata"] = JsonSerializer.Serialize(metadata);
    }

    private async Task ExecuteSummaryTask(DocumentProcessingJob job)
    {
        var response = await SendAnthropicRequest(CreateSummaryPrompt(job));
        var summary = await ParseSummaryResponse(response);
        job.Parameters["summary"] = JsonSerializer.Serialize(summary);
    }

    private async Task ExecuteChunkingTask(DocumentProcessingJob job)
    {
        var response = await SendAnthropicRequest(CreateChunkingPrompt(job));
        var chunks = await ParseChunksResponse(response);
        job.Parameters["chunks"] = JsonSerializer.Serialize(chunks);
    }

    private async Task ExecuteFinalizationTask(DocumentProcessingJob job)
    {
        _logger.LogInformation("Executing finalization for job {jobId}", job.Id);
        job.Status = ProcessingStatus.Completed;
        await _jobRepository.UpdateAsync(job);
    }

    private async Task<string> SendAnthropicRequest(object[] messages)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = "claude-3-5-sonnet-20241022",
                max_tokens = 4096,
                messages = messages
            }), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Anthropic API error: {content}");
        }

        return content;
    }

    private static async Task<string> ConvertToBase64(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    private async Task<MetadataResult> ParseMetadataResponse(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var metadata = doc.Element("metadata");
            
            if (metadata == null)
                throw new InvalidDataException("Invalid metadata XML structure");

            return new MetadataResult
            {
                Title = metadata.Element("title")?.Value ?? throw new InvalidDataException("Missing title"),
                Authors = metadata.Element("authors")?
                    .Elements("author")
                    .Select(a => a.Value)
                    .ToArray() ?? Array.Empty<string>(),
                Year = metadata.Element("year") != null ? 
                    int.Parse(metadata.Element("year")!.Value) : null,
                Abstract = metadata.Element("abstract")?.Value ?? string.Empty,
                Keywords = metadata.Element("keywords")?
                    .Elements("keyword")
                    .Select(k => k.Value)
                    .ToArray() ?? Array.Empty<string>(),
                DOI = metadata.Element("doi")?.Value
            };
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Failed to parse metadata XML");
            throw new InvalidDataException("Invalid metadata XML", ex);
        }
    }

    private async Task<SummaryResult> ParseSummaryResponse(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var summary = doc.Element("summary");
            
            if (summary == null)
                throw new InvalidDataException("Invalid summary XML structure");

            return new SummaryResult
            {
                Overview = summary.Element("overview")?.Value ?? 
                    throw new InvalidDataException("Missing overview"),
                KeyPoints = summary.Element("key_points")?
                    .Elements("point")
                    .Select(p => p.Value)
                    .ToArray() ?? Array.Empty<string>(),
                DetailedSummary = summary.Element("detailed_summary")?.Value ?? 
                    throw new InvalidDataException("Missing detailed summary")
            };
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Failed to parse summary XML");
            throw new InvalidDataException("Invalid summary XML", ex);
        }
    }

    private async Task<List<PageChunk>> ParseChunksResponse(string xmlResponse)
    {
        try
        {
            var doc = XDocument.Parse(xmlResponse);
            var chunks = doc.Element("document_chunks");
            
            if (chunks == null)
                throw new InvalidDataException("Invalid chunks XML structure");

            return chunks.Elements("page")
                .Select(page => new PageChunk
                {
                    PageNumber = int.Parse(page.Attribute("number")?.Value ?? "0"),
                    ContentSummary = page.Element("content_summary")?.Value ?? string.Empty,
                    Figures = page.Element("figures")?
                        .Elements("figure")
                        .Select(f => f.Value)
                        .ToArray() ?? Array.Empty<string>(),
                    Tables = page.Element("tables")?
                        .Elements("table")
                        .Select(t => t.Value)
                        .ToArray() ?? Array.Empty<string>(),
                    Citations = page.Element("citations")?
                        .Elements("citation")
                        .Select(c => c.Value)
                        .ToArray() ?? Array.Empty<string>(),
                    PreviousPageContext = page.Element("context")?.Element("previous_page")?.Value,
                    NextPageContext = page.Element("context")?.Element("next_page")?.Value
                })
                .ToList();
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Failed to parse chunks XML");
            throw new InvalidDataException("Invalid chunks XML", ex);
        }
    }

    private object[] CreateMetadataPrompt(DocumentProcessingJob job)
    {
        return new object[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "document",
                        source = new { type = "base64", media_type = job.ContentType, data = job.Base64Content },
                        cache_control = new { type = "ephemeral" }
                    },
                    new
                    {
                        type = "text",
                        text = """
                            Extract metadata from this document and return as XML. Include all available information:
                            <metadata>
                                <title>Full document title</title>
                                <authors>
                                    <author>Author full name</author>
                                    <!-- Repeat for each author -->
                                </authors>
                                <year>Publication year (YYYY)</year>
                                <abstract>Full abstract or executive summary</abstract>
                                <keywords>
                                    <keyword>Main topic or keyword</keyword>
                                    <!-- Repeat for each keyword -->
                                </keywords>
                                <doi>DOI if available</doi>
                            </metadata>
                            
                            Important:
                            1. Include all author names found
                            2. Extract actual publication year, not submission/revision dates
                            3. Keep the abstract complete and unmodified
                            4. Extract 5-10 relevant keywords
                            """
                    }
                }
            }
        };
    }

    private object[] CreateSummaryPrompt(DocumentProcessingJob job)
    {
        return new object[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "document",
                        source = new { type = "base64", media_type = job.ContentType, data = job.Base64Content },
                        cache_control = new { type = "ephemeral" }
                    },
                    new
                    {
                        type = "text",
                        text = """
                            Provide a comprehensive summary of this document as XML:
                            <summary>
                                <overview>2-3 sentence high-level overview</overview>
                                <key_points>
                                    <point>Key finding or argument</point>
                                    <!-- Include 5-7 main points -->
                                </key_points>
                                <detailed_summary>
                                    Detailed 500-word summary covering:
                                    - Main arguments and findings
                                    - Methodology if applicable
                                    - Key conclusions
                                    - Significant implications
                                </detailed_summary>
                            </summary>
                            
                            Important:
                            1. Be objective and accurate
                            2. Maintain academic/technical language level
                            3. Include quantitative results when available
                            4. Preserve original terminology
                            """
                    }
                }
            }
        };
    }

    private object[] CreateChunkingPrompt(DocumentProcessingJob job)
    {
        return new object[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "document",
                        source = new { type = "base64", media_type = job.ContentType, data = job.Base64Content },
                        cache_control = new { type = "ephemeral" }
                    },
                    new
                    {
                        type = "text",
                        text = """
                            Analyze each page and provide structured content as XML:
                            <document_chunks>
                                <page number='1'>
                                    <content_summary>
                                        Detailed summary of the page content
                                    </content_summary>
                                    <figures>
                                        <figure>Description and interpretation of figure</figure>
                                        <!-- Repeat for each figure -->
                                    </figures>
                                    <tables>
                                        <table>Description and key findings from table</table>
                                        <!-- Repeat for each table -->
                                    </tables>
                                    <citations>
                                        <citation>Full citation text</citation>
                                        <!-- Repeat for each citation -->
                                    </citations>
                                    <context>
                                        <previous_page>Relevant context from previous page</previous_page>
                                        <next_page>Relevant context for next page</next_page>
                                    </context>
                                </page>
                                <!-- Repeat for each page -->
                            </document_chunks>
                            
                            Important:
                            1. Maintain page number sequence
                            2. Include all figures and tables with their interpretations
                            3. Capture citations and references
                            4. Provide context between pages for coherence
                            """
                    }
                }
            }
        };
    }
} 