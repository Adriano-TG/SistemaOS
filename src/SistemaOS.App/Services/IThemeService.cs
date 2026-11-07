using System.ComponentModel;

namespace SistemaOS.App.Services;

/// <summary>
/// Interface para o serviço de temas - lógica de negócio testável sem WPF
/// </summary>
public interface IThemeService : INotifyPropertyChanged
{
    ConfiguracoesTema Config { get; }
    void Carregar();
    void AlternarTema(); // Mantido para compatibilidade
    void DefinirTema(string tema); // Novo: define tema por string
    void DefinirCor(string hex);
    void Salvar();
    event Action? TemaMudado;
}

/// <summary>
/// Configurações de tema (DTO serializável)
/// </summary>
public class ConfiguracoesTema
{
    public string Tema { get; set; } = "Dark"; // Light, Dark, System, HighContrastLight, HighContrastDark, Blue, Green, Purple, Orange
    public string CorDestaque { get; set; } = "#6C5CE7";
}