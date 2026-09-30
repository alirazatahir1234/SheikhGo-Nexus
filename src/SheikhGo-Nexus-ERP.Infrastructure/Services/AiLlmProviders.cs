using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SheikhGoNexusERP.Application.Interfaces;
using SheikhGoNexusERP.Infrastructure.Options;

namespace SheikhGoNexusERP.Infrastructure.Services;

public class NullLlmProvider : ILlmProvider
{
    public Task<string> CompleteAsync(IReadOnlyList<LlmChatMessage> messages, CancellationToken ct = default)
    {
        var facts = messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";
        // Prefer last assistant-ready FACTS block from user message that contains FACTS JSON
        var sb = new StringBuilder();
        sb.AppendLine("FACT");
        sb.AppendLine(facts);
        sb.AppendLine();
        sb.AppendLine("INTERPRETATION");
        sb.AppendLine("Figures above come from Nexus posted ledgers via controlled report tools. No SQL was generated.");
        return Task.FromResult(sb.ToString().Trim());
    }
}

public class OllamaLlmProvider(HttpClient http, IOptions<AiOptions> options) : ILlmProvider
{
    public async Task<string> CompleteAsync(IReadOnlyList<LlmChatMessage> messages, CancellationToken ct = default)
    {
        var cfg = options.Value.Ollama;
        var body = new
        {
            model = cfg.Model,
            stream = false,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        };
        using var resp = await http.PostAsJsonAsync($"{cfg.BaseUrl.TrimEnd('/')}/api/chat", body, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

public class OpenAiLlmProvider(HttpClient http, IOptions<AiOptions> options) : ILlmProvider
{
    public async Task<string> CompleteAsync(IReadOnlyList<LlmChatMessage> messages, CancellationToken ct = default)
    {
        var cfg = options.Value.OpenAI;
        if (string.IsNullOrWhiteSpace(cfg.ApiKey))
            throw new InvalidOperationException("OpenAI ApiKey is not configured.");

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{cfg.BaseUrl.TrimEnd('/')}/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);
        req.Content = JsonContent.Create(new
        {
            model = cfg.Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        });
        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

/// <summary>Resolves configured provider; falls back to Null on failure.</summary>
public class ConfigurableLlmProvider(
    IOptions<AiOptions> options,
    NullLlmProvider nullProvider,
    OllamaLlmProvider ollama,
    OpenAiLlmProvider openAi) : ILlmProvider
{
    public async Task<string> CompleteAsync(IReadOnlyList<LlmChatMessage> messages, CancellationToken ct = default)
    {
        var name = (options.Value.Provider ?? "Null").Trim();
        try
        {
            return name.ToLowerInvariant() switch
            {
                "ollama" => await ollama.CompleteAsync(messages, ct),
                "openai" => await openAi.CompleteAsync(messages, ct),
                _ => await nullProvider.CompleteAsync(messages, ct)
            };
        }
        catch
        {
            return await nullProvider.CompleteAsync(messages, ct);
        }
    }
}
