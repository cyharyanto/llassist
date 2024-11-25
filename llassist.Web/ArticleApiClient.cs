public class ArticleApiClient
{
    private readonly HttpClient _httpClient;

    public ArticleApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task DeleteArticleAsync(string projectId, string articleId)
    {
        var response = await _httpClient.DeleteAsync($"api/project/{projectId}/article/{articleId}");
        response.EnsureSuccessStatusCode();
    }
}