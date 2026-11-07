using SistemaOS.Core.Entities;

namespace SistemaOS.Core.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> GetByCpfCnpjAsync(string cpfCnpj);
    Task<IReadOnlyList<Cliente>> SearchByNomeAsync(string termo);
    Task<IReadOnlyList<Cliente>> GetAtivosAsync();
}