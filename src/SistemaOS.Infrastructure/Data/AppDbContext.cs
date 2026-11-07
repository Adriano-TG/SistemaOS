using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;

namespace SistemaOS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    // 👇 1. Construtor vazio essencial para o Entity Framework trabalhar internamente 👇
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<OrdemServico> Ordens => Set<OrdemServico>();
    public DbSet<ItemOS> Itens => Set<ItemOS>();
    public DbSet<Empresa> DadosEmpresa => Set<Empresa>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<MovimentoEstoque> MovimentosEstoque => Set<MovimentoEstoque>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    // 👇 2. A configuração do motor do SQLite que estava a faltar 👇
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Define o nome e o motor do banco de dados
            optionsBuilder.UseSqlite("Data Source=sistemaos.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Status gravado como texto
        modelBuilder.Entity<OrdemServico>()
            .Property(o => o.Status)
            .HasConversion<string>();

        // Uma O.S. tem vários itens; apagar a O.S. apaga os itens
        modelBuilder.Entity<OrdemServico>()
            .HasMany(o => o.Itens)
            .WithOne()
            .HasForeignKey(i => i.OrdemServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Um cliente tem várias O.S.; não deixa apagar cliente que tenha O.S.
        modelBuilder.Entity<OrdemServico>()
            .HasOne(o => o.Cliente)
            .WithMany()
            .HasForeignKey(o => o.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Produto - MovimentoEstoque (1:N)
        modelBuilder.Entity<Produto>()
            .HasMany(p => p.Movimentos)
            .WithOne(m => m.Produto)
            .HasForeignKey(m => m.ProdutoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Produto - ItemOS (1:N)
        modelBuilder.Entity<Produto>()
            .HasMany(p => p.ItensOS)
            .WithOne(i => i.Produto)
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.SetNull);

        // MovimentoEstoque - OrdemServico (N:1, opcional)
        modelBuilder.Entity<MovimentoEstoque>()
            .HasOne(m => m.OrdemServico)
            .WithMany()
            .HasForeignKey(m => m.OrdemServicoId)
            .OnDelete(DeleteBehavior.SetNull);

        // Usuario - OrdemServico (1:N, opcional)
        modelBuilder.Entity<OrdemServico>()
            .HasOne(o => o.Usuario)
            .WithMany(u => u.OrdensAtribuidas)
            .HasForeignKey(o => o.UsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // Usuario - Login único
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Login)
            .IsUnique();

        // Índices para performance
        modelBuilder.Entity<Produto>()
            .HasIndex(p => p.Codigo)
            .IsUnique();

        modelBuilder.Entity<Produto>()
            .HasIndex(p => p.Nome);

        modelBuilder.Entity<MovimentoEstoque>()
            .HasIndex(m => new { m.ProdutoId, m.DataMovimento });

        // Conversão de enum para string
        modelBuilder.Entity<MovimentoEstoque>()
            .Property(m => m.Tipo)
            .HasConversion<string>();
    }
}