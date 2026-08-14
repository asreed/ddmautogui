using System;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    public interface IFlowCalibrationService
    {
        /// <summary>
        /// Generates and saves calibration results to local data file.
        /// </summary>
        void GenerateAndSaveCalibration(RunCalibResult result);

        /// <summary>
        /// Runs the full dispense calibration routine for a specific motor.
        /// </summary>
        Task<RunCalibResult> RunDispenseForManualCalibration(
            CellSettings settings,
            LocalData localData,
            string motorName);

        /// <summary>
        /// Sets pressures from current calibration data.
        /// </summary>
        void SetPressuresFromCalibration(
            CellSettings settings,
            LocalData localData,
            string motorName);

        /// <summary>
        /// Calculates new scale factors for flow calibration based on shot data.
        /// </summary>
        void CalculateNewScaleFactors(
            ResultsDispData shotData,
            CellSettings settings,
            LocalData localData,
            out bool success,
            out string message,
            out float sf1,
            out float sf2);
    }
}