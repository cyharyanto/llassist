using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using llassist.Common.Models;
using llassist.Common.ViewModels;
using llassist.Common.Mappers;
using Assert = Xunit.Assert;

namespace llassist.Tests;


public class ModelMappersTests
{
    [Fact]
    public void ToArticleKeySemantics_ShouldMapCorrectly()
    {
        // Arrange
        var articleId = Ulid.NewUlid();
        var keySemantics = new KeySemantics
        {
            Topics = new[] { "Topic1", "Topic2" },
            Entities = new[] { "Entity1", "Entity2" },
            Keywords = new[] { "Keyword1", "Keyword2" }
        };

        // Act
        var result = ModelMappers.ToArticleKeySemantics(articleId, keySemantics);

        // Assert
        Assert.Equal(6, result.Count);
        Assert.Equal(2, result.Count(aks => aks.Type == ArticleKeySemantic.TypeTopic));
        Assert.Equal(2, result.Count(aks => aks.Type == ArticleKeySemantic.TypeEntity));
        Assert.Equal(2, result.Count(aks => aks.Type == ArticleKeySemantic.TypeKeyword));
        Assert.All(result, aks => Assert.Equal(articleId, aks.ArticleId));
    }

    [Fact]
    public void ToArticleRelevances_ShouldMapCorrectly()
    {
        // Arrange
        var articleId = Ulid.NewUlid();
        var jobId = Ulid.NewUlid();
        var relevances = new List<Relevance>
        {
            new Relevance { Question = "Q1", RelevanceScore = 0.5, ContributionScore = 0.3, IsRelevant = true, IsContributing = false, RelevanceReason = "R1", ContributionReason = "C1" },
            new Relevance { Question = "Q2", RelevanceScore = 0.7, ContributionScore = 0.6, IsRelevant = true, IsContributing = true, RelevanceReason = "R2", ContributionReason = "C2" }
        };

        // Act
        var result = ModelMappers.ToArticleRelevances(articleId, jobId, relevances);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, ar =>
        {
            Assert.Equal(articleId, ar.ArticleId);
            Assert.Equal(jobId, ar.EstimateRelevanceJobId);
        });
        Assert.Equal("Q1", result[0].Question);
        Assert.Equal("Q2", result[1].Question);
    }

    [Fact]
    public void ToProjectViewModel_ShouldMapCorrectly()
    {
        // Arrange
        var project = new Project
        {
            Id = Ulid.NewUlid(),
            Name = "Test Project",
            Description = "Test Description",
            Articles = new List<Article>
            {
                new Article { Title = "Article 1", Authors = "Author 1", Year = 2021, Abstract = "Abstract 1", MustRead = true },
                new Article { Title = "Article 2", Authors = "Author 2", Year = 2022, Abstract = "Abstract 2", MustRead = false }
            },
            ProjectDefinitions = new List<ProjectDefinition>
            {
                new ProjectDefinition { Definition = "Definition 1" },
                new ProjectDefinition { Definition = "Definition 2" }
            },
            ResearchQuestions = new List<ResearchQuestion>
            {
                new ResearchQuestion { QuestionText = "Question 1", QuestionDefinitions = new List<QuestionDefinition> { new QuestionDefinition { Definition = "Q1 Definition" } } },
                new ResearchQuestion { QuestionText = "Question 2", QuestionDefinitions = new List<QuestionDefinition> { new QuestionDefinition { Definition = "Q2 Definition" } } }
            }
        };

        // Act
        var result = ModelMappers.ToProjectViewModel(project);

        // Assert
        Assert.Equal(project.Id.ToString(), result.Id);
        Assert.Equal(project.Name, result.Name);
        Assert.Equal(project.Description, result.Description);
        Assert.Equal(2, result.Articles.Count);
        Assert.Equal(2, result.ResearchQuestions.Definitions.Count);
        Assert.Equal(2, result.ResearchQuestions.Questions.Count);
    }

    [Fact]
    public void ToArticleViewModel_ShouldMapCorrectly()
    {
        // Arrange
        var article = new Article
        {
            Title = "Test Article",
            Authors = "Test Author",
            Year = 2023,
            Abstract = "Test Abstract",
            MustRead = true,
            ArticleRelevances = new List<ArticleRelevance>
            {
                new ArticleRelevance { Question = "Q1", RelevanceScore = 0.5, IsRelevant = true },
                new ArticleRelevance { Question = "Q2", RelevanceScore = 0.7, IsRelevant = false }
            }
        };

        // Act
        var result = ModelMappers.ToArticleViewModel(article);

        // Assert
        Assert.Equal(article.Id.ToString(), result.Id);
        Assert.Equal(article.Title, result.Title);
        Assert.Equal(article.Authors, result.Authors);
        Assert.Equal(article.Year, result.Year);
        Assert.Equal(article.Abstract, result.Abstract);
        Assert.Equal(article.MustRead, result.MustRead);
        Assert.Equal(2, result.Relevances.Count);
    }

    [Fact]
    public void ToSnapshots_ShouldMapCorrectly()
    {
        // Arrange
        var jobId = Ulid.NewUlid();
        var projectDefinitions = new List<ProjectDefinition>
        {
            new ProjectDefinition { Id = Ulid.NewUlid(), Definition = "Definition 1" },
            new ProjectDefinition { Id = Ulid.NewUlid(), Definition = "Definition 2" }
        };
        var researchQuestions = new List<ResearchQuestion>
        {
            new ResearchQuestion
            {
                Id = Ulid.NewUlid(),
                QuestionText = "Question 1",
                QuestionDefinitions = new List<QuestionDefinition>
                {
                    new QuestionDefinition { Id = Ulid.NewUlid(), Definition = "Q1 Definition" }
                }
            },
            new ResearchQuestion
            {
                Id = Ulid.NewUlid(),
                QuestionText = "Question 2",
                QuestionDefinitions = new List<QuestionDefinition>
                {
                    new QuestionDefinition { Id = Ulid.NewUlid(), Definition = "Q2 Definition" }
                }
            }
        };

        // Act
        var result = ModelMappers.ToSnapshots(jobId, projectDefinitions, researchQuestions);

        // Assert
        Assert.Equal(6, result.Count);
        Assert.Equal(2, result.Count(s => s.EntityType == Snapshot.EntityTypeProjectDefinition));
        Assert.Equal(2, result.Count(s => s.EntityType == Snapshot.EntityTypeResearchQuestion));
        Assert.Equal(2, result.Count(s => s.EntityType == Snapshot.EntityTypeQuestionDefinition));
        Assert.All(result, s => Assert.Equal(jobId, s.EstimateRelevanceJobId));
    }

    [Fact]
    public void ToResearchQuestionDTOList_ShouldMapCorrectly()
    {
        // Arrange
        var projectDefinitions = new List<ProjectDefinition>
        {
            new ProjectDefinition { Definition = "Project Definition 1" },
            new ProjectDefinition { Definition = "Project Definition 2" }
        };
        var researchQuestions = new List<ResearchQuestion>
        {
            new ResearchQuestion
            {
                QuestionText = "Question 1",
                QuestionDefinitions = new List<QuestionDefinition>
                {
                    new QuestionDefinition { Definition = "Q1 Definition" }
                }
            },
            new ResearchQuestion
            {
                QuestionText = "Question 2",
                QuestionDefinitions = new List<QuestionDefinition>
                {
                    new QuestionDefinition { Definition = "Q2 Definition" }
                }
            }
        };

        // Act
        var result = ModelMappers.ToResearchQuestionDTOList(projectDefinitions, researchQuestions);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Question 1", result[0].Question);
        Assert.Equal("Question 2", result[1].Question);
        Assert.Equal(3, result[0].CombinedDefinitions.Count);
        Assert.Equal(3, result[1].CombinedDefinitions.Count);
    }
}
