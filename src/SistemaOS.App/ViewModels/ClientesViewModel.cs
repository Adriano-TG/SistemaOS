using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SistemaOS.App.Helpers;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Core.Validations;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaOS.App.ViewModels;

public partial class ClientesViewModel : ObservableObject
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ICepService _cepService;
    private readonly IUIStateService _uiState;

    // Variável interna para saber qual cliente estamos a editar
    private Cliente? _clienteEmEdicao;

    public ObservableCollection<Cliente> Clientes { get; } = new();

    [ObservableProperty]
    private Cliente? _clienteSelecionado;

    // Campos Básicos
    [ObservableProperty] private string _nome = string.Empty;
    [ObservableProperty] private string _telefone = string.Empty;
    [ObservableProperty] private string _cpfCnpj = string.Empty;

    // Campos de Endereço
    [ObservableProperty] private string _cep = string.Empty;
    [ObservableProperty] private string _logradouro = string.Empty;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private string _complemento = string.Empty;
    [ObservableProperty] private string _bairro = string.Empty;
    [ObservableProperty] private string _cidade = string.Empty;
    [ObservableProperty] private string _uf = string.Empty;
    [ObservableProperty] private string _mensagem = string.Empty;
    [ObservableProperty] private string _textoPesquisa = string.Empty;
    [ObservableProperty] private string _whatsApp = string.Empty;

    public ClientesViewModel(IClienteRepository clienteRepository, ICepService cepService, IUIStateService uiState)
    {
        _clienteRepository = clienteRepository;
        _cepService = cepService;
        _uiState = uiState;

        Carregar();
    }

    // Propriedades para bind no XAML (modo seleção)
    public bool EmModoSelecaoPdv => _uiState.SelecionandoParaPdv;
    public bool EmModoSelecaoOS => _uiState.CadastrandoDaOS;

    async partial void OnTextoPesquisaChanged(string value)
    {
        // Aguarda 300 milissegundos antes de pesquisar (Debounce)
        await Task.Delay(300);

        if (TextoPesquisa == value)
        {
            Carregar();
        }
    }

    private async void Carregar()
    {
        Clientes.Clear();
        IEnumerable<Cliente> resultados;

        if (!string.IsNullOrWhiteSpace(TextoPesquisa))
        {
            var termo = TextoPesquisa.ToLower();
            // Search by name, CPF/CNPJ, or phone
            resultados = await _clienteRepository.SearchByNomeAsync(termo);
            // Also search by CPF/CNPJ and phone - we could add these methods to the repository
            // For now, filter in memory for CPF/CNPJ and phone
            var todos = await _clienteRepository.GetAtivosAsync();
            resultados = todos.Where(c =>
                c.Nome.ToLower().Contains(termo) ||
                (c.CpfCnpj != null && c.CpfCnpj.Contains(termo)) ||
                (c.Telefone != null && c.Telefone.Contains(termo)))
                .OrderBy(c => c.Nome)
                .Take(100);
        }
        else
        {
            resultados = (await _clienteRepository.GetAtivosAsync()).Take(100);
        }

        foreach (var c in resultados)
        {
            Clientes.Add(c);
        }
    }

    [RelayCommand]
    private void Editar(Cliente clienteClicado)
    {
        if (clienteClicado is null) return;

        // Se estamos em modo seleção para PDV, define ClienteIdPendente e volta
        if (_uiState.SelecionandoParaPdv)
        {
            _uiState.ClienteIdPendente = clienteClicado.Id;
            _uiState.SelecionandoParaPdv = false;
            WeakReferenceMessenger.Default.Send(new MudarTelaMessage("Pdv"));
            return;
        }

        _clienteEmEdicao = clienteClicado;

        Nome = clienteClicado.Nome;
        Telefone = clienteClicado.Telefone ?? string.Empty;
        CpfCnpj = clienteClicado.CpfCnpj ?? string.Empty;
        WhatsApp = clienteClicado.WhatsApp ?? string.Empty;

        // Zera os campos primeiro
        Cep = Logradouro = Numero = Complemento = Bairro = Cidade = Uf = string.Empty;

        var endereco = clienteClicado.Endereco ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(endereco))
        {
            // 1. Extrai o CEP (Ex: "... - CEP: 12345678")
            var idxCep = endereco.LastIndexOf(" - CEP: ");
            if (idxCep >= 0)
            {
                Cep = endereco.Substring(idxCep + 8).Trim();
                endereco = endereco.Substring(0, idxCep); // Remove o CEP do texto
            }

            // 2. Extrai Bairro, Cidade e UF (Ex: "... - Bairro, Cidade/UF")
            var idxBairro = endereco.LastIndexOf(" - ");
            if (idxBairro >= 0)
            {
                var bairroCidadeUf = endereco.Substring(idxBairro + 3);
                endereco = endereco.Substring(0, idxBairro);

                var partesBairro = bairroCidadeUf.Split(',');
                Bairro = partesBairro[0].Trim();

                if (partesBairro.Length > 1)
                {
                    var cidadeUf = partesBairro[1].Split('/');
                    Cidade = cidadeUf[0].Trim();
                    if (cidadeUf.Length > 1) Uf = cidadeUf[1].Trim();
                }
            }

            // 3. Extrai Complemento se existir (Ex: "... - Sala 2")
            var idxComp = endereco.LastIndexOf(" - ");
            if (idxComp >= 0)
            {
                Complemento = endereco.Substring(idxComp + 3).Trim();
                endereco = endereco.Substring(0, idxComp); // Resta apenas Logradouro e Número
            }

            // 4. Extrai Logradouro e Número (Ex: "Rua X, 123")
            var idxNum = endereco.LastIndexOf(", ");
            if (idxNum >= 0)
            {
                Numero = endereco.Substring(idxNum + 2).Trim();
                Logradouro = endereco.Substring(0, idxNum).Trim();
            }
            else
            {
                // Se for um endereço legado sem formato reconhecido, lança tudo no Logradouro para não perder os dados
                Logradouro = endereco.Trim();
            }
        }

        Mensagem = $"A editar: {clienteClicado.Nome}. Altere os dados e clique em Salvar.";
    }

    [RelayCommand]
    private void Selecionar(Cliente clienteClicado)
    {
        if (clienteClicado is null) return;

        if (_uiState.SelecionandoParaPdv)
        {
            _uiState.ClienteIdPendente = clienteClicado.Id;
            _uiState.SelecionandoParaPdv = false;
            WeakReferenceMessenger.Default.Send(new MudarTelaMessage("Pdv"));
        }
        else if (_uiState.CadastrandoDaOS)
        {
            _uiState.ClienteIdPendente = clienteClicado.Id;
            _uiState.CadastrandoDaOS = false;
            WeakReferenceMessenger.Default.Send(new MudarTelaMessage("Ordens"));
        }
    }

    async partial void OnCepChanged(string value)
    {
        var cepLimpo = new string(value?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());

        if (cepLimpo.Length == 8)
        {
            Mensagem = "A procurar o endereço...";
            var resultado = await _cepService.BuscarCepAsync(cepLimpo);
            if (resultado != null)
            {
                Logradouro = resultado.Logradouro;
                Bairro = resultado.Bairro;
                Cidade = resultado.Localidade;
                Uf = resultado.Uf;
                Mensagem = "Endereço encontrado!";
            }
            else
            {
                Mensagem = "CEP não encontrado.";
            }
        }
    }

    private void LimparCampos()
    {
        _clienteEmEdicao = null;
        ClienteSelecionado = null;
        Nome = Telefone = CpfCnpj = Cep = Logradouro = Numero = Complemento = Bairro = Cidade = Uf = string.Empty;
        Mensagem = string.Empty;
        WhatsApp = string.Empty;
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        Mensagem = string.Empty;

        // 1. Validações Iniciais
        if (string.IsNullOrWhiteSpace(Nome))
        {
            Mensagem = "O nome do cliente é obrigatório.";
            return;
        }

        var docLimpo = CpfCnpj?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(docLimpo))
        {
            var soNumeros = new string(docLimpo.Where(char.IsDigit).ToArray());
            if (soNumeros.Length > 11 && !DocumentoValidator.IsCnpjValido(docLimpo))
            {
                Mensagem = "O CNPJ informado é inválido.";
                return;
            }
            else if (soNumeros.Length <= 11 && !DocumentoValidator.IsCpfValido(docLimpo))
            {
                Mensagem = "O CPF informado é inválido.";
                return;
            }
        }

        var enderecoCompleto = string.Empty;
        if (!string.IsNullOrWhiteSpace(Logradouro))
        {
            enderecoCompleto = $"{Logradouro}, {Numero}";
            if (!string.IsNullOrWhiteSpace(Complemento)) enderecoCompleto += $" - {Complemento}";
            enderecoCompleto += $" - {Bairro}, {Cidade}/{Uf} - CEP: {Cep}";
        }

        Cliente? clienteSalvo = null;

        // 2. Gravação (Novo ou Edição)
        if (_clienteEmEdicao is null)
        {
            var novoCliente = new Cliente
            {
                Nome = Nome.Trim(),
                Telefone = Telefone?.Trim() ?? string.Empty,
                WhatsApp = WhatsApp?.Trim() ?? string.Empty,
                CpfCnpj = docLimpo,
                Endereco = enderecoCompleto
            };
            await _clienteRepository.AddAsync(novoCliente);
            clienteSalvo = novoCliente;
            Mensagem = "Cliente gravado com sucesso!";
        }
        else
        {
            _clienteEmEdicao.Nome = Nome.Trim();
            _clienteEmEdicao.Telefone = Telefone?.Trim() ?? string.Empty;
            _clienteEmEdicao.WhatsApp = WhatsApp?.Trim() ?? string.Empty;
            _clienteEmEdicao.CpfCnpj = docLimpo;

            if (!string.IsNullOrWhiteSpace(enderecoCompleto))
                _clienteEmEdicao.Endereco = enderecoCompleto;

            await _clienteRepository.UpdateAsync(_clienteEmEdicao);
            clienteSalvo = _clienteEmEdicao;
            Mensagem = "Cliente atualizado com sucesso!";
        }

        // 3. Limpa a tela
        var msgBackup = Mensagem;
        LimparCampos();
        Mensagem = msgBackup;
        await CarregarAsync();

        // 4. A Mágica do Retorno
        if (_uiState.CadastrandoDaOS && clienteSalvo != null)
        {
            _uiState.CadastrandoDaOS = false;
            _uiState.ClienteIdPendente = clienteSalvo.Id;

            // Só precisamos pedir para mudar a tela, o resto acontece do outro lado
            WeakReferenceMessenger.Default.Send(new SistemaOS.App.Helpers.MudarTelaMessage("Ordens"));
        }
    }

    private async Task CarregarAsync()
    {
        Clientes.Clear();
        IEnumerable<Cliente> resultados;

        if (!string.IsNullOrWhiteSpace(TextoPesquisa))
        {
            var termo = TextoPesquisa.ToLower();
            var todos = await _clienteRepository.GetAtivosAsync();
            resultados = todos.Where(c =>
                c.Nome.ToLower().Contains(termo) ||
                (c.CpfCnpj != null && c.CpfCnpj.Contains(termo)) ||
                (c.Telefone != null && c.Telefone.Contains(termo)))
                .OrderBy(c => c.Nome)
                .Take(100);
        }
        else
        {
            resultados = (await _clienteRepository.GetAtivosAsync()).Take(100);
        }

        foreach (var c in resultados)
        {
            Clientes.Add(c);
        }
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        // Verifica se há algum cliente selecionado para edição
        if (_clienteEmEdicao is null)
        {
            Mensagem = "Dê um duplo clique no cliente que deseja excluir primeiro.";
            return;
        }

        // Mostra a caixa de aviso na tela
        var resposta = MessageBox.Show(
            $"Tem a certeza de que deseja excluir definitivamente o cliente '{_clienteEmEdicao.Nome}'?\n\nEsta ação não pode ser desfeita.",
            "Confirmar Exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        // Se o utilizador clicar em "Sim" (Yes)
        if (resposta == MessageBoxResult.Yes)
        {
            try
            {
                await _clienteRepository.DeleteAsync(_clienteEmEdicao);

                LimparCampos();
                await CarregarAsync();

                Mensagem = "Cliente excluído com sucesso!";
            }
            catch (Exception ex)
            {
                Mensagem = $"Erro ao excluir: {ex.Message}";
            }
        }
    }
}