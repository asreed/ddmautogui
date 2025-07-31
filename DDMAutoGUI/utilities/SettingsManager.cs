using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace DDMAutoGUI.utilities
{
    public class DDMSettingsLocation
    {
        public float? x { get; set; }
        public float? th { get; set; }
    }
    public class DDMSettingShotCalibration
    {
        public float? time_id { get; set; }
        public float? time_od { get; set; }
        public float? pressure_id { get; set; }
        public float? pressure_od { get; set; }
        public float? target_vol_id { get; set; }
        public float? target_vol_od { get; set; }
    }
    public class DDMSettingsCommon
    {
        public DDMSettingsLocation? load { get; set; }
        public DDMSettingsLocation? camera_top { get; set; }
    }
    public class DDMSettingsSingleSize
    {
        public DDMSettingShotCalibration? shot_calibration { get; set; }
        public DDMSettingsLocation? camera_side { get; set; }
        public DDMSettingsLocation? disp_id { get; set; }
        public DDMSettingsLocation? disp_od { get; set; }
        public DDMSettingsLocation? laser_mag { get; set; }
        public DDMSettingsLocation? laser_ring { get; set; }
    }
    public class DDMSettings
    {
        public DateTime? last_saved { get; set; }
        public DDMSettingsCommon? common { get; set; }
        public DDMSettingsSingleSize? ddm_57 { get; set; }
        public DDMSettingsSingleSize? ddm_95 { get; set; }
        public DDMSettingsSingleSize? ddm_116 { get; set; }
        public DDMSettingsSingleSize? ddm_170 { get; set; }
        public DDMSettingsSingleSize? ddm_170_tall { get; set; }
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

        private DDMSize selectedSize;
        private DDMSettings SETTINGS;




        public SettingsManager()
        {
            SETTINGS = new DDMSettings();
            SETTINGS = ReadSettingsFile();
            selectedSize = DDMSize.none;
            Debug.Print("Settings manager initialized");
        }

        public DDMSettings ReadSettingsFile()
        {
            string rawJson = File.ReadAllText(settingsFilePath);
            try
            {
                return JsonSerializer.Deserialize<DDMSettings>(rawJson);
            }
            catch (JsonException ex)
            {
                Debug.Print("Error deserializing settings file: " + ex.Message);
                return new DDMSettings();
            }
        }

        public DDMSettings GetAllSettings(DDMSize size)
        {
            return SETTINGS;
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
    }
}
