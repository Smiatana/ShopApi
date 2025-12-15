using System.Text;
using System.Text.Json;

public class TranslatorService
{
    private readonly HttpClient _client;

    public TranslatorService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient();
        _client.BaseAddress = new Uri("https://localhost:6769/");
    }

    public async Task<string> TranslateAsync(
        string text,
        string source = "auto",
        string target = "be")
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var payload = new
        {
            q = text,
            source,
            target,
            format = "text",
            alternatives = 0,
            api_key = ""
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("translate", content);
        if (!response.IsSuccessStatusCode)
            return text;

        var resultJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resultJson);


        Console.WriteLine(doc.RootElement
            .GetProperty("translatedText")
            .GetString() ?? text);

        return doc.RootElement
            .GetProperty("translatedText")
            .GetString() ?? text;
    }
}
