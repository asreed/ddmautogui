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
            CellSettings settings = App.SettingsManager.GetAllSettings();
            PopulateSettingsTree(settings);
            PopulateRawSettings(settings);

        }







        private void PopulateRawSettings(CellSettings settings)
        {
            settingsTxb.Clear();
            string settingsString = App.SettingsManager.SerializeSettingsToJson(settings);
            settingsTxb.Text = settingsString;
        }




        private void PopulateSettingsTree(CellSettings settings)
        {
            SettingsTreeViewRoot.Items.Clear();
            GenerateTree(settings, SettingsTreeViewRoot);

        }
        private void GenerateTree(object obj, TreeViewItem parent)
        {
            if (obj == null) return;

            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    // Recursively add properties of nested class
                    TreeViewItem nestedParent = new TreeViewItem
                    {
                        Header = property.Name,
                        IsExpanded = true,
                    };
                    parent.Items.Add(nestedParent);
                    GenerateTree(property.GetValue(obj), nestedParent);
                }
                else
                {
                    //Debug.Print($"Property Name: {property.Name}, Value: {property.GetValue(obj)}");
                    TreeViewItem item = new TreeViewItem
                    {
                        Header = $"{property.Name}: {property.GetValue(obj)?.ToString() ?? "null"}",
                    };
                    parent.Items.Add(item);
                }
            }
        }



        private void GenerateFields(object obj, StackPanel parent)
        {
            //if (obj == null) return;

            //Type type = obj.GetType();
            //PropertyInfo[] properties = type.GetProperties();

            //foreach (PropertyInfo property in properties)
            //{
            //    if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            //    {
            //        // Recursively add properties of nested class
            //        StackPanel nestedParent = InsertFieldGroup(property.Name, parent);
            //        GenerateFields(property.GetValue(obj), nestedParent);
            //    }
            //    else
            //    {
            //        // Process the property (e.g., print its name and value)
            //        Debug.Print($"Property Name: {property.Name}, Value: {property.GetValue(obj)}");
            //        InsertField(property.Name, property.GetValue(obj)?.ToString() ?? "null", parent);
            //    }
            //}
        }

        private StackPanel InsertFieldGroup(string header, StackPanel parent)
        {
            //var converter = new UnderscoreEscapeConverter();
            //Label headerLabel = new Label
            //{
            //    Content = converter.Convert(header, null, null, null),
            //    FontWeight = FontWeights.Bold,
            //    Margin = new Thickness(0),
            //    Padding = new Thickness(0)
            //};
            //StackPanel stackPanel = new StackPanel
            //{
            //    Orientation = Orientation.Vertical,
            //    Background = new SolidColorBrush(Color.FromArgb(3, 0, 0, 0)),
            //    Margin = new Thickness(12,3,3,3),
            //};
            //parent.Children.Add(headerLabel);
            //parent.Children.Add(stackPanel);
            //return stackPanel;
            return null;

        }

        //private void InsertField(string label, string value, StackPanel parent)
        //{
        //    SettingsField field = new SettingsField
        //    {
        //        LabelText = label,
        //        ValueText = value,
        //        FontWeight = FontWeights.Normal,
        //        Margin = new Thickness(0),
        //        Padding = new Thickness(0)
        //    };
        //    parent.Children.Add(field);
        //}

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            App.SettingsManager.ReloadSettings();
            PopulateSettingsTree(App.SettingsManager.GetAllSettings());
            PopulateRawSettings(App.SettingsManager.GetAllSettings());
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            CellSettings newSettings = App.SettingsManager.DeserializeSettingsFromJson(settingsTxb.Text);
            App.SettingsManager.SaveSettingsToController(newSettings);
        }
    }
}
