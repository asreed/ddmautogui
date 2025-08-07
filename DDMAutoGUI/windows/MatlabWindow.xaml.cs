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

namespace DDMAutoGUI.windows {


    public class MatlabResult
    {
        public string? sn {  get; set; }
        public string? result { get; set; }
        public string? file_path_top_input { get; set; }
        public string? file_path_side_input { get; set; }
    }


    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class MatlabWindow : Window
    {


        public MatlabWindow()
        {
            InitializeComponent();
        }

        private void runBtn_Click(object sender, RoutedEventArgs e)
        {

            string exePath = @"C:\Users\areed\Documents\MATLAB\MatlabTestProject1\StandaloneDesktopApp1\output\build\MyDesktopApplication.exe";
            string filePath1 = "fake_path.jpg";
            string filePath2 = "fake_path_2.jpg";

            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = $"{filePath1} {filePath2}";
            process.Start();

            process.WaitForExit();

            string resultsFilePath = AppDomain.CurrentDomain.BaseDirectory + "results\\matlab_results.json";

            MatlabResult result = new MatlabResult();
            Debug.Print("Reading settings file from: " + resultsFilePath);
            try
            {
                if (File.Exists(resultsFilePath))
                {
                    string rawJson = File.ReadAllText(resultsFilePath);
                    result = JsonSerializer.Deserialize<MatlabResult>(rawJson);
                }
                else
                {
                    Debug.Print("Settings file does not exist!");
                }
            }
            catch (JsonException ex)
            {
                Debug.Print("Error deserializing settings file: " + ex.Message);
            }

            resultText.Text = "";
            resultText.Text += $"serial number: {result.sn}\n";
            resultText.Text += $"serial number: {result.result}\n";
            resultText.Text += $"serial number: {result.file_path_top_input}\n";
            resultText.Text += $"serial number: {result.file_path_side_input}\n";

        }

    }
}
