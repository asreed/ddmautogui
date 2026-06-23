using System;

namespace DDMAutoGUI.Services
{
    public class AdvancedOptions
    {
        public ConnectionOptions ConnectionOptions { get; set; } = new();
        public DispenseOptions DispenseOptions { get; set; } = new();
    }

    public class ConnectionOptions
    {
        public bool Controller { get; set; } = true;
        public bool IoLinkDevices { get; set; } = true;
        public bool TopCamera { get; set; } = true;
        public bool SideCamera { get; set; } = true;
        public bool LaserSensor { get; set; } = true;
        public bool DaqDevice { get; set; } = true;
    }

    public class DispenseOptions
    {
        public bool CheckHealth { get; set; } = true;
        public bool Dispense { get; set; } = true;
        public bool PhotoTop { get; set; } = true;
        public bool PhotoSide { get; set; } = true;
        public bool RunOCR { get; set; } = true;
        public bool CheckPolarity { get; set; } = true;
        public bool MeasureHeights { get; set; } = true;
        public bool Autocalibrate { get; set; } = true;
        public bool PhotoTopAfter { get; set; } = true;
        public bool OverrideWarnings { get; set; } = false;
    }
}
