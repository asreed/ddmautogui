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

        /// <summary>
        /// I/O-Link port numbers (1-indexed) that must report "connected" for the
        /// connection to be considered valid. Ports not listed here are ignored.
        /// </summary>
        public int[] ExpectedIoLinkPorts { get; set; } = new[] { 1, 2, 5, 6, 7 };
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
