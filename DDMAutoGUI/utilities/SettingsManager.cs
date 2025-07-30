using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

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






    class SettingsManager
    {

        private string settingsFilePath = AppDomain.CurrentDomain.BaseDirectory + "settings\\settings.json";
        private DDMSettings settings;




        private static readonly Lazy<SettingsManager> lazy =
            new Lazy<SettingsManager>(() => new SettingsManager());
        public static SettingsManager Instance { get { return lazy.Value; } }

        public SettingsManager()
        {
            
        }

        public void LoadSettingsFile()
        {
            string rawJson = File.ReadAllText(settingsFilePath);
            settings = JsonSerializer.Deserialize<DDMSettings>(rawJson);
        }

        public DDMSettings GetSettings()
        {
            return settings;
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
