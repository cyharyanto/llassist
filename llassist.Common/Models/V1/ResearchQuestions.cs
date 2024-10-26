namespace llassist.Common.Models.V1;

public class ResearchQuestions
{
    public IList<string> Definitions { get; set; } = [];
    public IList<Question> Questions { get; set; } = [];
}

public class Question
{
    public string Text { get; set; } = string.Empty;
    public IList<string> Definitions { get; set; } = [];
}
