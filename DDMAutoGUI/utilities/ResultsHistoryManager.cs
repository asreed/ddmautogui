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
        public ResultsHistoryEvent[]? results { get; set; }
        public DateTime? last_sys_1_calib_update { get; set; }
        public DateTime? last_sys_2_calib_update { get; set; }
        public CellSettingsDispenseCalib[]? current_sys_1_flow_calib {  get; set; }
        public CellSettingsDispenseCalib[]? current_sys_2_flow_calib { get; set; }

    }

    public class ResultsHistoryEvent
    {
        public DateTime date_complete { get; set; }
        public string? motor_type { get; set; }
        public int? valve_num_id { get; set; }
        public int? valve_num_od { get; set; }
        public float? pressure_id { get; set; }
        public float? pressure_od { get; set; }
        public float? time_id { get; set; }
        public float? time_od { get; set; }
        public float? vol_id { get; set; }
        public float? vol_od { get; set; }

    }

    public class ResultsHistoryManager
    {
        private string localDataFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\LocalData.json";
        private LocalData localData;

        public ResultsHistoryManager()
        {
            localData = new LocalData();
            localData = GetLocalDataFromFile(localDataFilePath);
            if (localData == null)
            {
                Debug.Print("Error reading process history file");
            } else
            {
                Debug.Print("Process history manager initialized");
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

        public void AddEvent(ResultsHistoryEvent historyEvent)
        {
            if (localData == null || localData.results == null)
            {
                Debug.Print("No local data object or no events array. Unable to add event");
                return;
            }
            // shift results to make room for new event (overwrites last entry)
            Array.Copy(localData.results, 0, localData.results, 1, localData.results.Length-1);
            // add new entry
            localData.results[0] = historyEvent;

        }

        public void AddEvent(ResultsShotData shotData, string motorType)
        {
            AddEvent(TranslateShotDataToEvent(shotData, motorType));
        }

        public ResultsHistoryEvent TranslateShotDataToEvent(ResultsShotData shotData, string motorType)
        {
            ResultsHistoryEvent e = new ResultsHistoryEvent
            {
                date_complete = DateTime.Now,
                motor_type = motorType,
                valve_num_id = shotData.valve_num_id,
                valve_num_od = shotData.valve_num_od,
                pressure_id = shotData.pressure_id,
                pressure_od = shotData.pressure_od,
                time_id = shotData.time_id,
                time_od = shotData.time_od,
                vol_id = shotData.vol_id,
                vol_od = shotData.vol_od
            };
            return e;
        }

        public ResultsHistoryEvent GetEvent(int index)
        {
            return localData.results[index];
        }
        public LocalData GetLocalData()
        {
            return localData;
        }

        public void UpdateCalib(int sysNum, CellSettingsDispenseCalib[] newCalib)
        {
            switch (sysNum)
            {
                case 1: localData.current_sys_1_flow_calib = newCalib; break;
                case 2: localData.current_sys_2_flow_calib = newCalib; break;
            }
        }







        private LocalData GetLocalDataFromFile(string historyFilePath)
        {
            string rawJson = File.ReadAllText(historyFilePath);
            return DeserializeLocalDataFromString(rawJson);
        }

        private string SerializeDataFromJson(LocalData data)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(data, options);
        }

        private LocalData DeserializeLocalDataFromString(string rawJson)
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

