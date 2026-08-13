using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
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
        public float? hall_spin_speed { get; set; } = 37.5f;
        public float? clearance_check_min { get; set; }
        public float? clearance_check_max { get; set; }
        public float? calib_surface_height { get; set; }
        public float? calib_tool_height { get; set; }
        public float? calib_tool_max_diff { get; set; }
        public CSHeightVerif? height_verification { get; set; }
        public CSDispenseSys? dispense_system { get; set; }
        public CSMotorCommon? ddm_common { get; set; }
        public CSMotor? ddm_57 { get; set; }
        public CSMotor? ddm_95 { get; set; }
        public CSMotor? ddm_116 { get; set; }
        public CSMotor? ddm_170 { get; set; }
        public CSMotor? ddm_170_tall { get; set; }
    }

    public class CSDispenseSys
    {
        public string? sys_1_contents { get; set; } = "Permabond 801";
        public float? sys_1_max_pressure { get; set; } = 55.0f;
        public float? sys_1_flush_pressure { get; set; }
        public float? sys_1_flush_time { get; set; } = 2.0f;
        public float? sys_1_fill_time { get; set; }
        public float? sys_1_vol_max_err_percent { get; set; } = 5.0f;

        public string? sys_2_contents { get; set; } = "Permabond UV632";
        public float? sys_2_max_pressure { get; set; } = 55.0f;
        public float? sys_2_flush_pressure { get; set; }
        public float? sys_2_flush_time { get; set; } = 2.0f;
        public float? sys_2_fill_time { get; set; }
        public float? sys_2_vol_max_err_percent { get; set; } = 5.0f;

        public float? calib_exp_hours { get; set; } = 2.0f;
        public CSDefaultPressures? default_pressures { get; set; }
    }
    public class CSMotorCommon
    {
        public CSLocation? pos_load { get; set; } = new CSLocation { x = 0, t = -50f };
        public CSLocation? pos_camera_top { get; set; } = new CSLocation { x = 271f, t = 40f };
        public CSLocation? pos_clearance_check { get; set; }
    }

    public class CSMotor
    {
        public int? ca_sys_num { get; set; }
        public int? uv_sys_num { get; set; }
        public float? ca_target_flow { get; set; }
        public float? ca_p1_target_vol { get; set; }
        public float? ca_p2_target_vol { get; set; }
        public float? ca_p3_target_vol { get; set; }
        public float? ca_p4_target_vol { get; set; }
        public float? ca_p1_delay { get; set; }
        public float? ca_p2_delay { get; set; }
        public float? ca_p3_delay { get; set; }
        public float? ca_p4_delay { get; set; }
        public float? uv_target_flow { get; set; }
        public float? uv_p1_target_vol { get; set; }
        public float? uv_p1_delay { get; set; }
        public float? uv_cure_time { get; set; }
        public float? uv_cure_spin_speed { get; set; }
        public int? laser_ref_num { get; set; } = 30;
        public int? laser_ring_num { get; set; } = 30;
        public int? laser_mag_num { get; set; }
        public float? laser_ref_time { get; set; }
        public float? laser_ring_time { get; set; }
        public float? laser_mag_time { get; set; }
        public float? sin_fit_max_amplitude { get; set; }
        public float? ring_height_min { get; set; }
        public float? ring_height_max { get; set; }
        public float? mag_height_min { get; set; }
        public float? mag_height_max { get; set; }
        public float? pol_expected_wavelength { get; set; }
        public int? pol_expected_magnets { get; set; }
        public CSLocation? pos_camera_side { get; set; }
        public CSLocation? pos_ca_p1 { get; set; }
        public CSLocation? pos_ca_p2 { get; set; }
        public CSLocation? pos_ca_p3 { get; set; }
        public CSLocation? pos_ca_p4 { get; set; }
        public CSLocation? pos_uv_p1 { get; set; }
        public CSLocation? pos_uv_cure { get; set; }
        public CSLocation? pos_laser_ref { get; set; }
        public CSLocation? pos_laser_ring { get; set; }
        public CSLocation? pos_laser_mag { get; set; }
        public CSLocation? pos_hall_sensor { get; set; }
        public CSLocation? pos_calib_tool_test { get; set; }

        public bool IsValid()
        {
            // TODO: add validation logic

            return true;
        }
    }

    public class CSLocation
    {
        public float? x { get; set; }
        public float? t { get; set; }
    }

    public class CSHeightVerif
    {
        // likely more to be added

        public float? max_height { get; set; }
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
                //DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
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





    public static class SettingsTemplateGenerator
    {
        public static string GenerateJson()
        {
            var root = (CellSettings)Populate(typeof(CellSettings));
            return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        }

        private static object Populate(Type type)
        {
            object instance = Activator.CreateInstance(type)!;

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite) continue;

                Type propType = prop.PropertyType;
                if (propType.IsClass && propType != typeof(string) && propType.Namespace == type.Namespace)
                {
                    prop.SetValue(instance, Populate(propType));
                }
            }

            return instance;
        }
    }
}
