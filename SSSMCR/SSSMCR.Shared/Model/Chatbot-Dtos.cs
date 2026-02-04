namespace SSSMCR.Shared.Model;

public class ChatRequest
{
    public string UserMessage { get; set; } = string.Empty;
    public string ContextKey { get; set; } = "general";
}

public class ChatResponse
{
    public string AiResponse { get; set; } = string.Empty;
}