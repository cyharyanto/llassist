namespace llassist.Common.Models;

public class Project : IEntity<Ulid>
{
    public Ulid Id { get; set; } = Ulid.NewUlid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IList<Article> Articles { get; set; } = [];
    public ResearchQuestions ResearchQuestions { get; set; } = new ResearchQuestions();
}