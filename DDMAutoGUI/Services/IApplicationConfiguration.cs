using System;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for application configuration.
    /// </summary>
    public interface IApplicationConfiguration
    {
        string DefaultControllerIPAddress { get; }
        string DisplayTitle { get; }
        bool IsSimulationMode { get; }
        string CalibrationPassword { get; }
        string ServicePassword { get; }
        string AdvancedSettingsPassword { get; }
        AdvancedOptions AdvancedOptions { get; }
    }
}