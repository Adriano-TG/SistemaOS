using SistemaOS.Core.Entities;

namespace SistemaOS.Core.Interfaces;

public interface IEmpresaRepository : IRepository<Empresa>
{
    Task<Empresa?> GetEmpresaAsync();
}