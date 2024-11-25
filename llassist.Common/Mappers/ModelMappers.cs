using llassist.Common.Models;
using llassist.Common.ViewModels;
using System.Text.Json;

namespace llassist.Common.Mappers;

public class ModelMappers
{
    public static List<ArticleKeySemantic> ToArticleKeySemantics(Ulid articleId, KeySemantics keySemantics)
    {
        var keySemanticsList = new List<ArticleKeySemantic>();
        keySemanticsList.AddRange(ToArticleKeySemantic(articleId, keySemantics.Topics, ArticleKeySemantic.TypeTopic, keySemanticsList.Count));
        keySemanticsList.AddRange(ToArticleKeySemantic(articleId, keySemantics.Entities, ArticleKeySemantic.TypeEntity, keySemanticsList.Count));
        keySemanticsList.AddRange(ToArticleKeySemantic(articleId, keySemantics.Keywords, ArticleKeySemantic.TypeKeyword, keySemanticsList.Count));
        return keySemanticsList;
    }

    private static List<ArticleKeySemantic> ToArticleKeySemantic(Ulid articleId, string[] values, string type, int indexOffset)
    {
        return values.Select((entity, index) => new ArticleKeySemantic
        {
            ArticleId = articleId,
            KeySemanticIndex = indexOffset + index,
            Type = type,
            Value = entity,
        }).ToList();
    }

    public static List<ArticleRelevance> ToArticleRelevances(Ulid articleId, Ulid jobId, IList<Relevance> relevances)
    {
        return relevances.Select((relevance, index) => new ArticleRelevance
        {
            ArticleId = articleId,
            EstimateRelevanceJobId = jobId,
            RelevanceIndex = index,
            Question = relevance.Question,
            RelevanceScore = relevance.RelevanceScore,
            ContributionScore = relevance.ContributionScore,
            IsRelevant = relevance.IsRelevant,
            IsContributing = relevance.IsContributing,
            RelevanceReason = relevance.RelevanceReason,
            ContributionReason = relevance.ContributionReason
        }).ToList();
    }

    public static ProjectViewModel ToProjectViewModel(Project project)
    {
        var projectViewModel = new ProjectViewModel
        {
            Id = project.Id.ToString(),
            Name = project.Name,
            Description = project.Description,
            Articles = ToArticleViewModels(project.Articles),
            ResearchQuestions = ToResearchQuestionsViewModel(project.ProjectDefinitions, project.ResearchQuestions)
        };

        return projectViewModel;
    }

    private static List<ArticleViewModel> ToArticleViewModels(ICollection<Article> articles)
    {
        return articles.Select(article => ToArticleViewModel(article)).ToList();
    }

    public static ArticleViewModel ToArticleViewModel(Article article)
    {
        return ToArticleViewModel(article, article.ArticleRelevances);
    }

    public static ArticleViewModel ToArticleViewModel(Article article, ICollection<ArticleRelevance> articleRelevances)
    {
        return new ArticleViewModel
        {
            Id = article.Id.ToString(),
            Title = article.Title,
            Authors = article.Authors,
            Year = article.Year,
            Abstract = article.Abstract,
            MustRead = article.MustRead,
            Relevances = ToRelevanceViewModels(articleRelevances)
        };
    }

    private static List<RelevanceViewModel> ToRelevanceViewModels(ICollection<ArticleRelevance> relevances)
    {
        return relevances.Select(relevance => new RelevanceViewModel
        {
            Question = relevance.Question,
            RelevanceScore = relevance.RelevanceScore,
            IsRelevant = relevance.IsRelevant,
        }).ToList();
    }

    public static ResearchQuestionsViewModel ToResearchQuestionsViewModel(
        ICollection<ProjectDefinition> projectDefinitions, 
        ICollection<ResearchQuestion> researchQuestions)
    {
        return new ResearchQuestionsViewModel
        {
            Definitions = projectDefinitions.Select(definition => definition.Definition).ToList(),
            Questions = researchQuestions.Select(question => new QuestionViewModel
            {
                Text = question.QuestionText,
                Definitions = question.QuestionDefinitions.Select(definition => definition.Definition).ToList()
            }).ToList()
        };
    }

    public static ICollection<Snapshot> ToSnapshots(
        Ulid EstimateRelevanceJobId,
        ICollection<ProjectDefinition> projectDefinitions, 
        ICollection<ResearchQuestion> researchQuestions)
    {
        var snapshots = projectDefinitions.Select(definition => new Snapshot
        {
            EntityType = Snapshot.EntityTypeProjectDefinition,
            EntityId = definition.Id,
            SerializedEntity = JsonSerializer.Serialize(definition),
            EstimateRelevanceJobId = EstimateRelevanceJobId
        }).ToList();

        snapshots.AddRange(researchQuestions.Select(question => new Snapshot
        {
            EntityType = Snapshot.EntityTypeResearchQuestion,
            EntityId = question.Id,
            SerializedEntity = JsonSerializer.Serialize(question),
            EstimateRelevanceJobId = EstimateRelevanceJobId
        }));

        foreach (var question in researchQuestions)
        {
            snapshots.AddRange(question.QuestionDefinitions.Select(definition => new Snapshot
            {
                EntityType = Snapshot.EntityTypeQuestionDefinition,
                EntityId = definition.Id,
                SerializedEntity = JsonSerializer.Serialize(definition),
                EstimateRelevanceJobId = EstimateRelevanceJobId
            }));
        }

        return snapshots;
    }

    public static List<ResearchQuestionDTO> ToResearchQuestionDTOList(ICollection<ProjectDefinition> projectDefinitions, ICollection<ResearchQuestion> researchQuestions)
    {
        var researchQuestionList = new List<ResearchQuestionDTO>();
        foreach (var question in researchQuestions)
        {
            researchQuestionList.Add(new ResearchQuestionDTO
            {
                Question = question.QuestionText,
                CombinedDefinitions = projectDefinitions.Select(d => d.Definition).Concat(
                    question.QuestionDefinitions.Select(d => d.Definition)).ToArray(),
            });
        }
        return researchQuestionList;
    }
}
