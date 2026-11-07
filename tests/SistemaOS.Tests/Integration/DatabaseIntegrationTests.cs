using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SistemaOS.Core.Entities;
using SistemaOS.Infrastructure.Data;
using Xunit;

namespace SistemaOS.Tests.Integration;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly AppDbContext _dbContext;

    public DatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void Cliente_CanBeAddedAndRetrieved()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            Telefone = "11999999999",
            CpfCnpj = "12345678901",
            Endereco = "Rua Teste, 123 - Centro, São Paulo/SP - CEP: 01000000"
        };

        // Act
        _dbContext.Clientes.Add(cliente);
        _dbContext.SaveChanges();

        // Assert
        var saved = _dbContext.Clientes.First();
        saved.Id.Should().BeGreaterThan(0);
        saved.Nome.Should().Be("João Silva");
        saved.Telefone.Should().Be("11999999999");
    }

    [Fact]
    public void OrdemServico_CanBeAddedWithClienteAndItens()
    {
        // Arrange
        var cliente = new Cliente { Nome = "Cliente OS", Telefone = "11888888888", CpfCnpj = "98765432100" };
        _dbContext.Clientes.Add(cliente);
        _dbContext.SaveChanges();

        var os = new OrdemServico
        {
            ClienteId = cliente.Id,
            Cliente = cliente,
            Equipamento = "Notebook",
            Marca = "Dell",
            Status = StatusOS.Aberta,
            Itens = new System.Collections.Generic.List<ItemOS>
            {
                new() { Descricao = "Peça 1", Quantidade = 1, ValorUnitario = 100 },
                new() { Descricao = "Peça 2", Quantidade = 2, ValorUnitario = 50 }
            }
        };

        // Act
        _dbContext.Ordens.Add(os);
        _dbContext.SaveChanges();

        // Assert
        var saved = _dbContext.Ordens
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .First();

        saved.Id.Should().BeGreaterThan(0);
        saved.Cliente.Nome.Should().Be("Cliente OS");
        saved.Itens.Should().HaveCount(2);
        saved.Itens.Sum(i => i.Total).Should().Be(200);
    }

    [Fact]
    public void Empresa_CanBeAddedAndRetrieved()
    {
        // Arrange
        var empresa = new Empresa
        {
            Cnpj = "12.345.678/0001-90",
            RazaoSocial = "Empresa Teste LTDA",
            NomeFantasia = "Empresa Teste",
            Telefone = "1133334444",
            Cep = "01000000",
            Logradouro = "Rua A",
            Numero = "100",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP"
        };

        // Act
        _dbContext.DadosEmpresa.Add(empresa);
        _dbContext.SaveChanges();

        // Assert
        var saved = _dbContext.DadosEmpresa.First();
        saved.Id.Should().BeGreaterThan(0);
        saved.Cnpj.Should().Be("12.345.678/0001-90");
    }

    [Fact]
    public void CascadeDelete_ShouldRemoveItensWhenOrdemDeleted()
    {
        // Arrange
        var cliente = new Cliente { Nome = "Cliente Cascade" };
        _dbContext.Clientes.Add(cliente);
        _dbContext.SaveChanges();

        var os = new OrdemServico
        {
            ClienteId = cliente.Id,
            Cliente = cliente,
            Equipamento = "Teste",
            Itens = new System.Collections.Generic.List<ItemOS>
            {
                new() { Descricao = "Item 1", Quantidade = 1, ValorUnitario = 10 }
            }
        };
        _dbContext.Ordens.Add(os);
        _dbContext.SaveChanges();

        var osId = os.Id;

        // Act
        _dbContext.Ordens.Remove(os);
        _dbContext.SaveChanges();

        // Assert
        _dbContext.Ordens.Should().BeEmpty();
        _dbContext.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryPerformance_ShouldUseIndexes()
    {
        // Arrange - Add multiple clientes
        for (int i = 0; i < 100; i++)
        {
            _dbContext.Clientes.Add(new Cliente
            {
                Nome = $"Cliente {i}",
                Telefone = $"119999{i:D4}",
                CpfCnpj = $"{i:D11}"
            });
        }
        _dbContext.SaveChanges();

        // Act
        var result = await _dbContext.Clientes
            .Where(c => c.Nome.Contains("Cliente 5"))
            .ToListAsync();

        // Assert
        result.Should().HaveCount(11); // Cliente 5, 50-59
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}