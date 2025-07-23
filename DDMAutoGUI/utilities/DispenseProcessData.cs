using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DDMAutoGUI.utilities
{
    public class DispenseProcessData
    {

        public string saveMainDirectory = AppDomain.CurrentDomain.BaseDirectory + "process_data\\";
        public string saveFolderPrefix = "ring_";

        public string fileNameLog = "process_log";
        public string fileNameRingMeasure = "ring_measurements";
        public string fileNameMagMeasure = "mag_measurements";
        public string fileNamePhotoBefore = "before";
        public string fileNamePhotoAfter = "after";


        public string ringSN = "";
        public string processLog = "";
        public string ringMeasurements = "";
        public string magMeasurements = "";
        public event EventHandler UpdateProcessLog;

        public DispenseProcessData()
        { 
            //
        }


        public void AddToLog(string line)
        {
            DateTime now = DateTime.Now;
            string newLine = now.ToShortDateString() + " " + now.ToLongTimeString() + ": " + line + "\n";
            Debug.Print(newLine);
            processLog += newLine;

            UpdateProcessLog?.Invoke(this, EventArgs.Empty);
        }

        public void SaveLogToFile()
        {
            string fullDirectory = saveMainDirectory + saveFolderPrefix + ringSN + "\\";
            string fullPath = fullDirectory + fileNameLog + ".txt";
            Directory.CreateDirectory(fullDirectory);
            File.WriteAllText(fullPath, processLog);
        }

        public void SaveRingMeasurenentsToFile(string measurements)
        {
            ringMeasurements = measurements;
            string fullDirectory = saveMainDirectory + saveFolderPrefix + ringSN + "\\";
            string fullPath = fullDirectory + fileNameRingMeasure + ".txt";
            Directory.CreateDirectory(fullDirectory);
            File.WriteAllText(fullPath, measurements);
        }

        public void SaveMagMeasurenentsToFile(string measurements)
        {
            magMeasurements = measurements;
            string fullDirectory = saveMainDirectory + saveFolderPrefix + ringSN + "\\";
            string fullPath = fullDirectory + fileNameMagMeasure + ".txt";
            Directory.CreateDirectory(fullDirectory);
            File.WriteAllText(fullPath, measurements);
        }

        public void OpenBrowserToDirectory()
        {
            string fullDirectory = saveMainDirectory + saveFolderPrefix + ringSN + "\\";
            Process.Start("explorer.exe", fullDirectory);
        }
    }
}
