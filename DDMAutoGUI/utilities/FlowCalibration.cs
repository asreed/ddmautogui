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
            out CSDispenseCalib[] newSys2Calib)
        {

            /// <summary>
            /// Takes cell settings data, compares to the latest entry in the results history, and
            /// estimates pressure adjustments required to improve shot volume accuracy for the next
            /// run. Updates results history flow calibrations. 
            /// </summary>

            success = false;
            CSShot targetShotData = null;

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



            // calculate real flow rate (from last dispense (results history))
            // get target flow rate (from settings (cell settings))
            // get corrective scale factor for flow rate
            // apply scale factor to flow rate lookup (results history)

            float lastFlowID = prevShotData.vol_id.Value / prevShotData.time_id.Value;
            float targetFlowID = targetShotData.target_flow_id.Value;
            float sfID = targetFlowID / lastFlowID;
            float sysID = targetShotData.sys_num_id.Value;

            float lastFlowOD = prevShotData.vol_od.Value / prevShotData.time_od.Value;
            float targetFlowOD = targetShotData.target_flow_od.Value;
            float sfOD = targetFlowOD / lastFlowOD;
            float sysOD = targetShotData.sys_num_od.Value;



            // default to 1.0 (no scaling)
            float sf1 = 1f;
            float sf2 = 1f; 

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

            Debug.Print($"Individual scale factors calculated: ID: {sfID}, OD: {sfOD}");
            Debug.Print($"Applying scale factors: system 1: {sf1}, system 2: {sf2}");

            CSDispenseCalib[] sys1Calib = localData.current_sys_1_flow_calib;
            CSDispenseCalib[] sys2Calib = localData.current_sys_2_flow_calib;



            // TODO ADD CHECKS HERE TO SEE IF PRESSURES ARE DRIFTING TOO FAR OUT OF RANGE

            // PRESSURES HIGHER THAN EXPECTED MEANS FLOW IS SLOWING AND EITHER MAINTENANCE OR RECALIBRATION IS REQUIRED



            Debug.Print("Updated calibration values:");
            for (int i = 0; i < sys1Calib.Length; i++)
            {
                sys1Calib[i].pressure *= sf1;
                Debug.Print($"  Sys 1: ({sys1Calib[i].flow}, {sys1Calib[i].pressure})");
            }
            for (int i=0; i < sys2Calib.Length; i++)
            {
                sys2Calib[i].pressure *= sf2;
                Debug.Print($"  Sys 2: ({sys2Calib[i].flow}, {sys2Calib[i].pressure})");
            }

            newSys1Calib = sys1Calib;
            newSys2Calib = sys2Calib;
            success = true;
        }




    }
}
