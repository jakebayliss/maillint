using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace API.Services;

public class LLMClient
{
	private readonly HttpClient _httpClient;
	private readonly string _model;
	private readonly int _maxOutputTokens;

	public LLMClient(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
		_model = configuration["LLM:Model"] ?? "kimi-k2-0905-preview";
		_maxOutputTokens = configuration.GetValue<int?>("LLM:MaxOutputTokens") ?? 768;
	}

	public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken)
	{
		var request = new
		{
			model = _model,
			max_tokens = _maxOutputTokens,
			temperature = 0.3,
			messages = new object[]
			{
				new
				{
					role = "user",
					content = new object[]
					{
						new { type = "text", text = prompt }
					}
				}
			}
		};

		var json = JsonSerializer.Serialize(request);
		using var content = new StringContent(json, Encoding.UTF8, "application/json");
		using var response = await _httpClient.PostAsync("messages", content, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new InvalidOperationException($"LLM request failed ({(int)response.StatusCode} {response.ReasonPhrase}): {errorBody}");
		}

		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
		if (doc.RootElement.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
		{
			var sb = new StringBuilder();
			foreach (var part in contentArray.EnumerateArray())
			{
				if (part.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
				{
					sb.Append(textEl.GetString());
				}
			}
			return sb.ToString();
		}

		return string.Empty;
	}
}

