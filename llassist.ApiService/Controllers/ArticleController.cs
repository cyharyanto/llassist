using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using llassist.Common.Models;
using llassist.ApiService.Services;

namespace llassist.ApiService.Controllers;

/// <summary>
/// Controller for managing articles within projects
/// </summary>
[ApiController]
[Route("api/project/{projectId}/article")]
public class ArticleController : ControllerBase
{
    private readonly ArticleService _articleService;
    private readonly ILogger<ArticleController> _logger;

    public ArticleController(ArticleService articleService, ILogger<ArticleController> logger)
    {
        _articleService = articleService ?? throw new ArgumentNullException(nameof(articleService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Deletes an article from a project
    /// </summary>
    /// <param name="projectId">The ID of the project</param>
    /// <param name="articleId">The ID of the article to delete</param>
    /// <returns>NoContent if successful, NotFound if article doesn't exist</returns>
    [HttpDelete("{articleId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteArticle(string projectId, string articleId)
    {
        if (!Ulid.TryParse(projectId, out var parsedProjectId) || 
            !Ulid.TryParse(articleId, out var parsedArticleId))
        {
            return BadRequest("Invalid ID format");
        }

        _logger.LogInformation("Attempting to delete article {ArticleId} from project {ProjectId}", 
            articleId, projectId);

        var success = await _articleService.DeleteArticleAsync(parsedArticleId);
        
        if (!success)
        {
            _logger.LogWarning("Article {ArticleId} not found in project {ProjectId}", 
                articleId, projectId);
            return NotFound();
        }

        _logger.LogInformation("Successfully deleted article {ArticleId} from project {ProjectId}", 
            articleId, projectId);
        return NoContent();
    }
}