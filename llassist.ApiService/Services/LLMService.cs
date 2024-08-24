using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;

namespace llassist.ApiService.Services;

public class LLMService
{
    private readonly string _openAIAPIKey;

    public LLMService(string openAIAPIKey)
    {
        _openAIAPIKey = openAIAPIKey;
    }

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
    public Kernel OllamaGemma2ChatCompletion()
    {
        return Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId: "gemma2", apiKey: "notrequired", endpoint: new Uri("http://localhost:11434")).Build();
    }

    public Kernel OllamaGemma227bChatCompletion()
    {
        return Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId: "gemma2:27b", apiKey: "notrequired", endpoint: new Uri("http://localhost:11434")).Build();
    }

    public Kernel OllamaLLaMA3ChatCompletion()
    {
        return Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId: "llama3", apiKey: "notrequired", endpoint: new Uri("http://localhost:11434")).Build();
    }

    public Kernel OllamaLLaMA31ChatCompletion()
    {
        return Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId: "llama3.1", apiKey: "notrequired", endpoint: new Uri("http://localhost:11434")).Build();
    }

    public Kernel GPT3_5TurboChatCompletion()
    {
        return Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId: "gpt-3.5-turbo", apiKey: _openAIAPIKey).Build();
    }

    public Kernel GPT4oChatCompletion()
    {
        return Kernel.CreateBuilder().AddOpenAIChatCompletion(modelId: "gpt-4o", apiKey: _openAIAPIKey).Build();
    }

    public ITextEmbeddingGenerationService TextEmbedding3Small(int dimensions)
    {
        return new OpenAITextEmbeddingGenerationService(modelId: "text-embedding-3-small", apiKey: _openAIAPIKey, dimensions: dimensions);
    }
}
