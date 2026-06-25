using System.Text;
using System.Text.Json;

namespace TrackerObjetos.Services;

public class GeminiService
{
    private const string ApiKey = "AQ.Ab8RN6LJQLNb5QyqIVDTH18H0eWmqO_nyki4rBEtjHqdw4kd9Q";
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<string> IdentifyObjectAsync(byte[] imageData)
    {
        var base64 = Convert.ToBase64String(imageData);

        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = "Identifica el objeto principal de esta imagen en español. Responde SOLO con el nombre del objeto seguido de '|' y una descripción breve de 1-2 oraciones. Ejemplo: 'Teléfono móvil | Este es un smartphone con pantalla táctil y diseño moderno.'" },
                        new { inlineData = new { mimeType = "image/jpeg", data = base64 } }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={ApiKey}";

        int maxRetries = 3;
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseJson);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "No se pudo identificar el objeto.";
            }

            if ((int)response.StatusCode == 429 && attempt < maxRetries)
            {
                int delayMs = (int)Math.Pow(2, attempt) * 2000;
                System.Diagnostics.Debug.WriteLine(
                    $"=== GEMINI 429 (intento {attempt + 1}/{maxRetries}), esperando {delayMs}ms...");
                await Task.Delay(delayMs);
                continue;
            }

            var preview = responseJson.Length > 500 ? responseJson[..500] : responseJson;
            var errorMsg = $"Gemini respondió {(int)response.StatusCode} {response.ReasonPhrase}: {preview}";
            System.Diagnostics.Debug.WriteLine($"=== GEMINI ERROR: {errorMsg}");
            throw new HttpRequestException(errorMsg);
        }

        throw new HttpRequestException("Se excedieron los reintentos por límite de velocidad de Gemini (429). Espera un minuto e intenta de nuevo.");
    }
}
