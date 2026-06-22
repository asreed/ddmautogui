using System;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Provides access to application-wide configuration and settings.
    /// </summary>
    public class ApplicationConfiguration : IApplicationConfiguration
    {
        private readonly AdvancedOptions _advancedOptions;
        private readonly bool _isSimulationMode;
        private readonly string _calibrationPassword;
        private readonly string _advancedSettingsPassword;

        public ApplicationConfiguration(
            bool isSimulationMode = false,
            string calibrationPassword = "ddm",
            string advancedSettingsPassword = "ddm",
            AdvancedOptions advancedOptions = null)
        {
            _isSimulationMode = isSimulationMode;
            _calibrationPassword = calibrationPassword;
            _advancedSettingsPassword = advancedSettingsPassword;
            _advancedOptions = advancedOptions ?? new AdvancedOptions();
        }

        public bool IsSimulationMode => _isSimulationMode;
        public string CalibrationPassword => _calibrationPassword;
        public string AdvancedSettingsPassword => _advancedSettingsPassword;
        public AdvancedOptions AdvancedOptions => _advancedOptions;
    }
}