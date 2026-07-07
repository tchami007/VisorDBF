using System.Globalization;
using System.Windows.Data;
namespace VisorDBF.UI.Converters;

public class ColumnFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;
        try
        {
            if (value is IFormattable formattable)
                return formattable.ToString(parameter as string, CultureInfo.CurrentCulture);
            return value.ToString() ?? string.Empty;
        }
        catch (FormatException)
        {
            return "ERROR";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
