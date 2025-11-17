namespace Application.DTOs;

public class AliceResponse
{
    public Response Response { get; set; } = null!;
    public string Version { get; set; } = "1.0";
    public Session Session { get; set; } = null!;
}

public class Response
{
    public string Text { get; set; } = string.Empty;
    public bool EndSession { get; set; } = true;
}