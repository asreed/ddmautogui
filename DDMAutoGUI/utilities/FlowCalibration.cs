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
            out bool success,
            out CSDispenseCalib[] newSys1Calib,
            out CSDispenseCalib[] newSys2Calib,
            out float sf1,
            out float sf2)
        {


            /// <summary>
            /// Takes cell settings data, compares to the latest entry in the results history, and
            /// estimates pressure adjustments required to improve shot volume accuracy for the next
            /// run.
            /// </summary>

            success = false;
            CSShot targetShotData = null;

            CSDispenseCalib[] sys1CalibOriginal = localData.current_sys_1_flow_calib;
            CSDispenseCalib[] sys2CalibOriginal = localData.current_sys_2_flow_calib;


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
            // get target flow rate (from settings (cell settings))
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

            Debug.Print($"Individual scale factors calculated: ID: {sfID:F3}, OD: {sfOD:F3}");
            Debug.Print($"Applying scale factors: system 1: {sf1:F3}, system 2: {sf2:F3}");

            CSDispenseCalib[] sys1Calib = localData.current_sys_1_flow_calib;
            CSDispenseCalib[] sys2Calib = localData.current_sys_2_flow_calib;

            Debug.Print("Updating calibration values:");
            for (int i = 0; i < sys1Calib.Length; i++)
            {
                sys1Calib[i].pressure *= sf1;
                Debug.Print($"  Sys 1: ({sys1Calib[i].flow:0.00}, {sys1Calib[i].pressure,5:0.000})");
            }
            for (int i = 0; i < sys2Calib.Length; i++)
            {
                sys2Calib[i].pressure *= sf2;
                Debug.Print($"  Sys 2: ({sys2Calib[i].flow:0.00}, {sys2Calib[i].pressure,5:0.000})");
            }






            // CHECK PRESSURES AGAINST ABSOLUTE LIMITS

            float sys1MaxPressure = cellSettings.dispense_system.sys_1_max_pressure.Value;
            float sys2MaxPressure = cellSettings.dispense_system.sys_2_max_pressure.Value;

            for (int i = 0; i < sys1Calib.Length; i++)
            {
                if (sys1Calib[i].pressure > sys1MaxPressure || sys1Calib[i].pressure < 0)
                {
                    Debug.Print($"Calibration failed: System 1 pressure {i} out of range ({sys1Calib[i].pressure})");
                    newSys1Calib = localData.current_sys_1_flow_calib;
                    newSys2Calib = localData.current_sys_2_flow_calib;
                    success = false;
                    return;
                }
            }
            for (int i = 0; i < sys2Calib.Length; i++) {

                if (sys2Calib[i].pressure > sys2MaxPressure || sys2Calib[i].pressure < 0)
                {
                    Debug.Print($"Calibration failed: System 2 pressure {i} out of range ({sys2Calib[i].pressure})");
                    newSys1Calib = localData.current_sys_1_flow_calib;
                    newSys2Calib = localData.current_sys_2_flow_calib;
                    success = false;
                    return;
                }
            }





            // CHECK PRESSURES AGAINST RELATIVE LIMITS (DRIFTING TOO FAR FROM ORIGINAL CALIBRATION)


            for (int i = 0; i < sys1Calib.Length; i++)
            {
                float newPressure = sys1Calib[i].pressure.Value;
                float originalPressure = localData.current_sys_1_flow_calib[i].pressure.Value;
                float diff = (newPressure - originalPressure) / originalPressure;

                if (diff > cellSettings.dispense_system.sys_1_max_pressure_dev_percent)
                {
                    Debug.Print($"Calibration failed: System 1 pressure {i} deviated too far from calib ({diff})");
                    newSys1Calib = localData.current_sys_1_flow_calib;
                    newSys2Calib = localData.current_sys_2_flow_calib;
                    success = false;
                    return;
                }
            }
            for (int i = 0; i < sys2Calib.Length; i++)
            {
                float newPressure = sys2Calib[i].pressure.Value;
                float originalPressure = localData.current_sys_2_flow_calib[i].pressure.Value;
                float diff = (newPressure - originalPressure) / originalPressure;

                if (diff > cellSettings.dispense_system.sys_2_max_pressure_dev_percent)
                {
                    Debug.Print($"Calibration failed: System 2 pressure {i} deviated too far from calib ({diff})");
                    newSys1Calib = localData.current_sys_1_flow_calib;
                    newSys2Calib = localData.current_sys_2_flow_calib;
                    success = false;
                    return;
                }
            }

            newSys1Calib = sys1Calib;
            newSys2Calib = sys2Calib;
            success = true;
        }

    }
}
