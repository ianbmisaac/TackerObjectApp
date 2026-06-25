using System.Text;
using System.Text.Json;

namespace TrackerObjetos.Services;

public class AnthropicService
{
    private const string ApiKey = "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";
    private const string Model = "claude-haiku-4-5-20251001";
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<string> IdentifyObjectAsync(byte[] imageData)
    {
        var url = "https://api.anthropic.com/v1/messages";
        var base64 = Convert.ToBase64String(imageData);

        var request = new
        {
            model = Model,
            max_tokens = 800,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = "Identifica este objeto con el nombre exacto del producto.\n\nFormato:\n[NOMBRE EXACTO] | TIPO: [categoría] | MARCA: [solo si ves logo] | MODELO: [solo si ves número] | PROPÓSITO: [uso] | COLOR: [colores] | MATERIAL: [material] | DESCRIPCIÓN: [detalles]\n\nEjemplos:\n'Mouse Inalámbrico Logitech M280' | TIPO: Periférico | MARCA: Logitech | MODELO: M280 | PROPÓSITO: Controlador de computadora | COLOR: Negro | MATERIAL: Plástico | DESCRIPCIÓN: Mouse inalámbrico compacto con rueda de desplazamiento, diseño ergonómico para diestros\n'Control Remoto Samsung BN59-01319A' | TIPO: Control remoto | MARCA: Samsung | MODELO: BN59-01319A | PROPÓSITO: Controlar TV Samsung | COLOR: Negro | MATERIAL: Plástico | DESCRIPCIÓN: Control remoto con teclado numérico y botones de navegación\n\nREGLAS:\n- Nombre = tipo + marca + modelo si visible\n- MARCA solo si ves logo/texto. Si no: Desconocida\n- No inventes. Responde solo el formato.\n"
                        },
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = "image/jpeg",
                                data = base64
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-api-key", ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Content = content;

        var response = await _httpClient.SendAsync(httpRequest);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(responseJson);
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();

            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return "No se pudo identificar el objeto.";
        }

        var preview = responseJson.Length > 500 ? responseJson[..500] : responseJson;
        throw new HttpRequestException($"Claude respondió {(int)response.StatusCode}: {preview}");
    }
}
