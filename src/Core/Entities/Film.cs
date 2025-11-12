using Core.Enums;

namespace Core.Entities;

public class Film
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public SearchSource Source { get; set; }
    public string? Description { get; set; }
    public int? Year { get; set; }
}

