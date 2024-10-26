using llassist.ApiService.Repositories;
using llassist.ApiService.Repositories.Specifications;
using llassist.Common;
using llassist.Common.Models;
using Microsoft.EntityFrameworkCore;
using Assert = Xunit.Assert;

namespace llassist.Tests;

public class ProjectRepositoryTests(DatabaseFixture fixture) : BaseRepositoryTests<Ulid, Project, BaseSearchSpec>(fixture)
{
    protected override ICRUDRepository<Ulid, Project, BaseSearchSpec> CreateRepository(ApplicationDbContext context)
    {
        return new ProjectRepository(context);
    }

    protected override Project CreateTestEntity(Project project)
    {
        var newProjectId = Ulid.NewUlid();
        return new Project
        {
            Id = newProjectId,
            Name = "Sample Project",
            Description = "This is a sample project.",
            Articles = CreateArticles(newProjectId),
            ProjectDefinitions =
            [
                new ProjectDefinition
                {
                    Id = Ulid.NewUlid(),
                    Definition = "Sample Definition",
                    ProjectId = newProjectId,
                },
            ],
            ResearchQuestions = CreateResearchQuestions(newProjectId),
        };
    }

    private static List<Article> CreateArticles(Ulid projectId)
    {
        var articleId = Ulid.NewUlid();
        return
        [
            new Article
            {
                Id = articleId,
                Authors = "John Doe",
                Year = 2022,
                Title = "Sample Article",
                DOI = "doi:10.1234/sample",
                Link = "https://sample.com",
                Abstract = "This is a sample article.",
                ProjectId = projectId,
                MustRead = true,
                ArticleKeySemantics =
                [
                    new() { ArticleId = articleId, KeySemanticIndex = 0, Type = ArticleKeySemantic.TypeTopic, Value = "Sample Topic 2" },
                    new() { ArticleId = articleId, KeySemanticIndex = 1, Type = ArticleKeySemantic.TypeEntity, Value = "Sample Entity 2" },
                    new() { ArticleId = articleId, KeySemanticIndex = 2, Type = ArticleKeySemantic.TypeEntity, Value = "Another Entity 2" },
                    new() { ArticleId = articleId, KeySemanticIndex = 3, Type = ArticleKeySemantic.TypeKeyword, Value = "Sample Keyword 2" },
                ],
                ArticleRelevances =
                [
                    new ArticleRelevance
                    {
                        ArticleId = articleId,
                        EstimateRelevanceJobId = Ulid.NewUlid(),
                        Question = "Sample Question",
                        RelevanceScore = 0.2,
                        ContributionScore = 0.3,
                        IsRelevant = true,
                        IsContributing = true,
                        RelevanceReason = "Sample Reason",
                        ContributionReason = "Sample Reason",
                        CreatedAt = DateTimeOffset.UtcNow,
                    }
                ],
            }
        ];
    }

    private static List<ResearchQuestion> CreateResearchQuestions(Ulid projectId)
    {
        var researchQuestionId = Ulid.NewUlid();
        return
        [
            new ResearchQuestion
            {
                Id = researchQuestionId,
                QuestionText = "Sample Question",
                ProjectId = projectId,
                QuestionDefinitions = 
                [
                    new QuestionDefinition
                    {
                        Id = Ulid.NewUlid(),
                        Definition = "Sample Definition 2",
                        ResearchQuestionId = researchQuestionId,
                    }
                ],
            },
        ];
    }

    protected override DbSet<Project> GetDbSet(ApplicationDbContext context)
    {
        return context.Projects;
    }

    protected override async Task UpdateThenRead()
    {
        // Read for update
        var repository = CreateRepository();
        var updateEntity = await repository.ReadAsync(TestEntity.Id);
        Assert.NotNull(updateEntity);

        // Update
        updateEntity.Name = "Updated Project";
        updateEntity.Articles.Add(CreateArticles(updateEntity.Id).First());
        updateEntity.ProjectDefinitions.Add(new ProjectDefinition
        {
            Id = Ulid.NewUlid(),
            Definition = "Updated Definition",
            ProjectId = updateEntity.Id,
        });
        updateEntity.ResearchQuestions.First().QuestionDefinitions.Add(new QuestionDefinition
        {
            Id = Ulid.NewUlid(),
            Definition = "New Definition",
            ResearchQuestionId = updateEntity.ResearchQuestions.First().Id,
        });
        await repository.UpdateAsync(updateEntity);

        // Read
        var readEntity = await CreateRepository().ReadAsync(TestEntity.Id);
        Assert.NotNull(readEntity);

        LogEntity(readEntity);
        VerifyEntity(updateEntity, readEntity);
    }

    protected override void VerifyEntity(Project expected, Project actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);

        // Verify Articles
        Assert.Equal(expected.Articles.Count, actual.Articles.Count);
        var expectedArticles = expected.Articles.ToList();
        var resultArticles = actual.Articles.ToList();
        for (int i = 0; i < expected.Articles.Count; i++)
        {
            ArticleRepositoryTests.VerifyArticles(expectedArticles[i], resultArticles[i]);
        }

        // Verify ProjectDefinitions
        Assert.Equal(expected.ProjectDefinitions.Count, actual.ProjectDefinitions.Count);
        var expectedDefinitions = expected.ProjectDefinitions.ToList();
        var resultDefinitions = actual.ProjectDefinitions.ToList();
        for (int i = 0; i < expected.ProjectDefinitions.Count; i++)
        {
            Assert.Equal(expectedDefinitions[i].Id, resultDefinitions[i].Id);
            Assert.Equal(expectedDefinitions[i].Definition, resultDefinitions[i].Definition);
            Assert.Equal(expectedDefinitions[i].ProjectId, resultDefinitions[i].ProjectId);
        }

        // Verify ResearchQuestions
        Assert.Equal(expected.ResearchQuestions.Count, actual.ResearchQuestions.Count);
        var expectedQuestions = expected.ResearchQuestions.ToList();
        var resultQuestions = actual.ResearchQuestions.ToList();
        for (int i = 0; i < expected.ResearchQuestions.Count; i++)
        {
            Assert.Equal(expectedQuestions[i].Id, resultQuestions[i].Id);
            Assert.Equal(expectedQuestions[i].QuestionText, resultQuestions[i].QuestionText);
            Assert.Equal(expectedQuestions[i].ProjectId, resultQuestions[i].ProjectId);

            // Verify QuestionDefinitions
            Assert.Equal(expectedQuestions[i].QuestionDefinitions.Count, resultQuestions[i].QuestionDefinitions.Count);
            var expectedQuestionDefinitions = expectedQuestions[i].QuestionDefinitions.ToList();
            var resultQuestionDefinitions = resultQuestions[i].QuestionDefinitions.ToList();
            for (int j = 0; j < expectedQuestions[i].QuestionDefinitions.Count; j++)
            {
                Assert.Equal(expectedQuestionDefinitions[j].Id, resultQuestionDefinitions[j].Id);
                Assert.Equal(expectedQuestionDefinitions[j].Definition, resultQuestionDefinitions[j].Definition);
                Assert.Equal(expectedQuestionDefinitions[j].ResearchQuestionId, resultQuestionDefinitions[j].ResearchQuestionId);
            }
        }
    }
}
