using System.Text.Json;

namespace TrackerObjetos.Services;

public class WebSearchService
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    public async Task<WebSearchResult?> SearchObjectAsync(string objectName)
    {
        try
        {
            var query = Uri.EscapeDataString(objectName);
            var url = $"https://en.wikipedia.org/api/rest_v1/search/page?q={query}&limit=1";
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var pages = doc.RootElement.GetProperty("pages");

            if (pages.GetArrayLength() == 0)
                return null;

            var page = pages[0];

            var title = page.GetProperty("title").GetString() ?? "";
            var extract = page.GetProperty("extract").GetString() ?? "";

            if (string.IsNullOrWhiteSpace(extract))
                return null;

            var description = page.TryGetProperty("description", out var desc)
                ? desc.GetString()
                : null;

            return new WebSearchResult
            {
                Title = title,
                Description = description,
                Extract = extract,
                Source = "Wikipedia"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSearch error: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }
}

public class WebSearchResult
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Extract { get; set; } = "";
    public string Source { get; set; } = "";
}
