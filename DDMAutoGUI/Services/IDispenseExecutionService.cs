using System;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Owns the physical part cycle choreography (power/home, pressure setup,
    /// dispense-to-ring). Performs no business decisions, no result persistence,
    /// and no direct logging. Callers supply a log delegate and interpret results.
    /// </summary>
    public interface IDispenseExecutionService
    {
        Task EnablePowerAndHomeAsync(Action<string> log = null);

        /// <summary>
        /// Sets system pressures from the supplied calibration, waits for them to
        /// settle, and zeroes the flow sensors.
        /// </summary>
        Task SetupPressuresAsync(CellSettings settings, LDMotorCalib calib, Action<string> log = null);

        /// <summary>
        /// Runs the ID/OD dispense for the given motor and returns parsed shot data.
        /// Throws <see cref="PartCycleException"/> if the shot itself reports failure.
        /// </summary>
        Task<ResultsShotData> DispenseToRingAsync(CellSettings settings, CSMotor motor, string motorName, Action<string> log = null);


        /// <summary>
        /// Runs the single track dispense for the given motor and returns parsed shot data.
        /// Throws <see cref="PartCycleException"/> if the shot itself reports failure.
        /// </summary>
        Task<ResultsShotData> DispenseSingleTrackToRingAsync(int valveIdx, float shotTime, float xPos, float tPos, int dir);
    }
}