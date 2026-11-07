using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaOS.App.Validators;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace SistemaOS.App.ViewModels;

public partial class UsuariosViewModel : ObservableObject
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAuthService _authService;
    private readonly UsuarioValidator _validator;

    public ObservableCollection<Usuario> Usuarios { get; } = new();

    [ObservableProperty] private Usuario? _usuarioSelecionado;
    [ObservableProperty] private string _mensagem = string.Empty;
    [ObservableProperty] private bool _mensagemErro = false;
    [ObservableProperty] private bool _carregando = false;
    [ObservableProperty] private bool _editando = false;

    // Formulário
    [ObservableProperty] private string _nome = string.Empty;
    [ObservableProperty] private string _login = string.Empty;
    [ObservableProperty] private string _senha = string.Empty;
    [ObservableProperty] private string _confirmarSenha = string.Empty;
    [ObservableProperty] private RoleUsuario _role = RoleUsuario.Atendente;
    [ObservableProperty] private bool _ativo = true;
    [ObservableProperty] private bool _primeiroLogin = true;

    public RoleUsuario[] RolesDisponiveis => Enum.GetValues<RoleUsuario>();

    public UsuariosViewModel(IUsuarioRepository usuarioRepository, IAuthService authService, UsuarioValidator validator)
    {
        _usuarioRepository = usuarioRepository;
        _authService = authService;
        _validator = validator;
    }

    public async Task CarregarAsync()
    {
        try
        {
            Carregando = true;
            var lista = await _usuarioRepository.GetAllAsync();
            Usuarios.Clear();
            foreach (var u in lista.OrderBy(u => u.Nome))
                Usuarios.Add(u);
            Mensagem = $"{Usuarios.Count} usuário(s) carregado(s)";
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar usuários: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    partial void OnUsuarioSelecionadoChanged(Usuario? value)
    {
        if (value is null)
        {
            LimparFormulario();
            return;
        }

        Editando = true;
        Nome = value.Nome;
        Login = value.Login;
        Senha = string.Empty; // Não mostra hash
        ConfirmarSenha = string.Empty;
        Role = value.Role;
        Ativo = value.Ativo;
        PrimeiroLogin = value.PrimeiroLogin;
        Mensagem = string.Empty;
    }

    [RelayCommand]
    private void Novo()
    {
        UsuarioSelecionado = null;
        Editando = false;
        LimparFormulario();
        Nome = string.Empty;
        Login = string.Empty;
        Senha = string.Empty;
        ConfirmarSenha = string.Empty;
        Role = RoleUsuario.Atendente;
        Ativo = true;
        PrimeiroLogin = true;
        Mensagem = "Preencha os dados do novo usuário";
    }

    private void LimparFormulario()
    {
        Nome = string.Empty;
        Login = string.Empty;
        Senha = string.Empty;
        ConfirmarSenha = string.Empty;
        Role = RoleUsuario.Atendente;
        Ativo = true;
        PrimeiroLogin = true;
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        Mensagem = string.Empty;

        // Validações básicas
        if (string.IsNullOrWhiteSpace(Nome))
        {
            MostrarErro("Informe o nome do usuário");
            return;
        }

        if (string.IsNullOrWhiteSpace(Login))
        {
            MostrarErro("Informe o login");
            return;
        }

        // Se é novo usuário ou está alterando a senha, validar senha
        var isNovo = UsuarioSelecionado == null;
        var alterandoSenha = !string.IsNullOrWhiteSpace(Senha);

        if (isNovo || alterandoSenha)
        {
            if (string.IsNullOrWhiteSpace(Senha))
            {
                MostrarErro("Informe a senha");
                return;
            }

            if (Senha.Length < 8)
            {
                MostrarErro("A senha deve ter no mínimo 8 caracteres");
                return;
            }

            if (Senha != ConfirmarSenha)
            {
                MostrarErro("A confirmação da senha não confere");
                return;
            }
        }

        // Verificar login duplicado
        var existente = await _usuarioRepository.GetByLoginAsync(Login);
        if (existente != null && (isNovo || existente.Id != UsuarioSelecionado?.Id))
        {
            MostrarErro("Já existe um usuário com este login");
            return;
        }

        try
        {
            Carregando = true;

            if (isNovo)
            {
                var novoUsuario = new Usuario
                {
                    Nome = Nome.Trim(),
                    Login = Login.Trim(),
                    SenhaHash = _authService.HashSenha(Senha),
                    Role = Role,
                    Ativo = Ativo,
                    DataCadastro = DateTime.Now,
                    PrimeiroLogin = PrimeiroLogin
                };

                await _usuarioRepository.AddAsync(novoUsuario);
                Usuarios.Add(novoUsuario);
                MostrarSucesso("Usuário criado com sucesso!");
            }
            else
            {
                var usuario = UsuarioSelecionado!;
                usuario.Nome = Nome.Trim();
                usuario.Login = Login.Trim();
                usuario.Role = Role;
                usuario.Ativo = Ativo;
                usuario.PrimeiroLogin = PrimeiroLogin;

                if (alterandoSenha)
                {
                    usuario.SenhaHash = _authService.HashSenha(Senha);
                    usuario.PrimeiroLogin = false;
                }

                await _usuarioRepository.UpdateAsync(usuario);
                MostrarSucesso("Usuário atualizado com sucesso!");
            }

            // Recarregar lista para garantir dados atuais
            await CarregarAsync();
            Editando = false;
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao salvar: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (UsuarioSelecionado == null)
        {
            MostrarErro("Selecione um usuário para excluir");
            return;
        }

        // Não permitir excluir a si mesmo
        // TODO: Verificar usuário logado via ICurrentUserService

        try
        {
            Carregando = true;
            await _usuarioRepository.DeleteAsync(UsuarioSelecionado);
            Usuarios.Remove(UsuarioSelecionado);
            UsuarioSelecionado = null;
            LimparFormulario();
            Editando = false;
            MostrarSucesso("Usuário excluído com sucesso!");
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao excluir: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    [RelayCommand]
    private async Task ResetarSenhaAsync()
    {
        if (UsuarioSelecionado == null)
        {
            MostrarErro("Selecione um usuário");
            return;
        }

        // Gerar senha temporária
        var senhaTemp = GerarSenhaTemporaria();
        var hash = _authService.HashSenha(senhaTemp);

        try
        {
            Carregando = true;
            var usuario = UsuarioSelecionado;
            usuario.SenhaHash = hash;
            usuario.PrimeiroLogin = true;
            await _usuarioRepository.UpdateAsync(usuario);

            MostrarSucesso($"Senha resetada! Nova senha temporária: {senhaTemp} (usuário deve trocar no próximo login)");
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao resetar senha: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    private static string GerarSenhaTemporaria(int length = 10)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new byte[length];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(random);
        var result = new System.Text.StringBuilder(length);
        foreach (var b in random)
            result.Append(chars[b % chars.Length]);
        return result.ToString();
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