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
        public string? type { get; set; }
        public DateTime? last_update {  get; set; }
        public float? sys_1_pressure { get; set; }
        //public float? sys_1_flow { get; set; }
        public float? sys_2_pressure { get; set; }
        //public float? sys_2_flow { get; set; }

        public LDCalib Clone()
        {
            return new LDCalib
            {
                type = this.type,
                last_update = this.last_update,
                sys_1_pressure = this.sys_1_pressure,
                //sys_1_flow = this.sys_1_flow,
                sys_2_pressure = this.sys_2_pressure,
                //sys_2_flow = this.sys_2_flow

            };
        }

    }

    public class LocalData
    {
        public string? controller_ip { get; set; }
        public LDCalib[]? calib_data { get; set; }

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

        public int GetCalibIdxFromMotorName(string name)
        {
            int idx = -1;
            for (int i = 0; i < localData.calib_data.Length; i++)
            {
                if (localData.calib_data[i].type == name)
                {
                    idx = i;
                    return idx;
                }
            }
            return idx;
        }

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

