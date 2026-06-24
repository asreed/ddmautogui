using System;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the settings manager service.
    /// Handles loading, saving, and managing cell settings.
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// Gets or sets the DDM size enum value for selecting which motor settings to use.
        /// </summary>
        SettingsManager.DDMSize SelectedSize { get; set; }

        /// <summary>
        /// Retrieves all current cell settings.
        /// </summary>
        CellSettings GetAllSettings();

        /// <summary>
        /// Serializes cell settings to a JSON string.
        /// </summary>
        string SerializeSettingsToJson(CellSettings settings);

        /// <summary>
        /// Deserializes cell settings from a JSON string.
        /// </summary>
        CellSettings DeserializeSettingsFromJson(string json);

        /// <summary>
        /// Gets the motor settings for the currently selected DDM size.
        /// </summary>
        CSMotor GetSettingsForSelectedSize();

        /// <summary>
        /// Gets motor settings by motor name (e.g., "ddm_57", "ddm_116", etc.).
        /// </summary>
        CSMotor GetMotorSettingsFromName(string motorName);

        /// <summary>
        /// Reloads settings from the controller.
        /// </summary>
        void ReloadSettings();

        /// <summary>
        /// Verifies that settings exist on the controller at the specified IP address.
        /// </summary>
        bool LoadAndVerifySettings(string ip);

        /// <summary>
        /// Saves the provided settings to the controller via FTP.
        /// </summary>
        void SaveSettingsToController(CellSettings settings);

        /// <summary>
        /// Saves a copy of settings to a local directory.
        /// </summary>
        void SaveSettingsCopyToLocal(CellSettings settings, string directoryPath);
    }
}