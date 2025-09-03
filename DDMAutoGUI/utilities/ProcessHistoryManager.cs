using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMAutoGUI.utilities
{

    public class ProcessHistory
    {
        public ProcessHistoryEvent[]? process_events { get; set; }
    }

    public class ProcessHistoryEvent
    {
        public DateTime timestamp { get; set; }
        public string? ring_sn { get; set; }
        public float? pressure_1 { get; set; }
        public float? pressure_2 { get; set; }

    }




    internal class ProcessHistoryManager
    {
    }
}
