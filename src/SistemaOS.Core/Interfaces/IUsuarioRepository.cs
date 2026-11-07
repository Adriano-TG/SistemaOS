using SistemaOS.Core.Entities;

namespace SistemaOS.Core.Interfaces;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByLoginAsync(string login);
    Task<IReadOnlyList<Usuario>> GetAtivosAsync();
    Task<IReadOnlyList<Usuario>> GetByRoleAsync(RoleUsuario role);
}