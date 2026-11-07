using System.Globalization;
using SistemaOS.Core.Interfaces;

namespace SistemaOS.Infrastructure.Services;

public class TextFormatterService : ITextFormatterService
{
    public int FormatoTextoIndex { get; set; } = 0;

    public string Formatar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return texto;

        return FormatoTextoIndex switch
        {
            0 => texto.ToUpper(),
            1 => texto.ToLower(),
            2 => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(texto.ToLower()),
            _ => texto
        };
    }
}