using System;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Contains advanced options that control various aspects of the application.
    /// </summary>
    public class AdvancedOptions
    {
        /// <summary>
        /// Options for controlling which devices to connect to during initialization.
        /// </summary>
        public ConnectionOptions ConnectionOptions { get; set; } = new();

        /// <summary>
        /// Options for controlling individual steps of the dispense process.
        /// </summary>
        public DispenseOptions DispenseOptions { get; set; } = new();
    }

    /// <summary>
    /// Options for controlling device connection initialization.
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        /// Whether to connect to the main workcell controller.
        /// </summary>
        public bool Controller { get; set; } = true;

        /// <summary>
        /// Whether to connect to IO-Link devices.
        /// </summary>
        public bool IoLinkDevices { get; set; } = true;

        /// <summary>
        /// Whether to connect to the top camera.
        /// </summary>
        public bool TopCamera { get; set; } = true;

        /// <summary>
        /// Whether to connect to the side camera.
        /// </summary>
        public bool SideCamera { get; set; } = true;

        /// <summary>
        /// Whether to connect to the laser sensor.
        /// </summary>
        public bool LaserSensor { get; set; } = true;

        /// <summary>
        /// Whether to connect to the DAQ device.
        /// </summary>
        public bool DaqDevice { get; set; } = true;
    }

    /// <summary>
    /// Controls which steps of the dispense process should be executed.
    /// </summary>
    public class DispenseOptions
    {
        /// <summary>
        /// Whether to perform system health check.
        /// </summary>
        public bool CheckHealth { get; set; } = true;

        /// <summary>
        /// Whether to perform dispense operation.
        /// </summary>
        public bool Dispense { get; set; } = true;

        /// <summary>
        /// Whether to acquire top preprocess photo.
        /// </summary>
        public bool PhotoTop { get; set; } = true;

        /// <summary>
        /// Whether to acquire side photo.
        /// </summary>
        public bool PhotoSide { get; set; } = true;

        /// <summary>
        /// Whether to run OCR processing on images.
        /// </summary>
        public bool RunOCR { get; set; } = true;

        /// <summary>
        /// Whether to check magnet polarity.
        /// </summary>
        public bool CheckPolarity { get; set; } = true;

        /// <summary>
        /// Whether to measure ring and magnet heights.
        /// </summary>
        public bool MeasureHeights { get; set; } = true;

        /// <summary>
        /// Whether to perform autocalibration after dispense.
        /// </summary>
        public bool Autocalibrate { get; set; } = true;

        /// <summary>
        /// Whether to acquire top post-process photo.
        /// </summary>
        public bool PhotoTopAfter { get; set; } = true;

        /// <summary>
        /// Whether to override dispense warnings and continue with operation.
        /// </summary>
        public bool OverrideWarnings { get; set; } = false;
    }
}