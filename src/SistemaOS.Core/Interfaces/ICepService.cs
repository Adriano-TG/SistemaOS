using System.Threading.Tasks;

namespace SistemaOS.Core.Interfaces;

// Modelo de dados que vai receber o JSON da API
public record EnderecoCep(
    string Cep,
    string Logradouro,
    string Complemento,
    string Bairro,
    string Localidade,
    string Uf,
    string Erro);

public interface ICepService
{
    Task<EnderecoCep?> BuscarCepAsync(string cep);
}