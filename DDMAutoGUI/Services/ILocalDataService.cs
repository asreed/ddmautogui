using System;
using System.Collections.Generic;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the local data manager service.
    /// Handles local data storage and retrieval.
    /// </summary>
    public interface ILocalDataService
    {
        /// <summary>
        /// Raised whenever the in-memory local data is replaced (e.g., after a flow
        /// calibration saves new results). Lets view models refresh derived state
        /// without polling.
        /// </summary>
        event EventHandler LocalDataChanged;

        void LoadLocalData();
        void SaveLocalData();

        LocalData GetLocalData();
        void SetLocalData(LocalData newData);
        LDMotorCalib GetCalibFromMotorName(string name);
        LDMotorCalib GetCalibFromMotorName(LocalData data, string name);
        float? GetPressureFromMotorName(string name, int systemNum);
        bool SaveLocalDataToFile(LocalData newData);
        bool SaveLocalDataToFile();

        /// <summary>
        /// Serializes local data to a JSON string.
        /// </summary>
        string SerializeDataFromJson(LocalData data);

        /// <summary>
        /// Deserializes local data from a JSON string.
        /// </summary>
        LocalData DeserializeLocalDataFromString(string rawJson);
    }
}