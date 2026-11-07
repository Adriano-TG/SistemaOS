using System.Windows;
using System.Windows.Controls;
using SistemaOS.App;
using SistemaOS.Core.Interfaces;

namespace SistemaOS.App.Helpers;

public static class FormatadorTexto
{
    public static readonly DependencyProperty AtivarProperty =
        DependencyProperty.RegisterAttached("Ativar", typeof(bool), typeof(FormatadorTexto),
        new PropertyMetadata(false, OnAtivarChanged));

    public static bool GetAtivar(DependencyObject obj) => (bool)obj.GetValue(AtivarProperty);
    public static void SetAtivar(DependencyObject obj, bool value) => obj.SetValue(AtivarProperty, value);

    private static void OnAtivarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
                textBox.TextChanged += TextBox_TextChanged;
            else
                textBox.TextChanged -= TextBox_TextChanged;
        }
    }

    private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && !textBox.IsReadOnly)
        {
            // Removemos o evento rápido para não entrar em loop infinito ao alterar o texto
            textBox.TextChanged -= TextBox_TextChanged;

            var cursor = textBox.CaretIndex;
            var formatter = App.Services?.GetService(typeof(ITextFormatterService)) as ITextFormatterService;
            textBox.Text = formatter?.Formatar(textBox.Text) ?? textBox.Text;
            textBox.CaretIndex = cursor;

            textBox.TextChanged += TextBox_TextChanged;
        }
    }
}