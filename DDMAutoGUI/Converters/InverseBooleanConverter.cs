using System;
using System.Globalization;
using System.Windows.Data;

namespace DDMAutoGUI.Converters
{
    /// <summary>
    /// Returns the logical negation of a bound bool. Used to enable "connect"-style
    /// controls only while disconnected.
    /// </summary>
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : value;
    }
}