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

    public class LocalData
    {
        public string? controller_ip { get; set; }
        public DateTime? last_sys_1_calib_update { get; set; }
        public DateTime? last_sys_2_calib_update { get; set; }
        public CellSettingsDispenseCalib[]? current_sys_1_flow_calib {  get; set; }
        public CellSettingsDispenseCalib[]? current_sys_2_flow_calib { get; set; }

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




            //AddEvent(new ResultsHistoryEvent
            //{
            //    date_complete = DateTime.Now,
            //    motor_type = "ddm_95",
            //    valve_num_id = 1,
            //    valve_num_od = 1,
            //    pressure_id = 6.8f,
            //    pressure_od = 6.8f,
            //    time_id = 1.808f,
            //    time_od = 2.232f,
            //    vol_id = 0.452f,
            //    vol_od = 0.558f
            //});

            //AddEvent(new ResultsHistoryEvent
            //{
            //    date_complete = DateTime.Now,
            //    motor_type = "ddm_95",
            //    valve_num_id = 1,
            //    valve_num_od = 1,
            //    pressure_id = 6.8f,
            //    pressure_od = 6.8f,
            //    time_id = 1.807f,
            //    time_od = 2.236f,
            //    vol_id = 0.451f,
            //    vol_od = 0.552f
            //});

            //AddEvent(new ResultsHistoryEvent
            //{
            //    date_complete = DateTime.Now,
            //    motor_type = "ddm_95",
            //    valve_num_id = 1,
            //    valve_num_od = 1,
            //    pressure_id = 6.8f,
            //    pressure_od = 6.8f,
            //    time_id = 1.812f,
            //    time_od = 2.238f,
            //    vol_id = 0.446f,
            //    vol_od = 0.540f
            //});


        }

        public void UpdateCalib(int sysNum, CellSettingsDispenseCalib[] newCalib)
        {
            switch (sysNum)
            {
                case 1:
                    localData.current_sys_1_flow_calib = newCalib;
                    localData.last_sys_1_calib_update = DateTime.Now;
                    break;
                case 2: 
                    localData.current_sys_2_flow_calib = newCalib;
                    localData.last_sys_2_calib_update = DateTime.Now;
                    break;
            }
        }

        public float? GetPressureFromFlowrate(int sys, float flow)
        {
            // sys must be 1 or 2

            var calib = sys == 1 ? localData.current_sys_1_flow_calib : localData.current_sys_2_flow_calib;
            for (int i = 0; i < calib.Length; i++)
            {
                if (flow == calib[i].flow)
                {
                    return calib[i].pressure.Value;
                }
            }
            Debug.Print($"No exact match found for flow {flow} in system {sys} current (local) calibration data");
            return null;
        }







        private LocalData GetLocalDataFromFile(string historyFilePath)
        {
            string rawJson = File.ReadAllText(historyFilePath);
            return DeserializeLocalDataFromString(rawJson);
        }

        public bool SaveLocalDataToFile()
        {
            try
            {
                string rawJson = SerializeDataFromJson(localData);
                File.WriteAllText(localDataFilePath, rawJson);
                Debug.Print($"Local data file saved successfully");
                return true;
            }
            catch (IOException ex)
            {
                Debug.Print($"Error saving local data to file: {ex.Message}");
                return false;
            }
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

