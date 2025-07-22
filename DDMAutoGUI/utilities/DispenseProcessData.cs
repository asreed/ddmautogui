using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMAutoGUI.utilities
{
    public class DispenseProcessData
    {
        public string ringSN = "";
        public string processLog = "";
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
    }
}
