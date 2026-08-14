using System;

namespace DDMAutoGUI.Services
{
    public class AdvancedOptions
    {
        public ConnectionOptions ConnectionOptions { get; set; } = new();
        public PartCycleOptions PartCycleOptions { get; set; } = new();
        public ResultsStorageOptions ResultsStorageOptions { get; set; } = new();
    }

    public class ConnectionOptions
    {
        public bool Controller { get; set; } = true;
        public bool IoLinkDevices { get; set; } = true;
        public bool TopCamera { get; set; } = true;
        public bool SideCamera { get; set; } = true;
        public bool LaserSensor { get; set; } = true;
        public bool DaqDevice { get; set; } = false;

        /// <summary>
        /// I/O-Link port numbers (1-indexed) that must report "connected" for the
        /// connection to be considered valid. Ports not listed here are ignored.
        /// </summary>
        public int[] ExpectedIoLinkPorts { get; set; } = new[] { 1, 2, 5, 6, 7 };

    }

    public class ResultsStorageOptions
    {
        /// <summary>UNC or mapped path to the server results share, e.g. @"\\us-wst-xxxxxx\share".</summary>
        public string ServerPath { get; set; } = @"\\us-wst-1-fsx-01\share";

        public bool VerifyServerOnConnect { get; set; } = false;

        /// <summary>When true, results are written locally only and never copied to the server (dev/debug).</summary>
        public bool SaveLocalOnly { get; set; } = false;

        /// <summary>When true, the local copy is deleted after a verified server copy. Off until the flow is proven.</summary>
        public bool DeleteLocalAfterCopy { get; set; } = false;
    }

    public class PartCycleOptions
    {
        public bool CheckHealth { get; set; } = true;
        public bool PhotoTop { get; set; } = true;
        public bool PhotoSide { get; set; } = true;
        public bool RunOCR { get; set; } = true;
        public bool CheckPolarity { get; set; } = true;
        public bool MeasureHeights { get; set; } = true;
        public bool DispenseCA { get; set; } = true;
        public bool DispenseUV { get; set; } = true;
        public bool CureUV { get; set; } = true;
        public bool Autocalibrate { get; set; } = true;
        public bool PhotoTopAfter { get; set; } = true;
        public bool OverrideWarnings { get; set; } = false;
    }
}
