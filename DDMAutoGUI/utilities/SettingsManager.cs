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
        public float x { get; set; }
        public float th { get; set; }
    }
    public class DDMSettingsAllSizes
    {
        public DDMSettingsLocation? load { get; set; }
        public DDMSettingsLocation? camera { get; set; }
    }
    public class DDMSettingsOneSize
    {
        public DDMSettingsLocation? disp_id { get; set; }
        public DDMSettingsLocation? disp_od { get; set; }
        public DDMSettingsLocation? laser_mag { get; set; }
        public DDMSettingsLocation? laser_ring { get; set; }
    }
    public class DDMSettings
    {
        public DDMSettingsAllSizes? all_sizes { get; set; }
        public DDMSettingsOneSize? ddm_57 { get; set; }
        public DDMSettingsOneSize? ddm_116 { get; set; }
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
            LoadSettingsFile(settingsFilePath);
        }

        public void LoadSettingsFile(string path)
        {
            string rawJson = File.ReadAllText(settingsFilePath);
            settings = JsonSerializer.Deserialize<DDMSettings>(rawJson);
        }

        public DDMSettings GetSettings()
        {
            return settings;
        }
    }
}
