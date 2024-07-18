namespace llassist.Models;

public class Article
{
    public string Authors { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DOI { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public KeySemantics KeySemantics { get; set; } = new KeySemantics();
    public bool MustRead { get; set; }
    public Relevance[] Relevances { get; set; } = Array.Empty<Relevance>();
}
