using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaOS.Core.Interfaces;
using System.Windows;

namespace SistemaOS.App.ViewModels;

public partial class TrocaSenhaViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty]
    private string _senhaAtual = string.Empty;

    [ObservableProperty]
    private string _novaSenha = string.Empty;

    [ObservableProperty]
    private string _confirmarNovaSenha = string.Empty;

    [ObservableProperty]
    private string _mensagem = string.Empty;

    [ObservableProperty]
    private bool _mensagemErro = false;

    [ObservableProperty]
    private bool _carregando = false;

    public TrocaSenhaViewModel(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    private int UsuarioId => _currentUser.UsuarioLogado?.Id ?? 0;

    [RelayCommand]
    private async Task SalvarAsync()
    {
        Mensagem = string.Empty;

        // Validações
        if (string.IsNullOrWhiteSpace(SenhaAtual))
        {
            MostrarErro("Informe a senha atual");
            return;
        }

        if (string.IsNullOrWhiteSpace(NovaSenha))
        {
            MostrarErro("Informe a nova senha");
            return;
        }

        if (NovaSenha.Length < 8)
        {
            MostrarErro("A nova senha deve ter no mínimo 8 caracteres");
            return;
        }

        if (NovaSenha != ConfirmarNovaSenha)
        {
            MostrarErro("A confirmação da nova senha não confere");
            return;
        }

        if (SenhaAtual == NovaSenha)
        {
            MostrarErro("A nova senha deve ser diferente da senha atual");
            return;
        }

        Carregando = true;

        try
        {
            var sucesso = await _authService.AlterarSenhaAsync(UsuarioId, SenhaAtual, NovaSenha);

            if (sucesso)
            {
                MostrarSucesso("Senha alterada com sucesso! Redirecionando...");
                await Task.Delay(1000);

                // Fecha a janela de troca de senha e abre a principal
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = new MainWindow(
                        App.Services!.GetRequiredService<MainViewModel>()
                    );
                    mainWindow.Show();

                    // Fecha a janela de troca de senha
                    var trocaSenhaWindow = Application.Current.Windows
                        .OfType<Window>()
                        .FirstOrDefault(w => w.DataContext == this);
                    trocaSenhaWindow?.Close();
                });
            }
            else
            {
                MostrarErro("Senha atual incorreta");
            }
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao alterar senha: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        // Fecha a aplicação se cancelar a troca de senha obrigatória
        Application.Current.Shutdown();
    }

    private void MostrarSucesso(string msg)
    {
        Mensagem = msg;
        MensagemErro = false;
    }

    private void MostrarErro(string msg)
    {
        Mensagem = msg;
        MensagemErro = true;
    }
}