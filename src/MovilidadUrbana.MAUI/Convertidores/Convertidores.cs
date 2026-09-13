using System.Globalization;

namespace MovilidadUrbana.MAUI.Convertidores;

/// <summary>Verdadero si hay texto: muestra un mensaje de error solo cuando existe.</summary>
public sealed class TextoPresenteConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Invierte un booleano.</summary>
public sealed class NegarConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}
