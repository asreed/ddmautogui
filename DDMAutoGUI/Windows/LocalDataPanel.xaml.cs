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

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            PopulateLocalDataTree(App.LocalDataManager.localData);
            PopulateRawLocalData(App.LocalDataManager.localData);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            App.LocalDataManager.SaveLocalDataToFile();
            PopulateLocalDataTree(App.LocalDataManager.localData);
            PopulateRawLocalData(App.LocalDataManager.localData);
        }
    }
}
