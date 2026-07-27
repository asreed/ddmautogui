using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;

namespace DDMAutoGUI.Services
{

    public class CellSettings
    {
        public DateTime? last_saved { get; set; }
        public string? camera_top_sn { get; set; }
        public string? camera_side_sn { get; set; }
        public float? laser_delay { get; set; }
        public float? hall_spin_delay { get; set; }
        public float? hall_spin_time { get; set; }
        public float? hall_spin_speed { get; set; }
        public float? clearance_check_min { get; set; }
        public float? clearance_check_max { get; set; }
        public float? calib_surface_height { get; set; }
        public float? calib_tool_height { get; set; }
        public float? calib_tool_max_diff { get; set; }
        public CSHeightVerif? height_verification { get; set; }
        public CSDispense? dispense_system { get; set; }
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
        public int? id_sys_num { get; set; }
        public float? id_target_vol { get; set; }
        public float? id_target_flow { get; set; }
        public int? od_sys_num { get; set; }
        public float? od_target_vol { get; set; }
        public float? od_target_flow { get; set; }
    }
    public class CSMotorCommon
    {
        public CSLocation? load { get; set; }
        public CSLocation? camera_top { get; set; }
        public CSLocation? clearance_check { get; set; }
    }
    public class CSMotor
    {
        public CSShot? shot_settings { get; set; }
        public float? post_spin_time { get; set; }
        public float? post_spin_speed { get; set; }
        public int? laser_ring_num { get; set; }
        public int? laser_mag_num { get; set; }
        public CSLocation? camera_side { get; set; }
        public CSLocation? id_disp { get; set; }
        public CSLocation? od_disp { get; set; }
        public CSLocation? laser_mag { get; set; }
        public CSLocation? laser_ring { get; set; }
        public CSLocation? hall_sensor { get; set; }
        public CSLocation? calib_tool_test { get; set; }

        public bool IsValid()
        {
            // validate logic. might want to expand checks

            if (id_disp == null || od_disp == null || laser_mag == null || laser_ring == null)
            {
                return false; // invalid if any location is null ...?
            }
            else
            {
                return true;
            }
        }
    }


    public class CSHeightVerif
    {
        public float? max_height { get; set; }
    }

    public class CSDispense
    {
        public string? sys_1_contents { get; set; }
        public string? sys_2_contents { get; set; }
        public float? sys_1_max_pressure { get; set; }
        public float? sys_2_max_pressure { get; set; }
        public float? sys_1_max_pressure_dev_percent { get; set; }
        public float? sys_2_max_pressure_dev_percent { get; set; }
        public float? sys_1_flush_pressure { get; set; }
        public float? sys_2_flush_pressure { get; set; }
        public float? sys_1_flush_time { get; set; }
        public float? sys_2_flush_time { get; set; }
        public float? sys_1_fill_time { get; set; }
        public float? sys_2_fill_time { get; set; }
        public float? id_vol_max_err_percent { get; set; }
        public float? od_vol_max_err_percent { get; set; }
        public float? calib_exp_hours { get; set; }
        public CSDefaultPressures? default_pressures { get; set; }

    }

    public class CSDefaultPressures
    {
        public CSDefaultCalib? ddm_57 { get; set; }
        public CSDefaultCalib? ddm_95 { get; set; }
        public CSDefaultCalib? ddm_116 { get; set; }
        public CSDefaultCalib? ddm_170 { get; set; }
        public CSDefaultCalib? ddm_170_tall { get; set; }

    }

    public class CSDefaultCalib
    {
        public float? sys_1_pressure { get; set; }
        public float? sys_2_pressure { get; set; }
    }

    public class SettingsService : ISettingsService
    {
        private string settingsFTPPath = "/flash/ddm_cell/Settings.json";
        private string settingsLocalName = "Settings.json";
        private string defaultSettingsPath = AppDomain.CurrentDomain.BaseDirectory + "_reference\\DefaultSettings.json";

        private readonly IControllerService _controllerService;
        private readonly IApplicationConfiguration _applicationConfiguration;

        public enum DDMSize
        {
            none,
            ddm_57,
            ddm_95,
            ddm_116,
            ddm_170,
            ddm_170_tall
        }

