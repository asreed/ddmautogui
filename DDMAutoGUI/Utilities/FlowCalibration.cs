using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMAutoGUI.Utilities
{
    public static class FlowCalibration
    {
        static FlowCalibration() { }

        public static float CalculateNewPressure(
            float prevPressure,
            float prevVolume,
            float targetVolume)
        {
            return (prevPressure * prevVolume) / targetVolume;
        }
    }
}
