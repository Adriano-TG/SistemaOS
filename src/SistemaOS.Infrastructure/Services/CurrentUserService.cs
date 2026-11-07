using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;

namespace SistemaOS.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    public Usuario? UsuarioLogado { get; set; }

    public bool IsAdmin => UsuarioLogado?.Role == RoleUsuario.Admin;
    public bool IsTecnico => UsuarioLogado?.Role == RoleUsuario.Tecnico;
    public bool IsAtendente => UsuarioLogado?.Role == RoleUsuario.Atendente;

    public bool PodeGerenciarUsuarios => IsAdmin;
    public bool PodeVerTodasOS => IsAdmin || IsTecnico;
    public bool PodeAlterarStatusOS => IsAdmin || IsTecnico;
    public bool PodeGerenciarEstoque => IsAdmin;
    public bool PodeVerFinanceiro => IsAdmin;

    public void Logout()
    {
        UsuarioLogado = null;
    }
}