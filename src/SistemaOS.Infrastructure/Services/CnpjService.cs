using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SistemaOS.Infrastructure.Services;

public class CnpjService
{
    private readonly HttpClient _http;

    public CnpjService(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        // Identifica-se para não ser bloqueado pela firewall da Receita/BrasilAPI
        _http.DefaultRequestHeaders.Add("User-Agent", "SistemaOS_App_WPF");
    }

    public async Task<RetornoCnpj?> BuscarCnpjAsync(string cnpj)
    {
        try
        {
            var cnpjLimpo = new string(cnpj.Where(char.IsDigit).ToArray());
            var url = $"https://brasilapi.com.br/api/cnpj/v1/{cnpjLimpo}";

            var response = await _http.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RetornoCnpj>();
            }

            return null;
        }
        catch
        {
            return null; // Se estiver sem internet, apenas ignora sem crashar
        }
    }
}

// O SEGREDO ESTÁ AQUI: O 'string?' permite que a empresa não tenha 
// Nome Fantasia ou Telefone sem que a aplicação trave!
public class RetornoCnpj
{
    public string? razao_social { get; set; }
    public string? nome_fantasia { get; set; }
    public string? ddd_telefone_1 { get; set; }
    public string? cep { get; set; }
    public string? logradouro { get; set; }
    public string? numero { get; set; }
    public string? bairro { get; set; }
    public string? municipio { get; set; }
    public string? uf { get; set; }
}