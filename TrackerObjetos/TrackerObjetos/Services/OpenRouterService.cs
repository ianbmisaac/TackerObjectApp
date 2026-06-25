using System.Text;
using System.Text.Json;

namespace TrackerObjetos.Services;

public class OpenRouterService
{
    private const string ApiKey = "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";
    private const string Model = "nvidia/nemotron-3-ultra-550b-a55b:free";
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<string> IdentifyObjectAsync(byte[] imageData)
    {
        var url = "https://openrouter.ai/api/v1/chat/completions";
        var base64 = Convert.ToBase64String(imageData);

        var request = new
        {
            model = Model,
            route = "fallback",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Identifica este objeto con el nombre exacto del producto.\n\nFormato:\n[NOMBRE EXACTO] | TIPO: [categoría] | MARCA: [solo si ves logo] | MODELO: [solo si ves número] | PROPÓSITO: [uso] | COLOR: [colores] | MATERIAL: [material] | DESCRIPCIÓN: [detalles]\n\nEjemplos:\n'Mouse Inalámbrico Logitech M280' | TIPO: Periférico | MARCA: Logitech | MODELO: M280 | PROPÓSITO: Controlador de computadora | COLOR: Negro | MATERIAL: Plástico | DESCRIPCIÓN: Mouse inalámbrico compacto con rueda de desplazamiento, diseño ergonómico para diestros'\n'Control Remoto Samsung BN59-01319A' | TIPO: Control remoto | MARCA: Samsung | MODELO: BN59-01319A | PROPÓSITO: Controlar TV Samsung | COLOR: Negro | MATERIAL: Plástico | DESCRIPCIÓN: Control remoto con teclado numérico y botones de navegación'\n\nREGLAS:\n- Nombre = tipo + marca + modelo si visible\n- MARCA solo si ves logo/texto. Si no: Desconocida\n- No inventes. Responde solo el formato.\n" },
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64}" } }
                    }
                }
            },
            max_tokens = 800
        };

        var json = JsonSerializer.Serialize(request);

        int maxRetries = 2;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Add("Authorization", $"Bearer {ApiKey}");
                httpRequest.Headers.Add("HTTP-Referer", "https://github.com/TrackerObjetos");
                httpRequest.Headers.Add("X-Title", "TrackerObjetos");
                httpRequest.Content = content;

                var response = await _httpClient.SendAsync(httpRequest);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    var text = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }

                    return "No se pudo identificar el objeto.";
                }

                int statusCode = (int)response.StatusCode;

                if (statusCode == 429 && attempt < maxRetries)
                {
                    int delay = (int)Math.Pow(2, attempt) * 500;
                    await Task.Delay(delay);
                    continue;
                }

                throw new HttpRequestException($"OpenRouter respondió {statusCode}");
            }

        throw new HttpRequestException("Máximo de reintentos alcanzado");
    }
}
