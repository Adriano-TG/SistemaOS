using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SistemaOS.App.Helpers;
using SistemaOS.App.Services;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaOS.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly IThemeService _tema;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmpresaRepository _empresaRepository;

    [ObservableProperty]
    private ObservableObject? _telaAtual;

    public MainViewModel(
        IServiceProvider services,
        IThemeService tema,
        ICurrentUserService currentUserService,
        IEmpresaRepository empresaRepository)
    {
        _services = services;
        _tema = tema;
        _currentUserService = currentUserService;
        _empresaRepository = empresaRepository;

        // 👇 O MENSAGEIRO: Fica à escuta de pedidos para mudar de ecrã lançados por outras janelas
        WeakReferenceMessenger.Default.Register<MudarTelaMessage>(this, (r, m) =>
        {
            if (m.Value == "Ordens")
                IrParaOrdens();
            else if (m.Value == "Clientes")
                IrParaClientes();
            else if (m.Value == "Pdv")
                IrParaPdv();
        });

        // Inicialização assíncrona - verifica empresa e abre tela apropriada
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        try
        {
            var empresa = await _empresaRepository.GetEmpresaAsync();
            if (empresa == null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => IrParaConfiguracoes());
                return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar empresa: {ex.Message}");
        }

        // Se chegou aqui, há empresa cadastrada - abre Dashboard por padrão
        await Application.Current.Dispatcher.InvokeAsync(() => IrParaDashboard());
    }

    [RelayCommand]
    private void IrParaClientes() =>
        TelaAtual = _services.GetRequiredService<ClientesViewModel>();

    [RelayCommand]
    private void IrParaOrdens() =>
     TelaAtual = _services.GetRequiredService<OrdensViewModel>();

    [RelayCommand]
    private void IrParaPdv() =>
        TelaAtual = _services.GetRequiredService<PdvViewModel>();

    [RelayCommand]
    private void IrParaFinanceiro() =>
        TelaAtual = new EmConstrucaoViewModel("Financeiro");

    [RelayCommand]
    private void IrParaConfiguracoes()
    {
        TelaAtual = _services.GetRequiredService<ConfiguracoesViewModel>();
    }

    [RelayCommand]
    private void IrParaEstoque() =>
        TelaAtual = _services.GetRequiredService<EstoqueViewModel>();

    [RelayCommand]
    private void IrParaUsuarios() =>
        TelaAtual = _services.GetRequiredService<UsuariosViewModel>();

    [RelayCommand]
    private void IrParaDashboard() =>
        TelaAtual = _services.GetRequiredService<DashboardViewModel>();

    [RelayCommand]
    private void AlternarTema() => _tema.AlternarTema();

    [RelayCommand]
    private void MudarCor(string? hex)
    {
        if (!string.IsNullOrEmpty(hex))
            _tema.DefinirCor(hex);
    }

    [RelayCommand]
    private void IrParaInicio()
    {
        // Ao definir como null, a área central fica vazia novamente!
        TelaAtual = null;
    }

    [RelayCommand]
    private void Logout()
    {
        _currentUserService.Logout();

        // Fecha a janela principal e abre a de login
        Application.Current.Dispatcher.Invoke(() =>
        {
            var loginWindow = _services.GetRequiredService<Views.LoginView>();
            loginWindow.Show();

            var mainWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            mainWindow?.Close();
        });
    }

}