using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using FluentValidation;
using Serilog;
using SistemaOS.App.ViewModels;
using SistemaOS.App.Services;
using SistemaOS.App.Validators;
using SistemaOS.Infrastructure.Data;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Services;
using SistemaOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace SistemaOS.App;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SistemaOS", "startup.log");

    public App()
    {
        // Configura Serilog PRIMEIRO para capturar logs de inicialização
        SerilogConfig.Configure();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"{DateTime.Now}: App constructor started\n");

            // Configura cultura brasileira (pt-BR) para formatação de números (vírgula como separador decimal)
            var culturaBR = new CultureInfo("pt-BR");
            CultureInfo.DefaultThreadCurrentCulture = culturaBR;
            CultureInfo.DefaultThreadCurrentUICulture = culturaBR;

            var services = new ServiceCollection();

            // DbContext com Factory para permitir criação de contextos de curta duração
            services.AddDbContextFactory<AppDbContext>();

            services.AddHttpClient();
            services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build());

            // FluentValidation
            services.AddValidatorsFromAssemblyContaining<ClienteValidator>();

            // Repositories (Scoped - usam DbContext via Factory)
            services.AddScoped<IProdutoRepository, ProdutoRepository>();
            services.AddScoped<IClienteRepository, ClienteRepository>();
            services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();

            // Serviços que usam DbContext via Factory -> Transient (criam contextos curtos por operação)
            services.AddTransient<PdfService>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IEstoqueService, EstoqueService>();

            // Serviços Scoped (estado por request/sessão)
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IUIStateService, UIStateService>();

            // Serviços sem DbContext -> Singleton
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<WpfThemeApplier>(); // Aplica o tema no WPF
            services.AddSingleton<ICepService, ViaCepService>();
            services.AddSingleton<CnpjService>();
            services.AddSingleton<IWhatsAppService, EvolutionApiService>();
            services.AddSingleton<ITextFormatterService, TextFormatterService>();
            services.AddSingleton<ILoggerService, SerilogLoggerService>();

            services.AddTransient<MigracaoService>();

            // ViewModels - MainViewModel now requires ICurrentUserService and IEmpresaRepository
            services.AddTransient<MainViewModel>(sp => new MainViewModel(
                sp,
                sp.GetRequiredService<IThemeService>(),
                sp.GetRequiredService<ICurrentUserService>(),
                sp.GetRequiredService<IEmpresaRepository>()
            ));
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<OrdensViewModel>();
            services.AddTransient<PdvViewModel>(sp => new PdvViewModel(
                sp.GetRequiredService<IProdutoRepository>(),
                sp.GetRequiredService<IOrdemServicoRepository>(),
                sp.GetRequiredService<IClienteRepository>(),
                sp.GetRequiredService<IEstoqueService>(),
                sp.GetRequiredService<IUIStateService>(),
                sp.GetRequiredService<IWhatsAppService>(),
                sp.GetRequiredService<PdfService>()
            ));
            services.AddTransient<EmConstrucaoViewModel>();
            services.AddTransient<ConfiguracoesViewModel>();
            services.AddTransient<EstoqueViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<TrocaSenhaViewModel>();
            services.AddTransient<UsuariosViewModel>();
            services.AddTransient<Views.LoginView>(sp => new Views.LoginView(sp.GetRequiredService<LoginViewModel>()));
            services.AddTransient<Views.PdvView>();
            services.AddTransient<Views.TrocaSenhaView>(sp => new Views.TrocaSenhaView(sp.GetRequiredService<TrocaSenhaViewModel>()));
            services.AddTransient<Views.UsuariosView>(sp => new Views.UsuariosView(sp.GetRequiredService<UsuariosViewModel>()));
            services.AddTransient<MainWindow>(sp => new MainWindow(sp.GetRequiredService<MainViewModel>()));

            Services = services.BuildServiceProvider();
            File.AppendAllText(LogPath, $"{DateTime.Now}: App constructor completed, Services built\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText(LogPath, $"{DateTime.Now}: ERROR in App constructor: {ex}\n");
            throw;
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        File.AppendAllText(LogPath, $"{DateTime.Now}: OnStartup started\n");

        try
        {
            // Inicializa o aplicador de tema WPF (conecta ao ThemeService e aplica)
            File.AppendAllText(LogPath, $"{DateTime.Now}: Getting WpfThemeApplier\n");
            _ = Services!.GetRequiredService<WpfThemeApplier>();

            // 👇 A LÓGICA DE AUTO-ATUALIZAÇÃO FOI RESTAURADA AQUI 👇
            File.AppendAllText(LogPath, $"{DateTime.Now}: Starting database migration\n");
            try
            {
                // Usar factory para criar contexto de curta duração
                var dbFactory = Services!.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using (var db = dbFactory.CreateDbContext())
                {
                    db.Database.Migrate(); // Cria a tabela Empresa no ficheiro que está a rodar!

                    // Seed: cria usuário admin padrão se não existir nenhum usuário
                    await SeedDefaultAdminAsync(db);
                }
                File.AppendAllText(LogPath, $"{DateTime.Now}: Database migration completed\n");
            }
            catch (Exception ex)
            {
                string errorMsg = $"{DateTime.Now}: ERROR in Migrate: {ex.Message}\n{ex.StackTrace}\n";
                try { File.AppendAllText(LogPath, errorMsg); } catch { }
                System.Diagnostics.Debug.WriteLine(errorMsg);
                throw;
            }

            // Mostra a tela principal diretamente (sem login por enquanto)
            File.AppendAllText(LogPath, $"{DateTime.Now}: Showing MainWindow\n");
            var mainWindow = Services!.GetRequiredService<MainWindow>();
            mainWindow.Show();
            File.AppendAllText(LogPath, $"{DateTime.Now}: MainWindow shown\n");

            // Aquece o Entity Framework em segundo plano, para a primeira tela abrir sem espera
            _ = Task.Run(() =>
            {
                var dbFactory = Services!.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var db = dbFactory.CreateDbContext();
                _ = db.Ordens.AsNoTracking().Include(o => o.Cliente).Take(1).ToList();
            });
        }
        catch (Exception ex)
        {
            string errorMsg = $"{DateTime.Now}: ERROR in OnStartup: {ex}\n";
            try { File.AppendAllText(LogPath, errorMsg); } catch { }
            System.Diagnostics.Debug.WriteLine(errorMsg);

            try
            {
                MessageBox.Show($"Erro ao iniciar aplicação:\n{ex.Message}\n\nStackTrace:\n{ex.StackTrace}",
                    "Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
                // Ignore if MessageBox fails
            }
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Flush Serilog buffers
        SerilogConfig.CloseAndFlush();
        base.OnExit(e);
    }

    private static async Task SeedDefaultAdminAsync(AppDbContext db)
    {
        if (!await db.Usuarios.AnyAsync())
        {
            // Lê senha padrão do appsettings.json (opcional)
            var config = Services!.GetRequiredService<IConfiguration>();
            var senhaConfig = config["Admin:DefaultPassword"];

            // AuthService usa factory internamente
            var authService = new AuthService(Services.GetRequiredService<IDbContextFactory<AppDbContext>>());

            string senhaInicial;
            if (!string.IsNullOrWhiteSpace(senhaConfig))
            {
                senhaInicial = senhaConfig;
            }
            else
            {
                // Gerar senha aleatória segura (16 caracteres)
                senhaInicial = GerarSenhaAleatoria(16);
            }

            var senhaHash = authService.HashSenha(senhaInicial);

            var admin = new Usuario
            {
                Nome = "Administrador",
                Login = "admin",
                SenhaHash = senhaHash,
                Role = RoleUsuario.Admin,
                Ativo = true,
                DataCadastro = DateTime.Now,
                PrimeiroLogin = true // Força troca de senha no primeiro acesso
            };

            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();

            // Log da senha inicial para o administrador poder acessar
            File.AppendAllText(LogPath, $"{DateTime.Now}: Default admin user created (login: admin, senha inicial: {senhaInicial} - OBRIGATÓRIO TROCAR NO PRIMEIRO LOGIN)\n");
        }
    }

    /// <summary>
    /// Gera uma senha aleatória segura com caracteres alfanuméricos e especiais
    /// </summary>
    private static string GerarSenhaAleatoria(int length = 16)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);

        var result = new StringBuilder(length);
        foreach (var b in random)
        {
            result.Append(chars[b % chars.Length]);
        }
        return result.ToString();
    }
}