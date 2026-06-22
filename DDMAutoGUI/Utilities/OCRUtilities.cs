using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Static utility class for OCR (Optical Character Recognition) processing.
    /// Invokes Python script for image processing and text extraction.
    /// </summary>
    public static class OCRUtilities
    {
        private static readonly string OcrScriptName = "process_images.py";
        private static readonly string OcrOutputFileName = "OCRResults.json";
        private static readonly string OcrScriptPath = Path.Combine(AppContext.BaseDirectory, "Utilities", "Vision", OcrScriptName);

        /// <summary>
        /// Runs OCR on images in the specified folder using a Python script.
        /// </summary>
        public static async Task<OCRData> RunOCR(string imageInputFolder)
        {
            string ocrOutputFile = Path.Combine(imageInputFolder, OcrOutputFileName);

            OCRData result = await Task.Run(() =>
            {
                string arguments =
                    $"\"{OcrScriptPath}\" " +
                    $"--input-folder \"{imageInputFolder}\" " +
                    $"--output-file \"{ocrOutputFile}\" " +
                    $"--min-score 0.1";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                            Debug.WriteLine(e.Data);
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                            Debug.WriteLine("ERR: " + e.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    int exitCode = process.ExitCode;
                    Debug.WriteLine($"Python process exited with code {exitCode}");
                }

                if (File.Exists(ocrOutputFile))
                {
                    try
                    {
                        string rawJson = File.ReadAllText(ocrOutputFile);
                        var ocrResult = JsonSerializer.Deserialize<OCRData>(rawJson);
                        Debug.Print("OCR results deserialized successfully");
                        return ocrResult;
                    }
                    catch (JsonException ex)
                    {
                        Debug.Print($"Error deserializing OCR results: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    Debug.Print($"OCR output file not found: {ocrOutputFile}");
                    return null;
                }
            });

            return result;
        }
    }

    /// <summary>
    /// Represents OCR (Optical Character Recognition) data extracted from images.
    /// </summary>
    public class OCRData
    {
        public string text { get; set; }
        public double confidence { get; set; }
        // Add additional properties as needed based on your Python script output
    }
}