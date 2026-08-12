using DDMAutoGUI.Constants;
using NationalInstruments;
using NationalInstruments.DAQmx;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DaqTask = NationalInstruments.DAQmx.Task;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Handles analog acquisition from the NI USB-6003 for Hall sensor measurements.
    /// Replaces the MATLAB PolarityTest.exe acquisition stage.
    /// </summary>
    public class DaqService : IDaqService
    {
        #region Constants

        /// <summary>Sample rate in Hz. Matches CollectProcessHall.m.</summary>
        public const double SampleRate = 2000.0;

        /// <summary>Acquisition duration in seconds. Matches CollectProcessHall.m.</summary>
        public const double SampleTime = 1.01;

        /// <summary>Short duration used by the signal test only.</summary>
        private const double TestSampleTime = 0.1;

        private const string ChannelName = "ai0";
        private const double MinVoltage = -10.0;
        private const double MaxVoltage = 10.0;

        /// <summary>Minimum peak-to-peak volts expected from a live Hall sensor.</summary>
        private const double MinExpectedAmplitude = 0.05;

        #endregion

        #region Fields

        // Serializes access to the DAQmx device. Tasks hold an exclusive
        // reservation, so a test must never overlap a live acquisition.
        private readonly SemaphoreSlim _deviceLock = new SemaphoreSlim(1, 1);

        private string _deviceId;
        private bool _isConnected;
        private bool _disposed;

        #endregion

        #region Events

        public event EventHandler DaqConnectionStateChanged;

        #endregion

        #region Properties

        public string DeviceId => _deviceId;

        public bool IsConnected => _isConnected;

        #endregion

        #region Construction

        public DaqService()
        {
            // No device enumeration here. The DAQ is discovered on the first
            // TestDaqConnection call, which runs as part of the work cell
            // connect routine - matching how the cameras are resolved.
            Debug.Print("DAQ service initialized (device not yet resolved)");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _deviceLock.Dispose();
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Enumerates devices and verifies the Hall input task can be configured.
        /// Fast; does not acquire samples.
        /// </summary>
        public async Task<DaqConnectionResult> TestDaqConnection()
        {
            var result = new DaqConnectionResult { success = false };

            await _deviceLock.WaitAsync();
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    _deviceId = FindFirstDevice();

                    if (_deviceId == null)
                    {
                        result.error_code = int.Parse(ErrorCodes.conDaq.Code);
                        result.error_message = ErrorCodes.conDaq.Message;
                        return;
                    }

                    result.device_id = _deviceId;

                    // Verify reserves the device and validates channel and timing
                    // without transferring samples. This is what catches a bad
                    // channel name or a reservation held by another application.
                    using (DaqTask task = CreateHallTask(_deviceId, SampleTime))
                    {
                        task.Control(TaskAction.Verify);
                    }

                    result.success = true;
                    result.error_code = 0;
                    result.error_message = "No error";
                });
            }
            catch (DaqException ex)
            {
                Debug.Print($"DAQ connection test failed: {ex.Message}");
                result.success = false;
                result.error_code = int.Parse(ErrorCodes.conDaqChan.Code);
                result.error_message = $"{ErrorCodes.conDaqChan.Message}: {ex.Message}";
            }
            catch (Exception ex)
            {
                Debug.Print($"DAQ connection test failed: {ex.Message}");
                result.success = false;
                result.error_code = int.Parse(ErrorCodes.conDaq.Code);
                result.error_message = ex.Message;
            }
            finally
            {
                _deviceLock.Release();
            }

            SetConnectionState(result.success);
            return result;
        }

        /// <summary>
        /// Performs a short live acquisition to confirm wiring and sensor power.
        /// </summary>
        public async Task<DaqConnectionResult> TestDaqSignal()
        {
            // Reuse the cheap test first so a missing device is reported
            // with the specific connection error rather than a read timeout.
            DaqConnectionResult result = await TestDaqConnection();
            if (!result.success) return result;

            await _deviceLock.WaitAsync();
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    double[] samples = ReadSamples(_deviceId, TestSampleTime);

                    if (samples == null || samples.Length == 0)
                    {
                        result.success = false;
                        result.error_code = int.Parse(ErrorCodes.daqEmpty.Code);
                        result.error_message = ErrorCodes.daqEmpty.Message;
                        return;
                    }

                    double amplitude = samples.Max() - samples.Min();
                    result.signal_amplitude = amplitude;

                    if (amplitude < MinExpectedAmplitude)
                    {
                        // Device responds but the line is flat - almost always
                        // unpowered or unwired sensor rather than a DAQ fault.
                        result.success = false;
                        result.error_code = int.Parse(ErrorCodes.daqNoSignal.Code);
                        result.error_message =
                            $"{ErrorCodes.daqNoSignal.Message} (amplitude {amplitude:F3} V)";
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.Print($"DAQ signal test failed: {ex.Message}");
                result.success = false;
                result.error_code = int.Parse(ErrorCodes.daqAcqFail.Code);
                result.error_message = $"{ErrorCodes.daqAcqFail.Message}: {ex.Message}";
            }
            finally
            {
                _deviceLock.Release();
            }

            SetConnectionState(result.success);
            return result;
        }

        #endregion

        #region Acquisition

        /// <summary>
        /// Acquires the full Hall record for polarity processing.
        /// </summary>
        public async Task<HallAcquisitionResult> AcquireHallData()
        {
            var result = new HallAcquisitionResult { success = false };

            await _deviceLock.WaitAsync();
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // Re-enumerate every acquisition. The USB-6003 can be
                    // unplugged between operations, and enumeration is cheap.
                    _deviceId = FindFirstDevice();

                    if (_deviceId == null)
                    {
                        result.error_code = int.Parse(ErrorCodes.conDaq.Code);
                        result.error_message = ErrorCodes.conDaq.Message;
                        return;
                    }

                    double[] signal = ReadSamples(_deviceId, SampleTime);

                    if (signal == null || signal.Length == 0)
                    {
                        result.error_code = int.Parse(ErrorCodes.daqEmpty.Code);
                        result.error_message = ErrorCodes.daqEmpty.Message;
                        return;
                    }

                    // Rebuild the time vector from the sample clock. The hardware
                    // clock is the authority on spacing, so this matches the
                    // MATLAB data.Time column.
                    var time = new double[signal.Length];
                    for (int i = 0; i < time.Length; i++)
                    {
                        time[i] = i / SampleRate;
                    }

                    result.time = time;
                    result.signal = signal;
                    result.success = true;
                    result.error_code = 0;
                    result.error_message = "No error";
                });
            }
            catch (Exception ex)
            {
                Debug.Print($"Hall acquisition failed: {ex.Message}");
                result.success = false;
                result.error_code = int.Parse(ErrorCodes.daqAcqFail.Code);
                result.error_message = $"{ErrorCodes.daqAcqFail.Message}: {ex.Message}";
            }
            finally
            {
                _deviceLock.Release();
            }

            return result;
        }

        /// <summary>
        /// Reads one instantaneous sample from ai0.
        /// </summary>
        public async Task<DaqSingleReadResult> ReadSingleValue()
        {
            var result = new DaqSingleReadResult { success = false };

            await _deviceLock.WaitAsync();
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    _deviceId = FindFirstDevice();

                    if (_deviceId == null)
                    {
                        result.error_code = int.Parse(ErrorCodes.conDaq.Code);
                        result.error_message = ErrorCodes.conDaq.Message;
                        return;
                    }

                    // On-demand read: no sample clock is configured, so the
                    // task returns the current value immediately.
                    using (DaqTask task = new DaqTask())
                    {
                        task.AIChannels.CreateVoltageChannel(
                            $"{_deviceId}/{ChannelName}",
                            "hall",
                            AITerminalConfiguration.Rse,
                            MinVoltage,
                            MaxVoltage,
                            AIVoltageUnits.Volts);

                        var reader = new AnalogSingleChannelReader(task.Stream);
                        result.voltage = reader.ReadSingleSample();
                    }

                    result.success = true;
                    result.error_code = 0;
                    result.error_message = "No error";
                });
            }
            catch (Exception ex)
            {
                Debug.Print($"Single read failed: {ex.Message}");
                result.success = false;
                result.error_code = int.Parse(ErrorCodes.daqAcqFail.Code);
                result.error_message = $"{ErrorCodes.daqAcqFail.Message}: {ex.Message}";
            }
            finally
            {
                _deviceLock.Release();
            }

            return result;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns the first connected NI device name, or null if none present.
        /// </summary>
        private static string FindFirstDevice()
        {
            string[] devices = DaqSystem.Local.Devices;
            return devices != null && devices.Length > 0 ? devices[0] : null;
        }

        /// <summary>
        /// Builds a finite-sample voltage input task on ai0.
        /// Caller owns disposal.
        /// </summary>
        private static DaqTask CreateHallTask(string deviceId, double sampleTime)
        {
            int samples = (int)Math.Round(SampleRate * sampleTime);
            DaqTask task = new DaqTask();

            try
            {
                task.AIChannels.CreateVoltageChannel(
                    $"{deviceId}/{ChannelName}",
                    "hall",
                    AITerminalConfiguration.Rse,
                    MinVoltage,
                    MaxVoltage,
                    AIVoltageUnits.Volts);

                task.Timing.ConfigureSampleClock(
                    string.Empty,
                    SampleRate,
                    SampleClockActiveEdge.Rising,
                    SampleQuantityMode.FiniteSamples,
                    samples);

                return task;
            }
            catch
            {
                // Never leak a partially configured task - it would keep the
                // device reserved and fail every subsequent attempt.
                task.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates, reads and disposes a task in one operation. Creating the task
        /// per read costs a few ms and guarantees the device reservation is released.
        /// Runs synchronously by design - only called from inside Task.Run.
        /// </summary>
        private static double[] ReadSamples(string deviceId, double sampleTime)
        {
            int samples = (int)Math.Round(SampleRate * sampleTime);

            using (DaqTask task = CreateHallTask(deviceId, sampleTime))
            {
                task.Control(TaskAction.Verify);
                var reader = new AnalogSingleChannelReader(task.Stream);
                return reader.ReadMultiSample(samples);
            }
        }

        private void SetConnectionState(bool connected)
        {
            if (_isConnected == connected) return;

            _isConnected = connected;
            DaqConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public void ResetConnectionState()
        {
            _deviceId = null;
            SetConnectionState(false);
            Debug.Write("DAQ connection state reset");
        }
    }
}