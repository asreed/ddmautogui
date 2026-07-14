using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Static utility class for DAQ and Matlab processing operations.
    /// </summary>
    public static class DAQUtilities
    {
        private static readonly string ExeDirectory = AppDomain.CurrentDomain.BaseDirectory + "MatlabExecutables\\";
        private static readonly string ExeName = "PolarityTest.exe";
        private static readonly string ResultsDirectory = AppDomain.CurrentDomain.BaseDirectory + "MatlabResults\\";
        private static readonly string ResultsName = "PolarityResults.json";
        private static readonly string PlotName = "PolarityPlot.png";

        /// <summary>
        /// Collects data and processes with Matlab executable for polarity detection.
        /// </summary>
        public static async Task<DAQMatlabResults> CollectDataAndProcessML(string motorName)
        {
            string exePath = Path.Combine(ExeDirectory, ExeName);
            string resultsFilePath = Path.Combine(ResultsDirectory, ResultsName);

            Debug.Print($"Starting Matlab process for motor {motorName}");

            var process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = $"{motorName} {ResultsDirectory} {ResultsName}";
            process.Start();

            await process.WaitForExitAsync();

            var result = new DAQMatlabResults();
            Debug.Print($"Reading Matlab results file from: {resultsFilePath}");

            try
            {
                if (File.Exists(resultsFilePath))
                {
                    string rawJson = File.ReadAllText(resultsFilePath);
                    result = JsonSerializer.Deserialize<DAQMatlabResults>(rawJson);
                }
                else
                {
                    Debug.Print("Matlab results file does not exist!");
                }
            }
            catch (JsonException ex)
            {
                Debug.Print($"Error deserializing Matlab results file: {ex.Message}");
            }

            if (result != null)
            {
                result.results_directory = ResultsDirectory;
                result.plot_filename = PlotName;
                Debug.Print($"Results:");
                Debug.Print($"  Version: {result.version}");
                Debug.Print($"  Result: {result.result}");
                Debug.Print($"  Peaks Detected: {result.peaks_detected}");
                Debug.Print($"  Short Wavelengths: {result.num_short_wavelengths}");
                Debug.Print($"  Long Wavelengths: {result.num_long_wavelengths}");
                Debug.Print($"  Error Code: {result.error_code}");
                Debug.Print($"  Error Message: {result.error_message}");
                Debug.Print($"  Plot File Name: {result.plot_filename}");
                Debug.Print($"  Results Directory: {result.results_directory}");
            }
            else
            {
                Debug.Print("Results structure null");
            }

            // Suppress raw hall data from being copied into results (maybe we want to consolidate data files?)
            // result.hall_data = null;
            return result;
        }
    }

    /// <summary>
    /// Represents results from Matlab DAQ processing (polarity test).
    /// Corresponds to version 1.0.0 of the Matlab function.
    /// Result values: -1 (did not complete), 0 (completed, polarity check failed), 1 (completed, polarity check passed)
    /// </summary>
    public class DAQMatlabResults
    {
        public string version { get; set; }
        public int result { get; set; }
        public int error_code { get; set; }
        public string error_message { get; set; }
        public int peaks_detected { get; set; }
        public int num_short_wavelengths { get; set; }
        public int num_long_wavelengths { get; set; }
        public string results_directory { get; set; }
        public string plot_filename { get; set; }
        public double[][] hall_data { get; set; }
    }
}