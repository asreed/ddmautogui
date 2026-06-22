using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using DDMAutoGUI.Data;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Static utility class for accessing application release information.
    /// Loads release history from a JSON file on first access.
    /// </summary>
    public static class ReleaseInfo
    {
        private static readonly string ReleaseInfoFileName = "releaseHistory.json";
        private static readonly string ReleaseInfoFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ReleaseInfoFileName);
        private static ReleaseInfoHistory _releaseInfoHistory;
        private static ReleaseInfoSingle _currentReleaseInfo;
        private static bool _isInitialized = false;
        private static bool _initializationFailed = false;

        static ReleaseInfo()
        {
            Initialize();
        }

        private static void Initialize()
        {
            try
            {
                // Check if file exists before attempting to read
                if (!File.Exists(ReleaseInfoFilePath))
                {
                    Debug.Print($"Release info file not found at: {ReleaseInfoFilePath}");
                    _initializationFailed = true;
                    return;
                }

                string rawJson = File.ReadAllText(ReleaseInfoFilePath);

                // Deserialize with appropriate options for JSON property names
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                _releaseInfoHistory = JsonSerializer.Deserialize<ReleaseInfoHistory>(rawJson, options);

                if (_releaseInfoHistory?.releases != null && _releaseInfoHistory.releases.Count > 0)
                {
                    _currentReleaseInfo = _releaseInfoHistory.releases.FirstOrDefault();
                    Debug.Print($"Release info initialized. Current version: {_currentReleaseInfo?.version ?? "Unknown"}");
                    _isInitialized = true;
                }
                else
                {
                    Debug.Print("Release history file is empty or has no releases");
                    _initializationFailed = true;
                }
            }
            catch (JsonException ex)
            {
                Debug.Print($"Error deserializing release info JSON: {ex.Message}");
                _initializationFailed = true;
            }
            catch (IOException ex)
            {
                Debug.Print($"Error reading release info file: {ex.Message}");
                _initializationFailed = true;
            }
            catch (Exception ex)
            {
                Debug.Print($"Unexpected error initializing release info: {ex.Message}");
                _initializationFailed = true;
            }
        }

        /// <summary>
        /// Gets the current release information.
        /// Returns null if initialization failed.
        /// </summary>
        public static ReleaseInfoSingle GetCurrentReleaseInfo()
        {
            if (_initializationFailed)
            {
                Debug.Print("Release info initialization failed. Returning null.");
                return null;
            }

            return _currentReleaseInfo;
        }

        /// <summary>
        /// Gets the complete release history.
        /// Returns null if initialization failed or if no releases are available.
        /// </summary>
        public static ReleaseInfoHistory GetReleaseHistory()
        {
            if (_initializationFailed)
            {
                Debug.Print("Release info initialization failed. Returning null.");
                return null;
            }

            return _releaseInfoHistory;
        }

        /// <summary>
        /// Gets the current application version string.
        /// Returns "Unknown" if initialization failed or no version is available.
        /// </summary>
        public static string GetCurrentVersion()
        {
            if (_initializationFailed || _currentReleaseInfo == null)
            {
                return "Unknown";
            }

            return _currentReleaseInfo.version ?? "Unknown";
        }

        /// <summary>
        /// Checks if the release info was successfully initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets a specific release by version number, or null if not found.
        /// </summary>
        public static ReleaseInfoSingle GetReleaseByVersion(string version)
        {
            if (_initializationFailed || _releaseInfoHistory?.releases == null)
            {
                return null;
            }

            return _releaseInfoHistory.releases.FirstOrDefault(r => r.version == version);
        }
    }
}