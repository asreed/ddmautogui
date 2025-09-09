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
    public class CellSettingsLocation
    {
        public float? x { get; set; }
        public float? t { get; set; }
    }
    public class CellSettingsShot
    {
        public int? valve_num_id { get; set; }
        public int? valve_num_od { get; set; }
        public float? time_id { get; set; }
        public float? time_od { get; set; }
        public float? target_vol_id { get; set; }
        public float? target_vol_od { get; set; }
        public float? target_flow_id { get; set; }
        public float? target_flow_od { get; set; }
    }
    public class CellSettingsMotorCommon
    {
        public CellSettingsLocation? load { get; set; }
        public CellSettingsLocation? camera_top { get; set; }
    }
    public class CellSettingsMotor
    {
        public CellSettingsShot? shot_settings { get; set; }
        public float? post_spin_time { get; set; }
        public float? post_spin_speed { get; set; }
        public int? laser_ring_num { get; set; }
        public int? laser_mag_num { get; set; }
        public CellSettingsLocation? camera_side { get; set; }
        public CellSettingsLocation? disp_id { get; set; }
        public CellSettingsLocation? disp_od { get; set; }
        public CellSettingsLocation? laser_mag { get; set; }
        public CellSettingsLocation? laser_ring { get; set; }

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

    public class CellSettingsLaserCoeff
    {
        public float? A { get; set; }
        public float? phi { get; set; }
        public float? R2 { get; set; }
    }

    public class CellSettingsLaser
    {
        public CellSettingsLaserCoeff? ddm_57_coeff { get; set; }
        public CellSettingsLaserCoeff? ddm_95_coeff { get; set; }
        public CellSettingsLaserCoeff? ddm_116_coeff { get; set; }
        public CellSettingsLaserCoeff? ddm_170_coeff { get; set; }
    }

    public class CellSettingsDispenseCalib
    {
        public float? pressure { get; set; }
        public float? flow { get; set; }
    }

    public class CellSettingsDispense
    {
        public string? sys_1_contents { get; set; }
        public string? sys_2_contents { get; set; }
        public CellSettingsDispenseCalib[] sys_1_flow_calib { get; set; }
        public CellSettingsDispenseCalib[] sys_2_flow_calib { get; set; }
    }

    public class CellSettings
    {
        public DateTime? last_saved { get; set; }
        public string? camera_top_sn { get; set; }
        public string? camera_side_sn { get; set; }
        public float? laser_delay { get; set; }
        public CellSettingsDispense? dispense_system { get; set; }
        public CellSettingsLaser? laser_calib { get; set; }
        public CellSettingsMotorCommon? ddm_common { get; set; }
        public CellSettingsMotor? ddm_57 { get; set; }
        public CellSettingsMotor? ddm_95 { get; set; }
        public CellSettingsMotor? ddm_116 { get; set; }
        public CellSettingsMotor? ddm_170 { get; set; }
        public CellSettingsMotor? ddm_170_tall { get; set; }
    }






    public class SettingsManager
    {
        private string settingsFilePath = AppDomain.CurrentDomain.BaseDirectory + "settings\\settings.json";
        private string settingsFTPPath = "ftp://10.33.240.47/flash/ddm_cell/Settings.json";

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

        public CellSettings currentSettings { get; private set; } = new CellSettings();



        public SettingsManager()
        {
            //currentSettings = ReadSettingsFromLocal();
            currentSettings = ReadSettingsFromController();
            Debug.Print("Settings manager initialized");
        }

        private CellSettings ReadSettingsFromLocal()
        {
            CellSettings settings = new CellSettings();
            string tb = "  ";
            Debug.Print($"{tb}Reading settings file from {settingsFilePath}");
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string rawJson = File.ReadAllText(settingsFilePath);
                    settings = JsonSerializer.Deserialize<CellSettings>(rawJson);
                    Debug.Print($"{tb}Settings file read successfully");
                    return settings;
                }
                else
                {
                    Debug.Print($"{tb}Settings file does not exist!");
                }
            }
            catch (JsonException ex)
            {
                Debug.Print($"{tb}Error deserializing settings file: {ex.Message}");
            }
            return new CellSettings();
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


        public CellSettingsMotor GetSettingsForSelectedSize()
        {
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

        public string GetSettingsFilePath()
        {
            return settingsFilePath;
        }

        public void OpenFolderToSettingsFile()
        {
            string folderPath = Path.GetDirectoryName(settingsFilePath);
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }

        public void ReloadSettings()
        {
            //currentSettings = ReadSettingsFromLocal();
            currentSettings = ReadSettingsFromController();
        }










        private CellSettings ReadSettingsFromController()
        {
            string rawJson = "";

            try
            {
                // Create an FTP request
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(settingsFTPPath);
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
                    Debug.Print($"  Settings file read successfully from controller");
                    Debug.Print("Download Complete.");
                    return settings;
                }

            }
            catch (Exception ex)
            {
                Debug.Print($"Error: {ex.Message}");
                return null;
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

        public void SaveSettingsToController(CellSettings settings)
        {

            settings.last_saved = DateTime.Now;
            string serializedSettings = SerializeSettingsToJson(settings);

            try
            {
                // Create an FTP request
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(settingsFTPPath);
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
    }
}
