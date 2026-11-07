using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SistemaOS.App.Services;

/// <summary>
/// Serviço de temas - lógica de negócio pura (testável sem WPF)
/// A aplicação WPF usa WpfThemeApplier para aplicar as mudanças visualmente
/// </summary>
public class ThemeService : IThemeService
{
    private readonly string _arquivo;
    private ConfiguracoesTema _config = new();

    public ConfiguracoesTema Config => _config;

    public event Action? TemaMudado;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ThemeService()
    {
        var pasta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SistemaOS");
        Directory.CreateDirectory(pasta);
        _arquivo = Path.Combine(pasta, "tema.json");
    }

    public void Carregar()
    {
        try
        {
            if (File.Exists(_arquivo))
            {
                var json = File.ReadAllText(_arquivo);

                // Primeiro, tentar ler como JsonNode para verificar formato antigo
                var node = JsonNode.Parse(json);
                if (node != null)
                {
                    // Verificar se é o formato antigo (tem propriedade "Escuro")
                    if (node["Escuro"] != null && node["Tema"] == null)
                    {
                        // Formato antigo: migrar Escuro para Tema
                        var escuro = node["Escuro"]?.GetValue<bool>() ?? true;
                        node["Tema"] = escuro ? "Dark" : "Light";
                        node["Escuro"] = null; // Remover propriedade antiga
                    }

                    // Deserializar para o novo formato
                    _config = JsonSerializer.Deserialize<ConfiguracoesTema>(node.ToJsonString()) ?? new();
                }
                else
                {
                    _config = new();
                }
            }
        }
        catch
        {
            _config = new();
        }

        OnPropertyChanged(nameof(Config));
        TemaMudado?.Invoke();
    }

    public void AlternarTema()
    {
        // Alternância simples entre Light e Dark para compatibilidade
        _config.Tema = _config.Tema == "Light" ? "Dark" : "Light";
        Salvar();
        OnPropertyChanged(nameof(Config));
        TemaMudado?.Invoke();
    }

    public void DefinirTema(string tema)
    {
        if (!string.IsNullOrEmpty(tema))
        {
            _config.Tema = tema;
            Salvar();
            OnPropertyChanged(nameof(Config));
            TemaMudado?.Invoke();
        }
    }

    public void DefinirCor(string hex)
    {
        _config.CorDestaque = hex;
        Salvar();
        OnPropertyChanged(nameof(Config));
        TemaMudado?.Invoke();
    }

    public void Salvar()
    {
        File.WriteAllText(_arquivo, JsonSerializer.Serialize(_config));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}