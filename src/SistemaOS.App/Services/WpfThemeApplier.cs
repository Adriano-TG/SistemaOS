using System.Windows;
using System.Windows.Media;

namespace SistemaOS.App.Services;

/// <summary>
/// Aplica o tema no WPF - depende de Application.Current (não testável em headless)
/// Registrado no App.xaml.cs e conectado ao ThemeService via evento
/// </summary>
public class WpfThemeApplier
{
    private readonly IThemeService _themeService;

    public WpfThemeApplier(IThemeService themeService)
    {
        _themeService = themeService;
        _themeService.TemaMudado += Aplicar;

        // Aplicar tema inicial se Application.Current já estiver disponível
        if (Application.Current != null)
        {
            Aplicar();
        }
    }

    public void Aplicar()
    {
        if (Application.Current == null) return;

        var recursos = Application.Current.Resources;
        var config = _themeService.Config;

        // Mapear tema para arquivo de ResourceDictionary
        var nome = config.Tema switch
        {
            "Light" => "LightTheme",
            "Dark" => "DarkTheme",
            "System" => "DarkTheme", // Padrão para sistema (pode ser expandido para detectar OS)
            "HighContrastLight" => "HighContrastLightTheme",
            "HighContrastDark" => "HighContrastDarkTheme",
            "Blue" => "BlueTheme",
            "Green" => "GreenTheme",
            "Purple" => "PurpleTheme",
            "Orange" => "OrangeTheme",
            _ => "DarkTheme"
        };

        recursos.MergedDictionaries[0] = new ResourceDictionary
        {
            Source = new Uri($"/SistemaOS.App;component/Themes/{nome}.xaml", UriKind.Relative)
        };

        var cor = (Color)ColorConverter.ConvertFromString(config.CorDestaque);
        recursos["AccentBrush"] = new SolidColorBrush(cor);
    }
}