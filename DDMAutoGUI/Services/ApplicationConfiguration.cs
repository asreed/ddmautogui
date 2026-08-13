using System;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Provides access to application-wide configuration and settings.
    /// </summary>
    public class ApplicationConfiguration : IApplicationConfiguration
    {
        private readonly string _defaultControllerIPAddress;
        private readonly string _displayTitle;
        private readonly string _calibrationPassword;
        private readonly string _servicePassword;
        private readonly string _advancedSettingsPassword;
        private readonly AdvancedOptions _advancedOptions;

        public ApplicationConfiguration(
            string defaultControllerIPAddress = "192.168.0.1",
            string displayTitle = "ADS Work Cell Manager",
            string calibrationPassword = "ddm",
            string servicePassword = "ddm",
            string advancedSettingsPassword = "DDM",
            AdvancedOptions advancedOptions = null)
        {
            _defaultControllerIPAddress = defaultControllerIPAddress;
            _displayTitle = displayTitle;
            _calibrationPassword = calibrationPassword;
            _servicePassword = servicePassword;
            _advancedSettingsPassword = advancedSettingsPassword;
            _advancedOptions = advancedOptions ?? new AdvancedOptions();
        }

        public string DefaultControllerIPAddress => _defaultControllerIPAddress;
        public string DisplayTitle => _displayTitle;
        public string CalibrationPassword => _calibrationPassword;
        public string AdvancedSettingsPassword => _advancedSettingsPassword;
        public string ServicePassword => _servicePassword;
        public AdvancedOptions AdvancedOptions => _advancedOptions;
    }
}