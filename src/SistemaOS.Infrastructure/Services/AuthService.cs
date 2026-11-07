using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;

namespace SistemaOS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AuthService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<LoginResult> LoginAsync(string login, string senha)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            return new LoginResult(false, Error: "Login e senha são obrigatórios");
        }

        using var db = _dbFactory.CreateDbContext();
        var usuario = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login == login && u.Ativo);

        if (usuario == null)
        {
            return new LoginResult(false, Error: "Usuário não encontrado ou inativo");
        }

        if (!VerificarSenha(senha, usuario.SenhaHash))
        {
            return new LoginResult(false, Error: "Senha incorreta");
        }

        // Atualiza último login - usar Update para compatibilidade com InMemory
        var usuarioParaAtualizar = await db.Usuarios.FindAsync(usuario.Id);
        if (usuarioParaAtualizar != null)
        {
            usuarioParaAtualizar.UltimoLogin = DateTime.Now;
            await db.SaveChangesAsync();
        }

        // Se for primeiro login, exige troca de senha
        var requerTroca = usuario.PrimeiroLogin;
        return new LoginResult(true, usuario, RequerTrocaSenha: requerTroca);
    }

    public async Task<Usuario?> GetUsuarioByIdAsync(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<bool> AlterarSenhaAsync(int usuarioId, string senhaAtual, string novaSenha)
    {
        using var db = _dbFactory.CreateDbContext();
        var usuario = await db.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return false;

        if (!VerificarSenha(senhaAtual, usuario.SenhaHash))
        {
            return false;
        }

        usuario.SenhaHash = HashSenha(novaSenha);
        usuario.PrimeiroLogin = false; // Marca que a senha foi trocada
        await db.SaveChangesAsync();
        return true;
    }

    public string HashSenha(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerificarSenha(string senha, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hash);
        }
        catch
        {
            return false;
        }
    }
}