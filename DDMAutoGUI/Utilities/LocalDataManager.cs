using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace DDMAutoGUI.utilities
{
    public class LDMotorCalib
    {
        public float? sys_1_pressure { get; set; }
        public float? sys_2_pressure { get; set; }

        public LDMotorCalib Clone()
        {
            return new LDMotorCalib
            {
                sys_1_pressure = this.sys_1_pressure,
                sys_2_pressure = this.sys_2_pressure,
            };
        }

    }

    public class LDCalib
    {
        public DateTime? last_calib { get; set; }
        public string? last_size { get; set; }
        public LDMotorCalib? ddm_57 { get; set; }
        public LDMotorCalib? ddm_95 { get; set; }
        public LDMotorCalib? ddm_116 { get; set; }
        public LDMotorCalib? ddm_170 { get; set; }
        public LDMotorCalib? ddm_170_tall { get; set; }

    }

    public class LocalData
    {
        public LDCalib? calib_data { get; set; }

        public LocalData Clone()
        {
            return new LocalData
            {
                calib_data = this.calib_data,
            };
        }

    }

    public class LocalDataManager
    {
        private string localDataFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\LocalData.json";
        private LocalData localData;

        public LocalDataManager()
        {
            localData = new LocalData();
            localData = GetLocalDataFromFile(localDataFilePath);
            if (localData == null)
            {
                Debug.Print("Error reading local data file");
            }
            else
            {
                Debug.Print("Local data manager initialized");
            }
        }

        public LocalData GetLocalData()
        {
            return localData.Clone();
        }

        public void SetLocalData(LocalData newData)
        {
            localData = newData.Clone();
        }

        public LDMotorCalib GetCalibFromMotorName(string name)
        {
            LDMotorCalib calib = null;
            switch (name)
            {
                case "ddm_57":
                    calib = localData.calib_data.ddm_57;
                    break;
                case "ddm_95":
                    calib = localData.calib_data.ddm_95;
                    break;
                case "ddm_116":
                    calib = localData.calib_data.ddm_116;
                    break;
                case "ddm_170":
                    calib = localData.calib_data.ddm_170;
                    break;
                case "ddm_170_tall":
                    calib = localData.calib_data.ddm_170_tall;
                    break;
            }
            return calib;
        }

        public float? GetPressureFromMotorName(string name, int systemNum)
        {
            LDMotorCalib calib = GetCalibFromMotorName(name);
            if (calib == null)
            {
                return null;
            }
            switch (systemNum)
            {
                case 1:
                    return calib.sys_1_pressure;
                case 2:
                    return calib.sys_2_pressure;
                default:
                    return null;
            }
        }

        public LDMotorCalib GetCalibFromMotorName(LocalData data, string name)
        {
            LDMotorCalib calib = null;
            switch (name)
            {
                case "ddm_57":
                    calib = data.calib_data.ddm_57;
                    break;
                case "ddm_95":
                    calib = data.calib_data.ddm_95;
                    break;
                case "ddm_116":
                    calib = data.calib_data.ddm_116;
                    break;
                case "ddm_170":
                    calib = data.calib_data.ddm_170;
                    break;
                case "ddm_170_tall":
                    calib = data.calib_data.ddm_170_tall;
                    break;
            }
            return calib;
        }

        //public int GetCalibIdxFromMotorName(string name)
        //{
        //    int idx = -1;
        //    for (int i = 0; i < localData.calib_data.Length; i++)
        //    {
        //        if (localData.calib_data[i].type == name)
        //        {
        //            idx = i;
        //            return idx;
        //        }
        //    }
        //    return idx;
        //}

        private LocalData GetLocalDataFromFile(string historyFilePath)
        {
            string rawJson = File.ReadAllText(historyFilePath);
            return DeserializeLocalDataFromString(rawJson);
        }

        public bool SaveLocalDataToFile(LocalData newData)
        {
            try
            {
                string rawJson = SerializeDataFromJson(newData);
                File.WriteAllText(localDataFilePath, rawJson);
                Debug.Print($"Local data file saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print($"Error saving local data to file: {ex.Message}");
                return false;
            }
        }

        public bool SaveLocalDataToFile()
        {
            return SaveLocalDataToFile(localData);
        }

        public string SerializeDataFromJson(LocalData data)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(data, options);
        }

        public LocalData DeserializeLocalDataFromString(string rawJson)
        {
            try
            {
                LocalData history = JsonSerializer.Deserialize<LocalData>(rawJson);
                Debug.Print($"Local data file read successfully");
                return history;
            }
            catch (JsonException ex)
            {
                Debug.Print($"Error deserializing local data from JSON: {ex.Message}");
                return null;
            }
        }
    }
}

