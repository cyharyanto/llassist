using System.Text.Json;

using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;

using llassist.Common.Models;

namespace llassist.ApiService.Services;

public class NLPService
{
    private readonly LLMService _llmService;
    private readonly ILogger<NLPService> _logger;

    public NLPService(LLMService llmService, ILogger<NLPService> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    // Common method to interact with LLM Service
    private async Task<string> InvokeLLMServiceAsync(Kernel kernel, string skPrompt, OpenAIPromptExecutionSettings executionSettings, KernelArguments parameters)
    {
        _logger.LogDebug($"Sending prompt to LLM:\n{skPrompt}");
        var function = kernel.CreateFunctionFromPrompt(skPrompt, executionSettings);
        var response = await kernel.InvokeAsync(function, parameters);
        _logger.LogDebug($"Response from LLM:\n{response}");
        return (response?.GetValue<string>() ?? string.Empty).Trim();
    }

    private string GetJsonTemplate<T>() where T : new()
    {
        return JsonSerializer.Serialize<T>(new T());
    }

    // Common method to handle the response
    private async Task<T> HandleResponse<T>(string response) where T : new()
    {
        try
        {
            return JsonSerializer.Deserialize<T>(response) ?? new T();
        }
        catch (JsonException ex)
        {
            _logger.LogError("Failed to parse JSON response: {Message}. Attempting to fix.", ex.Message);
            // Attempt to fix the JSON format using LLM
            var kernel = _llmService.OllamaGemma2ChatCompletion();
            var fixPrompt = """"
Fix the JSON so it can be deserialized into the target object.

Input JSON object:
{{$input}}

Return the fixed JSON in the following format:
{{$format}}
"""";

#pragma warning disable SKEXP0010
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 4096,
                Temperature = 0,
                TopP = 1,
                ResponseFormat = "json_object",
            };
#pragma warning restore SKEXP0010

            response = await InvokeLLMServiceAsync(kernel, fixPrompt, executionSettings,
                new() { ["input"] = response, ["format"] = GetJsonTemplate<T>() });
        }

        _logger.LogError("Failed to fix JSON response.");
        return new T();
    }

    public async Task<KeySemantics> ExtractKeySemantics(string content)
    {
        var kernel = _llmService.OllamaGemma2ChatCompletion();

        var skPrompt = """"
Semantically analyze the content to extract its topics, entities, and keywords.

Content:
{{$content}}

Return the result in the following JSON format:
{{$format}}
"""";

#pragma warning disable SKEXP0010
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 4096,
            Temperature = 0,
            TopP = 1,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            ResponseFormat = "json_object",
        };
#pragma warning restore SKEXP0010

        var response = await InvokeLLMServiceAsync(kernel, skPrompt, executionSettings,
            new() { ["content"] = content, ["format"] = GetJsonTemplate<KeySemantics>() });
        return await HandleResponse<KeySemantics>(response);
    }

    public async Task<Relevance> EstimateRevelance(string content, string contentType, string question, string[] definitions)
    {
        var kernel = _llmService.OllamaGemma2ChatCompletion();

        var skPrompt = """"
Analyze the following {{$contentType}}:
1. Estimate its relevance to the given question.
2. Assess whether it contributes to the question.
- Provide a relevance and contribution score between 0 and 1.
- It is relevant or contributing only if the score is above 0.7.
- Provide a reason for the relevance and contribution score.

Content:
{{$content}}

Question:
{{$question}}

Definitions:
{{$definitions}}

Return the result in the following JSON format:
{{$format}}
"""";

#pragma warning disable SKEXP0010
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 4096,
            Temperature = 0,
            TopP = 1,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            ResponseFormat = "json_object",
        };
#pragma warning restore SKEXP0010

        var response = await InvokeLLMServiceAsync(kernel, skPrompt, executionSettings,
            new()
            {
                ["content"] = content,
                ["contentType"] = contentType,
                ["question"] = question,
                ["definitions"] = string.Join("\n", definitions),
                ["format"] = GetJsonTemplate<Relevance>()
            });
        return await HandleResponse<Relevance>(response);
    }

    public async Task<Dictionary<string, float[]>> GenerateEmbeddings(string[] keywords, int dimensions)
    {
        var embeddingsService = _llmService.TextEmbedding3Small(dimensions);
        var embeddingsDictionary = new Dictionary<string, float[]>();

        try
        {
            var embeddingsList = await embeddingsService.GenerateEmbeddingsAsync(keywords);
            for (int i = 0; i < keywords.Length; i++)
            {
                var embedding = embeddingsList[i];
                var embeddingArray = new float[embedding.Length];
                embedding.Span.CopyTo(embeddingArray);
                embeddingsDictionary[keywords[i]] = embeddingArray;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to generate embeddings: {ex.Message}");
        }

        return embeddingsDictionary;
    }
}
