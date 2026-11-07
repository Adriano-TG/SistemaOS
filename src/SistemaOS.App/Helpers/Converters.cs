using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SistemaOS.App.Helpers;

/// <summary>
/// Converte bool para Visibility (True = Visible, False = Collapsed)
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}

/// <summary>
/// Inverte bool (True → False, False → True)
/// </summary>
public class BooleanInverterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not true;
    }
}

/// <summary>
/// Converte bool para Visibility invertido (True = Collapsed, False = Visible)
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Collapsed;
    }
}

/// <summary>
/// Converte objeto nulo/não-nulo para Visibility
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converte objeto nulo para texto customizado
/// </summary>
public class NullToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return parameter?.ToString() ?? string.Empty;

        var format = parameter?.ToString() ?? "{0}";
        return string.Format(format, value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converte bool (EstoqueBaixo) para texto de status
/// </summary>
public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool baixo)
        {
            // Precisamos acessar a propriedade SemEstoque do objeto pai -
            // isso é uma limitação do converter simples
            return baixo ? "BAIXO" : "OK";
        }
        return "OK";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converte bool (EstoqueBaixo/SemEstoque) para cor
/// </summary>
public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Note: Em DataGrid, não temos fácil acesso a ambas propriedades
        // Retornamos cor baseada apenas no parâmetro
        if (value is bool baixo && parameter is string param)
        {
            if (param == "sem" && baixo) return new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)); // Vermelho escuro
            if (param == "baixo" && baixo) return new SolidColorBrush(Color.FromRgb(0xF5, 0x7F, 0x17)); // Laranja
            if (baixo) return new SolidColorBrush(Color.FromRgb(0xF5, 0x7F, 0x17)); // Laranja padrão
        }
        return new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)); // Verde
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converte multibinding para cor de status do estoque
/// </summary>
public class EstoqueStatusColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // values[0] = SemEstoque, values[1] = EstoqueBaixo
        if (values.Length >= 2 &&
            values[0] is bool sem &&
            values[1] is bool baixo)
        {
            if (sem) return new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)); // Vermelho
            if (baixo) return new SolidColorBrush(Color.FromRgb(0xF5, 0x7F, 0x17)); // Laranja
        }
        return new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)); // Verde
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converte multibinding para texto de status do estoque
/// </summary>
public class EstoqueStatusTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 &&
            values[0] is bool sem &&
            values[1] is bool baixo)
        {
            if (sem) return "SEM ESTOQUE";
            if (baixo) return "BAIXO";
        }
        return "OK";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converte enum para Visibility (True = Visible se o valor do enum corresponder ao parâmetro)
/// </summary>
public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;

        var enumValue = value.ToString();
        var expectedValue = parameter.ToString();

        return enumValue == expectedValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}