using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    public partial class LocalDataPanel : UserControl
    {
        private readonly ILocalDataService _localDataManager;

        public LocalDataPanel()
        {
            InitializeComponent();

            _localDataManager = App.Services?.GetService<ILocalDataService>();

            if (_localDataManager == null)
            {
                // Design-time or DI misconfiguration — degrade gracefully
                return;
            }

            Loaded += LocalDataPanel_Loaded;
        }

        private void LocalDataPanel_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshPanel();
        }

        private void RefreshPanel()
        {
            if (_localDataManager == null) return;

            LocalData data = _localDataManager.GetLocalData();
            if (data == null) return;

            PopulateLocalDataTree(data);
            PopulateRawLocalData(data);
        }

        private void PopulateRawLocalData(LocalData data)
        {
            LocalDataTxt.Clear();
            LocalDataTxt.Text = _localDataManager.SerializeDataFromJson(data);
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

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                Type propertyType = property.PropertyType;

                if (propertyType.IsClass && !propertyType.IsArray && propertyType != typeof(string))
                {
                    var nestedParent = new TreeViewItem { Header = property.Name, IsExpanded = true };
                    parent.Items.Add(nestedParent);
                    GenerateTree(property.GetValue(obj), nestedParent);
                }
                else if (propertyType.IsArray)
                {
                    var arrayParent = new TreeViewItem { Header = property.Name, IsExpanded = true };
                    parent.Items.Add(arrayParent);

                    if (property.GetValue(obj) is Array array)
                    {
                        int index = 0;
                        foreach (var element in array)
                        {
                            var elementParent = new TreeViewItem { Header = $"[{index}]", IsExpanded = true };
                            arrayParent.Items.Add(elementParent);
                            GenerateTree(element, elementParent);
                            index++;
                        }
                    }
                }
                else
                {
                    parent.Items.Add(new TreeViewItem
                    {
                        Header = $"{property.Name}: {property.GetValue(obj)?.ToString() ?? "null"}"
                    });
                }
            }
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            _localDataManager?.LoadLocalData();
            RefreshPanel();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_localDataManager == null) return;

            LocalData newData = _localDataManager.DeserializeLocalDataFromString(LocalDataTxt.Text);
            if (newData == null) return;

            _localDataManager.SetLocalData(newData.Clone());
            _localDataManager.SaveLocalDataToFile(newData);
            RefreshPanel();
        }
    }
}
