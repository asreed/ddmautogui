using DDMAutoGUI.Utilities;
using DDMAutoGUI.Vision;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

/// <summary>
/// Manages the results of the DDM process, including options selected, shots taken, heights measured, and process logs. Provides functionality to save results to a file and open the results directory.
/// </summary>

namespace DDMAutoGUI.Services
{
    public class ResultsVersionInfo
    {
        public string? gui_version { get; set; }
        public string? tcs_version { get; set; }
        public string? pac_version { get; set; }
        public string? polarity_version { get; set; }
    }
    public class ResultsShotData
    {
        // Contains only directly measured shot data

        public string? motor_type { get; set; }
        public bool? shot_result { get; set; }
        public string? shot_message { get; set; }
        public int? id_valve_num { get; set; }
        public float? id_pressure { get; set; }
        public float? id_time { get; set; }
        public float? id_vol { get; set; }
        public int? od_valve_num { get; set; }
        public float? od_pressure { get; set; }
        public float? od_time { get; set; }
        public float? od_vol { get; set; }
    }

    public class ResultsReferenceData
    {
        // Contains reference/calibration data 
        // (may be redundant to settings and local data)
        public string? id_substance { get; set; }
        public float? id_target_vol { get; set; }
        public float? id_target_flow { get; set; }
        public float? id_calib_pressure { get; set; }
        public string? od_substance { get; set; }
        public float? od_target_vol { get; set; }
        public float? od_target_flow { get; set; }
        public float? od_calib_pressure { get; set; }
        public float? sys_1_autocal_sf { get; set; }
        public float? sys_2_autocal_sf { get; set; }
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
        public string? ring_sn_user { get; set; }
        public string? ring_sn_detected { get; set; }
        public string? tool_sn_detected { get; set; }
        public OCRData? ocr_data { get; set; }
        public bool? overall_part_cycle_result { get; set; }
        public string? overall_part_cycle_message { get; set; }
        public ResultsShotData? shot_data { get; set; }
        public ResultsReferenceData? reference_data { get; set; }
        public ResultsVersionInfo? version_info { get; set; }
        public HeightVerificationResult? height_verification_result { get; set; }
        public DAQMatlabResults? daq_matlab_results { get; set; }
        public List<ResultsLogLine>? process_log { get; set; }
    }

    public class ResultsService : IResultsService
    {
        private readonly IServiceProvider _serviceProvider;
        private IControllerService? _controllerService;

        public string saveMainDirectory = AppDomain.CurrentDomain.BaseDirectory + "Results\\";
        public string saveFolderPrefix = "Ring_";
        public string saveFolderNoSNPrefix = "Ring_No_SN_";

        public string fileNameResults = "ProcessResults";

        public string DateFormatLong { get; } = "MM-dd-yyyy HH:mm:ss.fff";
        public string DateFormatShort { get; } = "HH:mm:ss.ff";
        public string DateFormatFolder { get; } = "yyMMdd_HHmmss";

        public event EventHandler UpdateProcessLog;

        public Results currentResults { get; set; }
        public string currentResultsFolderPath { get; set; }

        public ResultsService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            currentResults = null;
            Debug.Print("Process results service initialized");
        }

        private IControllerService? GetControllerService()
            => _controllerService ??=
               _serviceProvider.GetService(typeof(IControllerService)) as IControllerService;

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
            if (results.ring_sn_detected == null || results.ring_sn_detected == "")
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

            if (results.shot_data.motor_type == null)
            {
                message = "No shot data available";
                return;
            }

