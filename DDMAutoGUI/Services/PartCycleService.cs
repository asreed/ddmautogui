using DDMAutoGUI.Utilities;
using DDMAutoGUI.Vision;
using System;
using System.Diagnostics;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Encapsulates the complex dispense process logic.
    /// Handles all steps of the dispense workflow in a clean, testable way.
    /// This service is used by the MainWindowViewModel to coordinate the dispense process.
    /// </summary>
    public class PartCycleService : IPartCycleService
    {
        private readonly IControllerService _controllerService;
        private readonly ISettingsService _settingsService;
        private readonly IResultsService _resultsService;
        private readonly ICameraService _cameraService;
        private readonly ILocalDataService _localDataService;
        private readonly IFlowCalibrationService _flowCalibrationService;
        private readonly IDispenseExecutionService _dispenseExecutionService;

        // Progress tracking
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        // Constants
        private const string LOG_INDENT = "  ";
        private const string LOG_DOUBLE_INDENT = "    ";

        public PartCycleService(
            IControllerService controllerService,
            ISettingsService settingsService,
            IResultsService resultsService,
            ICameraService cameraService,
            ILocalDataService localDataService,
            IFlowCalibrationService flowCalibrationService,
            IDispenseExecutionService dispenseExecutionService)
        {
            _controllerService = controllerService ?? throw new ArgumentNullException(nameof(controllerService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _resultsService = resultsService ?? throw new ArgumentNullException(nameof(resultsService));
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _localDataService = localDataService ?? throw new ArgumentNullException(nameof(localDataService));
            _flowCalibrationService = flowCalibrationService ?? throw new ArgumentNullException(nameof(flowCalibrationService));
            _dispenseExecutionService = dispenseExecutionService ?? throw new ArgumentNullException(nameof(dispenseExecutionService));
        }

        /// <summary>
        /// Main entry point for the part cycle process.
        /// Orchestrates all steps of the part cycle workflow.
        /// </summary>
        public async Task<PartCycleResult> ExecutePartCycleAsync(
            string motorName,
            string ringSerialNumber,
            AdvancedOptions advancedOptions)
        {
            var result = new PartCycleResult();
            string resultsPath = string.Empty;
            CellSettings settings = null;

            try
            {
                // Initialize process
                _resultsService.ClearCurrentResults();
                _resultsService.CreateNewResults();
                resultsPath = _resultsService.CreateResultsFolder();

                ReportProgress(0, "Initializing");
                _resultsService.AddToLog($"Dispense process started for motor {motorName}");

                // Get settings and motor configuration
                settings = _settingsService.GetAllSettings();
                CSMotor motor = GetMotorSettingsByName(settings, motorName);

                if (motor == null)
                {
                    throw new InvalidOperationException($"Motor settings not found for {motorName}");
                }

                // Store user input
                _resultsService.currentResults.ring_sn_detected = ringSerialNumber?.Trim() ?? string.Empty;

                // Step 1: System Health Check
                //if (advancedOptions.DispenseOptions.CheckHealth)
                //{
                    await ExecuteHealthCheckAsync();
                    ReportProgress(5, "System health checked");
                //}

                // Step 2: Power and Home
                await ExecutePowerAndHomeAsync();
                ReportProgress(10, "Powered and homed");

                // Step 3: Clearance Check
                await ExecuteClearanceCheckAsync(settings);
                ReportProgress(15, "Clearance checked");

                // Step 4: Setup Dispense System (pressures, etc)
                if (advancedOptions.PartCycleOptions.Dispense)
                {
                    await ExecuteDispenseSetupAsync(settings, motorName);
                    ReportProgress(20, "Dispense system ready");
                }

                // Step 5: Acquire Preprocess Top Photo
                string topImagePath = string.Empty;
                if (advancedOptions.PartCycleOptions.PhotoTop)
                {
                    topImagePath = await ExecuteTopPhotoAcquisitionAsync(settings, "Top");
                    ReportProgress(25, "Top photo acquired");
                }

                // Step 6: Acquire Side Photo
                string sideImagePath = string.Empty;
                if (advancedOptions.PartCycleOptions.PhotoSide)
                {
                    sideImagePath = await ExecuteSidePhotoAcquisitionAsync(motor);
                    ReportProgress(30, "Side photo acquired");
                }

                // Step 7: Process Images with OCR
                if (advancedOptions.PartCycleOptions.RunOCR)
                {
                    await ExecuteOCRProcessingAsync(resultsPath, motorName);
                    ReportProgress(40, "OCR processed");
                }

                // Step 8: Measure Heights
                if (advancedOptions.PartCycleOptions.MeasureHeights)
                {
                    await ExecuteHeightMeasurementsAsync(settings, motor);
                    ReportProgress(50, "Heights measured");
                }

                // Step 9: Check Magnet Polarity
                if (advancedOptions.PartCycleOptions.CheckPolarity)
                {
                    await ExecutePolarityCheckAsync(settings, motor, motorName);
                    ReportProgress(60, "Magnet polarity checked");
                }

                // Step 10: Perform Dispense
                if (advancedOptions.PartCycleOptions.Dispense)
                {
                    await ExecuteDispenseAsync(settings, motor, motorName);
                    ReportProgress(70, "Dispense complete");
                }

                // Step 11: Autocalibration
                if (advancedOptions.PartCycleOptions.Autocalibrate)
                {
                    await ExecuteAutocalibrationAsync(settings, motorName);
                    ReportProgress(80, "Autocalibration complete");
                }

                // Step 12: Post-process Photo
                string topAfterImagePath = string.Empty;
                if (advancedOptions.PartCycleOptions.PhotoTopAfter)
                {
                    topAfterImagePath = await ExecuteTopPhotoAcquisitionAsync(settings, "TopPost");
                    ReportProgress(90, "Post-process photo acquired");
                }

                // Step 13: Move to Unload
                await ExecuteMoveToUnloadAsync(settings);
                ReportProgress(95, "Moving to unload");

                // Determine Pass/Fail
                _resultsService.DeterminePassFail(
                    _resultsService.currentResults,
                    settings,
                    motor,
                    out bool pass,
                    out string message);

                _resultsService.currentResults.overall_part_cycle_result = pass;
                _resultsService.currentResults.overall_part_cycle_message = message;
                _resultsService.AddToLog("Process complete");
                ReportProgress(100, "Finishing up");

                result.Success = true;
                result.Pass = pass;
                result.Message = message;
                result.ResultsPath = resultsPath;
            }
            catch (PartCycleException ex)
            {

                result.Success = false;
                result.Message = ex.Message;
                _resultsService.AddToLog($"Process failed: {ex.Message}");

                try
                {
                    await AttemptMoveToUnloadAsync();
                }
                catch (Exception unloadEx)
                {
                    _resultsService.AddToLog($"Failed to move to unload position: {unloadEx.Message}");
                }



            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Unexpected error: {ex.Message}";
                _resultsService.AddToLog($"Process failed: {ex.Message}");
                Debug.Print($"Dispense error: {ex}");
            }


            // Save Results
            await ExecuteSaveResultsAsync(settings, resultsPath);
            ReportProgress(100, "Process complete");
            return result;
        }

        #region Private Step Methods

        private async Task ExecuteHealthCheckAsync()
        {
            _resultsService.AddToLog("Checking system health...");
            var healthResult = await _controllerService.CheckSystemHealth();
            if (!healthResult.isHealthy)
            {
                _resultsService.AddToLog("Issues found:");
                foreach (string issue in healthResult.issues)
                {
                    _resultsService.AddToLog($"{LOG_INDENT}{issue}");
                }
                throw new PartCycleException("System health check failed");
            }

            await _controllerService.ActuateHallUp();
            await _controllerService.SetZeroShift(3);
            await _controllerService.WaitBothRegPressures(5);
            _resultsService.AddToLog("System OK");
        }

        private async Task ExecutePowerAndHomeAsync()
            => await _dispenseExecutionService.EnablePowerAndHomeAsync(_resultsService.AddToLog);

        private async Task ExecuteClearanceCheckAsync(CellSettings settings)
        {
            _resultsService.AddToLog("Checking clearance on center screw...");
            float x = settings.ddm_common.clearance_check.x.Value;
            float t = settings.ddm_common.clearance_check.t.Value;
            await _controllerService.MoveJ(x, t);

            string response = await _controllerService.MeasureHeightSingle();
            float height = float.Parse(response.Split(" ")[1]);
            float min = settings.clearance_check_min.Value;
            float max = settings.clearance_check_max.Value;

            if (height > max || height < min)
            {
                _resultsService.AddToLog($"Clearance check failed: measured height {height} um outside of range ({min} - {max} um)");
                throw new PartCycleException("Clearance check failed");
            }

            _resultsService.AddToLog($"Clearance check passed: {height} um within range ({min} - {max} um)");
        }

        private async Task ExecuteDispenseSetupAsync(CellSettings settings, string motorName)
        {
            _resultsService.AddToLog($"Setting dispense system pressures for {motorName}...");
            LDMotorCalib calib = _localDataService.GetCalibFromMotorName(motorName);
            await _dispenseExecutionService.SetupPressuresAsync(settings, calib, _resultsService.AddToLog);
        }

        private async Task<string> ExecuteTopPhotoAcquisitionAsync(CellSettings settings, string resultFileName)
        {
            _resultsService.AddToLog("Acquiring top photo...");
            float x = settings.ddm_common.camera_top.x.Value;
            float t = settings.ddm_common.camera_top.t.Value;
            await _controllerService.MoveJ(x, t);

            var camResult = await _cameraService.AcquireAndSave(CameraService.CellCamera.top, null);

            if (!camResult.success)
            {
                throw new PartCycleException($"Top camera acquisition failed: {camResult.errorMsg}");
            }

            if (!string.IsNullOrEmpty(camResult.filePath))
            {
                _resultsService.CopyPhotoToResultsFolder(camResult.filePath, resultFileName);
            }

            _resultsService.AddToLog("Top photo acquired");
            return camResult.filePath;
        }

        private async Task<string> ExecuteSidePhotoAcquisitionAsync(CSMotor motor)
        {
            _resultsService.AddToLog("Acquiring side photo...");
            float x = motor.camera_side.x.Value;
            float t = motor.camera_side.t.Value;
            await _controllerService.MoveJ(x, t);

            var camResult = await _cameraService.AcquireAndSave(CameraService.CellCamera.side, null);

            if (!camResult.success)
            {
                throw new PartCycleException($"Side camera acquisition failed: {camResult.errorMsg}");
            }

            if (!string.IsNullOrEmpty(camResult.filePath))
            {
                _resultsService.CopyPhotoToResultsFolder(camResult.filePath, "Side");
            }

            _resultsService.AddToLog("Side photo acquired");
            return camResult.filePath;
        }

        private async Task ExecuteOCRProcessingAsync(string resultsPath, string motorName)
        {
            _resultsService.AddToLog("Processing images...");
            OCRData? ocrData = await OCRProcessor.RunOCRAsync(resultsPath);
            
            if (ocrData == null)
            {
                _resultsService.AddToLog("OCR processing failed");
                throw new PartCycleException("OCR processing failed");
            }

            _resultsService.currentResults.ocr_data = ocrData;
            string toolType = ocrData.GetToolType("Top_compressed.jpg");
            if (toolType == null)
            {
                _resultsService.AddToLog("Unable to determine tool type from image");
            }
            else
            {
                if (toolType != motorName)
                {
                    _resultsService.AddToLog($"Tool type detected from image ({toolType}) does not match expected motor type ({motorName})");
                    throw new PartCycleException("Tool type mismatch");
                }

                string toolSN = ocrData.GetToolSN(motorName, "Top_compressed.jpg");
                _resultsService.currentResults.tool_sn_detected = toolSN;
                _resultsService.AddToLog($"Tool SN found: {toolSN}");
            }

            string ringSN = ocrData.GetRingSN(motorName, "Side_compressed.jpg");
            if (ringSN == null)
            {
                _resultsService.AddToLog("Unable to determine ring SN from image");
                throw new PartCycleException("Ring SN not found");
            }

            _resultsService.currentResults.ring_sn_detected = ringSN;
            _resultsService.AddToLog($"Ring SN detected: {ringSN}");
            _resultsService.AddToLog("Images processed");
        }

        private async Task ExecutePolarityCheckAsync(CellSettings settings, CSMotor motor, string motorName)
        {
            _resultsService.AddToLog("Checking magnet polarity...");
            await _controllerService.ActuateHallUp();

            float x = motor.hall_sensor.x.Value;
            float t = motor.hall_sensor.t.Value;
            await _controllerService.MoveJ(x, t);

            float hallTime = settings.hall_spin_time.Value;
            float hallSpeed = settings.hall_spin_speed.Value;

            // Start MATLAB processing and spin simultaneously
            Task<DAQMatlabResults> matlabTask = DAQUtilities.CollectDataAndProcessML(motorName);

            // Push hall effect sensor down near magnets
            await _controllerService.ActuateHallDown();

            int delay = 5000;
            if (settings.hall_spin_delay != null)
                delay = (int)settings.hall_spin_delay.Value * 1000;

            await Task.Delay(delay);
            Task spinTask = _controllerService.SpinInPlace(hallTime, hallSpeed);

            DAQMatlabResults matlabResult = await matlabTask;
            _resultsService.currentResults.daq_matlab_results = matlabResult;
            _resultsService.currentResults.version_info.polarity_version = matlabResult.version;

            // Note 7/14/2026: Save data into main process results to reduce number of files. Plot generated by external report script.
            //_resultsService.CopyPolarityPlotToResultsFolder(matlabResult.results_directory + "plot.png", "PolarityPlot");
            //_resultsService.CopyPolarityDataToResultsFolder(matlabResult.results_directory + "PolarityResults.json", "PolarityData");

            // Raise the hall effect sensor back up
            await _controllerService.ActuateHallUp();

            if (matlabResult.result == 1)
            {
                _resultsService.AddToLog("Magnet polarity OK");
            }
            else if (matlabResult.result == 0)
            {
                _resultsService.AddToLog($"Magnet polarity failed: {matlabResult.error_code} {matlabResult.error_message}");
                throw new PartCycleException("Magnet polarity check failed");
            }
            else if (matlabResult.result == -1)
            {
                _resultsService.AddToLog($"Magnet polarity check did not complete: {matlabResult.error_code} {matlabResult.error_message}");
                throw new PartCycleException("Magnet polarity check failed");
            }
            else
            {
                _resultsService.AddToLog($"Unexpected result from magnet polarity check: {matlabResult.result}");
                throw new PartCycleException("Magnet polarity check failed");
            }

            await spinTask;

        }

        private async Task ExecuteHeightMeasurementsAsync(CellSettings settings, CSMotor motor)
        {
            _resultsService.AddToLog("Collecting ring height data...");
            float x = motor.laser_ring.x.Value;
            float t = motor.laser_ring.t.Value;
            await _controllerService.MoveJ(x, t);

            int n = motor.laser_ring_num.Value;
            string response = await _controllerService.MeasureHeightsContinuous(x, t, n, 10);
            List<ResultsHeightMeasurement> ring_heights = _controllerService.ParseHeightData(response);
            _resultsService.AddToLog("Ring height data collected");

            _resultsService.AddToLog("Collecting magnet/concentrator height data...");
            x = motor.laser_mag.x.Value;
            t = motor.laser_mag.t.Value;
            await _controllerService.MoveJ(x, t);

            n = motor.laser_mag_num.Value;
            response = await _controllerService.MeasureHeightsContinuous(x, t, n, 20);
            List<ResultsHeightMeasurement> mag_heights = _controllerService.ParseHeightData(response);
            _resultsService.AddToLog("Magnet/concentrator height data collected");
            _resultsService.AddToLog("Processing height data...");

            HeightVerificationResult heightResult = HeightVerification.VerifyHeightData(
                ring_heights,
                mag_heights,
                settings);

            _resultsService.currentResults.height_verification_result = heightResult;
            if (heightResult.passed)
            {
                _resultsService.AddToLog($"Height verification passed: {heightResult.message}");
            }
            else
            {
                _resultsService.AddToLog($"Height verification failed: {heightResult.message}");
                throw new PartCycleException($"Height verification failed");
            }
        }

        private async Task ExecuteDispenseAsync(CellSettings settings, CSMotor motor, string motorName)
        {
            int sysID = motor.shot_settings.id_sys_num.Value;
            int sysOD = motor.shot_settings.od_sys_num.Value;

            string substance_id = motor.shot_settings.id_sys_num == 1
                ? settings.dispense_system.sys_1_contents
                : settings.dispense_system.sys_2_contents;
            string substance_od = motor.shot_settings.od_sys_num == 1
                ? settings.dispense_system.sys_1_contents
                : settings.dispense_system.sys_2_contents;

            ResultsShotData shotData = await _dispenseExecutionService.DispenseToRingAsync(settings, motor, motorName, _resultsService.AddToLog);

            // production-only concern: build reference data + persist to currentResults
            var referenceData = new ResultsReferenceData
            {
                id_substance = substance_id,
                od_substance = substance_od,
                id_target_vol = motor.shot_settings.id_target_vol,
                od_target_vol = motor.shot_settings.od_target_vol,
                id_target_flow = motor.shot_settings.id_target_flow,
                od_target_flow = motor.shot_settings.od_target_flow,
                id_calib_pressure = _localDataService.GetPressureFromMotorName(motorName, sysID),
                od_calib_pressure = _localDataService.GetPressureFromMotorName(motorName, sysOD)
            };

            _resultsService.currentResults.shot_data = shotData;
            _resultsService.currentResults.reference_data = referenceData;

            _resultsService.AddToLog("Results:");
            _resultsService.AddToLog($"{LOG_INDENT}ID:");
            _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}Valve {motor.shot_settings.id_sys_num} ({substance_id})");
            _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}Dispense volume: {shotData.id_vol:F3} mL ({shotData.id_vol.Value * 100 / motor.shot_settings.id_target_vol.Value:F1}% of target)");
            _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}Dispense time: {shotData.id_time:F3} s");
            _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}Pressure: {shotData.id_pressure:F3} psi");
            _resultsService.AddToLog($"{LOG_INDENT}OD:");
            _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}Valve {motor.shot_settings.od_sys_num} ({substance_od})");
            _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}Dispense volume: {shotData.od_vol:F3} mL ({shotData.od_vol.Value * 100 / motor.shot_settings.od_target_vol.Value:F1}% of target)");
            _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}Dispense time: {shotData.od_time:F3} s");
            _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}Pressure: {shotData.od_pressure:F3} psi");
        }

        private async Task ExecuteAutocalibrationAsync(CellSettings settings, string motorName)
        {
            _resultsService.AddToLog("Autocalibrating pressures...");

            if (_resultsService.currentResults.shot_data == null)
            {
                _resultsService.AddToLog("Autocalibration failed: no results data loaded");
                throw new PartCycleException("Autocalibration failed");
            }

            bool calibSuccess;
            string calibMessage;
            float sf1, sf2;

            _flowCalibrationService.CalculateNewScaleFactors(
                _resultsService.currentResults.shot_data,
                _settingsService.GetAllSettings(),
                _localDataService.GetLocalData(),
                out calibSuccess,
                out calibMessage,
                out sf1,
                out sf2);

            if (!calibSuccess)
            {
                throw new PartCycleException($"Calibration calculation failed: {calibMessage}");
            }

            _resultsService.AddToLog("Calibration calculation succeeded.");
            _resultsService.AddToLog("Saving new calibration to local file...");

            var resultContainer = new RunCalibResult
            {
                success = calibSuccess,
                message = calibMessage,
                time = DateTime.Now,
                motorName = motorName,
                sf1 = sf1,
                sf2 = sf2
            };

            _flowCalibrationService.GenerateAndSaveCalibration(resultContainer);
            _resultsService.AddToLog("Calibration saved to local file");

            _resultsService.currentResults.reference_data.sys_1_autocal_sf = sf1;
            _resultsService.currentResults.reference_data.sys_2_autocal_sf = sf2;

            LDMotorCalib calib = _localDataService.GetCalibFromMotorName(motorName);
            float? pressure1 = calib.sys_1_pressure;
            float? pressure2 = calib.sys_2_pressure;

            _resultsService.AddToLog("Autocalibration succeeded");
            if (pressure1 != null)
            {
                _resultsService.AddToLog($"{LOG_INDENT}System 1:");
                _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}SF: {sf1:F3}");
                _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}New pressure: {pressure1:F3}");
            }
            if (pressure2 != null)
            {
                _resultsService.AddToLog($"{LOG_INDENT}System 2:");
                _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}SF: {sf2:F3}");
                _resultsService.AddToLog($"{LOG_INDENT}{LOG_INDENT}New pressure: {pressure2:F3}");
            }

            _resultsService.AddToLog("Adjusting dispense system pressures...");

            if (pressure1 != null)
            {
                _resultsService.AddToLog($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {pressure1.Value:F3} psi");
                await _controllerService.SetRegPressure(1, pressure1.Value);
            }
            else
            {
                _resultsService.AddToLog($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
            }

            if (pressure2 != null)
            {
                _resultsService.AddToLog($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {pressure2.Value:F3} psi");
                await _controllerService.SetRegPressure(2, pressure2.Value);
            }
            else
            {
                _resultsService.AddToLog($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
            }

            _resultsService.AddToLog("System pressures adjusted");
        }

        private async Task ExecuteMoveToUnloadAsync(CellSettings settings)
        {
            _resultsService.AddToLog("Moving to unload...");
            float x = settings.ddm_common.load.x.Value;
            float t = settings.ddm_common.load.t.Value;
            await _controllerService.MoveJ(x, t);
        }

        private async Task AttemptMoveToUnloadAsync()
        {
            try
            {
                CellSettings settings = _settingsService.GetAllSettings();
                if (settings != null)
                {
                    _resultsService.AddToLog("Attempting to move to unload position...");
                    await _controllerService.ActuateHallUp();
                    float x = settings.ddm_common.load.x.Value;
                    float t = settings.ddm_common.load.t.Value;
                    await _controllerService.MoveJ(x, t);
                }
            }
            catch (Exception ex)
            {
                _resultsService.AddToLog($"Failed to move to unload position: {ex.Message}");
            }
        }

        private async Task ExecuteSaveResultsAsync(CellSettings settings, string resultsPath)
        {

            _resultsService.AddToLog("Saving settings to results folder");
            _settingsService.SaveSettingsCopyToLocal(settings, resultsPath);

            _resultsService.AddToLog("Saving all results data to results folder");
            _resultsService.SaveDataToFile();
            _resultsService.RenameResultsFolder(_resultsService.currentResults.ring_sn_detected);
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
    public class PartCycleException : Exception
    {
        public PartCycleException(string message) : base(message) { }
        public PartCycleException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Result of a dispense process execution.
    /// </summary>
    public class PartCycleResult
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
}
