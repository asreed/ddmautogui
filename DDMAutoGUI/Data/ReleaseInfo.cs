using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DDMAutoGUI.Data
{
    /// <summary>
    /// Represents a single release entry in the release history.
    /// </summary>
    public class ReleaseInfoSingle
    {
        /// <summary>
        /// The version number (e.g., "1.0.0")
        /// </summary>
        [JsonPropertyName("version")]
        public string version { get; set; }

        /// <summary>
        /// The release date
        /// </summary>
        [JsonPropertyName("releaseDate")]
        public DateTime releaseDate { get; set; }

        /// <summary>
        /// Summary of changes in this release
        /// </summary>
        [JsonPropertyName("summary")]
        public string summary { get; set; }

        /// <summary>
        /// Detailed release notes
        /// </summary>
        [JsonPropertyName("releaseNotes")]
        public string releaseNotes { get; set; }
    }

    /// <summary>
    /// Represents the complete release history.
    /// </summary>
    public class ReleaseInfoHistory
    {
        /// <summary>
        /// List of all releases, typically ordered with newest first.
        /// </summary>
        [JsonPropertyName("releases")]
        public List<ReleaseInfoSingle> releases { get; set; } = new List<ReleaseInfoSingle>();
    }
}