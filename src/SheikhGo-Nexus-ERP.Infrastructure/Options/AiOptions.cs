namespace SheikhGoNexusERP.Infrastructure.Options;

public class AiOptions
{
    public const string SectionName = "Ai";

    public bool Enabled { get; set; } = true;
    /// <summary>Null | Ollama | OpenAI</summary>
    public string Provider { get; set; } = "Null";
    public bool UseLlmIntent { get; set; }
    public int MaxHistoryMessages { get; set; } = 12;
    public OllamaAiOptions Ollama { get; set; } = new();
    public OpenAiOptions OpenAI { get; set; } = new();
}

public class OllamaAiOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string Model { get; set; } = "llama3.2";
}

public class OpenAiOptions
{
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
}
