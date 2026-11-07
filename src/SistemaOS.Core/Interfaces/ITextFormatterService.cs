using System.Globalization;

namespace SistemaOS.Core.Interfaces;

public interface ITextFormatterService
{
    int FormatoTextoIndex { get; set; }
    string Formatar(string texto);
}