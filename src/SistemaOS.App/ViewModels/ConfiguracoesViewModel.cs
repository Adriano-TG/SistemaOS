using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SistemaOS.App.Services;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaOS.App.ViewModels;

public partial class ConfiguracoesViewModel : ObservableObject
{
    private readonly MigracaoService _migracaoService;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly CnpjService _cnpjService;
    private readonly IThemeService _themeService;
    private readonly ITextFormatterService _textFormatter;

    // Temas disponíveis
    public List<TemaItem> TemasDisponiveis { get; } = new()
    {
        new TemaItem { Nome = "Claro", Valor = "Light" },
        new TemaItem { Nome = "Escuro", Valor = "Dark" },
        new TemaItem { Nome = "Sistema (padrão do Windows)", Valor = "System" },
        new TemaItem { Nome = "Alto Contraste Claro", Valor = "HighContrastLight" },
        new TemaItem { Nome = "Alto Contraste Escuro", Valor = "HighContrastDark" },
        new TemaItem { Nome = "Azul Profissional", Valor = "Blue" },
        new TemaItem { Nome = "Verde Natureza", Valor = "Green" },
        new TemaItem { Nome = "Roxo Elegante", Valor = "Purple" },
        new TemaItem { Nome = "Laranja Energético", Valor = "Orange" }
    };

    // Cores de destaque disponíveis
    public List<CorItem> CoresDisponiveis { get; } = new()
    {
        new CorItem { Nome = "Roxo (#6C5CE7)", Hex = "#6C5CE7" },
        new CorItem { Nome = "Verde (#00B894)", Hex = "#00B894" },
        new CorItem { Nome = "Azul (#0984E3)", Hex = "#0984E3" },
        new CorItem { Nome = "Laranja (#E17055)", Hex = "#E17055" },
        new CorItem { Nome = "Rosa (#E84393)", Hex = "#E84393" },
        new CorItem { Nome = "Vermelho (#D63031)", Hex = "#D63031" }
    };

    public string[] OpcoesFormato { get; } = new[] { "TUDO MAIÚSCULO", "tudo minúsculo", "Primeiras Letras Maiúsculas" };

    public string[] OpcoesTipoOs { get; } = new[] { "1 Via (Padrão)", "2 Vias na mesma folha" };
    public string[] OpcoesOrientacao { get; } = new[] { "Retrato (Em pé)", "Paisagem (Deitada)" };
    public string[] OpcoesTamanhoPapel { get; } = new[] { "Folha A4", "Bobina 80mm", "Bobina 58mm" };

    private int _formatoSelecionadoIndex;
    public int FormatoSelecionadoIndex
    {
        get => _formatoSelecionadoIndex;
        set
        {
            if (SetProperty(ref _formatoSelecionadoIndex, value))
            {
                _textFormatter.FormatoTextoIndex = value;
            }
        }
    }

    // Tema selecionado
    private int _temaSelecionado;
    public int TemaSelecionado
    {
        get => _temaSelecionado;
        set
        {
            if (SetProperty(ref _temaSelecionado, value))
            {
                var temaSelecionado = TemasDisponiveis[value].Valor;
                _themeService.DefinirTema(temaSelecionado);
            }
        }
    }

    // Cor selecionada
    private int _corSelecionadaIndex;
    public int CorSelecionadaIndex
    {
        get => _corSelecionadaIndex;
        set
        {
            if (SetProperty(ref _corSelecionadaIndex, value))
            {
                _themeService.DefinirCor(CoresDisponiveis[value].Hex);
            }
        }
    }

    [ObservableProperty] private string _mensagem = string.Empty;

    [ObservableProperty] private string _cnpj = string.Empty;
    [ObservableProperty] private string _razaoSocial = string.Empty;
    [ObservableProperty] private string _nomeFantasia = string.Empty;
    [ObservableProperty] private string _telefoneEmpresa = string.Empty;
    [ObservableProperty] private string _cepEmpresa = string.Empty;
    [ObservableProperty] private string _logradouroEmpresa = string.Empty;
    [ObservableProperty] private string _numeroEmpresa = string.Empty;
    [ObservableProperty] private string _bairroEmpresa = string.Empty;
    [ObservableProperty] private string _cidadeEmpresa = string.Empty;
    [ObservableProperty] private string _ufEmpresa = string.Empty;
    [ObservableProperty] private string _caminhoLogo = string.Empty;

