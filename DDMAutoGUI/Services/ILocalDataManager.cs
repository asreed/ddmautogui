using System;
using System.Collections.Generic;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the local data manager service.
    /// Handles local data storage and retrieval.
    /// </summary>
    public interface ILocalDataManager
    {
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