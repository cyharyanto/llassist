namespace llassist.Models;

public class ResearchQuestions
{
    public List<string> Definitions { get; set; } = new List<string>();
    public List<Question> Questions { get; set; } = new List<Question>();
}

public class Question
{
    public string Text { get; set; } = string.Empty;
    public List<string> Definitions { get; set; } = new List<string>();
}
