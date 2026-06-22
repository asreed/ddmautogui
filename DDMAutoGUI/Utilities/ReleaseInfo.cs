using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace DDMAutoGUI.Utilities
{
    /// <summary>
    /// Represents a single release information entry.
    /// </summary>
    public class ReleaseInfoEntry
    {
        public string? version { get; set; }
        public string? releaseDate { get; set; }
        public string? releaseIntent { get; set; }
        public string? releaseNotes { get; set; }
        public string? releaseDisplayNotes { get; set; }
    }

    /// <summary>
    /// Represents the release history collection.
    /// </summary>
    public class ReleaseInfoHistory
    {
        public ReleaseInfoEntry[]? releases { get; set; }
    }

    /// <summary>
    /// Static utility for accessing release information.
    /// </summary>
    public static class ReleaseInfo
    {
        private static ReleaseInfoHistory? _releaseHistory;
        private static ReleaseInfoEntry? _currentRelease;
        private static readonly string ReleaseInfoFileName = "releaseHistory.json";

        static ReleaseInfo()
        {
            LoadReleaseInfo();
        }

        private static void LoadReleaseInfo()
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, ReleaseInfoFileName);

                if (!File.Exists(filePath))
                {
                    Debug.Print($"Release info file not found: {filePath}");
                    return;
                }

                string rawJson = File.ReadAllText(filePath);
                _releaseHistory = JsonSerializer.Deserialize<ReleaseInfoHistory>(rawJson);

                if (_releaseHistory?.releases?.Length > 0)
                {
                    _currentRelease = _releaseHistory.releases[0];
                    Debug.Print($"Release info loaded: v{_currentRelease.version}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error loading release info: {ex.Message}");
            }
        }

        public static ReleaseInfoEntry? GetCurrentRelease() => _currentRelease;

        public static ReleaseInfoEntry? GetCurrentReleaseInfo() => _currentRelease;

        public static ReleaseInfoHistory? GetReleaseHistory() => _releaseHistory;

        public static string? GetCurrentVersion() => _currentRelease?.version;
    }
}