using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace DDMAutoGUI.utilities
{
    public class DDMResultsOptions
    {
        public bool pre_photo { get; set; }
        public bool ring_measure { get; set; }
        public bool mag_measure { get; set; }
        public bool shot_id { get; set; }
        public bool shot_od { get; set; }
        public bool post_photo { get; set; }
    }
    public class DDMResultsShot
    {
        public int? valve_num { get; set; }
        public float? vol { get; set; }
        public float? time { get; set; }
        public float? pressure { get; set; }
    }
    public class DDMResultsSingleHeight
    {
        public float? t { get; set; }
        public float? z { get; set; }
    }

    public class DDMResultsLogLine
    {
        public DateTime? date { get; set; }
        public string? message { get; set; }
    }

    public class DDMResults
    {
        public DateTime? date_saved { get; set; }
        public string? ring_sn { get; set; }
        public DDMResultsOptions? selected_options { get; set; }
        public DDMResultsOptions? completed_options { get; set; }
        public DDMResultsShot? shot_id { get; set; }
        public DDMResultsShot? shot_od { get; set; }
        public List<DDMResultsSingleHeight>? ring_heights { get; set; }
        public List<DDMResultsSingleHeight>? mag_heights { get; set; }
        public List<DDMResultsLogLine>? process_log { get; set; }

    }














    public class ProcessResults
    {

        public string saveMainDirectory = AppDomain.CurrentDomain.BaseDirectory + "results\\";
        public string saveFolderPrefix = "ring_";

        public string fileNameLog = "process_data";
        public string fileNamePhotoBefore = "before";
        public string fileNamePhotoAfter = "after";

        public string dateFormatLong = "MM-dd-yyy HH:mm:ss.fff";
        public string dateFormatShort = "HH:mm:ss.fff";

        public event EventHandler UpdateProcessLog;

        public DDMResults results;


        public ProcessResults()
        { 
            results = new DDMResults {
                process_log = new List<DDMResultsLogLine>(),
                ring_heights = new List<DDMResultsSingleHeight>(),
                mag_heights = new List<DDMResultsSingleHeight>()
            };
        }

        public void AddToLog(string line)
        {
            DDMResultsLogLine newLine = new DDMResultsLogLine();
            newLine.date = DateTime.Now;
            newLine.message = line;
            results.process_log.Add(newLine);

            UpdateProcessLog?.Invoke(this, EventArgs.Empty);
        }

        public void SaveDataToFile()
        {
            results.date_saved = DateTime.Now;

            var options = new JsonSerializerOptions { WriteIndented = true };
            string resultsString = JsonSerializer.Serialize<DDMResults>(results, options);

            string fullDirectory = saveMainDirectory + saveFolderPrefix + results.ring_sn + "\\";
            string fullPath = fullDirectory + fileNameLog + ".txt";

            Directory.CreateDirectory(fullDirectory);
            File.WriteAllText(fullPath, resultsString);

            ZipFile.CreateFromDirectory(fullDirectory, saveMainDirectory + saveFolderPrefix + results.ring_sn + ".zip");
        }

        public void OpenBrowserToDirectory()
        {
            string fullDirectory = saveMainDirectory + saveFolderPrefix + results.ring_sn + "\\";
            Process.Start("explorer.exe", fullDirectory);
        }
        
        public string GetLogAsString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < results.process_log?.Count; i++)
            {
                sb.Append(results.process_log[i].date?.ToString(dateFormatLong));
                sb.Append(": ");
                sb.Append(results.process_log[i].message?.ToString());
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
