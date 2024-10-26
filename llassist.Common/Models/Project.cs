namespace llassist.Common.Models;

public class Project : IEntity<Ulid>
{
    public Ulid Id { get; set; } = Ulid.NewUlid();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<Article> Articles { get; set; } = [];
    public ICollection<ProjectDefinition> ProjectDefinitions { get; set; } = [];
    public ICollection<ResearchQuestion> ResearchQuestions { get; set; } = [];
}
