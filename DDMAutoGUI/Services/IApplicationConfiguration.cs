using System;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for application configuration.
    /// </summary>
    public interface IApplicationConfiguration
    {
        /// <summary>
        /// Gets whether the application is running in simulation mode.
        /// </summary>
        bool IsSimulationMode { get; }

        /// <summary>
        /// Gets the password required to access calibration features.
        /// </summary>
        string CalibrationPassword { get; }

        /// <summary>
        /// Gets the password required to access advanced settings.
        /// </summary>
        string AdvancedSettingsPassword { get; }

        /// <summary>
        /// Gets the advanced options for controlling dispense process behavior.
        /// </summary>
        AdvancedOptions AdvancedOptions { get; }
    }
}