    [ObservableProperty] private int _tipoOsSelecionado;
    [ObservableProperty] private int _orientacaoSelecionada;
    [ObservableProperty] private int _tamanhoPapelSelecionado;
    // A orientação só vale para a folha A4; bobina é sempre em pé (retrato)
    public bool PermiteOrientacao => TamanhoPapelSelecionado == 0;

    partial void OnTamanhoPapelSelecionadoChanged(int value)
    {
        OnPropertyChanged(nameof(PermiteOrientacao));

        if (value != 0)
            OrientacaoSelecionada = 0;
    }

    public ConfiguracoesViewModel(
        MigracaoService migracaoService,
        IEmpresaRepository empresaRepository,
        CnpjService cnpjService,
        IThemeService themeService,
        ITextFormatterService textFormatter)
    {
        _migracaoService = migracaoService;
        _empresaRepository = empresaRepository;
        _cnpjService = cnpjService;
        _themeService = themeService;
        _textFormatter = textFormatter;

        _themeService.Carregar();

        // Inicializar seleções baseadas no tema atual
        var temaAtual = _themeService.Config.Tema;
        for (int i = 0; i < TemasDisponiveis.Count; i++)
        {
            if (TemasDisponiveis[i].Valor.Equals(temaAtual, StringComparison.OrdinalIgnoreCase))
            {
                TemaSelecionado = i;
                break;
            }
        }

        // Encontrar índice da cor atual
        var corAtual = _themeService.Config.CorDestaque;
        for (int i = 0; i < CoresDisponiveis.Count; i++)
        {
            if (CoresDisponiveis[i].Hex.Equals(corAtual, StringComparison.OrdinalIgnoreCase))
            {
                CorSelecionadaIndex = i;
                break;
            }
        }

        // Inicializar formato de texto
        FormatoSelecionadoIndex = _textFormatter.FormatoTextoIndex;

        CarregarEmpresa();
    }

    // DTOs para os ComboBoxes
    public class TemaItem
    {
        public string Nome { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }

    public class CorItem
    {
        public string Nome { get; set; } = string.Empty;
        public string Hex { get; set; } = string.Empty;
    }

    async partial void OnCnpjChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var soNumeros = new string(value.Where(char.IsDigit).ToArray());
        if (soNumeros.Length > 14) soNumeros = soNumeros.Substring(0, 14);

        string cnpjFormatado = soNumeros;
        if (soNumeros.Length > 12) cnpjFormatado = $"{soNumeros.Substring(0, 2)}.{soNumeros.Substring(2, 3)}.{soNumeros.Substring(5, 3)}/{soNumeros.Substring(8, 4)}-{soNumeros.Substring(12)}";
        else if (soNumeros.Length > 8) cnpjFormatado = $"{soNumeros.Substring(0, 2)}.{soNumeros.Substring(2, 3)}.{soNumeros.Substring(5, 3)}/{soNumeros.Substring(8)}";
        else if (soNumeros.Length > 5) cnpjFormatado = $"{soNumeros.Substring(0, 2)}.{soNumeros.Substring(2, 3)}.{soNumeros.Substring(5)}";
        else if (soNumeros.Length > 2) cnpjFormatado = $"{soNumeros.Substring(0, 2)}.{soNumeros.Substring(2)}";

        if (Cnpj != cnpjFormatado) { Cnpj = cnpjFormatado; return; }

        if (soNumeros.Length == 14)
        {
            Mensagem = "A consultar CNPJ na Receita Federal...";
            var resultado = await _cnpjService.BuscarCnpjAsync(soNumeros);

            if (resultado != null)
            {
                RazaoSocial = resultado.razao_social ?? string.Empty;
                NomeFantasia = resultado.nome_fantasia ?? string.Empty;
                TelefoneEmpresa = resultado.ddd_telefone_1 ?? string.Empty;
                CepEmpresa = resultado.cep ?? string.Empty;
                LogradouroEmpresa = resultado.logradouro ?? string.Empty;
                NumeroEmpresa = resultado.numero ?? string.Empty;
                BairroEmpresa = resultado.bairro ?? string.Empty;
                CidadeEmpresa = resultado.municipio ?? string.Empty;
                UfEmpresa = resultado.uf ?? string.Empty;
                Mensagem = "Dados da empresa encontrados com sucesso!";
            }
            else Mensagem = "CNPJ não encontrado ou erro na consulta.";
        }
    }

