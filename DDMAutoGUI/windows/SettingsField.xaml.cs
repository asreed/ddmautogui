using DDMAutoGUI.utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;








namespace DDMAutoGUI.windows
{

    public class UnderscoreEscapeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.Replace("_", "__"); // Escape underscores
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.Replace("__", "_"); // Un-escape underscores (?)
            }
            return value;
        }
    }













    /// <summary>
    /// Interaction logic for SettingsField.xaml
    /// </summary>
    public partial class SettingsField : UserControl
    {

        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }
        public string ValueText
        {
            get { return (string)GetValue(ValueTextProperty); }
            set { SetValue(ValueTextProperty, value); }
        }

        public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register("Label", typeof(string), typeof(SettingsField), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register("Value", typeof(string), typeof(SettingsField), new PropertyMetadata(string.Empty));


        public SettingsField()
        {
            InitializeComponent();
            this.DataContext = this;
        }

    }
}
