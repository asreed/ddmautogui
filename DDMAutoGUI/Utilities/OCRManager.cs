using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DDMAutoGUI.Utilities
{
    /// <summary>
    /// Static utility class for OCR (Optical Character Recognition) processing.
    /// Invokes Python script for image processing and text extraction.
    /// </summary>
    public static class OCRManager
    {
        private static readonly string OcrScriptName = "process_images.py";
        private static readonly string OcrOutputFileName = "OCRResults.json";
        private static readonly string OcrScriptPath = Path.Combine(AppContext.BaseDirectory, "Utilities", "Vision", OcrScriptName);

        /// <summary>
        /// Runs OCR on images in the specified folder using a Python script.
        /// </summary>
        public static async Task<OCRData> RunOCRAsync(string imageInputFolder)
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
        public OCRMetadata metadata { get; set; }
        public OCRTiming[] timings { get; set; }
        public List<OCRResult> results { get; set; }
    }

    public class OCRMetadata
    {
        public DateTime start_time { get; set; }
        public float total_time_seconds { get; set; }
        public int image_count { get; set; }
    }

    public class OCRTiming
    {
        public string file { get; set; }
        public float compress_time_seconds { get; set; }
        public float ocr_time_seconds { get; set; }
    }

    public class OCRResult
    {
        public string file { get; set; }
        public List<OCRString> strings { get; set; }
    }

    public class OCRString
    {
        public string text { get; set; }
        public float score { get; set; }
    }

    /// <summary>
    /// Extension methods for OCR data processing and analysis.
    /// </summary>
    public static class OCRDataExtensions
    {
        public static string GetBestResultForFile(this OCRData ocrData, string fileName)
        {
            var result = ocrData.results.FirstOrDefault(r => r.file == fileName);
            if (result != null && result.strings.Count > 0)
            {
                var bestString = result.strings.OrderByDescending(s => s.score).First();
                return bestString.text;
            }
            return null;
        }

        public static string GetToolType(this OCRData ocrData, string fileName)
        {
            var result = ocrData.results.FirstOrDefault(r => r.file == fileName);
            if (result != null && result.strings.Count > 0)
            {
                if (result.strings.Any(s => s.text.StartsWith("57-")))
                {
                    return "ddm_57";
                }
                else if (result.strings.Any(s => s.text.StartsWith("95-")))
                {
                    return "ddm_95";
                }
                else if (result.strings.Any(s => s.text.StartsWith("116-")))
                {
                    return "ddm_116";
                }
                else if (result.strings.Any(s => s.text.StartsWith("170-")))
                {
                    return "ddm_170";
                }
            }
            return null;
        }

        public static string GetToolSN(this OCRData ocrData, string motorName, string fileName)
        {
            string toolPrefix = motorName switch
            {
                "ddm_57" => "57-",
                "ddm_95" => "95-",
                "ddm_116" => "116-",
                "ddm_170" => "170-",
                "ddm_170_tall" => "", // Define prefix if needed
                _ => ""
            };

            if (string.IsNullOrEmpty(toolPrefix))
            {
                return null;
            }

            var result = ocrData.results.FirstOrDefault(r => r.file == fileName);
            if (result != null && result.strings.Count > 0)
            {
                var snString = result.strings.FirstOrDefault(s => s.text.StartsWith(toolPrefix));
                if (snString != null)
                {
                    return snString.text;
                }

                Debug.Print($"Could not find string with correct prefix {toolPrefix}");
            }
            return null;
        }

        public static string GetRingSN(this OCRData ocrData, string motorName, string fileName)
        {
            int snLength = motorName switch
            {
                "ddm_116" => 12,
                _ => 0
            };

            if (snLength == 0)
            {
                return null;
            }

            var result = ocrData.results.FirstOrDefault(r => r.file == fileName);
            if (result != null && result.strings.Count > 0)
            {
                var snString = result.strings
                    .FirstOrDefault(s => s.text.Length == snLength && s.text.Contains("TW"));

                if (snString != null)
                {
                    return snString.text;
                }

                Debug.Print(snLength > 0
                    ? $"Could not find string of correct length ({snLength}) with 'TW'"
                    : "Could not find 'TW' in string of correct length");
            }
            return null;
        }
    }
}