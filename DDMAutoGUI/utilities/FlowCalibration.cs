using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DDMAutoGUI.utilities
{
    public class FlowCalibration
    {

        public FlowCalibration() { }

        public static void CalibratePressures(
            ResultsShotData prevShotData,
            CellSettings cellSettings,
            LocalData localData,
            int calibIdx,

            out bool success,
            out float sf1,
            out float sf2)
        {


            /// <summary>
            /// Takes cell settings data, compares to the latest shot data, and
            /// estimates pressure adjustments required to improve shot volume accuracy for the next
            /// run. If new calib looks OK, new calib is copied into local data and saved to file.
            /// </summary>

            success = false;
            CSShot targetShotData = null;
            LDCalib calibOriginal = localData.calib_data[calibIdx];
            LDCalib calibNew = calibOriginal.Clone();


            switch (prevShotData.motor_type)
            {
                case "ddm_57":
                    targetShotData = cellSettings.ddm_57.shot_settings;
                    break;
                case "ddm_95":
                    targetShotData = cellSettings.ddm_95.shot_settings;
                    break;
                case "ddm_116":
                    targetShotData = cellSettings.ddm_116.shot_settings;
                    break;
                case "ddm_170":
                    targetShotData = cellSettings.ddm_170.shot_settings;
                    break;
                case "ddm_170_tall":
                    targetShotData = cellSettings.ddm_170_tall.shot_settings;
                    break;
            }

            Debug.Print($"Calculating scale factors based on motor size {prevShotData.motor_type}");



            // calculate real flow rate (from given shot data)
            // get target flow rate (from cell settings)
            // get calib data (from local data)
            // apply scale factor to flow rate lookup
            // return new calib data

            float lastFlowID = prevShotData.vol_id.Value / prevShotData.time_id.Value;
            float targetFlowID = targetShotData.target_flow_id.Value;
            float sfID = targetFlowID / lastFlowID;
            float sysID = targetShotData.sys_num_id.Value;

            float lastFlowOD = prevShotData.vol_od.Value / prevShotData.time_od.Value;
            float targetFlowOD = targetShotData.target_flow_od.Value;
            float sfOD = targetFlowOD / lastFlowOD;
            float sysOD = targetShotData.sys_num_od.Value;



            // default to 1.0 (no scaling)
            sf1 = 1f;
            sf2 = 1f;

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

            Debug.Print($"Individual scale factors calculated:\n  ID: {sfID:F3}\n  OD: {sfOD:F3}");
            Debug.Print($"Applying scale factors:\n  Sys 1: {sf1:F3}\n  Sys 2: {sf2:F3}");

            Debug.Print("Updating calibration values:");
            calibNew.sys_1_pressure *= sf1;
            calibNew.sys_2_pressure *= sf2;
            Debug.Print($"  Sys 1: ({calibNew.sys_1_pressure,5:0.000})");
            Debug.Print($"  Sys 1: ({calibNew.sys_2_pressure,5:0.000})");
            //Debug.Print($"  Sys 1: ({calibNew.sys_1_flow:0.00}, {calibNew.sys_1_pressure,5:0.000})");
            //Debug.Print($"  Sys 1: ({calibNew.sys_2_flow:0.00}, {calibNew.sys_2_pressure,5:0.000})");







            // CHECK PRESSURES AGAINST ABSOLUTE LIMITS

            float sys1MaxPressure = cellSettings.dispense_system.sys_1_max_pressure.Value;
            float sys2MaxPressure = cellSettings.dispense_system.sys_2_max_pressure.Value;

            if (calibNew.sys_1_pressure > sys1MaxPressure || calibNew.sys_1_pressure < 0)
            {
                Debug.Print($"Calibration failed: System 1 pressure out of range ({calibNew.sys_1_pressure})");
                success = false;
                return;
            }
            if (calibNew.sys_2_pressure > sys2MaxPressure || calibNew.sys_2_pressure < 0)
            {
                Debug.Print($"Calibration failed: System 2 pressure out of range ({calibNew.sys_2_pressure})");
                success = false;
                return;
            }





            // CHECK PRESSURES AGAINST RELATIVE LIMITS (DRIFTING TOO FAR FROM ORIGINAL CALIBRATION)

            float newPressure;
            float originalPressure;
            float diff;

            newPressure = calibNew.sys_1_pressure.Value;
            originalPressure = calibOriginal.sys_1_pressure.Value;
            diff = Math.Abs((newPressure - originalPressure) / originalPressure);

            if (diff > cellSettings.dispense_system.sys_1_max_pressure_dev_percent)
            {
                Debug.Print($"Calibration failed: System 1 pressure deviated too far from calib ({diff})");
                success = false;
                return;
            }

            newPressure = calibNew.sys_1_pressure.Value;
            originalPressure = calibOriginal.sys_1_pressure.Value;
            diff = Math.Abs((newPressure - originalPressure) / originalPressure);

            if (diff > cellSettings.dispense_system.sys_2_max_pressure_dev_percent)
            {
                Debug.Print($"Calibration failed: System 2 pressure deviated too far from calib ({diff})");
                success = false;
                return;
            }



            // If checks pass, copy new calib data to local data

            localData.calib_data[calibIdx] = calibNew.Clone();
            App.LocalDataManager.SaveLocalDataToFile();
            success = true;
        }

    }
}
