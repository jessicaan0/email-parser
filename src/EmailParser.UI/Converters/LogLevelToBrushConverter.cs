using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace EmailParser.UI.Converters;

/// <summary>
/// Converts a three-letter log level abbreviation (INF, WRN, ERR, etc.)
/// to a colored <see cref="SolidColorBrush"/> for the log-level badge.
/// </summary>
public sealed class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value as string) switch
        {
            "FTL" => new SolidColorBrush(ColorHelper.FromArgb(255, 180, 0, 0)),
            "ERR" => new SolidColorBrush(ColorHelper.FromArgb(255, 220, 50, 50)),
            "WRN" => new SolidColorBrush(ColorHelper.FromArgb(255, 200, 150, 0)),
            "DBG" => new SolidColorBrush(ColorHelper.FromArgb(255, 120, 120, 120)),
            "VRB" => new SolidColorBrush(ColorHelper.FromArgb(255, 160, 160, 160)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 0, 120, 215)), // INF
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
