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
        public float? ref_pressure_1 { get; set; }
        public float? ref_pressure_2 { get; set; }
        public float? target_vol_id { get; set; }
        public float? target_vol_od { get; set; }
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
    public class CellSettings
    {
        public DateTime? last_saved { get; set; }
        public string? camera_top_sn { get; set; }
        public string? camera_side_sn { get; set; }
        public int? linear_axis_num { get; set; }
        public int? rotary_axis_num { get; set; }
        public string? system_1_contents { get; set; }
        public string? system_2_contents { get; set; }
        public float? laser_delay { get; set; }
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
            currentSettings = ReadSettingsFile();
            Debug.Print("Settings manager initialized");
        }

        public CellSettings ReadSettingsFile()
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
            currentSettings = ReadSettingsFile();
        }




        private void CopySettingsFromController()
        {
            string ftpUrl = "ftp://10.33.240.47/flash/ddm_cell/test.txt"; // Replace with your FTP URL
            string localFilePath = @"C:\Users\areed\Desktop\copy.txt"; // Replace with your local file path

            try
            {
                // Create an FTP request
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                // Get the response from the server
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (FileStream fileStream = new FileStream(localFilePath, FileMode.Create))
                {
                    // Copy the response stream to the local file
                    responseStream.CopyTo(fileStream);
                }

                Console.WriteLine("Download Complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void SaveSettingsToController()
        {
            string localFilePath2 = @"C:\Users\areed\Desktop\copy.txt"; // Replace with your local file path
            string ftpUrl2 = "ftp://10.33.240.47/flash/ddm_cell/test1.txt"; // Replace with your FTP URL

            try
            {
                // Create an FTP request
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl2);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                // Read the file contents into a byte array
                byte[] fileContents;
                using (FileStream fileStream = new FileStream(localFilePath2, FileMode.Open, FileAccess.Read))
                {
                    fileContents = new byte[fileStream.Length];
                    fileStream.Read(fileContents, 0, fileContents.Length);
                }

                // Write the file to the request stream
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                // Get the response from the server
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Console.WriteLine($"Upload Complete. Status: {response.StatusDescription}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
