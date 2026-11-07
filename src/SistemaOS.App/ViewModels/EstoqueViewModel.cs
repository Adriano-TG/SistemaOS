using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaOS.App.ViewModels;

public partial class EstoqueViewModel : ObservableObject
{
    private readonly IProdutoRepository _produtoRepository;
    private readonly IEstoqueService _estoqueService;

    public EstoqueViewModel(IProdutoRepository produtoRepository, IEstoqueService estoqueService)
    {
        _produtoRepository = produtoRepository;
        _estoqueService = estoqueService;
        _ = CarregarProdutosAsync();
    }

    #region Propriedades

    [ObservableProperty]
    private ObservableCollection<Produto> _produtos = new();

    [ObservableProperty]
    private Produto? _produtoSelecionado;

    [ObservableProperty]
    private string _mensagem = string.Empty;

    [ObservableProperty]
    private bool _modoEdicao;

    [ObservableProperty]
    private string _textoPesquisa = string.Empty;

    [ObservableProperty]
    private bool _mostrarApenasBaixoEstoque = false;

    // Formulário
    [ObservableProperty]
    private string _codigo = string.Empty;

    [ObservableProperty]
    private string _nome = string.Empty;

    [ObservableProperty]
    private string _descricao = string.Empty;

    [ObservableProperty]
    private string _unidadeMedida = "UN";

    [ObservableProperty]
    private decimal _precoCusto;

    [ObservableProperty]
    private decimal _precoVenda;

    [ObservableProperty]
    private int _quantidadeMinima;

    [ObservableProperty]
    private decimal _quantidadeInicial;

    // Movimentação
    [ObservableProperty]
    private ObservableCollection<MovimentoEstoque> _movimentos = new();

    [ObservableProperty]
    private TipoMovimentoEstoque _tipoMovimento = TipoMovimentoEstoque.Entrada;

    [ObservableProperty]
    private decimal _quantidadeMovimento;

    [ObservableProperty]
    private decimal _precoUnitarioMovimento;

    [ObservableProperty]
    private string _observacaoMovimento = string.Empty;

    #endregion

    #region Computed

    public string[] OpcoesUnidadeMedida { get; } = new[] { "UN", "PC", "KG", "M", "CM", "L", "ML", "CX", "PT", "RO" };

    public string[] OpcoesTipoMovimento { get; } = Enum.GetNames(typeof(TipoMovimentoEstoque));

    public int TotalProdutos => Produtos.Count;

    public int ProdutosBaixoEstoque => Produtos.Count(p => p.EstoqueBaixo);

    public int ProdutosSemEstoque => Produtos.Count(p => p.SemEstoque);

    public decimal ValorTotalEstoque => Produtos.Sum(p => p.ValorEstoque);

    #endregion

    #region Comandos

    [RelayCommand]
    private async Task CarregarProdutosAsync()
    {
        try
        {
            IEnumerable<Produto> lista;

            if (MostrarApenasBaixoEstoque)
            {
                lista = await _produtoRepository.GetBaixoEstoqueAsync();
            }
            else if (!string.IsNullOrWhiteSpace(TextoPesquisa))
            {
                var termo = TextoPesquisa.Trim().ToLower();
                lista = await _produtoRepository.SearchAsync(termo);
            }
            else
            {
                lista = await _produtoRepository.GetAtivosAsync();
            }

            Produtos.Clear();
            foreach (var p in lista)
                Produtos.Add(p);

            OnPropertyChanged(nameof(TotalProdutos));
            OnPropertyChanged(nameof(ProdutosBaixoEstoque));
            OnPropertyChanged(nameof(ProdutosSemEstoque));
            OnPropertyChanged(nameof(ValorTotalEstoque));
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao carregar: {ex.Message}";
        }
    }

    partial void OnTextoPesquisaChanged(string value)
    {
        _ = CarregarProdutosAsync();
    }

