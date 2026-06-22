using DDMAutoGUI.Services;
using DDMAutoGUI.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDMAutoGUI.ViewModels
{
    /// <summary>
    /// Encapsulates the complex dispense process logic.
    /// Handles all steps of the dispense workflow in a clean, testable way.
    /// This service is used by the MainWindowViewModel to coordinate the dispense process.
    /// </summary>
    public class DispenseProcessService
    {
        private readonly IControllerManager _controllerManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IResultsManager _resultsManager;
        private readonly ICameraManager _cameraManager;
        private readonly ILocalDataManager _localDataManager;
        private readonly IFlowCalibrationManager _flowCalibrationManager;

        // Progress tracking
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        public event EventHandler<ProcessStepEventArgs> StepChanged;

        // Constants
        private const string LOG_INDENT = "  ";
        private const string LOG_DOUBLE_INDENT = "    ";

        public DispenseProcessService(
            IControllerManager controllerManager,
            ISettingsManager settingsManager,
            IResultsManager resultsManager,
            ICameraManager cameraManager,
            ILocalDataManager localDataManager,
            IFlowCalibrationManager flowCalibrationManager)
        {
            _controllerManager = controllerManager ?? throw new ArgumentNullException(nameof(controllerManager));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _resultsManager = resultsManager ?? throw new ArgumentNullException(nameof(resultsManager));
            _cameraManager = cameraManager ?? throw new ArgumentNullException(nameof(cameraManager));
            _localDataManager = localDataManager ?? throw new ArgumentNullException(nameof(localDataManager));
            _flowCalibrationManager = flowCalibrationManager ?? throw new ArgumentNullException(nameof(flowCalibrationManager));
        }

        /// <summary>
        /// Main entry point for the dispense process.
        /// Orchestrates all steps of the dispense workflow.
        /// </summary>
        public async Task<DispenseProcessResult> ExecuteFullDispenseProcessAsync(
            string motorName,
            string ringSerialNumber,
            AdvancedOptions advancedOptions)
        {
            var result = new DispenseProcessResult();

            try
            {
                // Initialize process
                _resultsManager.ClearCurrentResults();
                _resultsManager.CreateNewResults();
                string resultsPath = _resultsManager.CreateResultsFolder();

                ReportProgress(0, "Initializing");
                _resultsManager.AddToLog($"Dispense process started for motor {motorName}");

                // Get settings and motor configuration
                CellSettings settings = _settingsManager.GetAllSettings();
                CSMotor motor = GetMotorSettingsByName(settings, motorName);

                if (motor == null)
                {
                    throw new InvalidOperationException($"Motor settings not found for {motorName}");
                }

                // Store user input
                _resultsManager.currentResults.ring_sn = ringSerialNumber?.Trim() ?? string.Empty;

                // Step 1: System Health Check
                if (advancedOptions.DispenseOptions.CheckHealth)
                {
                    await ExecuteHealthCheckAsync();
                    ReportProgress(5);
                }

                // Step 2: Power and Home
                await ExecutePowerAndHomeAsync();
                ReportProgress(10);

                // Step 3: Clearance Check
                await ExecuteClearanceCheckAsync(settings);
                ReportProgress(15);

                // Step 4: Setup Dispense System (pressures, etc)
                if (advancedOptions.DispenseOptions.Dispense)
                {
                    await ExecuteDispenseSetupAsync(settings, motorName);
                    ReportProgress(20);
                }

                // Step 5: Acquire Preprocess Top Photo
                string topImagePath = string.Empty;
                if (advancedOptions.DispenseOptions.PhotoTop)
                {
                    topImagePath = await ExecuteTopPhotoAcquisitionAsync(settings);
                    ReportProgress(25);
                }

                // Step 6: Acquire Side Photo
                string sideImagePath = string.Empty;
                if (advancedOptions.DispenseOptions.PhotoSide)
                {
                    sideImagePath = await ExecuteSidePhotoAcquisitionAsync(motor);
                    ReportProgress(30);
                }

                // Step 7: Process Images with OCR
                if (advancedOptions.DispenseOptions.RunOCR)
                {
                    await ExecuteOCRProcessingAsync(resultsPath, motorName);
                    ReportProgress(40);
                }

                // Step 8: Check Magnet Polarity
                if (advancedOptions.DispenseOptions.CheckPolarity)
                {
                    await ExecutePolarityCheckAsync(settings, motor, motorName);
                    ReportProgress(50);
                }

                // Step 9: Measure Heights
                if (advancedOptions.DispenseOptions.MeasureHeights)
                {
                    await ExecuteHeightMeasurementsAsync(settings, motor);
                    ReportProgress(60);
                }

                // Step 10: Perform Dispense
                if (advancedOptions.DispenseOptions.Dispense)
                {
                    await ExecuteDispenseAsync(settings, motor, motorName);
                    ReportProgress(70);
                }

                // Step 11: Autocalibration
                if (advancedOptions.DispenseOptions.Autocalibrate)
                {
                    await ExecuteAutocalibrationAsync(settings, motorName);
                    ReportProgress(80);
                }

                // Step 12: Post-process Photo
                string topAfterImagePath = string.Empty;
                if (advancedOptions.DispenseOptions.PhotoTopAfter)
                {
                    topAfterImagePath = await ExecuteTopPhotoAcquisitionAsync(settings);
                    ReportProgress(90);
                }

                // Step 13: Move to Unload
                await ExecuteMoveToUnloadAsync(settings);
                ReportProgress(95);

                // Determine Pass/Fail
                _resultsManager.DeterminePassFail(
                    _resultsManager.currentResults,
                    settings,
                    motor,
                    out bool pass,
                    out string message);

                _resultsManager.currentResults.overall_process_result = pass;
                _resultsManager.currentResults.overall_proces_message = message;
                _resultsManager.AddToLog("Process complete");
                ReportProgress(100);

                // Save Results
                await ExecuteSaveResultsAsync(settings, resultsPath, topAfterImagePath);

                result.Success = true;
                result.Pass = pass;
                result.Message = message;
                result.ResultsPath = resultsPath;
            }
            catch (DispenseException ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                _resultsManager.AddToLog($"Process failed: {ex.Message}");

                try
                {
                    await AttemptMoveToUnloadAsync();
                }
                catch (Exception unloadEx)
                {
                    _resultsManager.AddToLog($"Failed to move to unload position: {unloadEx.Message}");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Unexpected error: {ex.Message}";
                _resultsManager.AddToLog($"Process failed: {ex.Message}");
                Debug.Print($"Dispense error: {ex}");
            }

            ReportProgress(100);
            return result;
        }

        #region Private Step Methods

        private async Task ExecuteHealthCheckAsync()
        {
            _resultsManager.AddToLog("Checking system health...");
            var healthResult = await _controllerManager.CheckSystemHealth();
            if (!healthResult.isHealthy)
            {
                _resultsManager.AddToLog("Issues found:");
                foreach (string issue in healthResult.issues)
                {
                    _resultsManager.AddToLog($"{LOG_INDENT}{issue}");
                }
                throw new DispenseException("System health check failed");
            }

            _resultsManager.AddToLog("System OK");
            await _controllerManager.SetZeroShift(3);
            await _controllerManager.WaitBothRegPressures(5);
        }

        private async Task ExecutePowerAndHomeAsync()
        {
            _resultsManager.AddToLog("Enabling power...");
            string response = await _controllerManager.EnablePower();
            if (response != "1")
            {
                throw new DispenseException("Failed to enable power");
            }
            _resultsManager.AddToLog("Power enabled");

            _resultsManager.AddToLog("Homing...");
            response = await _controllerManager.Home();
            if (response != "0")
            {
                throw new DispenseException("Failed to home");
            }
            _resultsManager.AddToLog("Homed");
        }

        private async Task ExecuteClearanceCheckAsync(CellSettings settings)
        {
            _resultsManager.AddToLog("Checking clearance on center screw...");
            float x = settings.ddm_common.clearance_check.x.Value;
            float t = settings.ddm_common.clearance_check.t.Value;
            await _controllerManager.MoveJ(x, t);

            string response = await _controllerManager.MeasureHeightSingle();
            float height = float.Parse(response.Split(" ")[1]);
            float min = settings.clearance_check_min.Value;
            float max = settings.clearance_check_max.Value;

            if (height > max || height < min)
            {
                _resultsManager.AddToLog($"Clearance check failed: measured height {height} um outside of range ({min} - {max} um)");
                throw new DispenseException("Clearance check failed");
            }

            _resultsManager.AddToLog($"Clearance check passed: {height} um within range ({min} - {max} um)");
        }

        private async Task ExecuteDispenseSetupAsync(CellSettings settings, string motorName)
        {
            _resultsManager.AddToLog($"Setting dispense system pressures for {motorName}...");

            LDMotorCalib calib = _localDataManager.GetCalibFromMotorName(motorName);
            float? pressure1 = calib.sys_1_pressure;
            float? pressure2 = calib.sys_2_pressure;

            if (pressure1 != null)
            {
                _resultsManager.AddToLog($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {pressure1.Value:F3} psi");
                await _controllerManager.SetRegPressure(1, pressure1.Value);
            }
            else
            {
                _resultsManager.AddToLog($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
            }

            if (pressure2 != null)
            {
                _resultsManager.AddToLog($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {pressure2.Value:F3} psi");
                await _controllerManager.SetRegPressure(2, pressure2.Value);
            }
            else
            {
                _resultsManager.AddToLog($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
            }

            _resultsManager.AddToLog("Waiting for pressures to settle...");
            await _controllerManager.WaitBothRegPressures(10);
            await Task.Delay(1000);
            _resultsManager.AddToLog("Pressures settled");
            _resultsManager.AddToLog("Zeroing flow sensors...");
            await _controllerManager.SetZeroShift(3);
            _resultsManager.AddToLog("Flow sensors zeroed");
            _resultsManager.AddToLog("Pressures set");
        }

        private async Task<string> ExecuteTopPhotoAcquisitionAsync(CellSettings settings)
        {
            _resultsManager.AddToLog("Acquiring preprocess top photo...");
            float x = settings.ddm_common.camera_top.x.Value;
            float t = settings.ddm_common.camera_top.t.Value;
            await _controllerManager.MoveJ(x, t);

            var camResult = await _cameraManager.AcquireAndSave(CameraManager.CellCamera.top, null);

            if (!camResult.success)
            {
                throw new DispenseException($"Top camera acquisition failed: {camResult.errorMsg}");
            }

            if (!string.IsNullOrEmpty(camResult.filePath))
            {
                _resultsManager.CopyPhotoToResultsFolder(camResult.filePath, "Top");
            }

            _resultsManager.AddToLog("Preprocess top photo acquired");
            return camResult.filePath;
        }

        private async Task<string> ExecuteSidePhotoAcquisitionAsync(CSMotor motor)
        {
            _resultsManager.AddToLog("Acquiring side photo...");
            float x = motor.camera_side.x.Value;
            float t = motor.camera_side.t.Value;
            await _controllerManager.MoveJ(x, t);

            var camResult = await _cameraManager.AcquireAndSave(CameraManager.CellCamera.side, null);

            if (!camResult.success)
            {
                throw new DispenseException($"Side camera acquisition failed: {camResult.errorMsg}");
            }

            if (!string.IsNullOrEmpty(camResult.filePath))
            {
                _resultsManager.CopyPhotoToResultsFolder(camResult.filePath, "Side");
            }

            _resultsManager.AddToLog("Side photo acquired");
            return camResult.filePath;
        }

        private async Task ExecuteOCRProcessingAsync(string resultsPath, string motorName)
        {
            _resultsManager.AddToLog("Processing images...");
            OCRData ocrData = await OCRManager.RunOCRAsync(resultsPath);

            string toolType = ocrData.GetToolType("Top_compressed.jpg");
            if (toolType == null)
            {
                _resultsManager.AddToLog("Unable to determine tool type from image");
            }
            else
            {
                if (toolType != motorName)
                {
                    _resultsManager.AddToLog($"Tool type detected from image ({toolType}) does not match expected motor type ({motorName})");
                    throw new DispenseException("Tool type mismatch");
                }

                string toolSN = ocrData.GetToolSN(motorName, "Top_compressed.jpg");
                _resultsManager.currentResults.tool_sn = toolSN;
                _resultsManager.AddToLog($"Tool SN found: {toolSN}");
            }

            string ringSN = ocrData.GetRingSN(motorName, "Side_compressed.jpg");
            if (ringSN == null)
            {
                _resultsManager.AddToLog("Unable to determine ring SN from image");
                throw new DispenseException("Ring SN not found");
            }

            _resultsManager.currentResults.ring_sn = ringSN;
            _resultsManager.AddToLog($"Ring SN detected: {ringSN}");
            _resultsManager.AddToLog("Images processed");
        }

        private async Task ExecutePolarityCheckAsync(CellSettings settings, CSMotor motor, string motorName)
        {
            _resultsManager.AddToLog("Checking magnet polarity...");
            float x = motor.hall_sensor.x.Value;
            float t = motor.hall_sensor.t.Value;
            await _controllerManager.MoveJ(x, t);

            float hallTime = settings.hall_spin_time.Value;
            float hallSpeed = settings.hall_spin_speed.Value;

            // Start MATLAB processing and spin simultaneously
            Task<DAQMatlabResults> matlabTask = DAQUtilities.CollectDataAndProcessML(motorName);

            int delay = 5000;
            if (settings.hall_spin_delay != null)
                delay = (int)settings.hall_spin_delay.Value * 1000;

            await Task.Delay(delay);
            Task spinTask = _controllerManager.SpinInPlace(hallTime, hallSpeed);

            DAQMatlabResults matlabResult = await matlabTask;

            _resultsManager.currentResults.daq_matlab_results = matlabResult;
            _resultsManager.CopyPolarityPlotToResultsFolder(matlabResult.results_directory + "plot.png", "PolarityPlot");
            _resultsManager.CopyPolarityDataToResultsFolder(matlabResult.results_directory + "PolarityResults.json", "PolarityData");

            if (matlabResult.result == 1)
            {
                _resultsManager.AddToLog("Magnet polarity OK");
            }
            else if (matlabResult.result == 0)
            {
                _resultsManager.AddToLog($"Magnet polarity failed: {matlabResult.error_code} {matlabResult.error_message}");
                throw new DispenseException("Magnet polarity check failed");
            }
            else if (matlabResult.result == -1)
            {
                _resultsManager.AddToLog($"Magnet polarity check did not complete: {matlabResult.error_code} {matlabResult.error_message}");
                throw new DispenseException("Magnet polarity check failed");
            }
            else
            {
                _resultsManager.AddToLog($"Unexpected result from magnet polarity check: {matlabResult.result}");
                throw new DispenseException("Magnet polarity check failed");
            }

            await spinTask;
        }

        private async Task ExecuteHeightMeasurementsAsync(CellSettings settings, CSMotor motor)
        {
            _resultsManager.AddToLog("Collecting ring height data...");
            float x = motor.laser_ring.x.Value;
            float t = motor.laser_ring.t.Value;
            await _controllerManager.MoveJ(x, t);

            int n = motor.laser_ring_num.Value;
            string response = await _controllerManager.MeasureHeightsContinuous(x, t, n, 10);
            _resultsManager.currentResults.ring_heights = _controllerManager.ParseHeightData(response);
            _resultsManager.AddToLog("Ring height data collected");

            _resultsManager.AddToLog("Collecting magnet/concentrator height data...");
            x = motor.laser_mag.x.Value;
            t = motor.laser_mag.t.Value;
            await _controllerManager.MoveJ(x, t);

            n = motor.laser_mag_num.Value;
            response = await _controllerManager.MeasureHeightsContinuous(x, t, n, 20);
            _resultsManager.currentResults.mag_heights = _controllerManager.ParseHeightData(response);
            _resultsManager.AddToLog("Magnet/concentrator height data collected");
            _resultsManager.AddToLog("Processing height data...");
        }

        private async Task ExecuteDispenseAsync(CellSettings settings, CSMotor motor, string motorName)
        {
            int sysID = motor.shot_settings.id_sys_num.Value;
            int sysOD = motor.shot_settings.od_sys_num.Value;

            float xID = motor.id_disp.x.Value;
            float tID = motor.id_disp.t.Value;
            float xOD = motor.od_disp.x.Value;
            float tOD = motor.od_disp.t.Value;
            float targetTimeID = motor.shot_settings.id_target_vol.Value / motor.shot_settings.id_target_flow.Value;
            float targetTimeOD = motor.shot_settings.od_target_vol.Value / motor.shot_settings.od_target_flow.Value;

            _resultsManager.AddToLog("Waiting for pressures to stabilize...");
            await _controllerManager.WaitBothRegPressures(5);
            _resultsManager.AddToLog("Pressures stabilized");

            float pressureID = float.Parse(await _controllerManager.GetRegPressureSetpoint(sysID));
            float pressureOD = float.Parse(await _controllerManager.GetRegPressureSetpoint(sysOD));

            _resultsManager.AddToLog("Dispensing cyanoacrylate...");
            string response = await _controllerManager.DispenseToRing(
                sysID, targetTimeID, xID, tID,
                sysOD, targetTimeOD, xOD, tOD);

            ResultsShotData shotData = _controllerManager.ParseDispenseResponse(response);
            shotData.motor_type = motorName;
            shotData.id_valve_num = sysID;
            shotData.od_valve_num = sysOD;
            shotData.id_pressure = pressureID;
            shotData.od_pressure = pressureOD;

            string substance_id = motor.shot_settings.id_sys_num == 1 
                ? settings.dispense_system.sys_1_contents 
                : settings.dispense_system.sys_2_contents;
            string substance_od = motor.shot_settings.od_sys_num == 1 
                ? settings.dispense_system.sys_1_contents 
                : settings.dispense_system.sys_2_contents;

            var referenceData = new ResultsReferenceData
            {
                id_substance = substance_id,
                od_substance = substance_od,
                id_target_vol = motor.shot_settings.id_target_vol,
                od_target_vol = motor.shot_settings.od_target_vol,
                id_target_flow = motor.shot_settings.id_target_flow,
                od_target_flow = motor.shot_settings.od_target_flow,
                id_calib_pressure = _localDataManager.GetPressureFromMotorName(motorName, sysID),
                od_calib_pressure = _localDataManager.GetPressureFromMotorName(motorName, sysOD)
            };

            _resultsManager.currentResults.shot_data = shotData;
            _resultsManager.currentResults.reference_data = referenceData;

            if ((bool)shotData.shot_result)
            {
                _resultsManager.AddToLog("Dispense successful");
                _resultsManager.AddToLog("Results:");
                _resultsManager.AddToLog($"{LOG_INDENT}ID:");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}Valve {motor.shot_settings.id_sys_num} ({substance_id})");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}Dispense volume: {shotData.id_vol:F3} mL ({shotData.id_vol.Value * 100 / motor.shot_settings.id_target_vol.Value:F1}% of target)");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}Dispense time: {shotData.id_time:F3} s");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}Pressure: {shotData.id_pressure:F3} psi");
                _resultsManager.AddToLog($"{LOG_INDENT}OD:");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}Valve {motor.shot_settings.od_sys_num} ({substance_od})");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}Dispense volume: {shotData.od_vol:F3} mL ({shotData.od_vol.Value * 100 / motor.shot_settings.od_target_vol.Value:F1}% of target)");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}Dispense time: {shotData.od_time:F3} s");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}Pressure: {shotData.od_pressure:F3} psi");
            }
            else
            {
                _resultsManager.AddToLog($"Dispense failed: {shotData.shot_message}");
                throw new DispenseException($"Dispense failed: {shotData.shot_message}");
            }
        }

        private async Task ExecuteAutocalibrationAsync(CellSettings settings, string motorName)
        {
            _resultsManager.AddToLog("Autocalibrating pressures...");

            if (_resultsManager.currentResults.shot_data == null)
            {
                _resultsManager.AddToLog("Autocalibration failed: no results data loaded");
                throw new DispenseException("Autocalibration failed");
            }

            bool calibSuccess;
            string calibMessage;
            float sf1, sf2;

            _flowCalibrationManager.CalculateNewScaleFactors(
                _resultsManager.currentResults.shot_data,
                _settingsManager.GetAllSettings(),
                _localDataManager.GetLocalData(),
                out calibSuccess,
                out calibMessage,
                out sf1,
                out sf2);

            if (!calibSuccess)
            {
                throw new DispenseException($"Calibration calculation failed: {calibMessage}");
            }

            _resultsManager.AddToLog("Calibration calculation succeeded.");
            _resultsManager.AddToLog("Saving new calibration to local file...");

            var resultContainer = new RunCalibResult
            {
                success = calibSuccess,
                message = calibMessage,
                time = DateTime.Now,
                motorName = motorName,
                sf1 = sf1,
                sf2 = sf2
            };

            _flowCalibrationManager.GenerateAndSaveCalibration(resultContainer);
            _resultsManager.AddToLog("Calibration saved to local file");

            _resultsManager.currentResults.reference_data.sys_1_autocal_sf = sf1;
            _resultsManager.currentResults.reference_data.sys_2_autocal_sf = sf2;

            LDMotorCalib calib = _localDataManager.GetCalibFromMotorName(motorName);
            float? pressure1 = calib.sys_1_pressure;
            float? pressure2 = calib.sys_2_pressure;

            _resultsManager.AddToLog("Autocalibration succeeded");
            if (pressure1 != null)
            {
                _resultsManager.AddToLog($"{LOG_INDENT}System 1:");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}SF: {sf1:F3}");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}New pressure: {pressure1:F3}");
            }
            if (pressure2 != null)
            {
                _resultsManager.AddToLog($"{LOG_INDENT}System 2:");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}SF: {sf2:F3}");
                _resultsManager.AddToLog($"{LOG_INDENT}{LOG_INDENT}New pressure: {pressure2:F3}");
            }

            _resultsManager.AddToLog("Adjusting dispense system pressures...");

            if (pressure1 != null)
            {
                _resultsManager.AddToLog($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {pressure1.Value:F3} psi");
                await _controllerManager.SetRegPressure(1, pressure1.Value);
            }
            else
            {
                _resultsManager.AddToLog($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
            }

            if (pressure2 != null)
            {
                _resultsManager.AddToLog($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {pressure2.Value:F3} psi");
                await _controllerManager.SetRegPressure(2, pressure2.Value);
            }
            else
            {
                _resultsManager.AddToLog($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
            }

            _resultsManager.AddToLog("System pressures adjusted");
        }

        private async Task ExecuteMoveToUnloadAsync(CellSettings settings)
        {
            _resultsManager.AddToLog("Moving to unload...");
            float x = settings.ddm_common.load.x.Value;
            float t = settings.ddm_common.load.t.Value;
            await _controllerManager.MoveJ(x, t);
        }

        private async Task AttemptMoveToUnloadAsync()
        {
            try
            {
                CellSettings settings = _settingsManager.GetAllSettings();
                if (settings != null)
                {
                    _resultsManager.AddToLog("Attempting to move to unload position...");
                    float x = settings.ddm_common.load.x.Value;
                    float t = settings.ddm_common.load.t.Value;
                    await _controllerManager.MoveJ(x, t);
                }
            }
            catch (Exception ex)
            {
                _resultsManager.AddToLog($"Failed to move to unload position: {ex.Message}");
            }
        }

        private async Task ExecuteSaveResultsAsync(CellSettings settings, string resultsPath, string topAfterImagePath)
        {
            if (!string.IsNullOrEmpty(topAfterImagePath))
            {
                _resultsManager.CopyPhotoToResultsFolder(topAfterImagePath, "TopPost");
            }

            _resultsManager.AddToLog("Saving settings to results folder");
            _settingsManager.SaveSettingsCopyToLocal(settings, resultsPath);

            _resultsManager.AddToLog("Saving all results data to results folder");
            _resultsManager.SaveDataToFile();
            _resultsManager.RenameResultsFolder(_resultsManager.currentResults.ring_sn);
        }

        #endregion

        #region Helper Methods

        private CSMotor GetMotorSettingsByName(CellSettings settings, string motorName)
        {
            return motorName switch
            {
                "ddm_57" => settings.ddm_57,
                "ddm_95" => settings.ddm_95,
                "ddm_116" => settings.ddm_116,
                "ddm_170" => settings.ddm_170,
                "ddm_170_tall" => settings.ddm_170_tall,
                _ => null
            };
        }

        private void ReportProgress(double percentage, string step = "")
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((int)percentage, step));
        }

        #endregion
    }

    /// <summary>
    /// Custom exception for dispense process failures.
    /// Makes error handling more specific to dispense operations.
    /// </summary>
    public class DispenseException : Exception
    {
        public DispenseException(string message) : base(message) { }
        public DispenseException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Result of a dispense process execution.
    /// </summary>
    public class DispenseProcessResult
    {
        public bool Success { get; set; }
        public bool Pass { get; set; }
        public string Message { get; set; }
        public string ResultsPath { get; set; }
    }

    /// <summary>
    /// Event arguments for progress reporting.
    /// </summary>
    public class ProgressChangedEventArgs : EventArgs
    {
        public int Percentage { get; set; }
        public string Step { get; set; }

        public ProgressChangedEventArgs(int percentage, string step = "")
        {
            Percentage = percentage;
            Step = step;
        }
    }

    /// <summary>
    /// Event arguments for process step changes.
    /// </summary>
    public class ProcessStepEventArgs : EventArgs
    {
        public string StepName { get; set; }
        public int StepNumber { get; set; }
    }
}