using System.Threading.Tasks;
using SistemaOS.Core.Entities;

namespace SistemaOS.Core.Interfaces;

public record LoginResult(
    bool Success,
    Usuario? Usuario = null,
    string? Error = null,
    bool RequerTrocaSenha = false);

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string login, string senha);
    Task<Usuario?> GetUsuarioByIdAsync(int id);
    Task<bool> AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha);
    string HashSenha(string senha);
    bool VerificarSenha(string senha, string hash);
}