using System.Text.Json.Serialization;

namespace Application.DTOs;

public class AliceRequest
{
    public Request Request { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
}

public class Request
{
    public string Command { get; set; } = string.Empty;
    public string? OriginalUtterance { get; set; }
    public string Type { get; set; } = string.Empty;
    public Nlu? Nlu { get; set; }
}

public class Nlu
{
    public string[] Tokens { get; set; } = [];

    [JsonIgnore] public object[] Entities { get; set; } = [];

    [JsonIgnore] public Dictionary<string, object> Intents { get; set; } = new();
}

public class Session
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int MessageId { get; set; }
}