        private DDMSize _selectedSize = DDMSize.ddm_116;

        public DDMSize SelectedSize
        {
            get => _selectedSize;
            set => _selectedSize = value;
        }

        private CellSettings currentSettings = new CellSettings();

        public SettingsService(
            IApplicationConfiguration applicationConfiguration,
            IControllerService controllerService)
        {
            _applicationConfiguration = applicationConfiguration ?? throw new ArgumentNullException(nameof(applicationConfiguration));
            _controllerService = controllerService ?? throw new ArgumentNullException(nameof(controllerService));

            _controllerService.ControllerConnected += SettingsService_OnConnected;
            _controllerService.ControllerDisconnected += SettingsService_OnDisconnected;

            Debug.Print("Settings service initialized");
        }

        public async void SettingsService_OnConnected(object sender, EventArgs e)
        {
            Debug.Print("Settings service detected controller connected");
            currentSettings = ReadSettingsFromController();
        }

        public void SettingsService_OnDisconnected(object sender, EventArgs e)
        {
            Debug.Print("Settings service detected controller disconnected");
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

            switch (_selectedSize)
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
                    throw new ArgumentException("Invalid DDM size specified.");
            }
        }

        public CSMotor GetMotorSettingsFromName(string motorName)
        {
            CellSettings settings = GetAllSettings();
            if (settings == null)
                return null;

            return motorName switch
            {
                "ddm_57" => settings.ddm_57,
                "ddm_95" => settings.ddm_95,
                "ddm_116" => settings.ddm_116,
                "ddm_170" => settings.ddm_170,
                "ddm_170_tall" => settings.ddm_170_tall,
                _ => null
            };
        }

        public void ReloadSettings()
        {
            currentSettings = ReadSettingsFromController();
        }

        public bool LoadAndVerifySettings(string ip)
        {
            string rawJson = "";
            try
            {
                FtpWebRequest request = WebRequest.Create("ftp://" + ip + "/" + settingsFTPPath) as FtpWebRequest;
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    rawJson = reader.ReadToEnd();
                    CellSettings settings = JsonSerializer.Deserialize<CellSettings>(rawJson);
                    currentSettings = settings;
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
            if (!_controllerService.CONNECTION_STATE.isConnected)
            {
                Debug.Print("Settings file could not be read because no controller is connected");
                return null;
            }

            if (_applicationConfiguration.IsSimulationMode)
            {
                Debug.Print("(!) Settings file simulated using default parameters (!)");
                rawJson = File.ReadAllText(defaultSettingsPath);
                CellSettings settings = JsonSerializer.Deserialize<CellSettings>(rawJson);
                Debug.Print($"Default settings file read successfully from file");
                currentSettings = settings;
                return settings;
            }

            try
            {
                string ip = _controllerService.CONNECTION_STATE.connectedIP;
                FtpWebRequest request = WebRequest.Create("ftp://" + ip + "/" + settingsFTPPath) as FtpWebRequest;
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    rawJson = reader.ReadToEnd();
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
            if (!_controllerService.CONNECTION_STATE.isConnected)
            {
                Debug.Print("Settings file could not be saved because no controller is connected");
                return;
            }

            settings.last_saved = DateTime.Now;
            string serializedSettings = SerializeSettingsToJson(settings);

            try
            {
                string ip = _controllerService.CONNECTION_STATE.connectedIP;
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + ip + "/" + settingsFTPPath);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                byte[] fileContents = System.Text.Encoding.UTF8.GetBytes(serializedSettings);
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

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

        public CSDefaultCalib GetDefaultPressuresFromName(string motorName)
        {
            CellSettings settings = GetAllSettings();
            if (settings?.dispense_system?.default_pressures == null)
                return null;

            CSDefaultPressures defaults = settings.dispense_system.default_pressures;

            return motorName switch
            {
                "ddm_57" => defaults.ddm_57,
                "ddm_95" => defaults.ddm_95,
                "ddm_116" => defaults.ddm_116,
                "ddm_170" => defaults.ddm_170,
                "ddm_170_tall" => defaults.ddm_170_tall,
                _ => null
            };
        }
    }
}
