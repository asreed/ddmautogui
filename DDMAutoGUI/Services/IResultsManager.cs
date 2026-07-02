using System;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the results manager service.
    /// Handles creation, storage, and management of process results.
    /// </summary>
    public interface IResultsManager
    {
        event EventHandler UpdateProcessLog;

        Results currentResults { get; set; }

        /// <summary>
        /// Absolute path to the folder where the current run's results are saved.
        /// Set by CreateResultsFolder() and updated by RenameResultsFolder().
        /// </summary>
        string currentResultsFolderPath { get; }

        /// <summary>
        /// Date format string for long timestamps (e.g., "MM-dd-yyyy HH:mm:ss.fff").
        /// </summary>
        string DateFormatLong { get; }

        /// <summary>
        /// Date format string for short timestamps (e.g., "HH:mm:ss.ff").
        /// </summary>
        string DateFormatShort { get; }

        /// <summary>
        /// Date format string for folder names (e.g., "yyMMdd_HHmmss").
        /// </summary>
        string DateFormatFolder { get; }

        Results CreateNewResults();
        void ClearCurrentResults();
        void AddToLog(string line);
        string CreateResultsFolder();
        void SaveDataToFile();
        void RenameResultsFolder(string ringSN);
        void CopyPhotoToResultsFolder(string photoPath, string fileName);
        void CopyPolarityPlotToResultsFolder(string plotPath, string fileName);
        void CopyPolarityDataToResultsFolder(string dataPath, string fileName);
        void DeterminePassFail(Results results, CellSettings settings, CSMotor motorSettings, out bool pass, out string message);
        void OpenBrowserToDirectory();
        string GetLogAsString();
        string GetCurrentResultsAsString();
    }
}