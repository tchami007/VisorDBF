using System.Globalization;
using System.Windows.Data;
namespace VisorDBF.UI.Converters;

public class StringEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var val = value?.ToString();
        var param = parameter?.ToString();
        if (param == "\\t") param = "\t";
        return val == param;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            var param = parameter?.ToString() ?? string.Empty;
            if (param == "\\t") param = "\t";
            return param;
        }
        return Binding.DoNothing;
    }
}
