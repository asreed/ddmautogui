using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Security.RightsManagement;

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

        public CellSettings SETTINGS { get; private set; } = new CellSettings();



        public SettingsManager()
        {
            SETTINGS = ReadSettingsFile();
            Debug.Print("Settings manager initialized");
        }

        public CellSettings ReadSettingsFile()
        {
            CellSettings settings = new CellSettings();
            Debug.Print("Reading settings file from: " + settingsFilePath);
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string rawJson = File.ReadAllText(settingsFilePath);
                    settings = JsonSerializer.Deserialize<CellSettings>(rawJson);
                    return settings;
                }
                else
                {
                    Debug.Print("Settings file does not exist!");
                }
            }
            catch (JsonException ex)
            {
                Debug.Print("Error deserializing settings file: " + ex.Message);
            }
            return new CellSettings();
        }

        public CellSettings GetAllSettings()
        {
            return SETTINGS;
        }

        public CellSettingsMotor GetSettingsForSelectedSize()
        {
            switch (selectedSize)
            {
                case DDMSize.ddm_57:
                    return SETTINGS.ddm_57;
                case DDMSize.ddm_95:
                    return SETTINGS.ddm_95;
                case DDMSize.ddm_116:
                    return SETTINGS.ddm_116;
                case DDMSize.ddm_170:
                    return SETTINGS.ddm_170;
                case DDMSize.ddm_170_tall:
                    return SETTINGS.ddm_170_tall;
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
            SETTINGS = ReadSettingsFile();
        }
    }
}
