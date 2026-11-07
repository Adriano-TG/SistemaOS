using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentValidation;
using Serilog;
using QuestPDF;
using SistemaOS.App.Services;
using SistemaOS.App.ViewModels;
using SistemaOS.App.Validators;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;
using SistemaOS.Infrastructure.Repositories;
using SistemaOS.Infrastructure.Services;

namespace SistemaOS.Tests;

/// <summary>
/// Base class for tests providing common setup for in-memory database and mocked services
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly AppDbContext DbContext;
    protected readonly IDbContextFactory<AppDbContext> DbFactory;
    protected readonly Mock<ICepService> CepServiceMock;
    protected readonly Mock<CnpjService> CnpjServiceMock;
    protected readonly PdfService PdfService; // Real instance with test factory
    protected readonly Mock<IThemeService> ThemeServiceMock;
    protected readonly Mock<IWhatsAppService> WhatsAppServiceMock;
    protected readonly MigracaoService MigracaoService;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IEmpresaRepository EmpresaRepository;

    protected TestBase()
    {
        // Configura Serilog para testes (console only, no file)
        SerilogConfig.Configure();

        // Configura QuestPDF para testes - usa fontes do sistema
        QuestPDF.Settings.UseSystemFonts = true;

        // Use a shared in-memory database name for both DbContext and DbFactory
        var dbName = Guid.NewGuid().ToString();

        // Configure in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        DbContext = new AppDbContext(options);
        DbContext.Database.EnsureCreated();

        // Initialize MigracaoService with the test DbContext
        MigracaoService = new MigracaoService(DbContext);

        // Create DbContextFactory for test services using the SAME database
        var factoryServices = new ServiceCollection();
        factoryServices.AddDbContextFactory<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var factoryProvider = factoryServices.BuildServiceProvider();
        DbFactory = factoryProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        // Seed test empresa in factory context
        using (var db = DbFactory.CreateDbContext())
        {
            db.Database.EnsureCreated();
            if (!db.DadosEmpresa.Any())
            {
                db.DadosEmpresa.Add(new Empresa
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
                db.SaveChanges();
            }
        }

        // Setup mocks
        CepServiceMock = new Mock<ICepService>();
        CnpjServiceMock = new Mock<CnpjService>(MockBehavior.Loose, new HttpClient());

        // Create EmpresaRepository for test
        EmpresaRepository = new EmpresaRepository(DbContext);

        // Use REAL PdfService with EmpresaRepository
        PdfService = new PdfService(EmpresaRepository);

        ThemeServiceMock = new Mock<IThemeService>();
        WhatsAppServiceMock = new Mock<IWhatsAppService>();

        // Configure default behavior for IThemeService mock
        ThemeServiceMock.Setup(x => x.Config).Returns(new ConfiguracoesTema());
        ThemeServiceMock.Setup(x => x.Carregar());
        ThemeServiceMock.Setup(x => x.AlternarTema());
        ThemeServiceMock.Setup(x => x.DefinirCor(It.IsAny<string>()));

        // Configure default behavior for IWhatsAppService mock
        WhatsAppServiceMock.Setup(x => x.SendTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new WhatsAppResult(true, "test-message-id"));
        WhatsAppServiceMock.Setup(x => x.IsConnectedAsync())
            .ReturnsAsync(false);

        // Configure DI container
        var services = new ServiceCollection();
        services.AddSingleton(DbContext);
        services.AddSingleton(DbFactory);
        services.AddSingleton(CepServiceMock.Object);
        services.AddSingleton(CnpjServiceMock.Object);
        services.AddSingleton(EmpresaRepository);
        services.AddSingleton(PdfService); // Real instance
        services.AddSingleton(ThemeServiceMock.Object);
        services.AddSingleton(WhatsAppServiceMock.Object);
        services.AddSingleton<ILoggerService, SerilogLoggerService>();

        // Register IAuthService (uses DbFactory)
        services.AddTransient<IAuthService, AuthService>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<ClienteValidator>();

        // Register Repositories (Scoped in production, Singleton in tests for shared DbContext)
        services.AddSingleton<IClienteRepository, ClienteRepository>();
        services.AddSingleton<IProdutoRepository, ProdutoRepository>();
        services.AddSingleton<IOrdemServicoRepository, OrdemServicoRepository>();
        services.AddSingleton<IUsuarioRepository, UsuarioRepository>();

        // Register new services
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IUIStateService, UIStateService>();
        services.AddSingleton<ITextFormatterService, TextFormatterService>();
        services.AddSingleton<IEstoqueService, EstoqueService>();

        services.AddSingleton(MigracaoService);

        // Register ViewModels - MainViewModel requires ICurrentUserService and IEmpresaRepository
        services.AddTransient<MainViewModel>(sp => new MainViewModel(
            sp,
            sp.GetRequiredService<IThemeService>(),
            sp.GetRequiredService<ICurrentUserService>(),
            sp.GetRequiredService<IEmpresaRepository>()
        ));
        services.AddTransient<ClientesViewModel>();
        services.AddTransient<OrdensViewModel>();
        services.AddTransient<PdvViewModel>();
        services.AddTransient<ConfiguracoesViewModel>();
        services.AddTransient<EmConstrucaoViewModel>();
        services.AddTransient<EstoqueViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<TrocaSenhaViewModel>();
        services.AddTransient<UsuariosViewModel>();

        ServiceProvider = services.BuildServiceProvider();
    }

    protected T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

    protected Cliente SeedCliente(string nome, string telefone = "11999999999", string cpfCnpj = "12345678909")
    {
        var cliente = new Cliente
        {
            Nome = nome,
            Telefone = telefone,
            CpfCnpj = cpfCnpj,
            Endereco = $"Rua Teste, 123 - Centro, São Paulo/SP - CEP: 01000000"
        };
        DbContext.Clientes.Add(cliente);
        DbContext.SaveChanges();
        return cliente;
    }

    protected void SeedEmpresa()
    {
        // Clear existing empresa data first
        DbContext.DadosEmpresa.RemoveRange(DbContext.DadosEmpresa);
        DbContext.SaveChanges();

        var empresa = new Empresa
        {
            Cnpj = "12.345.678/0001-90",
            RazaoSocial = "Empresa Teste LTDA",
            NomeFantasia = "Empresa Teste",
            Telefone = "1133334444",
            Cep = "01000000",
            Logradouro = "Rua das Empresas",
            Numero = "100",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP",
            TipoOsIndex = 0,
            OrientacaoIndex = 0,
            TamanhoPapelIndex = 0
        };
        DbContext.DadosEmpresa.Add(empresa);
        DbContext.SaveChanges();
    }

    protected OrdemServico SeedOrdemServico(Cliente cliente, StatusOS status = StatusOS.Aberta)
    {
        var os = new OrdemServico
        {
            ClienteId = cliente.Id,
            Cliente = cliente,
            Equipamento = "Notebook Dell",
            Marca = "Dell",
            NumeroSerie = "SN123456",
            EmGarantia = false,
            DefeitoRelatado = "Não liga",
            LaudoTecnico = "Troca de fonte",
            Status = status,
            Itens = new List<ItemOS>
            {
                new() { Descricao = "Fonte 90W", Quantidade = 1, ValorUnitario = 150.00m }
            }
        };
        DbContext.Ordens.Add(os);
        DbContext.SaveChanges();
        return os;
    }

    public void Dispose()
    {
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Clears the change tracker to force fresh queries from the database
    /// </summary>
    protected void ClearChangeTracker()
    {
        DbContext.ChangeTracker.Clear();
    }
}