using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SistemaOS.App.Helpers;

public static class FormatadorMascara
{
    public enum TipoMascara { Nenhuma, CpfCnpj, Telefone }

    public static readonly DependencyProperty TipoProperty =
        DependencyProperty.RegisterAttached("Tipo", typeof(TipoMascara), typeof(FormatadorMascara),
        new PropertyMetadata(TipoMascara.Nenhuma, OnTipoChanged));

    public static TipoMascara GetTipo(DependencyObject obj) => (TipoMascara)obj.GetValue(TipoProperty);
    public static void SetTipo(DependencyObject obj, TipoMascara value) => obj.SetValue(TipoProperty, value);

    private static void OnTipoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            textBox.TextChanged -= TextBox_TextChanged;
            if ((TipoMascara)e.NewValue != TipoMascara.Nenhuma)
                textBox.TextChanged += TextBox_TextChanged;
        }
    }

    private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && !textBox.IsReadOnly)
        {
            textBox.TextChanged -= TextBox_TextChanged;

            var cursor = textBox.CaretIndex;
            var tamanhoAntigo = textBox.Text.Length;

            string formatado = GetTipo(textBox) == TipoMascara.CpfCnpj
                ? FormatarCpfCnpj(textBox.Text)
                : FormatarTelefone(textBox.Text);

            textBox.Text = formatado;

            // Mantém o cursor no lugar certo enquanto digita
            var novoCursor = cursor + (formatado.Length - tamanhoAntigo);
            textBox.CaretIndex = novoCursor > 0 ? novoCursor : 0;

            textBox.TextChanged += TextBox_TextChanged;
        }
    }

    private static string FormatarCpfCnpj(string t)
    {
        string v = new string(t.Where(char.IsDigit).ToArray());
        if (v.Length <= 11)
        {
            if (v.Length > 9) return $"{v.Substring(0, 3)}.{v.Substring(3, 3)}.{v.Substring(6, 3)}-{v.Substring(9)}";
            if (v.Length > 6) return $"{v.Substring(0, 3)}.{v.Substring(3, 3)}.{v.Substring(6)}";
            if (v.Length > 3) return $"{v.Substring(0, 3)}.{v.Substring(3)}";
            return v;
        }
        else
        {
            if (v.Length > 14) v = v.Substring(0, 14);
            if (v.Length > 12) return $"{v.Substring(0, 2)}.{v.Substring(2, 3)}.{v.Substring(5, 3)}/{v.Substring(8, 4)}-{v.Substring(12)}";
            if (v.Length > 8) return $"{v.Substring(0, 2)}.{v.Substring(2, 3)}.{v.Substring(5, 3)}/{v.Substring(8)}";
            if (v.Length > 5) return $"{v.Substring(0, 2)}.{v.Substring(2, 3)}.{v.Substring(5)}";
            if (v.Length > 2) return $"{v.Substring(0, 2)}.{v.Substring(2)}";
            return v;
        }
    }

    private static string FormatarTelefone(string t)
    {
        string v = new string(t.Where(char.IsDigit).ToArray());
        if (v.Length > 11) v = v.Substring(0, 11);
        if (v.Length > 10) return $"({v.Substring(0, 2)}) {v.Substring(2, 5)}-{v.Substring(7)}";
        if (v.Length > 6) return $"({v.Substring(0, 2)}) {v.Substring(2, 4)}-{v.Substring(6)}";
        if (v.Length > 2) return $"({v.Substring(0, 2)}) {v.Substring(2)}";
        return v;
    }
}