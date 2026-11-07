using SistemaOS.Core.Entities;

namespace SistemaOS.Core.Interfaces;

public interface ICurrentUserService
{
    Usuario? UsuarioLogado { get; set; }
    bool IsAdmin { get; }
    bool IsTecnico { get; }
    bool IsAtendente { get; }
    bool PodeGerenciarUsuarios { get; }
    bool PodeVerTodasOS { get; }
    bool PodeAlterarStatusOS { get; }
    bool PodeGerenciarEstoque { get; }
    bool PodeVerFinanceiro { get; }
    void Logout();
}