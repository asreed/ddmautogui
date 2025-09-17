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
    public partial class LocalDataPanel : UserControl
    {
        public LocalDataPanel()
        {
            InitializeComponent();
            LocalData data = App.LocalDataManager.localData;
            PopulateLocalDataTree(data);
            PopulateRawLocalData(data);

        }







        private void PopulateRawLocalData(LocalData data)
        {
            LocalDataTxt.Clear();
            string dataString = App.LocalDataManager.SerializeDataFromJson(data);
            LocalDataTxt.Text = dataString;
        }




        private void PopulateLocalDataTree(LocalData data)
        {
            LocalDataTreeViewRoot.Items.Clear();
            LocalDataTreeViewRoot.Header = "Local Data";
            GenerateTree(data, LocalDataTreeViewRoot);

        }
        private void GenerateTree(object obj, TreeViewItem parent)
        {
            if (obj == null) return;

            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                var p = property.PropertyType;
                if (p.IsClass && !p.IsArray && p != typeof(string))
                {
                    // Recursively add properties of nested class

                    //Debug.Print($"Adding class node. {property.Name}");
                    TreeViewItem nestedParent = new TreeViewItem
                    {
                        Header = property.Name,
                        IsExpanded = true,
                    };
                    parent.Items.Add(nestedParent);
                    GenerateTree(property.GetValue(obj), nestedParent);
                }
                else if (p.IsClass && p.IsArray && p != typeof(string))
                {

                    // Recursively add properties of array

                    //Debug.Print($"Adding array node. {property.Name}");
                    Array array = (Array)property.GetValue(obj);
                    TreeViewItem arrayParent = new TreeViewItem
                    {
                        Header = $"{property.Name}",
                        IsExpanded = true,
                    };
                    parent.Items.Add(arrayParent);
                    int index = 0;
                    foreach (var element in array)
                    {
                        TreeViewItem elementParent = new TreeViewItem
                        {
                            Header = $"[{index}]",
                            IsExpanded = true,
                        };
                        arrayParent.Items.Add(elementParent);
                        GenerateTree(element, elementParent);
                        index++;
                    }
                }
                else
                {
                    // Add individual property

                    //Debug.Print($"Adding property. {property.Name}: {property.GetValue(obj)}");
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
            PopulateLocalDataTree(App.LocalDataManager.localData);
            PopulateLocalDataTree(App.LocalDataManager.localData);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            App.LocalDataManager.SaveLocalDataToFile();
            PopulateLocalDataTree(App.LocalDataManager.localData);
            PopulateLocalDataTree(App.LocalDataManager.localData);
        }
    }
}
