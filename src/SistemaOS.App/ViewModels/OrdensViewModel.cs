using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SistemaOS.App.Helpers;
using SistemaOS.App.Services;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Services;
using System.IO;
using System.Threading;

namespace SistemaOS.App.ViewModels;

public partial class OrdensViewModel : ObservableObject
{
    [ObservableProperty]
    private string _textoPesquisa = string.Empty;

    private CancellationTokenSource? _pesquisaCts;

    async partial void OnTextoPesquisaChanged(string value)
    {
        // Cancela a espera anterior: só pesquisa quando para de digitar
        _pesquisaCts?.Cancel();
        var cts = _pesquisaCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, cts.Token);
            await CarregarOrdensAsync();
        }
        catch (OperationCanceledException)
        {
            // digitou outra letra: ignora
        }
    }

    private readonly IOrdemServicoRepository _ordemRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IEstoqueService _estoqueService;
    private readonly IUIStateService _uiState;
    private readonly PdfService _pdf;
    private readonly IWhatsAppService _whatsApp;
    private OrdemServico? _ordemEmEdicao;

    [ObservableProperty] private ObservableCollection<OrdemServico> _ordens = new();
    [ObservableProperty] private ObservableCollection<Cliente> _clientes = new();

    public ObservableCollection<ItemOS> ItensForm { get; } = new();
    public StatusOS[] StatusDisponiveis { get; } = Enum.GetValues<StatusOS>();

    [ObservableProperty] private OrdemServico? _ordemSelecionada;
    [ObservableProperty] private Cliente? _clienteSelecionado;
    [ObservableProperty] private string _equipamento = string.Empty;
    [ObservableProperty] private string _marca = string.Empty;
    [ObservableProperty] private string _numeroSerie = string.Empty;
    [ObservableProperty] private bool _emGarantia;
    [ObservableProperty] private string _defeitoRelatado = string.Empty;
    [ObservableProperty] private string _laudoTecnico = string.Empty;
    [ObservableProperty] private StatusOS _status = StatusOS.Aberta;

    [ObservableProperty] private string _novoItemDescricao = string.Empty;
    [ObservableProperty] private decimal _novoItemQuantidade = 1;
    [ObservableProperty] private decimal _novoItemValor;
    [ObservableProperty] private ItemOS? _itemSelecionado;

    [ObservableProperty] private decimal _totalForm;
    [ObservableProperty] private string _titulo = "Nova ordem de serviço";
    [ObservableProperty] private string _mensagem = string.Empty;

    public OrdensViewModel(
        IOrdemServicoRepository ordemRepository,
        IClienteRepository clienteRepository,
        IEstoqueService estoqueService,
        IUIStateService uiState,
        PdfService pdf,
        IWhatsAppService whatsApp)
    {
        _ordemRepository = ordemRepository;
        _clienteRepository = clienteRepository;
        _estoqueService = estoqueService;
        _uiState = uiState;
        _pdf = pdf;
        _whatsApp = whatsApp;

        // O ecrã não espera isto terminar para abrir!
        _ = InicializarDadosAsync();

        WeakReferenceMessenger.Default.Register<ClienteSalvoMessage>(this, async (r, m) =>
        {
            await CarregarClientesAsync();
            ClienteSelecionado = Clientes.FirstOrDefault(c => c.Id == m.Value.Id);
        });
    }

    private async Task InicializarDadosAsync()
    {
        await CarregarClientesAsync();
        await CarregarOrdensAsync();

        if (_uiState.ClienteIdPendente.HasValue)
        {
            ClienteSelecionado = Clientes.FirstOrDefault(c => c.Id == _uiState.ClienteIdPendente.Value);
            _uiState.ClienteIdPendente = null;
        }
    }

    // Roda automaticamente quando OrdemSelecionada muda (clique na tabela)
    async partial void OnOrdemSelecionadaChanged(OrdemServico? value)
    {
        if (value is null)
            return;

        // A lista não carrega os itens: busca só os desta O.S.
        var osCompleta = await _ordemRepository.GetByIdWithItensAsync(value.Id);

        if (osCompleta != null)
        {
            CarregarNoFormulario(osCompleta);
        }
    }

    // Buscas assíncronas
    private async Task CarregarClientesAsync()
    {
        var lista = await _clienteRepository.GetAtivosAsync();
        Clientes = new ObservableCollection<Cliente>(lista);
    }

    public async Task CarregarOrdensAsync()
    {
        IEnumerable<OrdemServico> lista;

        if (!string.IsNullOrWhiteSpace(TextoPesquisa))
        {
            var termo = TextoPesquisa.ToLower();
            bool isNumero = int.TryParse(termo, out int idPesquisa);

            // Busca otimizada via repository
            if (isNumero)
            {
                var os = await _ordemRepository.GetByIdWithItensAsync(idPesquisa);
                lista = os != null ? new[] { os } : Enumerable.Empty<OrdemServico>();
            }
            else
            {
                lista = await _ordemRepository.SearchByTermoAsync(termo);
            }
        }
        else
        {
            lista = await _ordemRepository.GetRecentAsync(200);
        }

        Ordens = new ObservableCollection<OrdemServico>(lista);
    }

    private void CarregarNoFormulario(OrdemServico os)
    {
        _ordemEmEdicao = os;
        Titulo = $"Ordem de serviço nº {os.Id}";

        ClienteSelecionado = Clientes.FirstOrDefault(c => c.Id == os.ClienteId);
        Equipamento = os.Equipamento;
        Marca = os.Marca ?? string.Empty;
        NumeroSerie = os.NumeroSerie ?? string.Empty;
        EmGarantia = os.EmGarantia;
        DefeitoRelatado = os.DefeitoRelatado ?? string.Empty;
        LaudoTecnico = os.LaudoTecnico ?? string.Empty;
        Status = os.Status;

        ItensForm.Clear();
        foreach (var i in os.Itens)
        {
            ItensForm.Add(new ItemOS
            {
                Descricao = i.Descricao,
                Quantidade = i.Quantidade,
                ValorUnitario = i.ValorUnitario
            });
        }

        AtualizarTotal();
        Mensagem = string.Empty;
    }

    private void LimparFormulario()
    {
        _ordemEmEdicao = null;
        Titulo = "Nova ordem de serviço";

        ClienteSelecionado = null;
        Equipamento = string.Empty;
        Marca = string.Empty;
        NumeroSerie = string.Empty;
        EmGarantia = false;
        DefeitoRelatado = string.Empty;
        LaudoTecnico = string.Empty;
        Status = StatusOS.Aberta;

        ItensForm.Clear();
        NovoItemDescricao = string.Empty;
        NovoItemQuantidade = 1;
        NovoItemValor = 0;
        AtualizarTotal();
    }

    private void AtualizarTotal() => TotalForm = ItensForm.Sum(i => i.Total);

    private static string? TextoOuNulo(string texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    [RelayCommand]
    private void Nova()
    {
        OrdemSelecionada = null;
        LimparFormulario();
        Mensagem = string.Empty;
    }

    [RelayCommand]
    private void AdicionarItem()
    {
        if (string.IsNullOrWhiteSpace(NovoItemDescricao) || NovoItemQuantidade <= 0)
        {
            Mensagem = "Informe a descrição e uma quantidade maior que zero.";
            return;
        }

        ItensForm.Add(new ItemOS
        {
            Descricao = NovoItemDescricao.Trim(),
            Quantidade = NovoItemQuantidade,
            ValorUnitario = NovoItemValor
        });

        NovoItemDescricao = string.Empty;
        NovoItemQuantidade = 1;
        NovoItemValor = 0;
        AtualizarTotal();
        Mensagem = string.Empty;
    }

    [RelayCommand]
    private void RemoverItem()
    {
        if (ItemSelecionado is null)
            return;

        ItensForm.Remove(ItemSelecionado);
        AtualizarTotal();
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (ClienteSelecionado is null || string.IsNullOrWhiteSpace(Equipamento))
        {
            Mensagem = "Escolha o cliente e informe o equipamento.";
            return;
        }

        try
        {
            OrdemServico os;
            StatusOS? statusAnterior = null;

            if (_ordemEmEdicao is null)
            {
                os = new OrdemServico();
            }
            else
            {
                os = await _ordemRepository.GetByIdWithItensAsync(_ordemEmEdicao.Id);
                statusAnterior = os.Status;

                os.Itens.Clear();
            }

            os.ClienteId = ClienteSelecionado.Id;
            // Don't set navigation property to avoid tracking conflict when OS was loaded with Include
            // os.Cliente = ClienteSelecionado;
            os.Equipamento = Equipamento.Trim();
            os.Marca = TextoOuNulo(Marca);
            os.NumeroSerie = TextoOuNulo(NumeroSerie);
            os.EmGarantia = EmGarantia;
            os.DefeitoRelatado = TextoOuNulo(DefeitoRelatado);
            os.LaudoTecnico = TextoOuNulo(LaudoTecnico);
            os.Status = Status;
            os.DataConclusao = Status == StatusOS.Entregue
                ? os.DataConclusao ?? DateTime.Now
                : null;

            foreach (var i in ItensForm)
            {
                os.Itens.Add(new ItemOS
                {
                    Descricao = i.Descricao,
                    Quantidade = i.Quantidade,
                    ValorUnitario = i.ValorUnitario
                });
            }

            if (_ordemEmEdicao is null)
            {
                await _ordemRepository.AddAsync(os);
            }
            else
            {
                await _ordemRepository.UpdateAsync(os);
            }

            // Auto-baixar estoque se OS foi entregue (e não estava entregue antes)
            if (Status == StatusOS.Entregue && statusAnterior != StatusOS.Entregue)
            {
                await BaixarEstoqueAsync(os);
            }

            // Envia notificação WhatsApp se o status mudou para estados importantes
            if (statusAnterior.HasValue && statusAnterior != Status)
            {
                _ = EnviarNotificacaoStatusAsync(os, statusAnterior.Value, Status);
            }

            var numero = os.Id;
            await CarregarOrdensAsync();

            OrdemSelecionada = Ordens.FirstOrDefault(o => o.Id == numero);
            Mensagem = $"Ordem de serviço nº {numero} salva com sucesso.";
        }
        catch (Exception ex)
        {
            Mensagem = "Erro ao salvar: " + ex.Message;
        }
    }

    private async Task EnviarNotificacaoStatusAsync(OrdemServico os, StatusOS statusAnterior, StatusOS statusNovo)
    {
        try
        {
            // Só notifica para transições importantes
            var notificar = statusNovo == StatusOS.Pronta || statusNovo == StatusOS.Entregue;

            if (!notificar || os.Cliente == null)
                return;

            var telefone = os.Cliente.Telefone;
            if (string.IsNullOrWhiteSpace(telefone))
                return;

            var statusTexto = statusNovo switch
            {
                StatusOS.Pronta => "PRONTA PARA RETIRADA",
                StatusOS.Entregue => "ENTREGUE",
                _ => statusNovo.ToString().ToUpper()
            };

            var mensagem = $"📱 *Sistema OS - Ordem de Serviço #{os.Id}*\n\n" +
                           $"Olá {os.Cliente.Nome},\n\n" +
                           $"Sua ordem de serviço *#{os.Id}* ({os.Equipamento}) está agora: *{statusTexto}*.\n\n";

            if (statusNovo == StatusOS.Pronta)
            {
                mensagem += "Seu equipamento está pronto para retirada! 📦\n" +
                           "Horário de atendimento: Segunda a Sexta, 8h às 18h.\n\n";
            }
            else if (statusNovo == StatusOS.Entregue)
            {
                mensagem += "Seu equipamento foi entregue. ✅\n" +
                           "Obrigado por confiar no nosso serviço! 🙏\n\n";
            }

            mensagem += "Atenciosamente,\nEquipe Sistema OS";

            await _whatsApp.SendTextAsync(telefone, mensagem);
        }
        catch
        {
            // Silencioso - não bloqueia o fluxo principal se WhatsApp falhar
        }
    }

    private async Task BaixarEstoqueAsync(OrdemServico os)
    {
        try
        {
            // Recarrega a OS com itens e produtos para ter acesso ao ProdutoId
            var osCompleta = await _ordemRepository.GetByIdWithItensAsync(os.Id);

            foreach (var item in osCompleta.Itens)
            {
                // Só baixa estoque se o item tem ProdutoId vinculado
                if (!item.ProdutoId.HasValue)
                    continue;

                var sucesso = await _estoqueService.BaixarEstoqueAsync(os.Id);
                if (!sucesso)
                {
                    // O serviço já loga o motivo (estoque insuficiente)
                    var produto = await _ordemRepository.GetProdutoByIdAsync(item.ProdutoId.Value);
                    if (produto != null)
                    {
                        Mensagem += $" ⚠ Estoque insuficiente para '{produto.Nome}' (disponível: {produto.QuantidadeAtual}, necessário: {item.Quantidade}).";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log silencioso - não bloqueia o fluxo principal
            Mensagem += $" ⚠ Erro ao baixar estoque: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImprimirOrdemAsync() => await GerarPdfAsync(os => _pdf.GerarOrdemServicoAsync(os));

    [RelayCommand]
    private async Task ImprimirLaudoAsync() => await GerarPdfAsync(os => _pdf.GerarLaudoAsync(os));

    [RelayCommand]
    private void CadastrarNovoCliente()
    {
        _uiState.CadastrandoDaOS = true;
        WeakReferenceMessenger.Default.Send(new MudarTelaMessage("Clientes"));
    }

    private async Task GerarPdfAsync(Func<OrdemServico, Task<string>> gerar)
    {
        if (_ordemEmEdicao is null)
        {
            Mensagem = "Salve a ordem ou selecione uma O.S. da tabela para imprimir.";
            return;
        }

        // O PdfService já busca os dados da empresa internamente
        try
        {
            var caminho = await gerar(_ordemEmEdicao);
            PdfService.Abrir(caminho);
            Mensagem = $"PDF gerado: {Path.GetFileName(caminho)} (pasta Documentos\\SistemaOS\\PDFs)";
        }
        catch (IOException)
        {
            Mensagem = "Não foi possível gravar o PDF. Se ele estiver aberto, feche-o e tente de novo.";
        }
        catch (Exception ex)
        {
            Mensagem = "Erro ao gerar o PDF: " + ex.Message;
        }
    }
}