using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DDMAutoGUI.Services
{
    public class RunCalibResult
    {
        public bool success { get; set; }
        public string message { get; set; }
        public DateTime time { get; set; }
        public string motorName { get; set; }
        public float sf1 { get; set; }
        public float sf2 { get; set; }
    }

    public class FlowCalibrationManager : IFlowCalibrationManager
    {
        private readonly IApplicationConfiguration _applicationConfiguration;
        private readonly ISettingsManager _settingsManager;
        private readonly IControllerManager _controllerManager;
        private readonly ILocalDataManager _localDataManager;
        private readonly IDispenseExecutionService _dispenseExecution;

        private static readonly Action<string> DebugLog = s => Debug.Print(s);

        public FlowCalibrationManager(
            IApplicationConfiguration applicationConfiguration,
            ISettingsManager settingsManager,
            IControllerManager controllerManager,
            ILocalDataManager localDataManager,
            IDispenseExecutionService dispenseExecution)
        {
            _applicationConfiguration = applicationConfiguration;
            _settingsManager = settingsManager;
            _controllerManager = controllerManager;
            _localDataManager = localDataManager;
            _dispenseExecution = dispenseExecution;
        }

        public async Task<RunCalibResult> RunDispenseForManualCalibration(
            CellSettings settings, LocalData localData, string motorName)
        {
            var result = new RunCalibResult { success = false, message = "" };
            if (_applicationConfiguration.IsSimulationMode) {
                await Task.Delay(1000); // simulate time delay
                result.success = true;
                result.time = DateTime.Now;
                result.motorName = motorName;
                result.sf1 = 0.99f;
                result.sf2 = 1.02f;
                result.message = "Success (!) SIMULATED (!)";
                return result;
            }

            try
            {
                CSMotor motorSettings = _settingsManager.GetMotorSettingsFromName(motorName);

                await _dispenseExecution.EnablePowerAndHomeAsync(DebugLog);

                await _controllerManager.MoveJ(settings.ddm_common.load.x.Value, settings.ddm_common.load.t.Value);

                LDMotorCalib oldCalib = _localDataManager.GetCalibFromMotorName(localData, motorName);
                await _dispenseExecution.SetupPressuresAsync(settings, oldCalib, DebugLog);

                ResultsShotData shotData = await _dispenseExecution.DispenseToRingAsync(settings, motorSettings, motorName, DebugLog);

                await _controllerManager.MoveJ(settings.ddm_common.load.x.Value, settings.ddm_common.load.t.Value);

                // calibration-only policy
                CalculateNewScaleFactors(shotData, settings, localData, out bool ok, out string msg, out float sf1, out float sf2);
                if (!ok) throw new Exception("Calibration failed: Calculation unsuccessful");

                result.time = DateTime.Now;
                result.motorName = motorName;
                result.sf1 = sf1;
                result.sf2 = sf2;
                result.message = "Success";
                result.success = true;
            }
            catch (Exception ex)
            {
                result.success = false;
                result.message = $"Error during calibration routine: {ex.Message}";
            }
            return result;
        }

        public async void SetPressuresFromCalibration(
            CellSettings settings,
            LocalData localData,
            string motorName)
        {
            // Set pressures based on current calib
            string response = string.Empty;
            LDMotorCalib calib = _localDataManager.GetCalibFromMotorName(localData, motorName);
            float? oldPressure1 = calib.sys_1_pressure;
            float? oldPressure2 = calib.sys_2_pressure;
            if (oldPressure1 != null)
            {
                Debug.Print($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {oldPressure1.Value:F3} psi");
                response = await _controllerManager.SetRegPressure(1, oldPressure1.Value);
            }
            else
            {
                Debug.Print($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
            }
            if (oldPressure2 != null)
            {
                Debug.Print($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {oldPressure2.Value:F3} psi");
                response = await _controllerManager.SetRegPressure(2, oldPressure2.Value);
            }
            else
            {
                Debug.Print($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
            }
            Debug.Print("Waiting for pressures to settle...");
            response = await _controllerManager.WaitBothRegPressures(10);
            await Task.Delay(1000);
            Debug.Print("Pressures settled");
            Debug.Print("Pressures set");
        }

        public void CalculateNewScaleFactors(
            ResultsShotData prevShotData,
            CellSettings cellSettings,
            LocalData localData,
            out bool success,
            out string message,
            out float sf1,
            out float sf2)
        {
            /// <summary>
            /// Takes cell settings data, compares to the latest shot data, and
            /// estimates pressure adjustments required to improve shot volume accuracy for the next
            /// run. Neither saves nor validates calibration.
            ///
            /// DOES NOT RUN THE ROBOT AROUND.
            ///
            /// DOES NOT SAVE TO FILE.
            ///
            /// </summary>

            success = false;
            message = string.Empty;
            sf1 = 1.00f;
            sf2 = 1.00f;
            CSShot targetShotData = null;
            LDMotorCalib calibOriginal = null;
            string tb = "  ";

            switch (prevShotData.motor_type)
            {
                case "ddm_57":
                    targetShotData = cellSettings.ddm_57.shot_settings;
                    calibOriginal = localData.calib_data.ddm_57;
                    break;
                case "ddm_95":
                    targetShotData = cellSettings.ddm_95.shot_settings;
                    calibOriginal = localData.calib_data.ddm_95;
                    break;
                case "ddm_116":
                    targetShotData = cellSettings.ddm_116.shot_settings;
                    calibOriginal = localData.calib_data.ddm_116;
                    break;
                case "ddm_170":
                    targetShotData = cellSettings.ddm_170.shot_settings;
                    calibOriginal = localData.calib_data.ddm_170;
                    break;
                case "ddm_170_tall":
                    targetShotData = cellSettings.ddm_170_tall.shot_settings;
                    calibOriginal = localData.calib_data.ddm_170_tall;
                    break;
            }

            LDMotorCalib calibNew = calibOriginal.Clone();

            Debug.Print($"Calculating scale factors based on motor size {prevShotData.motor_type}");

            // calculate real flow rate (from given shot data)
            // get target flow rate (from cell settings)
            // get calib data (from local data)
            // apply scale factor to flow rate lookup
            // verify new pressures are OK
            // copy new calib to local data and save to file

            float lastFlowID = prevShotData.id_vol.Value / prevShotData.id_time.Value;
            float targetFlowID = targetShotData.id_target_flow.Value;
            float sfID = targetFlowID / lastFlowID;
            float sysID = targetShotData.id_sys_num.Value;

            float lastFlowOD = prevShotData.od_vol.Value / prevShotData.od_time.Value;
            float targetFlowOD = targetShotData.od_target_flow.Value;
            float sfOD = targetFlowOD / lastFlowOD;
            float sysOD = targetShotData.od_sys_num.Value;

            if (sysID == sysOD)
            {
                switch (sysID)
                {
                    case 1: sf1 = (sfID + sfOD) / 2; break;
                    case 2: sf2 = (sfID + sfOD) / 2; break;
                }
            }
            else
            {
                sf1 = sysID == 1 ? sfID : sfOD;
                sf2 = sysID == 2 ? sfID : sfOD;
            }

            Debug.Print($"{tb}Individual scale factors calculated:");
            Debug.Print($"{tb}{tb}ID: {sfID:F3}");
            Debug.Print($"{tb}{tb}OD: {sfOD:F3}");
            Debug.Print($"{tb}Applying scale factors:");
            Debug.Print($"{tb}{tb}Sys 1: {sf1:F3}");
            Debug.Print($"{tb}{tb}Sys 2: {sf2:F3}");

            Debug.Print($"{tb}Updating calibration values:");
            calibNew.sys_1_pressure *= sf1;
            calibNew.sys_2_pressure *= sf2;
            Debug.Print($"{tb}{tb}Sys 1: ({calibNew.sys_1_pressure,5:0.000})");
            Debug.Print($"{tb}{tb}Sys 2: ({calibNew.sys_2_pressure,5:0.000})");
            //Debug.Print($"  Sys 1: ({calibNew.sys_1_flow:0.00}, {calibNew.sys_1_pressure,5:0.000})");
            //Debug.Print($"  Sys 2: ({calibNew.sys_2_flow:0.00}, {calibNew.sys_2_pressure,5:0.000})");

            // Basic validation

            // Check pressures against absolute limits

            bool validated = false;
            float sys1MaxPressure = cellSettings.dispense_system.sys_1_max_pressure.Value;
            float sys2MaxPressure = cellSettings.dispense_system.sys_2_max_pressure.Value;

            if (calibNew.sys_1_pressure > sys1MaxPressure || calibNew.sys_1_pressure < 0)
            {
                message = $"Calibration failed: System 1 pressure out of range ({calibNew.sys_1_pressure})";
                return;
            }
            if (calibNew.sys_2_pressure > sys2MaxPressure || calibNew.sys_2_pressure < 0)
            {
                message = $"Calibration failed: System 2 pressure out of range ({calibNew.sys_2_pressure})";
                return;
            }

            // Check pressures against relative limits

            float newPressure;
            float originalPressure;
            float diff;

            newPressure = calibNew.sys_1_pressure.Value;
            originalPressure = calibOriginal.sys_1_pressure.Value;
            diff = Math.Abs((newPressure - originalPressure) / originalPressure);

            if (diff > cellSettings.dispense_system.sys_1_max_pressure_dev_percent)
            {
                message = $"Validation failed: System 1 pressure deviated too far from calib ({diff})";
                return;
            }

            newPressure = calibNew.sys_2_pressure.Value;
            originalPressure = calibOriginal.sys_2_pressure.Value;
            diff = Math.Abs((newPressure - originalPressure) / originalPressure);

            if (diff > cellSettings.dispense_system.sys_2_max_pressure_dev_percent)
            {
                message = $"Validation failed: System 2 pressure deviated too far from calib ({diff})";
                return;
            }

            Debug.Print($"{tb}Calibration calculation successful");
            message = "Calibration calculation successful";
            success = true;
        }

        public void GenerateAndSaveCalibration(RunCalibResult result)
        {
            LocalData newLocalData = _localDataManager.GetLocalData();

            newLocalData.calib_data.last_size = result.motorName;
            newLocalData.calib_data.last_calib = result.time;

            switch (result.motorName)
            {
                case "ddm_57":
                    newLocalData.calib_data.ddm_57.sys_1_pressure *= result.sf1;
                    newLocalData.calib_data.ddm_57.sys_2_pressure *= result.sf2;
                    break;
                case "ddm_95":
                    newLocalData.calib_data.ddm_95.sys_1_pressure *= result.sf1;
                    newLocalData.calib_data.ddm_95.sys_2_pressure *= result.sf2;
                    break;
                case "ddm_116":
                    newLocalData.calib_data.ddm_116.sys_1_pressure *= result.sf1;
                    newLocalData.calib_data.ddm_116.sys_2_pressure *= result.sf2;
                    break;
                case "ddm_170":
                    newLocalData.calib_data.ddm_170.sys_1_pressure *= result.sf1;
                    newLocalData.calib_data.ddm_170.sys_2_pressure *= result.sf2;
                    break;
                case "ddm_170_tall":
                    newLocalData.calib_data.ddm_170_tall.sys_1_pressure *= result.sf1;
                    newLocalData.calib_data.ddm_170_tall.sys_2_pressure *= result.sf2;
                    break;
            }

            _localDataManager.SetLocalData(newLocalData);
            _localDataManager.SaveLocalDataToFile();
        }
    }
}
