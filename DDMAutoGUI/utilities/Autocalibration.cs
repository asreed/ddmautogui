using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMAutoGUI.utilities
{
    public class Autocalibration
    {
        public Autocalibration() { }

        public static float GetTargetPressure(float prevPressure, float prevFlow, float targetFlow)
        {
            // assuming linear relations
            float a = targetFlow / prevFlow;
            float pressure = a * prevPressure;
            return pressure;
        }


    }
}