    private async void CarregarEmpresa()
    {
        var empresa = await _empresaRepository.GetEmpresaAsync();
        if (empresa != null)
        {
            Cnpj = empresa.Cnpj;
            RazaoSocial = empresa.RazaoSocial;
            NomeFantasia = empresa.NomeFantasia;
            TelefoneEmpresa = empresa.Telefone;
            CepEmpresa = empresa.Cep;
            LogradouroEmpresa = empresa.Logradouro;
            NumeroEmpresa = empresa.Numero;
            BairroEmpresa = empresa.Bairro;
            CidadeEmpresa = empresa.Cidade;
            UfEmpresa = empresa.Uf;
            CaminhoLogo = empresa.CaminhoLogo;

            TipoOsSelecionado = empresa.TipoOsIndex;
            OrientacaoSelecionada = empresa.OrientacaoIndex;
            TamanhoPapelSelecionado = empresa.TamanhoPapelIndex;
        }
    }

    [RelayCommand]
    private void SelecionarLogo()
    {
        var dialog = new OpenFileDialog { Title = "Selecione o logótipo da empresa", Filter = "Imagens (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" };
        if (dialog.ShowDialog() == true) { CaminhoLogo = dialog.FileName; Mensagem = "Logótipo selecionado!"; }
    }

    [RelayCommand]
    private async Task SalvarEmpresaAsync()
    {
        var empresa = await _empresaRepository.GetEmpresaAsync() ?? new Empresa();

        empresa.Cnpj = Cnpj;
        empresa.RazaoSocial = RazaoSocial;
        empresa.NomeFantasia = NomeFantasia;
        empresa.Telefone = TelefoneEmpresa;
        empresa.Cep = CepEmpresa;
        empresa.Logradouro = LogradouroEmpresa;
        empresa.Numero = NumeroEmpresa;
        empresa.Bairro = BairroEmpresa;
        empresa.Cidade = CidadeEmpresa;
        empresa.Uf = UfEmpresa;
        empresa.CaminhoLogo = CaminhoLogo;

        empresa.TipoOsIndex = TipoOsSelecionado;
        empresa.TamanhoPapelIndex = TamanhoPapelSelecionado;
        empresa.OrientacaoIndex = TamanhoPapelSelecionado == 0 ? OrientacaoSelecionada : 0;

        if (empresa.Id == 0)
            await _empresaRepository.AddAsync(empresa);
        else
            await _empresaRepository.UpdateAsync(empresa);

        Mensagem = "Dados gravados com sucesso!";
    }

    [RelayCommand]
    private async Task ImportarLegadoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecione o banco de dados antigo (dados_legado.sqlite)",
            Filter = "Ficheiros SQLite (*.sqlite;*.db)|*.sqlite;*.db|Todos os ficheiros (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            Mensagem = "A ler o banco de dados antigo... Aguarde.";
            try
            {
                await Task.Run(() => _migracaoService.MigrarClientesLegado(dialog.FileName));
                Mensagem = "Migração concluída com sucesso!";
            }
            catch (Exception ex)
            {
                Mensagem = $"Erro na migração: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ExportarLegadoParaExcelAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecione o banco de dados antigo para exportar",
            Filter = "Ficheiros SQLite (*.sqlite;*.db)|*.sqlite;*.db|Todos os ficheiros (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            Mensagem = "A gerar planilha do Excel... Aguarde.";
            try
            {
                await Task.Run(() => _migracaoService.ExportarLegadoParaExcel(dialog.FileName));
                Mensagem = "Excel gerado e aberto com sucesso!";
            }
            catch (Exception ex)
            {
                Mensagem = $"Erro ao exportar: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task TransferirTelefonesParaWhatsAppAsync()
    {
        // This would need a ClienteRepository - for now we'll keep a direct approach
        // but ideally this should also use a repository
        Mensagem = "Funcionalidade a ser implementada com o novo padrão de repository";
        await Task.CompletedTask;
    }
}