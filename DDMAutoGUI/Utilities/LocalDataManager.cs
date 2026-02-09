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
    public class LDCalib
    {
        public float? sys_1_pressure { get; set; }
        public float? sys_2_pressure { get; set; }

        public LDCalib Clone()
        {
            return new LDCalib
            {
                sys_1_pressure = this.sys_1_pressure,
                sys_2_pressure = this.sys_2_pressure,
            };
        }

    }

    public class LocalData
    {
        public DateTime? last_calib { get; set; }
        public string? last_size { get; set; }
        public LDCalib? ddm_57 { get; set; }
        public LDCalib? ddm_95 { get; set; }
        public LDCalib? ddm_116 { get; set; }
        public LDCalib? ddm_170 { get; set; }
        public LDCalib? ddm_170_tall { get; set; }

    }

    public class LocalDataManager
    {
        private string localDataFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\LocalData.json";
        public LocalData localData;

        public LocalDataManager()
        {
            localData = new LocalData();
            localData = GetLocalDataFromFile(localDataFilePath);
            if (localData == null)
            {
                Debug.Print("Error reading local data file");
            } else
            {
                Debug.Print("Local data manager initialized");
            }
        }

        public LDCalib GetCalibFromMotorName(string name)
        {
            LDCalib calib = null;
            switch (name)
            {
                case "ddm_57":
                    calib = localData.ddm_57;
                    break;
                case "ddm_95":
                    calib = localData.ddm_95;
                    break;
                case "ddm_116":
                    calib = localData.ddm_116;
                    break;
                case "ddm_170":
                    calib = localData.ddm_170;
                    break;
                case "ddm_170_tall":
                    calib = localData.ddm_170_tall;
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

