using System.Text;
using System.Text.Json;

namespace TrackerObjetos.Services;

public class HuggingFaceService
{
    private const string ApiToken = "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";
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(90) };

    public async Task<string> IdentifyObjectAsync(byte[] imageData)
    {
        var url = "https://api-inference.huggingface.co/models/Salesforce/blip-image-captioning-base";
        var base64 = Convert.ToBase64String(imageData);
        int maxRetries = 3;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var payload = new { inputs = $"data:image/jpeg;base64,{base64}" };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {ApiToken}");
                request.Content = content;

                System.Diagnostics.Debug.WriteLine($"=== HF solicitando (intento {attempt + 1})...");
                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"=== HF respuesta ({response.StatusCode}): {responseJson[..Math.Min(responseJson.Length, 300)]}");

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    var text = doc.RootElement[0].GetProperty("generated_text").GetString();
                    return text ?? "No se pudo identificar el objeto.";
                }

                int statusCode = (int)response.StatusCode;

                if ((statusCode == 503 || statusCode == 429) && attempt < maxRetries)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 3000;
                    System.Diagnostics.Debug.WriteLine(
                        $"=== HF reintento (status {statusCode}), esperando {delayMs}ms...");
                    await Task.Delay(delayMs);
                    continue;
                }

                var preview = responseJson.Length > 500 ? responseJson[..500] : responseJson;
                var errorMsg = $"Hugging Face respondió {statusCode}: {preview}";
                System.Diagnostics.Debug.WriteLine($"=== HF ERROR: {errorMsg}");
                throw new HttpRequestException(errorMsg);
            }
            catch (HttpRequestException) when (attempt == maxRetries)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                System.Diagnostics.Debug.WriteLine($"=== HF error de red (intento {attempt + 1}): {ex.GetType().Name}: {ex.Message}");
                await Task.Delay(3000);
            }
        }

        throw new HttpRequestException("Se excedieron los reintentos. Intenta de nuevo más tarde.");
    }
}
