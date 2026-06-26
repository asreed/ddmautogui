using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DDMAutoGUI.Converters
{
    /// <summary>
    /// Converts a bool readout value to a human-readable "Yes"/"No" string.
    /// </summary>
    public sealed class BoolToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? "Yes" : "No";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Picks a readout background brush from the bound value and whether the readouts
    /// are currently live. Disabled readouts always fall back to the neutral colour.
    /// values[0] = bool state value, values[1] = bool ReadoutsEnabled.
    /// </summary>
    public sealed class ReadoutBackgroundConverter : IMultiValueConverter
    {
        private static readonly Brush ActiveBrush =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD3DDF5"));
        private static readonly Brush NeutralBrush =
            new SolidColorBrush(Colors.WhiteSmoke);

        static ReadoutBackgroundConverter()
        {
            ActiveBrush.Freeze();
            NeutralBrush.Freeze();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool enabled = values.Length > 1 && values[1] is bool e && e;
            bool active = values.Length > 0 && values[0] is bool v && v;
            return enabled && active ? ActiveBrush : NeutralBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}