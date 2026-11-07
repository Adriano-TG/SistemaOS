using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SistemaOS.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IOrdemServicoRepository _ordemRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly DispatcherTimer _clockTimer;

    public DashboardViewModel(IOrdemServicoRepository ordemRepository, IProdutoRepository produtoRepository)
    {
        _ordemRepository = ordemRepository;
        _produtoRepository = produtoRepository;

        // Inicializar propriedades do calendário
        var hoje = DateTime.Today;
        MesAtualInicio = new DateTime(hoje.Year, hoje.Month, 1);
        ProximoMesInicio = MesAtualInicio.AddMonths(1);

        // Inicializar relógio
        AtualizarRelogio();

        // Timer para atualizar o relógio a cada segundo
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => AtualizarRelogio();
        _clockTimer.Start();

        // Inicialização assíncrona - evita bloquear o construtor
        _ = InicializarAsync();
    }

    // Método para atualizar o relógio (chamado pelo timer a cada segundo)
    private void AtualizarRelogio()
    {
        var agora = DateTime.Now;
        HoraAtualFormatada = agora.ToString("HH:mm:ss");

        // Ângulos para o relógio analógico (em graus)
        // Hora: 360° / 12 = 30° por hora + 0.5° por minuto
        HourHandAngle = (agora.Hour % 12) * 30 + agora.Minute * 0.5;
        // Minuto: 360° / 60 = 6° por minuto + 0.1° por segundo
        MinuteHandAngle = agora.Minute * 6 + agora.Second * 0.1;
        // Segundo: 360° / 60 = 6° por segundo
        SecondHandAngle = agora.Second * 6;

        OnPropertyChanged(nameof(HourHandAngle));
        OnPropertyChanged(nameof(MinuteHandAngle));
        OnPropertyChanged(nameof(SecondHandAngle));
    }

    #region Calendário e Relógio

    [ObservableProperty]
    private DateTime _mesAtualInicio;

    public string MesAtualNome => MesAtualInicio.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR"));

    [ObservableProperty]
    private DateTime _proximoMesInicio;

    public string ProximoMesNome => ProximoMesInicio.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR"));

    [ObservableProperty]
    private string _horaAtualFormatada = string.Empty;

    [ObservableProperty]
    private double _hourHandAngle;

    [ObservableProperty]
    private double _minuteHandAngle;

    [ObservableProperty]
    private double _secondHandAngle;

    #endregion

    private async Task InicializarAsync()
    {
        try
        {
            await CarregarDashboardAsync();
        }
        catch (Exception ex)
        {
            // Log para debug - em produção usar ILogger
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar Dashboard: {ex.Message}");
        }
    }

    #region KPIs Principais

    [ObservableProperty]
    private int _totalOrdensMes;

    [ObservableProperty]
    private int _ordensAbertas;

    [ObservableProperty]
    private int _ordensEmAndamento;

    [ObservableProperty]
    private int _ordensProntas;

    [ObservableProperty]
    private int _ordensEntregues;

    [ObservableProperty]
    private int _ordensCanceladas;

    [ObservableProperty]
    private decimal _faturamentoMes;

    [ObservableProperty]
    private decimal _faturamentoTotal;

    [ObservableProperty]
    private decimal _ticketMedio;

    [ObservableProperty]
    private double _tempoMedioAtendimentoHoras;

    [ObservableProperty]
    private double _slaCumprimentoPercentual;

    [ObservableProperty]
    private int _clientesAtivos;

    [ObservableProperty]
    private int _produtosBaixoEstoque;

    #endregion

    #region Gráficos / Listas

    [ObservableProperty]
    private ObservableCollection<StatusCount> _ordensPorStatus = new();

    [ObservableProperty]
    private ObservableCollection<FaturamentoDiario> _faturamentoUltimos7Dias = new();

    [ObservableProperty]
    private ObservableCollection<TopCliente> _topClientes = new();

    [ObservableProperty]
    private ObservableCollection<TopProduto> _topProdutos = new();

    #endregion

    #region Computed

    public string FaturamentoMesFormatado => $"R$ {FaturamentoMes:N2}";
    public string FaturamentoTotalFormatado => $"R$ {FaturamentoTotal:N2}";
    public string TicketMedioFormatado => $"R$ {TicketMedio:N2}";
    public string TempoMedioFormatado => $"{TempoMedioAtendimentoHoras:F1}h";
    public string SlaFormatado => $"{SlaCumprimentoPercentual:F1}%";

    #endregion

    #region Comandos

    [RelayCommand]
    private async Task CarregarDashboardAsync()
    {
        try
        {
            var hoje = DateTime.Today;
            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            var seteDiasAtras = hoje.AddDays(-6);

            // Carregar ordens do mês
            var ordensMes = await _ordemRepository.GetByDateRangeAsync(inicioMes, hoje);

            // Carregar todas as ordens para faturamento total
            var ordensTodas = await _ordemRepository.GetAllWithItensAsync();

            var ordensEntreguesMes = ordensMes.Where(o => o.Status == StatusOS.Entregue).ToList();
            var ordensEntreguesTodas = ordensTodas.Where(o => o.Status == StatusOS.Entregue).ToList();

            // KPIs básicos
            TotalOrdensMes = ordensMes.Count;
            OrdensAbertas = ordensMes.Count(o => o.Status == StatusOS.Aberta);
            OrdensEmAndamento = ordensMes.Count(o => o.Status == StatusOS.EmAnalise || o.Status == StatusOS.AguardandoPeca);
            OrdensProntas = ordensMes.Count(o => o.Status == StatusOS.Pronta);
            OrdensEntregues = ordensMes.Count(o => o.Status == StatusOS.Entregue);
            OrdensCanceladas = ordensMes.Count(o => o.Status == StatusOS.Cancelada);

            // Faturamento
            FaturamentoMes = ordensEntreguesMes.Sum(o => o.Itens.Sum(i => i.Total));
            FaturamentoTotal = ordensEntreguesTodas.Sum(o => o.Itens.Sum(i => i.Total));
            TicketMedio = ordensEntreguesMes.Count > 0 ? FaturamentoMes / ordensEntreguesMes.Count : 0;

            // SLA - Tempo médio de atendimento (apenas entregues do mês)
            var ordensComData = ordensEntreguesMes.Where(o => o.DataConclusao.HasValue).ToList();
            if (ordensComData.Count > 0)
            {
                var tempoTotalHoras = ordensComData.Sum(o => (o.DataConclusao!.Value - o.DataAbertura).TotalHours);
                TempoMedioAtendimentoHoras = tempoTotalHoras / ordensComData.Count;

                // SLA: considera cumprido se atendido em até 48h
                var dentroSla = ordensComData.Count(o => (o.DataConclusao!.Value - o.DataAbertura).TotalHours <= 48);
                SlaCumprimentoPercentual = (double)dentroSla / ordensComData.Count * 100;
            }

            // Clientes ativos (com OS no mês)
            ClientesAtivos = ordensMes.Select(o => o.ClienteId).Distinct().Count();

            // Produtos baixo estoque
            ProdutosBaixoEstoque = await _produtoRepository.GetBaixoEstoqueCountAsync();

            // Ordens por Status (para gráfico)
            var statusGroups = ordensMes
                .GroupBy(o => o.Status)
                .Select(g => new StatusCount(g.Key.ToString(), g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList();

            OrdensPorStatus.Clear();
            foreach (var s in statusGroups)
                OrdensPorStatus.Add(s);

            // Faturamento últimos 7 dias
            var faturamentoDiario = ordensEntreguesMes
                .Where(o => o.DataConclusao.HasValue && o.DataConclusao.Value.Date >= seteDiasAtras && o.DataConclusao.Value.Date <= hoje)
                .GroupBy(o => o.DataConclusao!.Value.Date)
                .Select(g => new FaturamentoDiario
                {
                    Data = g.Key,
                    Valor = g.Sum(o => o.Itens.Sum(i => i.Total))
                })
                .OrderBy(x => x.Data)
                .ToList();

            // Preencher dias sem faturamento
            for (int i = 0; i < 7; i++)
            {
                var data = seteDiasAtras.AddDays(i);
                if (!faturamentoDiario.Any(f => f.Data == data))
                {
                    faturamentoDiario.Add(new FaturamentoDiario { Data = data, Valor = 0 });
                }
            }

            FaturamentoUltimos7Dias.Clear();
            foreach (var f in faturamentoDiario.OrderBy(x => x.Data))
                FaturamentoUltimos7Dias.Add(f);

            // Top 5 Clientes por faturamento
            var topClientes = ordensEntreguesMes
                .Where(o => o.Cliente != null)
                .GroupBy(o => new { o.ClienteId, o.Cliente!.Nome })
                .Select(g => new TopCliente
                {
                    ClienteId = g.Key.ClienteId,
                    Nome = g.Key.Nome,
                    TotalFaturado = g.Sum(o => o.Itens.Sum(i => i.Total)),
                    QuantidadeOrdens = g.Count()
                })
                .OrderByDescending(x => x.TotalFaturado)
                .Take(5)
                .ToList();

            TopClientes.Clear();
            foreach (var c in topClientes)
                TopClientes.Add(c);

            // Top 5 Produtos mais vendidos
            var topProdutos = ordensEntreguesMes
                .SelectMany(o => o.Itens)
                .Where(i => i.ProdutoId.HasValue)
                .GroupBy(i => new { i.ProdutoId, i.Produto!.Nome, i.Produto.Codigo })
                .Select(g => new TopProduto
                {
                    ProdutoId = g.Key.ProdutoId!.Value,
                    Codigo = g.Key.Codigo,
                    Nome = g.Key.Nome,
                    QuantidadeVendida = g.Sum(i => i.Quantidade),
                    TotalFaturado = g.Sum(i => i.Total)
                })
                .OrderByDescending(x => x.QuantidadeVendida)
                .Take(5)
                .ToList();

            TopProdutos.Clear();
            foreach (var p in topProdutos)
                TopProdutos.Add(p);

            // Notificar propriedades computadas
            OnPropertyChanged(nameof(FaturamentoMesFormatado));
            OnPropertyChanged(nameof(FaturamentoTotalFormatado));
            OnPropertyChanged(nameof(TicketMedioFormatado));
            OnPropertyChanged(nameof(TempoMedioFormatado));
            OnPropertyChanged(nameof(SlaFormatado));
        }
        catch (Exception ex)
        {
            // Log para debug - em produção usar ILogger
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar Dashboard: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            throw; // Re-throw para que testes possam capturar
        }
    }

    [RelayCommand]
    private async Task AtualizarAsync()
    {
        await CarregarDashboardAsync();
    }

    #endregion

    // IDisposable para limpar o timer
    public void Dispose()
    {
        _clockTimer?.Stop();
        GC.SuppressFinalize(this);
    }
}

// DTOs para gráficos
public record StatusCount(string Status, int Count);

public record FaturamentoDiario
{
    public DateTime Data { get; init; }
    public decimal Valor { get; init; }
    public string DataFormatada => Data.ToString("dd/MM");
}

public record TopCliente
{
    public int ClienteId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public decimal TotalFaturado { get; init; }
    public int QuantidadeOrdens { get; init; }
    public string TotalFormatado => $"R$ {TotalFaturado:N2}";
}

public record TopProduto
{
    public int ProdutoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nome { get; init; } = string.Empty;
    public decimal QuantidadeVendida { get; init; }
    public decimal TotalFaturado { get; init; }
    public string TotalFormatado => $"R$ {TotalFaturado:N2}";
}