using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SistemaOS.Core.Interfaces;

namespace SistemaOS.Infrastructure.Services;

public class EvolutionApiService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _instanceName;
    private readonly string _apiKey;

    public EvolutionApiService(IConfiguration configuration, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();

        var whatsAppSection = configuration.GetSection("WhatsApp:EvolutionApi");
        _baseUrl = whatsAppSection["BaseUrl"] ?? "";
        _instanceName = whatsAppSection["InstanceName"] ?? "";
        _apiKey = whatsAppSection["ApiKey"] ?? "";

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
        }
    }

    public async Task<WhatsAppResult> SendTextAsync(string phoneNumber, string message)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_instanceName))
        {
            return new WhatsAppResult(false, Error: "Evolution API não configurada");
        }

        try
        {
            var payload = new
            {
                number = FormatPhoneNumber(phoneNumber),
                text = message
            };

            var url = $"{_baseUrl.TrimEnd('/')}/message/sendText/{_instanceName}";
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                var messageId = result.TryGetProperty("key", out var key) && key.TryGetProperty("id", out var id)
                    ? id.GetString()
                    : null;

                return new WhatsAppResult(true, messageId);
            }

            return new WhatsAppResult(false, Error: $"Erro HTTP {response.StatusCode}: {responseJson}");
        }
        catch (Exception ex)
        {
            return new WhatsAppResult(false, Error: ex.Message);
        }
    }

    public async Task<WhatsAppResult> SendMediaAsync(string phoneNumber, string mediaUrl, string? caption = null)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_instanceName))
        {
            return new WhatsAppResult(false, Error: "Evolution API não configurada");
        }

        try
        {
            var payload = new
            {
                number = FormatPhoneNumber(phoneNumber),
                mediaUrl = mediaUrl,
                caption = caption ?? ""
            };

            var url = $"{_baseUrl.TrimEnd('/')}/message/sendMedia/{_instanceName}";
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                var messageId = result.TryGetProperty("key", out var key) && key.TryGetProperty("id", out var id)
                    ? id.GetString()
                    : null;

                return new WhatsAppResult(true, messageId);
            }

            return new WhatsAppResult(false, Error: $"Erro HTTP {response.StatusCode}: {responseJson}");
        }
        catch (Exception ex)
        {
            return new WhatsAppResult(false, Error: ex.Message);
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_instanceName))
        {
            return false;
        }

        try
        {
            var url = $"{_baseUrl.TrimEnd('/')}/instance/connectionState/{_instanceName}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                return result.TryGetProperty("instance", out var instance)
                    && instance.TryGetProperty("state", out var state)
                    && state.GetString() == "open";
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        // Remove tudo que não for número
        var numbersOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Se já tem código do país (55) e DDD, retorna como está
        if (numbersOnly.StartsWith("55") && numbersOnly.Length >= 12)
            return numbersOnly;

        // Se tem 11 dígitos (celular com 9) + DDD, adiciona 55
        if (numbersOnly.Length == 11 || numbersOnly.Length == 10)
            return "55" + numbersOnly;

        // Se já começa com 55
        if (numbersOnly.StartsWith("55"))
            return numbersOnly;

        return numbersOnly;
    }
}