using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SistemaOS.Core.Interfaces;

namespace SistemaOS.Infrastructure.Services;

public class ViaCepService : ICepService
{
    private readonly HttpClient _httpClient;

    public ViaCepService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<EnderecoCep?> BuscarCepAsync(string cep)
    {
        if (string.IsNullOrWhiteSpace(cep)) return null;

        // Limpa tudo que não for número (tira o traço, se houver)
        var cepLimpo = Regex.Replace(cep, "[^0-9]", "");

        if (cepLimpo.Length != 8) return null;

        try
        {
            var url = $"https://viacep.com.br/ws/{cepLimpo}/json/";
            var resultado = await _httpClient.GetFromJsonAsync<EnderecoCep>(url);

            // Se o ViaCEP não achar o CEP, ele retorna "erro": "true" no JSON
            if (resultado != null && resultado.Erro == "true")
            {
                return null;
            }

            return resultado;
        }
        catch
        {
            // Em caso de falta de internet ou erro no servidor do ViaCEP
            return null;
        }
    }
}