using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.RightsManagement;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DDMAutoGUI.utilities
{

    public class CellSettings
    {
        public DateTime? last_saved { get; set; }
        public string? camera_top_sn { get; set; }
        public string? camera_side_sn { get; set; }
        public float? laser_delay { get; set; }
        public CSDispesePassCriteria? dispense_pass_criteria { get; set; }
        public CSDispense? dispense_system { get; set; }
        public CSLaser? laser_calib { get; set; }
        public CSMotorCommon? ddm_common { get; set; }
        public CSMotor? ddm_57 { get; set; }
        public CSMotor? ddm_95 { get; set; }
        public CSMotor? ddm_116 { get; set; }
        public CSMotor? ddm_170 { get; set; }
        public CSMotor? ddm_170_tall { get; set; }
    }

    public class CSLocation
    {
        public float? x { get; set; }
        public float? t { get; set; }
    }
    public class CSShot
    {
        public int? sys_num_id { get; set; }
        public int? sys_num_od { get; set; }
        public float? target_vol_id { get; set; }
        public float? target_vol_od { get; set; }
        public float? target_flow_id { get; set; }
        public float? target_flow_od { get; set; }
    }
    public class CSMotorCommon
    {
        public CSLocation? load { get; set; }
        public CSLocation? camera_top { get; set; }
    }
    public class CSMotor
    {
        public CSShot? shot_settings { get; set; }
        public float? post_spin_time { get; set; }
        public float? post_spin_speed { get; set; }
        public int? laser_ring_num { get; set; }
        public int? laser_mag_num { get; set; }
        public CSLocation? camera_side { get; set; }
        public CSLocation? disp_id { get; set; }
        public CSLocation? disp_od { get; set; }
        public CSLocation? laser_mag { get; set; }
        public CSLocation? laser_ring { get; set; }
        public CSLocation? hall_sensor { get; set; }

        public bool IsValid()
        {
            // validate logic. might want to expand checks

            if (disp_id == null || disp_od == null || laser_mag == null || laser_ring == null)
            {
                return false; // invalid if any location is null ...?
            }
            else
            {
                return true;
            }
        }
    }

    public class CSLaserCoeff
    {
        public float? A { get; set; }
        public float? phi { get; set; }
        public float? R2 { get; set; }
    }

    public class CSLaser
    {
        public CSLaserCoeff? ddm_57_coeff { get; set; }
        public CSLaserCoeff? ddm_95_coeff { get; set; }
        public CSLaserCoeff? ddm_116_coeff { get; set; }
        public CSLaserCoeff? ddm_170_coeff { get; set; }
    }

    public class CSDispenseCalib
    {
        public float? pressure { get; set; }
        public float? flow { get; set; }
    }

    public class CSDispense
    {
        public string? sys_1_contents { get; set; }
        public string? sys_2_contents { get; set; }
        public float? sys_1_max_pressure { get; set; }
        public float? sys_2_max_pressure { get; set; }
        public float? sys_1_max_pressure_dev_percent { get; set; }
        public float? sys_2_max_pressure_dev_percent { get; set; }
        public CSDispenseCalib[] sys_1_flow_calib { get; set; }
        public CSDispenseCalib[] sys_2_flow_calib { get; set; }
    }

    public class CSDispesePassCriteria
    {
        public float? max_id_vol_dev_percent { get; set; }
        public float? max_od_vol_dev_percent { get; set; }
    }








    public class SettingsManager
    {
        //private string settingsFilePath = AppDomain.CurrentDomain.BaseDirectory + "settings\\settings.json";
        private string settingsFTPPath = "/flash/ddm_cell/Settings.json";
        private string settingsLocalName = "Settings.json";

        public enum DDMSize
        {
            none,
            ddm_57,
            ddm_95,
            ddm_116,
            ddm_170,
            ddm_170_tall
        }
        public DDMSize selectedSize = DDMSize.ddm_116; // default to 116
        private CellSettings currentSettings = new CellSettings();

        public SettingsManager()
        {
            //currentSettings = ReadSettingsFromController();
            App.ControllerManager.ControllerConnected += SettingsManager_OnConnected;
            App.ControllerManager.ControllerDisconnected += SettingsManager_OnDisconnected;
            Debug.Print("Settings manager initialized");
        }


        public async void SettingsManager_OnConnected(object sender, EventArgs e)
        {
            Debug.Print("Settings manager detected controller connected");
            currentSettings = ReadSettingsFromController();
        }

        public void SettingsManager_OnDisconnected(object sender, EventArgs e)
        {
            Debug.Print("Settings manager detected controller disconnected");
            currentSettings = null;
        }

        public CellSettings GetAllSettings()
        {
            return currentSettings;
        }

        public string SerializeSettingsToJson(CellSettings settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(settings, options);
        }

        public CellSettings DeserializeSettingsFromJson(string json)
        {
            try
            {
                CellSettings settings = JsonSerializer.Deserialize<CellSettings>(json);
                Debug.Print($"Settings file read successfully");
                return settings;
            }
            catch (JsonException ex)
            {
                Debug.Print($"Error deserializing settings from JSON: {ex.Message}");
                return new CellSettings();
            }
        }

        public CSMotor GetSettingsForSelectedSize()
        {
            if (currentSettings == null) return null;

            switch (selectedSize)
            {
                case DDMSize.ddm_57:
                    return currentSettings.ddm_57;
                case DDMSize.ddm_95:
                    return currentSettings.ddm_95;
                case DDMSize.ddm_116:
                    return currentSettings.ddm_116;
                case DDMSize.ddm_170:
                    return currentSettings.ddm_170;
                case DDMSize.ddm_170_tall:
                    return currentSettings.ddm_170_tall;
                default:
                    throw new ArgumentException("Invalid DDM size specified."); // autogenerated ...?
            }
        }

        //public string GetSettingsFilePath()
        //{
        //    return settingsFilePath;
        //}

        //public void OpenFolderToSettingsFile()
        //{
        //    string folderPath = Path.GetDirectoryName(settingsFilePath);
        //    System.Diagnostics.Process.Start("explorer.exe", folderPath);
        //}

        public void ReloadSettings()
        {
            currentSettings = ReadSettingsFromController();
        }

        public bool VerifySettingsExistOnController(string ip)
        {
            string rawJson = "";
            try
            {
                // Create an FTP request
                FtpWebRequest? request = WebRequest.Create("ftp://" + ip + "/" + settingsFTPPath) as FtpWebRequest;
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                // Get the response from the server
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    // Read the contents of the response stream into a string
                    rawJson = reader.ReadToEnd();

                    // Now you can use fileContents as needed
                    CellSettings settings = JsonSerializer.Deserialize<CellSettings>(rawJson);
                    Debug.Print($"Settings file read successfully from controller");
                    if (settings != null)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.Print($"Error reading settings file: {ex.Message}");
                return false;
            }

        }


        private CellSettings ReadSettingsFromController()
        {
            string rawJson = "";
            if (App.ControllerManager.CONNECTION_STATE.isConnected == false)
            {
                Debug.Print("Settings file could not be read because no controller is connected");
                return null;
            }

            try
            {
                // Create an FTP request
                string ip = App.ControllerManager.CONNECTION_STATE.connectedIP;
                FtpWebRequest? request = WebRequest.Create("ftp://" + ip + "/" + settingsFTPPath) as FtpWebRequest;
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                // Get the response from the server
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    // Read the contents of the response stream into a string
                    rawJson = reader.ReadToEnd();

                    // Now you can use fileContents as needed
                    CellSettings settings = JsonSerializer.Deserialize<CellSettings>(rawJson);
                    Debug.Print($"Settings file read successfully from controller");
                    currentSettings = settings;
                    return settings;
                }

            }
            catch (Exception ex)
            {
                Debug.Print($"Error reading settings file: {ex.Message}");
                return null;
            }
        }

        public void SaveSettingsCopyToLocal(CellSettings settings, string directoryPath)
        {
            string serializedSettings = SerializeSettingsToJson(settings);
            string tb = "  ";
            Debug.Print($"{tb}Saving settings file to {directoryPath}");
            try
            {
                string path = Path.Combine(directoryPath, settingsLocalName);
                File.WriteAllText(path, serializedSettings);
                Debug.Print($"{tb}Settings file saved successfully");
            }
            catch (Exception ex)
            {
                Debug.Print($"{tb}Error saving settings file: {ex.Message}");
            }
        }

        public void SaveSettingsToController(CellSettings settings)
        {
            if (App.ControllerManager.CONNECTION_STATE.isConnected == false)
            {
                Debug.Print("Settings file could not be saved because no controller is connected");
                return;
            }

            settings.last_saved = DateTime.Now;
            string serializedSettings = SerializeSettingsToJson(settings);

            try
            {
                // Create an FTP request
                string ip = App.ControllerManager.CONNECTION_STATE.connectedIP;
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + ip + "/" + settingsFTPPath);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // Convert the string data to a byte array
                byte[] fileContents = System.Text.Encoding.UTF8.GetBytes(serializedSettings);
                request.ContentLength = fileContents.Length;

                // Write the string data to the request stream
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                // Get the response from the server
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Debug.Print($"Save to controller complete. Status: {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error: {ex.Message}");
            }
        }



        //private void CopySettingsFromController()
        //{
        //    string ftpUrl = "ftp://10.33.240.47/flash/ddm_cell/test.txt"; // Replace with your FTP URL
        //    string localFilePath = @"C:\Users\areed\Desktop\copy.txt"; // Replace with your local file path

        //    try
        //    {
        //        // Create an FTP request
        //        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
        //        request.Method = WebRequestMethods.Ftp.DownloadFile;

        //        // Get the response from the server
        //        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
        //        using (Stream responseStream = response.GetResponseStream())
        //        using (FileStream fileStream = new FileStream(localFilePath, FileMode.Create))
        //        {
        //            // Copy the response stream to the local file
        //            responseStream.CopyTo(fileStream);
        //        }

        //        Console.WriteLine("Download Complete.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //    }
        //}


        //private CellSettings ReadSettingsFromLocal()
        //{
        //    CellSettings settings = new CellSettings();
        //    string tb = "  ";
        //    Debug.Print($"{tb}Reading settings file from {settingsFilePath}");
        //    try
        //    {
        //        if (File.Exists(settingsFilePath))
        //        {
        //            string rawJson = File.ReadAllText(settingsFilePath);
        //            settings = JsonSerializer.Deserialize<CellSettings>(rawJson);
        //            Debug.Print($"{tb}Settings file read successfully");
        //            return settings;
        //        }
        //        else
        //        {
        //            Debug.Print($"{tb}Settings file does not exist!");
        //        }
        //    }
        //    catch (JsonException ex)
        //    {
        //        Debug.Print($"{tb}Error deserializing settings file: {ex.Message}");
        //    }
        //    return new CellSettings();
        //}
    }
}
