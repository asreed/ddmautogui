using System;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Provides access to application-wide configuration and settings.
    /// </summary>
    public class ApplicationConfiguration : IApplicationConfiguration
    {
        private readonly string _displayTitle;
        private readonly bool _isSimulationMode;
        private readonly string _calibrationPassword;
        private readonly string _servicePassword;
        private readonly string _advancedSettingsPassword;
        private readonly AdvancedOptions _advancedOptions;

        public ApplicationConfiguration(
            string displayTitle = "ADS Work Cell Manager",
            bool isSimulationMode = false,
            string calibrationPassword = "ddm",
            string advancedSettingsPassword = "ddm",
            string servicePassword = "ddm",
            AdvancedOptions advancedOptions = null)
        {
            _isSimulationMode = isSimulationMode;
            _calibrationPassword = calibrationPassword;
            _displayTitle = displayTitle;
            _servicePassword = servicePassword;
            _advancedSettingsPassword = advancedSettingsPassword;
            _advancedOptions = advancedOptions ?? new AdvancedOptions();
        }

        public string DisplayTitle => _displayTitle;
        public bool IsSimulationMode => _isSimulationMode;
        public string CalibrationPassword => _calibrationPassword;
        public string AdvancedSettingsPassword => _advancedSettingsPassword;
        public string ServicePassword => _servicePassword;
        public AdvancedOptions AdvancedOptions => _advancedOptions;
    }
}