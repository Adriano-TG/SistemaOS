using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SistemaOS.App.Helpers;
using SistemaOS.App.Services;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaOS.App.ViewModels;

public partial class PdvViewModel : ObservableObject
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IOrdemServicoRepository _ordemRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IEstoqueService _estoqueService;
    private readonly IUIStateService _uiState;
    private readonly IWhatsAppService _whatsApp;
    private readonly PdfService _pdf;

    // Produto sendo digitado no campo de busca
    [ObservableProperty]
    private string _codigoBarras = string.Empty;

    // Cliente selecionado (opcional)
    [ObservableProperty]
    private Cliente? _clienteSelecionado;

    // Lista de itens do carrinho
    public ObservableCollection<ItemPdv> Carrinho { get; } = new();

    // Totais
    [ObservableProperty]
    private decimal _subTotal = 0;

    [ObservableProperty]
    private decimal _desconto = 0;

    [ObservableProperty]
    private decimal _total = 0;

    // Pagamento
    [ObservableProperty]
    private decimal _valorRecebido = 0;

    [ObservableProperty]
    private decimal _troco = 0;

    [ObservableProperty]
    private string _formaPagamento = "Dinheiro";

    [ObservableProperty]
    private bool _pagamentoMisto = false;

    // Mensagens
    [ObservableProperty]
    private string _mensagem = string.Empty;

    [ObservableProperty]
    private bool _mensagemErro = false;

    // Foco no campo de código
    public event Action? FocarCodigoBarras;

    public string[] FormasPagamento { get; } = new[] { "Dinheiro", "Cartão Débito", "Cartão Crédito", "PIX", "Misto" };

    public PdvViewModel(
        IProdutoRepository produtoRepository,
        IOrdemServicoRepository ordemRepository,
        IClienteRepository clienteRepository,
        IEstoqueService estoqueService,
        IUIStateService uiState,
        IWhatsAppService whatsApp,
        PdfService pdf)
    {
        _produtoRepository = produtoRepository;
        _ordemRepository = ordemRepository;
        _clienteRepository = clienteRepository;
        _estoqueService = estoqueService;
        _uiState = uiState;
        _whatsApp = whatsApp;
        _pdf = pdf;

        // Atualiza totais quando carrinho muda
        Carrinho.CollectionChanged += (s, e) => AtualizarTotais();

        // Foco inicial
        _ = Task.Delay(100).ContinueWith(_ => FocarCodigoBarras?.Invoke());

        // Verifica se há cliente pendente vindo da seleção (padrão ClienteIdPendente)
        if (_uiState.ClienteIdPendente.HasValue)
        {
            _ = CarregarClientePendenteAsync(_uiState.ClienteIdPendente.Value);
            _uiState.ClienteIdPendente = null;
        }
    }

    private async Task CarregarClientePendenteAsync(int clienteId)
    {
        try
        {
            var cliente = await _clienteRepository.GetByIdAsync(clienteId);
            if (cliente != null)
            {
                ClienteSelecionado = cliente;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MostrarSucesso($"Cliente selecionado: {cliente.Nome}");
                    FocarCodigoBarras?.Invoke();
                });
            }
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar cliente: {ex.Message}");
        }
    }

    // Enter no campo de código de barras = adicionar produto
    partial void OnCodigoBarrasChanged(string value)
    {
        if (value.EndsWith("\n") || value.EndsWith("\r"))
        {
            var codigo = value.TrimEnd('\n', '\r');
            CodigoBarras = string.Empty;
            _ = AdicionarProdutoAsync(codigo);
        }
    }

    [RelayCommand]
    private async Task AdicionarProdutoAsync(string? codigo = null)
    {
        var codigoBusca = codigo ?? CodigoBarras;
        if (string.IsNullOrWhiteSpace(codigoBusca))
        {
            FocarCodigoBarras?.Invoke();
            return;
        }

        CodigoBarras = string.Empty;

        try
        {
            var codigoLimpo = new string(codigoBusca.Where(char.IsDigit).ToArray());

            // Busca por código, código de barras ou nome
            var produto = await _produtoRepository.GetByCodigoAsync(codigoBusca);

            if (produto == null)
            {
                MostrarErro("Produto não encontrado: " + codigoBusca);
                return;
            }

            if (produto.QuantidadeAtual <= 0)
            {
                MostrarErro($"Produto sem estoque: {produto.Nome}");
                return;
            }

            // Verifica se já está no carrinho
            var itemExistente = Carrinho.FirstOrDefault(i => i.ProdutoId == produto.Id);
            if (itemExistente != null)
            {
                if (itemExistente.Quantidade >= produto.QuantidadeAtual)
                {
                    MostrarErro($"Estoque insuficiente. Disponível: {produto.QuantidadeAtual}");
                    return;
                }
                itemExistente.Quantidade++;
                itemExistente.AtualizarTotal();
            }
            else
            {
                Carrinho.Add(new ItemPdv
                {
                    ProdutoId = produto.Id,
                    Codigo = produto.Codigo,
                    Nome = produto.Nome,
                    PrecoUnitario = produto.PrecoVenda,
                    Quantidade = 1,
                    EstoqueDisponivel = produto.QuantidadeAtual
                });
            }

            AtualizarTotais();
            MostrarSucesso($"Adicionado: {produto.Nome}");
            FocarCodigoBarras?.Invoke();
        }
        catch (Exception ex)
        {
            MostrarErro("Erro ao adicionar: " + ex.Message);
        }
    }

    [RelayCommand]
    private void RemoverItem(ItemPdv? item)
    {
        if (item != null)
        {
            Carrinho.Remove(item);
            AtualizarTotais();
            MostrarSucesso("Item removido");
        }
    }

    [RelayCommand]
    private void AlterarQuantidade(object? param)
    {
        if (param is not (ItemPdv item, decimal delta)) return;
        if (item == null) return;

        var novaQtd = item.Quantidade + delta;
        if (novaQtd < 1)
        {
            RemoverItem(item);
            return;
        }

        if (novaQtd > item.EstoqueDisponivel)
        {
            MostrarErro($"Estoque insuficiente. Máximo: {item.EstoqueDisponivel}");
            return;
        }

        item.Quantidade = novaQtd;
        item.AtualizarTotal();
        AtualizarTotais();
    }

    [RelayCommand]
    private async Task SelecionarClienteAsync()
    {
        // Marca que estamos selecionando cliente para o PDV
        _uiState.SelecionandoParaPdv = true;
        _uiState.CadastrandoDaOS = false;
        _uiState.ClienteIdPendente = null;

        // Navega para a tela de Clientes
        WeakReferenceMessenger.Default.Send(new MudarTelaMessage("Clientes"));
    }

    [RelayCommand]
    private void LimparCliente()
    {
        ClienteSelecionado = null;
        MostrarSucesso("Cliente removido");
    }

    [RelayCommand]
    private void LimparCarrinho()
    {
        Carrinho.Clear();
        ClienteSelecionado = null;
        Desconto = 0;
        ValorRecebido = 0;
        FormaPagamento = "Dinheiro";
        AtualizarTotais();
        MostrarSucesso("Carrinho limpo");
        FocarCodigoBarras?.Invoke();
    }

    // Atualiza totais quando valor recebido ou desconto muda
    partial void OnValorRecebidoChanged(decimal value) => AtualizarTroco();
    partial void OnDescontoChanged(decimal value) => AtualizarTotais();
    partial void OnFormaPagamentoChanged(string value) => AtualizarTroco();

    private void AtualizarTotais()
    {
        SubTotal = Carrinho.Sum(i => i.Total);
        Total = Math.Max(0, SubTotal - Desconto);
        AtualizarTroco();
    }

    private void AtualizarTroco()
    {
        if (FormaPagamento == "Dinheiro" || FormaPagamento == "Misto")
        {
            Troco = Math.Max(0, ValorRecebido - Total);
        }
        else
        {
            Troco = 0;
            ValorRecebido = Total; // Auto-preenche para cartão/PIX
        }
    }

    [RelayCommand]
    private async Task FinalizarVendaAsync()
    {
        if (!Carrinho.Any())
        {
            MostrarErro("Carrinho vazio!");
            return;
        }

        if (FormaPagamento == "Dinheiro" && ValorRecebido < Total)
        {
            MostrarErro("Valor recebido insuficiente!");
            return;
        }

        try
        {
            // Cria a Ordem de Serviço como venda (PDV)
            var os = new OrdemServico
            {
                ClienteId = ClienteSelecionado?.Id ?? 0,
                Equipamento = "VENDA PDV",
                Status = StatusOS.Entregue,
                DataAbertura = DateTime.Now,
                DataConclusao = DateTime.Now,
                LaudoTecnico = $"Venda PDV - {FormaPagamento}" +
                               (ClienteSelecionado != null ? $" - Cliente: {ClienteSelecionado.Nome}" : "")
            };

            foreach (var item in Carrinho)
            {
                os.Itens.Add(new ItemOS
                {
                    ProdutoId = item.ProdutoId,
                    Descricao = item.Nome,
                    Quantidade = item.Quantidade,
                    ValorUnitario = item.PrecoUnitario
                });
            }

            await _ordemRepository.AddAsync(os);

            // Baixa estoque usando o EstoqueService
            await _estoqueService.BaixarEstoqueAsync(os.Id);

            // Imprime cupom
            await ImprimirCupomAsync(os);

            // Notifica WhatsApp se cliente tem telefone
            if (ClienteSelecionado != null && !string.IsNullOrWhiteSpace(ClienteSelecionado.WhatsApp))
            {
                _ = EnviarComprovanteWhatsApp(os);
            }

            MostrarSucesso($"Venda finalizada! OS #{os.Id}");
            LimparCarrinho();
        }
        catch (Exception ex)
        {
            MostrarErro("Erro ao finalizar: " + ex.Message);
        }
    }

    private async Task ImprimirCupomAsync(OrdemServico os)
    {
        try
        {
            // O PdfService já busca os dados da empresa internamente
            var caminho = await _pdf.GerarCupomPdvAsync(os, ClienteSelecionado, FormaPagamento, ValorRecebido, Troco);
            PdfService.Abrir(caminho);
        }
        catch { /* silencioso */ }
    }

    private async Task EnviarComprovanteWhatsApp(OrdemServico os)
    {
        try
        {
            var telefone = ClienteSelecionado?.WhatsApp ?? ClienteSelecionado?.Telefone;
            if (string.IsNullOrWhiteSpace(telefone)) return;

            var msg = $"🧾 *Comprovante de Venda - Sistema OS*\n\n" +
                      $"Venda #{os.Id} - {os.DataAbertura:dd/MM/yyyy HH:mm}\n" +
                      $"Cliente: {ClienteSelecionado?.Nome}\n\n";

            foreach (var item in os.Itens)
            {
                msg += $"{item.Quantidade:0.##}x {item.Descricao} - R$ {item.Total:N2}\n";
            }

            msg += $"\nTotal: R$ {os.ValorTotal:N2}\n";
            msg += $"Pagamento: {FormaPagamento}\n";

            if (FormaPagamento == "Dinheiro")
                msg += $"Recebido: R$ {ValorRecebido:N2} | Troco: R$ {Troco:N2}\n";

            msg += "\nObrigado pela preferência! 🙏";

            await _whatsApp.SendTextAsync(telefone, msg);
        }
        catch { /* silencioso */ }
    }

    private void MostrarSucesso(string msg)
    {
        Mensagem = msg;
        MensagemErro = false;
        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            if (Mensagem == msg) Mensagem = string.Empty;
        });
    }

    private void MostrarErro(string msg)
    {
        Mensagem = msg;
        MensagemErro = true;
        _ = Task.Delay(5000).ContinueWith(_ =>
        {
            if (Mensagem == msg) Mensagem = string.Empty;
        });
    }
}

// Item do carrinho do PDV
public partial class ItemPdv : ObservableObject
{
    public int ProdutoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;

    [ObservableProperty]
    private decimal _precoUnitario;

    [ObservableProperty]
    private decimal _quantidade = 1;

    [ObservableProperty]
    private decimal _estoqueDisponivel;

    [ObservableProperty]
    private decimal _total;

    partial void OnQuantidadeChanged(decimal value) => AtualizarTotal();
    partial void OnPrecoUnitarioChanged(decimal value) => AtualizarTotal();

    public void AtualizarTotal() => Total = Quantidade * PrecoUnitario;
}