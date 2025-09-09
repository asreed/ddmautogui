using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

/// <summary>
/// Manages the results of the DDM process, including options selected, shots taken, heights measured, and process logs. Provides functionality to save results to a file and open the results directory.
/// </summary>




namespace DDMAutoGUI.utilities
{
    public class ProcessResultsShotData
    {
        public int? valve_num_id { get; set; }
        public int? valve_num_od { get; set; }
        public float? pressure_id { get; set; }
        public float? pressure_od { get; set; }
        public float? time_id { get; set; }
        public float? time_od { get; set; }
        public float? vol_id { get; set; }
        public float? vol_od { get; set; }
        public bool? success { get; set; }
        public string? error_message { get; set; }
    }

    public class ProcessResultsHeightMeasurement
    {
        public float? t { get; set; }
        public float? z { get; set; }
    }

    public class ProcessResultsLogLine
    {
        public DateTime? timestamp { get; set; }
        public string? message { get; set; }
    }

    public class ProcessResults
    {
        public DateTime? date_saved { get; set; }
        public string? ring_sn { get; set; }
        public List<ProcessResultsHeightMeasurement>? ring_heights { get; set; }
        public List<ProcessResultsHeightMeasurement>? mag_heights { get; set; }
        public ProcessResultsShotData? shot_data { get; set; }
        public List<ProcessResultsLogLine>? process_log { get; set; }


    }














    public class ResultsManager
    {

        public string saveMainDirectory = AppDomain.CurrentDomain.BaseDirectory + "results\\";
        public string saveFolderPrefix = "ring_";
        public string saveFolderNoSNPrefix = "ring_no_sn_";

        public string fileNameResults = "process_results";
        public string fileNamePhotoBefore = "before";
        public string fileNamePhotoAfter = "after";

        public string dateFormatLong = "MM-dd-yyy HH:mm:ss.fff";
        public string dateFormatShort = "HH:mm:ss.fff";
        public string dateFormatFolder = "yyMMdd_HHmmss";

        public event EventHandler UpdateProcessLog;

        public ProcessResults currentResults;


        public ResultsManager()
        {
            currentResults = null;
            Debug.Print("Process results manager initialized");
        }

        public ProcessResults CreateNewResults()
        {
            if (currentResults == null)
            {
                currentResults = new ProcessResults
                {
                    ring_heights = new List<ProcessResultsHeightMeasurement>(),
                    mag_heights = new List<ProcessResultsHeightMeasurement>(),
                    shot_data = new ProcessResultsShotData(),
                    process_log = new List<ProcessResultsLogLine>()
                };
                return currentResults;
            }
            else
            {
                Debug.Print("Results were not null. Clear results first.");
                return null;
            }
        }

        public void ClearCurrentResults()
        {
            currentResults = null;
        }

        public void AddToLog(string line)
        {
            if (currentResults == null)
            {
                Debug.Print("Current results are null. Cannot add to log.");
                return;
            }
            ProcessResultsLogLine newLine = new ProcessResultsLogLine();
            newLine.timestamp = DateTime.Now;
            newLine.message = line;
            currentResults.process_log.Add(newLine);

            UpdateProcessLog?.Invoke(this, EventArgs.Empty);
        }

        public void SaveDataToFile()
        {
            if (currentResults == null)
            {
                Debug.Print("Current results are null. Cannot save to file.");
                return;
            }
            currentResults.date_saved = DateTime.Now;

            var options = new JsonSerializerOptions { WriteIndented = true };
            string resultsString = JsonSerializer.Serialize<ProcessResults>(currentResults, options);

            string resultsFolderPath;
            string resultsFilePath;
            string zipFolderPath;

            if (currentResults.ring_sn == null || currentResults.ring_sn == "")
            {
                resultsFolderPath = saveMainDirectory + saveFolderNoSNPrefix + DateTime.Now.ToString(dateFormatFolder);
                resultsFilePath = resultsFolderPath + "\\" + fileNameResults + ".json";
                zipFolderPath = resultsFolderPath;
            }
            else
            {
                resultsFolderPath = saveMainDirectory + saveFolderPrefix + currentResults.ring_sn;
                resultsFilePath = resultsFolderPath + "\\" + fileNameResults + ".json";
                zipFolderPath = resultsFolderPath;
            }


            Directory.CreateDirectory(resultsFolderPath);
            File.WriteAllText(resultsFilePath, resultsString);

            File.Copy(App.SettingsManager.GetSettingsFilePath(), resultsFolderPath + "\\settings.json", true);

            ZipFile.CreateFromDirectory(resultsFolderPath, zipFolderPath + ".zip");
        }


        public void OpenBrowserToDirectory()
        {
            string directory = saveMainDirectory;
            Process.Start("explorer.exe", directory);
        }

        public string GetLogAsString()
        {
            if (currentResults == null)
            {
                Debug.Print("Current results are null. Cannot get log.");
                return null;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < currentResults.process_log?.Count; i++)
            {
                sb.Append(currentResults.process_log[i].timestamp?.ToString(dateFormatLong));
                sb.Append(": ");
                sb.Append(currentResults.process_log[i].message?.ToString());
                sb.Append('\n');
            }
            return sb.ToString();
        }


    }
}