    partial void OnMostrarApenasBaixoEstoqueChanged(bool value)
    {
        _ = CarregarProdutosAsync();
    }

    [RelayCommand]
    private void Novo()
    {
        LimparFormulario();
        ModoEdicao = true;
        Mensagem = "Novo produto - preencha os dados";
    }

    [RelayCommand]
    private void Editar()
    {
        if (ProdutoSelecionado == null)
        {
            Mensagem = "Selecione um produto para editar";
            return;
        }

        CarregarFormulario(ProdutoSelecionado);
        ModoEdicao = true;
        Mensagem = $"Editando: {ProdutoSelecionado.Nome}";
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(Codigo) || string.IsNullOrWhiteSpace(Nome))
        {
            Mensagem = "Código e Nome são obrigatórios";
            return;
        }

        try
        {
            var produtoIdExcluir = ProdutoSelecionado?.Id ?? 0;
            var existeCodigo = await _produtoRepository.ExistsAsync(p => p.Codigo == Codigo && p.Id != produtoIdExcluir);
            if (existeCodigo)
            {
                Mensagem = "Já existe produto com este código";
                return;
            }

            Produto? produto;

            if (ProdutoSelecionado == null)
            {
                // Novo
                produto = new Produto
                {
                    Codigo = Codigo,
                    Nome = Nome,
                    Descricao = Descricao,
                    UnidadeMedida = UnidadeMedida,
                    PrecoCusto = PrecoCusto,
                    PrecoVenda = PrecoVenda,
                    QuantidadeMinima = QuantidadeMinima,
                    QuantidadeAtual = QuantidadeInicial,
                    DataCadastro = DateTime.Now
                };

                await _produtoRepository.AddAsync(produto);

                // Registrar estoque inicial se > 0
                if (QuantidadeInicial > 0)
                {
                    await _estoqueService.AdicionarMovimentoAsync(
                        produto.Id,
                        TipoMovimentoEstoque.Inicial,
                        QuantidadeInicial,
                        PrecoCusto,
                        "Estoque inicial no cadastro");
                }

                Mensagem = "Produto cadastrado com sucesso!";
            }
            else
            {
                // Editar
                produto = await _produtoRepository.GetByIdAsync(ProdutoSelecionado.Id);
                if (produto == null)
                {
                    Mensagem = "Produto não encontrado";
                    return;
                }

                produto.Codigo = Codigo;
                produto.Nome = Nome;
                produto.Descricao = Descricao;
                produto.UnidadeMedida = UnidadeMedida;
                produto.PrecoCusto = PrecoCusto;
                produto.PrecoVenda = PrecoVenda;
                produto.QuantidadeMinima = QuantidadeMinima;

                await _produtoRepository.UpdateAsync(produto);
                Mensagem = "Produto atualizado com sucesso!";
            }

            ModoEdicao = false;
            LimparFormulario();
            await CarregarProdutosAsync();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao salvar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancelar()
    {
        ModoEdicao = false;
        LimparFormulario();
        Mensagem = string.Empty;
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (ProdutoSelecionado == null)
        {
            Mensagem = "Selecione um produto para excluir";
            return;
        }

        // Verificar se tem movimentos ou itens de OS
        var podeExcluir = await _produtoRepository.CanDeleteAsync(ProdutoSelecionado.Id);
        if (!podeExcluir)
        {
            Mensagem = "Não é possível excluir: produto possui movimentações ou itens em ordens de serviço. Desative-o.";
            return;
        }

        try
        {
            await _produtoRepository.DeleteAsync(ProdutoSelecionado);
            Mensagem = "Produto excluído com sucesso!";
            await CarregarProdutosAsync();
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao excluir: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MovimentarAsync()
    {
        if (ProdutoSelecionado == null)
        {
            Mensagem = "Selecione um produto";
            return;
        }

        if (QuantidadeMovimento <= 0)
        {
            Mensagem = "Informe uma quantidade maior que zero";
            return;
        }

        try
        {
            var qtdAnterior = ProdutoSelecionado.QuantidadeAtual;

            var sucesso = await _estoqueService.AdicionarMovimentoAsync(
                ProdutoSelecionado.Id,
                TipoMovimento,
                QuantidadeMovimento,
                PrecoUnitarioMovimento > 0 ? PrecoUnitarioMovimento : ProdutoSelecionado.PrecoCusto,
                ObservacaoMovimento);

            if (!sucesso)
            {
                if (TipoMovimento == TipoMovimentoEstoque.Saida)
                {
                    Mensagem = $"Estoque insuficiente! Atual: {ProdutoSelecionado.QuantidadeAtual}";
                }
                else
                {
                    Mensagem = "Erro ao registrar movimento";
                }
                return;
            }

            Mensagem = $"{TipoMovimento} registrada! Estoque: {qtdAnterior} → {await _estoqueService.GetEstoqueAtualAsync(ProdutoSelecionado.Id)}";
            await CarregarProdutosAsync();
            await CarregarMovimentosAsync();

            // Limpar campos de movimento
            QuantidadeMovimento = 0m;
            PrecoUnitarioMovimento = 0;
            ObservacaoMovimento = string.Empty;
        }
        catch (Exception ex)
        {
            Mensagem = $"Erro ao movimentar: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CarregarMovimentosAsync()
    {
        if (ProdutoSelecionado == null)
        {
            Movimentos.Clear();
            return;
        }

        // Note: We need a method in repository to get movements
        // For now, we'll use a simple query - this would need a proper repository method
        // We can add it later if needed
    }

    partial void OnProdutoSelecionadoChanged(Produto? value)
    {
        _ = CarregarMovimentosAsync();

        if (value != null && ModoEdicao)
        {
            CarregarFormulario(value);
        }
    }

    [RelayCommand]
    private async Task ExportarCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exportar estoque para CSV",
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"Estoque_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var linhas = new List<string>
                {
                    "Código,Nome,Descrição,Unidade,Preço Custo,Preço Venda,Qtd Atual,Qtd Mínima,Valor Estoque,Status"
                };

                foreach (var p in Produtos)
                {
                    var status = p.SemEstoque ? "SEM ESTOQUE" : (p.EstoqueBaixo ? "BAIXO" : "OK");
                    linhas.Add($"{p.Codigo},{p.Nome},\"{p.Descricao}\",{p.UnidadeMedida},{p.PrecoCusto:F2},{p.PrecoVenda:F2},{p.QuantidadeAtual},{p.QuantidadeMinima},{p.ValorEstoque:F2},{status}");
                }

                await System.IO.File.WriteAllLinesAsync(dialog.FileName, linhas);
                Mensagem = $"Exportado {Produtos.Count} produtos para CSV";
            }
            catch (Exception ex)
            {
                Mensagem = $"Erro ao exportar: {ex.Message}";
            }
        }
    }

    #endregion

    #region Helpers

    private void LimparFormulario()
    {
        Codigo = string.Empty;
        Nome = string.Empty;
        Descricao = string.Empty;
        UnidadeMedida = "UN";
        PrecoCusto = 0;
        PrecoVenda = 0;
        QuantidadeMinima = 0;
        QuantidadeInicial = 0m;
        ProdutoSelecionado = null;
    }

    private void CarregarFormulario(Produto p)
    {
        Codigo = p.Codigo;
        Nome = p.Nome;
        Descricao = p.Descricao ?? string.Empty;
        UnidadeMedida = p.UnidadeMedida ?? "UN";
        PrecoCusto = p.PrecoCusto;
        PrecoVenda = p.PrecoVenda;
        QuantidadeMinima = p.QuantidadeMinima;
        QuantidadeInicial = p.QuantidadeAtual; // Para edição, mostra estoque atual
    }

    #endregion
}