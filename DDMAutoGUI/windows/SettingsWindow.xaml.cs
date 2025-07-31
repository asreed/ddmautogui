using DDMAutoGUI.utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace DDMAutoGUI.windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {


        public SettingsWindow()
        {
            InitializeComponent();
            filePathText.Text = App.SettingsManager.GetSettingsFilePath();
        }

        private void viewRawBtn_Click(object sender, RoutedEventArgs e)
        {

            string rawString = string.Empty;
            string filePath = App.SettingsManager.GetSettingsFilePath();
            rawString = File.ReadAllText(filePath);
            if (rawString == string.Empty)
            {
                rawString = $"Unable to read from file ({filePath})";
            }

            TextDataViewer viewer = new TextDataViewer();
            viewer.PopulateData(rawString, "Raw Settings");
            viewer.Owner = this;
            viewer.Show();
        }

        private void loadParseBtn_Click(object sender, RoutedEventArgs e)
        {
            //SettingsManager.Instance.LoadSettingsFile();
            //DDMSettings settings = SettingsManager.Instance.GetSettings();
            //string settingsString = string.Empty;
            //if (settings != null)
            //{
            //    var options = new JsonSerializerOptions { WriteIndented = true };
            //    settingsString = JsonSerializer.Serialize(settings, options);
            //}
            //else
            //{
            //    settingsString = "No settings parsed from file.";
            //}
            //TextDataViewer viewer = new TextDataViewer();
            //viewer.PopulateData(settingsString, "Parsed Settings");
            //viewer.Owner = this;
            //viewer.Show();
        }

        private void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            App.SettingsManager.OpenFolderToSettingsFile();
        }
    }
}
