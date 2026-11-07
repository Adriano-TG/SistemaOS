using System.ComponentModel.DataAnnotations;

namespace SistemaOS.Core.Entities;

public enum RoleUsuario
{
    Admin = 1,
    Tecnico = 2,
    Atendente = 3
}

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string SenhaHash { get; set; } = string.Empty; // BCrypt hash

    public RoleUsuario Role { get; set; } = RoleUsuario.Atendente;

    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; } = DateTime.Now;

    public DateTime? UltimoLogin { get; set; }

    /// <summary>
    /// Indica se o usuário deve trocar a senha no próximo login (primeiro acesso ou reset de admin)
    /// </summary>
    public bool PrimeiroLogin { get; set; } = false;

    // Navigation
    public virtual ICollection<OrdemServico> OrdensAtribuidas { get; set; } = new List<OrdemServico>();
}