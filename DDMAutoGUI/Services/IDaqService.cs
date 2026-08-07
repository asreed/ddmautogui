using System;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the DAQ service.
    /// Handles analog acquisition from the NI USB-6003 for Hall sensor measurements.
    /// </summary>
    public interface IDaqService : IDisposable
    {
        #region Events

        event EventHandler DaqConnectionStateChanged;

        #endregion

        #region Properties

        /// <summary>Device identifier in use (e.g. "Dev1"), or null if none resolved.</summary>
        string DeviceId { get; }

        /// <summary>True if a device was present and verified at last check.</summary>
        bool IsConnected { get; }

        #endregion

        #region Connection Management

        /// <summary>
        /// Enumerates devices and verifies the Hall input task can be configured.
        /// Fast; does not acquire samples.
        /// </summary>
        Task<DaqConnectionResult> TestDaqConnection();

        /// <summary>
        /// Performs a short live acquisition to confirm wiring and sensor power.
        /// </summary>
        Task<DaqConnectionResult> TestDaqSignal();

        #endregion

        #region Acquisition

        Task<HallAcquisitionResult> AcquireHallData();

        /// <summary>
        /// Reads one instantaneous sample from ai0. Useful for verifying wiring
        /// and observing the static Hall level without a full acquisition.
        /// </summary>
        Task<DaqSingleReadResult> ReadSingleValue();

        #endregion

        #region Device Management

        /// <summary>
        /// Clears cached connection state. No hardware handle is held between
        /// operations, so this only resets status for the UI.
        /// </summary>
        void ResetConnectionState();

        #endregion
    }

    /// <summary>
    /// Result of a DAQ connection or signal test.
    /// Error codes follow the -61xx range reserved for Hall/DAQ faults.
    /// </summary>
    public class DaqConnectionResult
    {
        public bool success { get; set; }
        public string device_id { get; set; }
        public int error_code { get; set; }
        public string error_message { get; set; }

        /// <summary>Peak-to-peak volts observed, when a signal test was run.</summary>
        public double? signal_amplitude { get; set; }
    }

    /// <summary>
    /// Raw Hall acquisition output, prior to processing.
    /// </summary>
    public class HallAcquisitionResult
    {
        public bool success { get; set; }
        public int error_code { get; set; }
        public string error_message { get; set; }
        public double[] time { get; set; }
        public double[] signal { get; set; }
    }

    /// <summary>
    /// Result of a single-point analog read.
    /// </summary>
    public class DaqSingleReadResult
    {
        public bool success { get; set; }
        public double voltage { get; set; }
        public int error_code { get; set; }
        public string error_message { get; set; }
    }
}