            try
            {
                float vol_id = results.shot_data.id_vol.Value;
                float vol_od = results.shot_data.od_vol.Value;
                float target_vol_id = motorSettings.shot_settings.id_target_vol.Value;
                float target_vol_od = motorSettings.shot_settings.od_target_vol.Value;

                //float dev_id = settings.dispense_system.id_vol_max_err_percent.Value;
                //float dev_od = settings.dispense_system.od_vol_max_err_percent.Value;

                //if (Math.Abs(target_vol_id - vol_id) / target_vol_id * 100 > dev_id)
                //{
                //    message = $"ID volume {vol_id:F3} mL is outside of acceptable deviation {dev_id:F1}% from target {target_vol_id:F3} mL";
                //    return;
                //}
                //if (Math.Abs(target_vol_od - vol_od) / target_vol_od * 100 > dev_od)
                //{
                //    message = $"OD volume {vol_id:F3} mL is outside of acceptable deviation {dev_id:F1}% from target {target_vol_id:F3} mL";
                //    return;
                //}

                pass = true;
                message = "Process completed successfully";
            }
            catch
            {
                message = "Unknown error processing shot data";
                return;
            }
        }

        // ==================================================================
        // Result object handling

        public Results CreateNewResults()
        {

            string _tcs_version = String.Empty;
            string _pac_version = String.Empty;
            var _controllerService = GetControllerService();
            if (_controllerService == null)
            {
                Debug.Print("Controller service is not available. Cannot create new results.");
            } else
            {
                _tcs_version = _controllerService.CONNECTION_STATE.connectedTCS;
                _pac_version = _controllerService.CONNECTION_STATE.connectedPAC;
            }

            if (currentResults == null)
            {
                currentResults = new Results
                {
                    shot_data = new ResultsShotData(),
                    process_log = new List<ResultsLogLine>(),
                    version_info = new ResultsVersionInfo
                    {
                        gui_version = ReleaseInfo.GetCurrentVersion(),
                        tcs_version = _tcs_version,
                        pac_version = _pac_version,
                        polarity_version = String.Empty
                    }
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

            if (currentResults.ring_sn_detected == null || currentResults.ring_sn_detected == "")
            {
                resultsFolderPath = saveMainDirectory + saveFolderNoSNPrefix + DateTime.Now.ToString(DateFormatFolder);
                zipFolderPath = resultsFolderPath;
            }
            else
            {
                resultsFolderPath = saveMainDirectory + saveFolderPrefix + currentResults.ring_sn_detected + "_" + DateTime.Now.ToString(DateFormatFolder);
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
            string resultsString = JsonSerializer.Serialize(currentResults, options);
            string resultsFilePath = currentResultsFolderPath + "\\" + fileNameResults + ".json";

            File.WriteAllText(resultsFilePath, resultsString);
        }

        public void RenameResultsFolder(string ringSN)
        {
            if (currentResultsFolderPath == null || currentResultsFolderPath == "")
            {
                Debug.Print("Current results folder path is null or empty. Cannot rename folder.");
                return;
            }
            if (ringSN == null || ringSN == "")
            {
                Debug.Print("New ring SN is null or empty. Cannot rename folder.");
                return;
            }
            string newFolderPath = saveMainDirectory + saveFolderPrefix + ringSN + "_" + DateTime.Now.ToString(DateFormatFolder);
            Directory.Move(currentResultsFolderPath, newFolderPath);
            currentResultsFolderPath = newFolderPath;
        }

        public void CopyPhotoToResultsFolder(string photoPath, string fileName)
        {
            string destPath = currentResultsFolderPath + "\\" + fileName + ".jpg";
            File.Copy(photoPath, destPath, true);
        }

        public void CopyPolarityPlotToResultsFolder(string plotPath, string fileName)
        {
            string destPath = currentResultsFolderPath + "\\" + fileName + ".png";
            File.Copy(plotPath, destPath, true);
        }

        public void CopyPolarityDataToResultsFolder(string dataPath, string fileName)
        {
            string destPath = currentResultsFolderPath + "\\" + fileName + ".json";
            File.Copy(dataPath, destPath, true);
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
                sb.Append(currentResults.process_log[i].timestamp?.ToString(DateFormatLong));
                sb.Append(": ");
                sb.Append(currentResults.process_log[i].message?.ToString());
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public string GetCurrentResultsAsString()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(currentResults, options);
        }
    }
}
