using DDMAutoGUI.utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class EditSettingsPanel : UserControl
    {
        public EditSettingsPanel()
        {
            InitializeComponent();
            PopulateSettingsFields(App.SettingsManager.GetAllSettings());

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }





        private void PopulateSettingsFields(CellSettings settings)
        {
            GenerateFields(settings, FieldsPanel);
        }

        private void GenerateFields(object obj, StackPanel parent)
        {
            if (obj == null) return;

            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    // Recursively add properties of nested class
                    StackPanel nestedParent = InsertFieldGroup(property.Name, parent);
                    GenerateFields(property.GetValue(obj), nestedParent);
                }
                else
                {
                    // Process the property (e.g., print its name and value)
                    Debug.Print($"Property Name: {property.Name}, Value: {property.GetValue(obj)}");
                    InsertField(property.Name, property.GetValue(obj)?.ToString() ?? "null", parent);
                }
            }
        }

        private StackPanel InsertFieldGroup(string header, StackPanel parent)
        {
            var converter = new UnderscoreEscapeConverter();
            GroupBox groupBox = new GroupBox
            {
                Header = converter.Convert(header, null, null, null),
                Foreground = Brushes.Black,
                Background = new SolidColorBrush(Color.FromArgb(3, 0, 0, 0)),
                Margin = new Thickness(3),
                Padding = new Thickness(12,3,3,3),
                //FontWeight = FontWeights.Bold
            };
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
            };
            groupBox.Content = stackPanel;
            parent.Children.Add(groupBox);
            return stackPanel;

        }

        private void InsertField(string label, string value, StackPanel parent)
        {
            SettingsField field = new SettingsField
            {
                LabelText = label,
                ValueText = value,
                FontWeight = FontWeights.Normal,
            };
            parent.Children.Add(field);
        }
    }
}
