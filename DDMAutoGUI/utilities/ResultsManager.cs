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
    public class ResultsShotData
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

    public class ResultsHeightMeasurement
    {
        public float? t { get; set; }
        public float? z { get; set; }
    }

    public class ResultsLogLine
    {
        public DateTime? timestamp { get; set; }
        public string? message { get; set; }
    }

    public class Results
    {
        public DateTime? date_saved { get; set; }
        public string? ring_sn { get; set; }
        public List<ResultsHeightMeasurement>? ring_heights { get; set; }
        public List<ResultsHeightMeasurement>? mag_heights { get; set; }
        public ResultsShotData? shot_data { get; set; }
        public List<ResultsLogLine>? process_log { get; set; }


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

        public Results currentResults;


        public ResultsManager()
        {
            currentResults = null;
            Debug.Print("Process results manager initialized");
        }

        public Results CreateNewResults()
        {
            if (currentResults == null)
            {
                currentResults = new Results
                {
                    ring_heights = new List<ResultsHeightMeasurement>(),
                    mag_heights = new List<ResultsHeightMeasurement>(),
                    shot_data = new ResultsShotData(),
                    process_log = new List<ResultsLogLine>()
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

        public void AddShotDataToResults(ResultsShotData data)
        {
            currentResults.shot_data = data;
        }

        public void AddToLog(string line)
        {
            if (currentResults == null)
            {
                Debug.Print("Current results are null. Cannot add to log.");
                return;
            }
            ResultsLogLine newLine = new ResultsLogLine();
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
            string resultsString = JsonSerializer.Serialize<Results>(currentResults, options);

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
