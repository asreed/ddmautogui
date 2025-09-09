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

    public class ProcessHistory
    {
        public ProcessHistoryEvent[]? events { get; set; }
    }

    public class ProcessHistoryEvent
    {
        public DateTime date_complete { get; set; }
        public string? motor_type { get; set; }
        public int? valve_num_id { get; set; }
        public int? valve_num_od { get; set; }
        public float? pressure_id { get; set; }
        public float? pressure_od { get; set; }
        public float? vol_id { get; set; }
        public float? vol_od { get; set; }
        public float? flow_id { get; set; }
        public float? flow_od { get; set; }

    }

    public class ResultsHistoryManager
    {
        private string historyFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\Autocalibration\\ResultsHistory.json";
        private ProcessHistory history;

        public ResultsHistoryManager()
        {
            history = new ProcessHistory();
            history = GetHistoryFromFile(historyFilePath);
            if (history == null)
            {
                Debug.Print("Error reading process history file");
            } else
            {
                Debug.Print("Process history manager initialized");
            }
        }


        private ProcessHistory GetHistoryFromFile(string historyFilePath)
        {
            string rawJson = File.ReadAllText(historyFilePath);
            return DeserializeHistoryFromString(rawJson);
        }

        private string SerializeHistoryFromJson(ProcessHistory history)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(history, options);
        }

        private ProcessHistory DeserializeHistoryFromString(string rawJson)
        {
            try
            {
                ProcessHistory history = JsonSerializer.Deserialize<ProcessHistory>(rawJson);
                Debug.Print($"Process history file read successfully");
                return history;
            }
            catch (JsonException ex)
            {
                Debug.Print($"Error deserializing process history from JSON: {ex.Message}");
                return null;
            }
        }

        public void AddEvent(ProcessHistoryEvent e)
        {

        }
    }
}

