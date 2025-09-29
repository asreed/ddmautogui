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
        public string? motor_type { get; set; }
        public bool? shot_result { get; set; }
        public string? shot_message { get; set; }
        public int? valve_num_id { get; set; }
        public int? valve_num_od { get; set; }
        public float? pressure_id { get; set; }
        public float? pressure_od { get; set; }
        public float? time_id { get; set; }
        public float? time_od { get; set; }
        public float? vol_id { get; set; }
        public float? vol_od { get; set; }
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
        public string? tool_sn { get; set; }
        public ResultsShotData? shot_data { get; set; }
        public bool? overall_process_result { get; set; }
        public string? overall_proces_message { get; set; }
        public List<ResultsHeightMeasurement>? ring_heights { get; set; }
        public List<ResultsHeightMeasurement>? mag_heights { get; set; }
        public List<ResultsLogLine>? process_log { get; set; }

    }

    public class ResultsManager
    {

        public string saveMainDirectory = AppDomain.CurrentDomain.BaseDirectory + "Results\\";
        public string saveFolderPrefix = "Ring_";
        public string saveFolderNoSNPrefix = "Ring_No_SN_";

        public string fileNameResults = "ProcessResults";
        public string fileNamePhotoBefore = "Before";
        public string fileNamePhotoAfter = "After";

        public string dateFormatLong = "MM-dd-yyy HH:mm:ss.fff";
        public string dateFormatShort = "HH:mm:ss.ff";
        public string dateFormatFolder = "yyMMdd_HHmmss";

        public event EventHandler UpdateProcessLog;

        public Results currentResults;
        public string currentResultsFolderPath;


        public ResultsManager()
        {
            currentResults = null;
            Debug.Print("Process results manager initialized");
        }



        // ==================================================================
        // Pass/fail determination

        public void DeterminePassFail(Results results, CellSettings settings, CSMotor motorSettings, out bool pass, out string message)
        {
            pass = false;
            message = "Unable to determine pass/fail";

            if (results == null)
            {
                message = "Results object is null";
                return;
            }
            if (results.ring_sn == null || results.ring_sn == "")
            {
                message = "Ring serial number is missing or empty";
                return;
            }
            if (results.shot_data == null)
            {
                message = "Shot data is null";
                return;
            }
            if (settings == null || motorSettings == null)
            {
                message = "Settings object is null";
                return;
            }

            float vol_id = results.shot_data.vol_id.Value;
            float vol_od = results.shot_data.vol_od.Value;
            float target_vol_id = motorSettings.shot_settings.target_vol_id.Value;
            float target_vol_od = motorSettings.shot_settings.target_vol_od.Value;
            float dev_id = settings.dispense_pass_criteria.max_id_vol_dev_percent.Value;
            float dev_od = settings.dispense_pass_criteria.max_od_vol_dev_percent.Value;

            if (Math.Abs(target_vol_id - vol_id) / target_vol_id * 100 > dev_id)
            {
                message = $"ID volume {vol_id:F3} mL is outside of acceptable deviation {dev_id:F1}% from target {target_vol_id:F3} mL";
                return;
            }
            if (Math.Abs(target_vol_od - vol_od) / target_vol_od * 100 > dev_od)
            {
                message = $"OD volume {vol_id:F3} mL is outside of acceptable deviation {dev_id:F1}% from target {target_vol_id:F3} mL";
                return;
            }

            pass = true;
            message = "Process completed successfully";
        }















        // ==================================================================
        // Result object handling

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

        public string CreateResultsFolder()
        {
            if (currentResults == null)
            {
                Debug.Print("Current results are null. Cannot save to file.");
                return null;
            }

            string resultsFolderPath;
            string resultsFilePath;
            string zipFolderPath;

            if (currentResults.ring_sn == null || currentResults.ring_sn == "")
            {
                resultsFolderPath = saveMainDirectory + saveFolderNoSNPrefix + DateTime.Now.ToString(dateFormatFolder);
                zipFolderPath = resultsFolderPath;
            }
            else
            {
                resultsFolderPath = saveMainDirectory + saveFolderPrefix + currentResults.ring_sn + "_" + DateTime.Now.ToString(dateFormatFolder);
                zipFolderPath = resultsFolderPath;
            }

            Directory.CreateDirectory(resultsFolderPath);
            currentResultsFolderPath = resultsFolderPath;
            return resultsFolderPath;
        }

        public void SaveDataToFile()
        {
            currentResults.date_saved = DateTime.Now;
            var options = new JsonSerializerOptions { WriteIndented = true };
            string resultsString = JsonSerializer.Serialize<Results>(currentResults, options);
            string resultsFilePath = currentResultsFolderPath + "\\" + fileNameResults + ".json";

            File.WriteAllText(resultsFilePath, resultsString);
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
