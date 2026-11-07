using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SistemaOS.App.Services;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;
using SistemaOS.Infrastructure.Repositories;
using Xunit;

namespace SistemaOS.Tests.Services;

public class PdfServiceTests
{
    private readonly PdfService _pdfService;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly AppDbContext _dbContext;

    public PdfServiceTests()
    {
        // Configure QuestPDF for testing
        QuestPDF.Settings.License = LicenseType.Community;

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("PdfServiceTestDb")
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        // Seed test data
        if (!_dbContext.DadosEmpresa.Any())
        {
            _dbContext.DadosEmpresa.Add(new Empresa
            {
                NomeFantasia = "Empresa Teste",
                Cnpj = "12.345.678/0001-99",
                RazaoSocial = "Empresa Teste LTDA",
                Telefone = "11999999999",
                Logradouro = "Rua Teste",
                Numero = "123",
                Bairro = "Centro",
                Cidade = "São Paulo",
                Uf = "SP",
                Cep = "01000-000"
            });
            _dbContext.SaveChanges();
        }

        _empresaRepository = new EmpresaRepository(_dbContext);
        _pdfService = new PdfService(_empresaRepository);
    }

    [Fact]
    public async Task GerarOrdemServicoAsync_ShouldCreatePdfFile_WhenValidData()
    {
        // Arrange
        var os = new OrdemServico
        {
            Id = 1,
            DataAbertura = DateTime.Now,
            Cliente = new Cliente { Nome = "Cliente Teste", Telefone = "11999999999", Endereco = "Rua Teste, 123" },
            Equipamento = "Notebook Dell",
            Marca = "Dell",
            NumeroSerie = "SN123",
            DefeitoRelatado = "Não liga",
            LaudoTecnico = "Troca da fonte",
            Status = StatusOS.EmAnalise,
            Itens = new System.Collections.Generic.List<ItemOS>
            {
                new() { Descricao = "Fonte 90W", Quantidade = 1, ValorUnitario = 150.00m }
            }
        };

        // Act
        var caminho = await _pdfService.GerarOrdemServicoAsync(os);

        // Assert
        caminho.Should().NotBeNullOrEmpty();
        File.Exists(caminho).Should().BeTrue();
        caminho.Should().Contain("OS_1_");
        caminho.Should().EndWith(".pdf");

        // Cleanup
        File.Delete(caminho);
    }

    [Fact]
    public async Task GerarLaudoAsync_ShouldCreatePdfFile_WhenValidData()
    {
        // Arrange
        var os = new OrdemServico
        {
            Id = 2,
            DataAbertura = DateTime.Now.AddDays(-5),
            DataConclusao = DateTime.Now,
            Cliente = new Cliente { Nome = "Cliente Laudo", Telefone = "11888888888", Endereco = "Av. Teste, 456" },
            Equipamento = "Impressora HP",
            Marca = "HP",
            NumeroSerie = "SN456",
            DefeitoRelatado = "Não imprime",
            LaudoTecnico = "Limpeza de cabeçote",
            Status = StatusOS.Entregue,
            Itens = new System.Collections.Generic.List<ItemOS>
            {
                new() { Descricao = "Limpeza", Quantidade = 1, ValorUnitario = 80.00m },
                new() { Descricao = "Tinta Preta", Quantidade = 1, ValorUnitario = 45.00m }
            }
        };

        // Act
        var caminho = await _pdfService.GerarLaudoAsync(os);

        // Assert
        caminho.Should().NotBeNullOrEmpty();
        File.Exists(caminho).Should().BeTrue();
        caminho.Should().Contain("OS_2_");
        caminho.Should().EndWith(".pdf");

        // Cleanup
        File.Delete(caminho);
    }

    [Fact]
    public void Abrir_ShouldNotThrow_WhenFileExists()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test");

        try
        {
            // Act & Assert - Should not throw (though it won't actually open in test environment)
            PdfService.Abrir(tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}