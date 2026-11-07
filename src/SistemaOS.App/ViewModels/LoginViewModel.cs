using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaOS.App.Services;
using SistemaOS.Core.Interfaces;
using System.Windows;

namespace SistemaOS.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IThemeService _themeService;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _senha = string.Empty;

    [ObservableProperty]
    private string _mensagem = string.Empty;

    [ObservableProperty]
    private bool _carregando = false;

    public LoginViewModel(IAuthService authService, IThemeService themeService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _themeService = themeService;
        _currentUser = currentUser;

        // Carrega o tema salvo
        _themeService.Carregar();
    }

    [RelayCommand]
    private async Task EntrarAsync()
    {
        Mensagem = string.Empty;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Senha))
        {
            Mensagem = "Informe login e senha";
            return;
        }

        Carregando = true;

        try
        {
            var resultado = await _authService.LoginAsync(Login, Senha);

            if (resultado.Success && resultado.Usuario != null)
            {
                // Salva o usuário logado no serviço de usuário atual
                _currentUser.UsuarioLogado = resultado.Usuario;

                // Se requer troca de senha (primeiro login), abre a tela de troca
                if (resultado.RequerTrocaSenha)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var trocaSenhaWindow = App.Services!.GetRequiredService<Views.TrocaSenhaView>();
                        trocaSenhaWindow.Show();

                        // Fecha a janela de login
                        var loginWindow = Application.Current.Windows
                            .OfType<Window>()
                            .FirstOrDefault(w => w.DataContext == this);
                        loginWindow?.Close();
                    });
                }
                else
                {
                    // Navega para a tela principal
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = new MainWindow(
                            App.Services!.GetRequiredService<MainViewModel>()
                        );
                        mainWindow.Show();

                        // Fecha a janela de login
                        var loginWindow = Application.Current.Windows
                            .OfType<Window>()
                            .FirstOrDefault(w => w.DataContext == this);
                        loginWindow?.Close();
                    });
                }
            }
            else
            {
                Mensagem = resultado.Error ?? "Erro ao fazer login";
            }
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro: {ex.Message}";
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        // IsCancel="True" no botão já fecha a janela automaticamente
        // Este comando existe para completar o binding se necessário
        Application.Current.Shutdown();
    }
}