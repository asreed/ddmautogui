using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl
    {
        private readonly ISettingsService _settingsManager;

        public SettingsPanel()
        {
            InitializeComponent();

            _settingsManager = App.Services?.GetService<ISettingsService>();

            if (_settingsManager == null)
            {
                // Design-time or DI misconfiguration — degrade gracefully
                return;
            }

            Loaded += SettingsPanel_Loaded;
        }

        private void SettingsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshPanel();
        }

        private void RefreshPanel()
        {
            if (_settingsManager == null) return;

            CellSettings settings = _settingsManager.GetAllSettings();
            PopulateSettingsTree(settings);
            PopulateSettingsString(settings);
        }

        private void PopulateSettingsString(CellSettings settings)
        {
            settingsTxb.Clear();

            if (settings == null)
            {
                settingsTxb.Text = "No settings loaded";
                return;
            }

            settingsTxb.Text = _settingsManager.SerializeSettingsToJson(settings);
        }

        private void PopulateSettingsTree(CellSettings settings)
        {
            SettingsTreeViewRoot.Items.Clear();

            if (settings == null)
            {
                SettingsTreeViewRoot.Header = "No settings loaded";
                return;
            }

            SettingsTreeViewRoot.Header = "Settings";
            GenerateTree(settings, SettingsTreeViewRoot);
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
            _settingsManager?.ReloadSettings();
            RefreshPanel();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsManager == null) return;

            CellSettings newSettings = _settingsManager.DeserializeSettingsFromJson(settingsTxb.Text);
            if (newSettings == null) return;

            _settingsManager.SaveSettingsToController(newSettings);
        }
    }
}
