using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;



namespace DDMAutoGUI.utilities
{

    public class ReleaseInfoSingle
    {
        public string? version { get; set; }
        public string? releaseDate { get; set; }
        public string? releaseIntent { get; set; }
        public string? releaseNotes { get; set; }
        public string? releaseDisplayNotes { get; set; }
    }

    public class ReleaseInfoHistory
    {
        public ReleaseInfoSingle[]? releases { get; set; }
    }


    public class ReleaseInfoManager
    {

        private string releaseInfoFileName = "releaseHistory.json";
        private string releaseInfoFilePath;
        private ReleaseInfoHistory releaseInfoHistory;
        private ReleaseInfoSingle currentReleaseInfo;

        public ReleaseInfoManager()
        {
            releaseInfoFilePath = AppDomain.CurrentDomain.BaseDirectory + releaseInfoFileName;

            string rawJson = File.ReadAllText(releaseInfoFilePath);
            releaseInfoHistory = new ReleaseInfoHistory();
            releaseInfoHistory = JsonSerializer.Deserialize<ReleaseInfoHistory>(rawJson);

            currentReleaseInfo = releaseInfoHistory.releases.First();

            Debug.Print("Release info manager initialized");

        }

        public ReleaseInfoSingle GetCurrentReleaseInfo()
        {
            return currentReleaseInfo;
        }

        public ReleaseInfoHistory GetReleaseHistory()
        {
            return releaseInfoHistory;
        }

        public string GetCurrentVersion()
        {
            return currentReleaseInfo.version;
        }
    }
}
