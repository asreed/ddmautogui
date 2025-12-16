using DDMAutoGUI.utilities;
using DDMAutoGUI.Utilities;
using DDMAutoGUI.windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DDMAutoGUI
{
    /// <summary>
    /// Interaction logic for MainWindowSingle.xaml
    /// </summary>








    public partial class MainWindow : Window
    {

        private List<Button> allButtons;

        private List<ResultsHeightMeasurement> laserRingData;
        private List<ResultsHeightMeasurement> laserMagData;
        private ResultsManager resultsManager;


        //private ProcessResults processData;
        private int currentStep = 0;
        private bool tabLock = true; // prevent user from clicking tabs directly


        public MainWindow()
        {

            App.ControllerManager.ControllerConnected += MainWindowSingle_OnConnected;
            App.ControllerManager.ControllerDisconnected += MainWindowSingle_OnDisconnected;
            App.ControllerManager.ControllerStateChanged += MainWindowSingle_OnChangeControllerState;
            App.ControllerManager.ConnectionLogUpdated += MainWindowSingle_OnChangeConnectionLog;
            App.ControllerManager.StatusLogUpdated += MainWindowSingle_OnChangeStatusLog;
            App.ControllerManager.RobotLogUpdated += MainWindowSingle_OnChangeRobotLog;
            App.ControllerManager.ConnectionStateChanged += MainWindowSingle_OnChangeConnectionState;



            InitializeComponent();
            this.Title += " " + App.ReleaseInfoManager.GetCurrentVersion();

            ReleaseInfoSingle currentRelease = App.ReleaseInfoManager.GetCurrentReleaseInfo();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{currentRelease.releaseIntent}");
            sb.AppendLine();
            sb.AppendLine($"Version: {currentRelease.version}");
            sb.AppendLine($"Release Date: {currentRelease.releaseDate}");
            sb.AppendLine($"Notes: {currentRelease.releaseDisplayNotes}");
            Adv_Abt_InfoTxb.Text = sb.ToString();

            Status_GUISimBdr.Visibility = App.GUI_SIM_MODE ? Visibility.Visible : Visibility.Collapsed;

            allButtons = new List<Button>()
            {
                Con_ConnectBtn,
                Adv_Con_ConnectBtn,
                Adv_Con_DisconnectBtn,
                Adv_Con_StatusSendBtn,
                Adv_Con_RobotSendBtn,
                //Adv_Cell_AutoStartBtn,
                //Adv_Cell_AutoStopBtn,
                Adv_Cell_EStopBtn,
                Adv_Cell_ECloseValvesBtn,

            };

            UpdateButtonLocks();

            Status_SimBdr.Visibility = Visibility.Collapsed;
            Adv_PWEntryBdr.Visibility = Visibility.Visible;
            Adv_AllControlsTcl.Visibility = Visibility.Collapsed;
            Disp_ProcessPrg.Value = 0;

            //Con_ErrorMsgTxb.Visibility = Visibility.Collapsed;

            string savedIP = App.LocalDataManager.localData.controller_ip;
            Con_IPTxt.Text = savedIP;
            Adv_Con_IPTxt.Text = savedIP;
        }


















        // ==================================================================
        // Main dispense process routine

        private async void RunFullDispenseProcess()
        {

            bool doSidePhoto = true;
            bool doPreTopPhoto = true;
            bool doMatlabPhoto = true;
            bool doRingMeasure = true;
            bool doMagMeasure = true;
            bool doDispense = true;
            bool doHallMeasure = true;
            bool doPostTopPhoto = true;
            bool doAutoCalib = true;

            float x, t, d;
            int n;

            CellSettings settings = App.SettingsManager.GetAllSettings();
            CSMotor motor = new CSMotor();
            string motorName = string.Empty;

            // radio buttons, so only one can be checked
            int motorSelection = -1;
            if (Disp_Motor57.IsChecked.Value)
            {
                motorSelection = 0;
                motor = settings.ddm_57;
                motorName = nameof(settings.ddm_57);
            }
            if (Disp_Motor95.IsChecked.Value)
            {
                motorSelection = 1;
                motor = settings.ddm_95;
                motorName = nameof(settings.ddm_95);
            }
            if (Disp_Motor116.IsChecked.Value)
            {
                motorSelection = 2;
                motor = settings.ddm_116;
                motorName = nameof(settings.ddm_116);
            }
            if (Disp_Motor170.IsChecked.Value)
            {
                motorSelection = 3;
                motor = settings.ddm_170;
                motorName = nameof(settings.ddm_170);
            }
            if (Disp_Motor170Tall.IsChecked.Value)
            {
                motorSelection = 4;
                motor = settings.ddm_170_tall;
                motorName = nameof(settings.ddm_170_tall);
            }
            if (motorSelection == -1)
            {
                // alert?
                return;
            }


            resultsManager = App.ResultsManager;
            resultsManager.UpdateProcessLog += MainWindowSingle_Disp_UpdateProcessLog;
            resultsManager.ClearCurrentResults();
            resultsManager.CreateNewResults();
            Disp_LogTxt.Text = "";

            //if (Disp_Sim_SimChk.IsChecked ?? false)
            //{
            //    bool errorEncountered = false;
            //    string errorMessage = string.Empty;
            //    string displayMessage = string.Empty;
            //    bool saveResults = false; // only save results if dispense step is reached
            //    try
            //    {

            //        float simFlowID = float.Parse(Disp_Sim_FlowIDTxt.Text);
            //        float simFlowOD = float.Parse(Disp_Sim_FlowODTxt.Text);

            //        Disp_ProcessPrg.Value = 0;
            //        Dispense_GoToStep(1);

            //        resultsManager.AddToLog("=== SIMULATION ENABLED ===");
            //        resultsManager.AddToLog($"Dispense process started for motor {motorName}");

            //        resultsManager.AddToLog("Verifying system health...");
            //        await Task.Delay(500);

            //        //Exception e = new Exception("System health check failed");
            //        //throw e;

            //        resultsManager.AddToLog("System OK");

            //        // Take top photo, verify size
            //        resultsManager.AddToLog("Taking pre-process top photo...");
            //        await Task.Delay(500);
            //        resultsManager.AddToLog($"Photo saved");
            //        resultsManager.AddToLog($"Motor size {motorName} verified");
            //        Disp_ProcessPrg.Value = 10;

            //        // Set pressures using local calib data
            //        resultsManager.AddToLog("Setting dispense system pressures...");
            //        int _sysID, _sysOD;
            //        float _pressureID, _pressureOD;
            //        _sysID = motor.shot_settings.sys_num_id.Value;
            //        _sysOD = motor.shot_settings.sys_num_od.Value;
            //        _pressureID = App.LocalDataManager.GetPressureFromFlowrate(_sysID, motor.shot_settings.target_flow_id.Value).Value;
            //        _pressureOD = App.LocalDataManager.GetPressureFromFlowrate(_sysOD, motor.shot_settings.target_flow_od.Value).Value;

            //        // maybe there's a cleaner way to do this:
            //        float? _pressure1 = null, _pressure2 = null;
            //        if (_sysID == 1)
            //        {
            //            _pressure1 = _pressureID;
            //        }
            //        else if (_sysID == 2)
            //        {
            //            _pressure2 = _pressureID;
            //        }
            //        if (_sysOD == 1)
            //        {
            //            _pressure1 = _pressureOD;
            //        }
            //        else if (_sysOD == 2)
            //        {
            //            _pressure2 = _pressureOD;
            //        }
            //        // (if sysID == sysOD, both local pressures will be identical anyway so no need to check)




            //        // TODO: VERIFY CALIBRATION HASN'T EXPIRED


            //        // TODO: VERIFY PRESSURES ARE WITHIN RANGE



            //        if (_pressure1 != null)
            //        {
            //            resultsManager.AddToLog($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {_pressure1:F3} psi");
            //        }
            //        else
            //        {
            //            resultsManager.AddToLog($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
            //        }
            //        if (_pressure2 != null)
            //        {
            //            resultsManager.AddToLog($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {_pressure2:F3} psi");
            //        }
            //        else
            //        {
            //            resultsManager.AddToLog($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
            //        }
            //        resultsManager.AddToLog("Pressures set");
            //        await Task.Delay(500);


            //        // Take side photo, read SN
            //        resultsManager.AddToLog("Taking side photo...");
            //        await Task.Delay(500);
            //        string sn = "SIM_123456";
            //        resultsManager.AddToLog($"Photo saved");
            //        resultsManager.AddToLog($"SN detected: {sn}");
            //        resultsManager.currentResults.ring_sn = sn;
            //        Disp_ProcessPrg.Value = 20;

            //        // Measure ring heights
            //        resultsManager.AddToLog("Measuring ring heights...");
            //        await Task.Delay(1000);
            //        List<ResultsHeightMeasurement> ring_heights = App.ControllerManager.GetSimulatedHeightData(motor.laser_ring_num.Value);
            //        resultsManager.AddToLog($"{ring_heights.Count} heights collected");
            //        resultsManager.currentResults.ring_heights = ring_heights;
            //        Disp_ProcessPrg.Value = 30;

            //        // Measure magnet heights
            //        resultsManager.AddToLog("Measuring magnet heights...");
            //        await Task.Delay(1000);
            //        List<ResultsHeightMeasurement> mag_heights = App.ControllerManager.GetSimulatedHeightData(motor.laser_mag_num.Value);
            //        resultsManager.AddToLog($"{mag_heights.Count} heights collected");
            //        resultsManager.currentResults.mag_heights = mag_heights;
            //        Disp_ProcessPrg.Value = 40;

            //        saveResults = true;

            //        // Dispense cyanoacrylate
            //        resultsManager.AddToLog("Dispensing cyanoacrylate...");
            //        await Task.Delay(2000);
            //        Disp_ProcessPrg.Value = 80;

            //        // Process dispense results
            //        float targetTimeID = motor.shot_settings.target_vol_id.Value / motor.shot_settings.target_flow_id.Value;
            //        float targetTimeOD = motor.shot_settings.target_vol_od.Value / motor.shot_settings.target_flow_od.Value;
            //        float simVolID = simFlowID * targetTimeID;
            //        float simVolOD = simFlowOD * targetTimeOD;
            //        ResultsShotData shotData = new ResultsShotData
            //        {
            //            motor_type = motorName,
            //            shot_result = true,
            //            shot_message = "",
            //            valve_num_id = 1,
            //            valve_num_od = 1,
            //            pressure_id = _pressureID,
            //            pressure_od = _pressureOD,
            //            time_id = targetTimeID,
            //            time_od = targetTimeOD,
            //            vol_id = simVolID,
            //            vol_od = simVolOD
            //        };

            //        string substance_id = motor.shot_settings.sys_num_id == 1 ? settings.dispense_system.sys_1_contents : settings.dispense_system.sys_2_contents;
            //        string substance_od = motor.shot_settings.sys_num_od == 1 ? settings.dispense_system.sys_1_contents : settings.dispense_system.sys_2_contents;
            //        string tb = "  ";

            //        resultsManager.currentResults.shot_data = shotData;
            //        resultsManager.AddToLog("Dispense complete");
            //        resultsManager.AddToLog("Results:");
            //        resultsManager.AddToLog($"{tb}ID:");
            //        resultsManager.AddToLog($"{tb}{tb}Valve {motor.shot_settings.sys_num_id} ({substance_id})");
            //        resultsManager.AddToLog($"{tb}{tb}Dispense volume: {shotData.vol_id:F3} mL ({shotData.vol_id.Value * 100 / motor.shot_settings.target_vol_id.Value:F1}% of target)");
            //        resultsManager.AddToLog($"{tb}{tb}Dispense time: {shotData.time_id:F3} s");
            //        resultsManager.AddToLog($"{tb}{tb}Pressure: {shotData.pressure_id:F3} psi");
            //        resultsManager.AddToLog($"{tb}OD:");
            //        resultsManager.AddToLog($"{tb}{tb}Valve {motor.shot_settings.sys_num_id} ({substance_od})");
            //        resultsManager.AddToLog($"{tb}{tb}Dispense volume: {shotData.vol_od:F3} mL ({shotData.vol_od.Value * 100 / motor.shot_settings.target_vol_od.Value:F1}% of target)");
            //        resultsManager.AddToLog($"{tb}{tb}Dispense time: {shotData.time_od:F3} s");
            //        resultsManager.AddToLog($"{tb}{tb}Pressure: {shotData.pressure_od:F3} psi");

            //        // Take post-process top photo
            //        resultsManager.AddToLog("Taking post-process top photo...");
            //        await Task.Delay(500);
            //        resultsManager.AddToLog($"Photo saved");
            //        Disp_ProcessPrg.Value = 90;

            //        // Move to load position
            //        resultsManager.AddToLog("Moving back to unload position...");
            //        await Task.Delay(500);
            //        Disp_ProcessPrg.Value = 100;

            //        // Adjust pressures
            //        CSDispenseCalib[] newSys1Calib;
            //        CSDispenseCalib[] newSys2Calib;
            //        bool calibSuccess;
            //        FlowCalibration.CalibratePressures(
            //            shotData,
            //            App.SettingsManager.GetAllSettings(),
            //            App.LocalDataManager.localData,
            //            out calibSuccess,
            //            out newSys1Calib,
            //            out newSys2Calib);

            //        if (calibSuccess)
            //        {
            //            App.LocalDataManager.UpdateCalib(1, newSys1Calib);
            //            App.LocalDataManager.UpdateCalib(2, newSys2Calib);
            //        }
            //        else
            //        {
            //            // ??
            //        }

            //        resultsManager.AddToLog("Saving updated calibration data to local storage...");
            //        App.LocalDataManager.SaveLocalDataToFile();
            //        resultsManager.AddToLog("Calibration data saved");

            //        resultsManager.AddToLog("Adjusting dispense system pressures...");
            //        _sysID = motor.shot_settings.sys_num_id.Value;
            //        _sysOD = motor.shot_settings.sys_num_od.Value;
            //        _pressureID = App.LocalDataManager.GetPressureFromFlowrate(_sysID, motor.shot_settings.target_flow_id.Value).Value;
            //        _pressureOD = App.LocalDataManager.GetPressureFromFlowrate(_sysOD, motor.shot_settings.target_flow_od.Value).Value;

            //        _pressure1 = null;
            //        _pressure2 = null;
            //        if (_sysID == 1)
            //        {
            //            _pressure1 = _pressureID;
            //        }
            //        else if (_sysID == 2)
            //        {
            //            _pressure2 = _pressureID;
            //        }
            //        if (_sysOD == 1)
            //        {
            //            _pressure1 = _pressureOD;
            //        }
            //        else if (_sysOD == 2)
            //        {
            //            _pressure2 = _pressureOD;
            //        }
            //        if (_pressure1 != null)
            //        {
            //            resultsManager.AddToLog($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {_pressure1:F3} psi");
            //        }
            //        else
            //        {
            //            resultsManager.AddToLog($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
            //        }
            //        if (_pressure2 != null)
            //        {
            //            resultsManager.AddToLog($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {_pressure2:F3} psi");
            //        }
            //        else
            //        {
            //            resultsManager.AddToLog($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
            //        }

            //        resultsManager.AddToLog("Process complete");

            //    }
            //    catch (Exception ex)
            //    {
            //        errorEncountered = true;
            //        errorMessage = ex.Message;
            //        resultsManager.AddToLog($"Process error: {ex.Message}");
            //    }


            //    // Determine pass/fail

            //    bool pass = false;
            //    string msg = string.Empty;
            //    if (errorEncountered)
            //    {
            //        pass = false;
            //        msg = errorMessage;
            //        displayMessage = msg;
            //    }
            //    else
            //    {
            //        App.ResultsManager.DeterminePassFail(
            //            resultsManager.currentResults,
            //            settings,
            //            motor,
            //            out pass,
            //            out msg);
            //        resultsManager.currentResults.overall_process_result = pass;
            //        resultsManager.currentResults.overall_proces_message = msg;
            //        displayMessage = msg;
            //    }

            //    // Save results to file
            //    if (saveResults)
            //    {
            //        string resultsPath = resultsManager.CreateResultsFolder();
            //        resultsManager.AddToLog("Saving settings to results folder");
            //        App.SettingsManager.SaveSettingsCopyToLocal(settings, resultsPath);
            //        resultsManager.AddToLog("Saving results to results folder");
            //        resultsManager.SaveDataToFile();
            //    }

            //    // Prepare and display results page

            //    Results res = resultsManager.currentResults;
            //    if (pass)
            //    {
            //        Disp_Res_PassBdr.Visibility = Visibility.Visible;
            //        Disp_Res_FailBdr.Visibility = Visibility.Collapsed;
            //    }
            //    else
            //    {
            //        Disp_Res_PassBdr.Visibility = Visibility.Collapsed;
            //        Disp_Res_FailBdr.Visibility = Visibility.Visible;
            //    }
            //    Disp_Res_ResMessageTxb.Text = displayMessage;


            //    Disp_Res_SNTxb.Text = resultsManager.currentResults.ring_sn;
            //    var data = resultsManager.currentResults.shot_data;
            //    Disp_Res_VolIDTxb.Text = $"{data.vol_id:F3} mL ({Math.Round(data.vol_id.Value * 100 / motor.shot_settings.target_vol_id.Value, 1):F1}% of target)";
            //    Disp_Res_VolODTxb.Text = $"{data.vol_od:F3} mL ({Math.Round(data.vol_od.Value * 100 / motor.shot_settings.target_vol_od.Value, 1):F1}% of target)";
            //    Dispense_GoToStep(2);

            //    // Clean up
            //    resultsManager.UpdateProcessLog -= MainWindowSingle_Disp_UpdateProcessLog;
            //    resultsManager.ClearCurrentResults();

            //    return;
            //}









            // Move to top camera and take photo

            if (doPreTopPhoto)
            {
                x = settings.ddm_common.camera_top.x.Value;
                t = settings.ddm_common.camera_top.t.Value;

                resultsManager.AddToLog("Taking top photo");
                resultsManager.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);
                App.CameraManager.AcquireAndSave(CameraManager.CellCamera.top, null);

                resultsManager.AddToLog("Top photo saved");
                Disp_ProcessPrg.Value = 5;
            }



            // Move to side camera and take photo

            if (doSidePhoto)
            {
                x = motor.camera_side.x.Value;
                t = motor.camera_side.t.Value;

                resultsManager.AddToLog("Taking side photo");
                resultsManager.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);
                App.CameraManager.AcquireAndSave(CameraManager.CellCamera.top, null);

                resultsManager.AddToLog("Top photo saved");
                Disp_ProcessPrg.Value = 10;
            }


            // Send photos to Matlab and read SN and size

            if (doMatlabPhoto)
            {
                resultsManager.AddToLog("Sending photos to Matlab for processing");



                Disp_ProcessPrg.Value = 15;

            }
            else
            {

            }


            // Set pressures

            resultsManager.AddToLog("Setting dispense system pressure setpoints");
            int sysID, sysOD;
            float pressureID, pressureOD;
            sysID = motor.shot_settings.sys_num_id.Value;
            sysOD = motor.shot_settings.sys_num_od.Value;
            pressureID = App.LocalDataManager.GetPressureFromFlowrate(sysID, motor.shot_settings.target_flow_id.Value).Value;
            pressureOD = App.LocalDataManager.GetPressureFromFlowrate(sysOD, motor.shot_settings.target_flow_od.Value).Value;

            // maybe there's a cleaner way to do this:
            float? pressure1 = null, pressure2 = null;
            if (sysID == 1)
            {
                pressure1 = pressureID;
            }
            else if (sysID == 2)
            {
                pressure2 = pressureID;
            }
            if (sysOD == 1)
            {
                pressure1 = pressureOD;
            }
            else if (sysOD == 2)
            {
                pressure2 = pressureOD;
            }
            // (if sysID == sysOD, both local pressures will be identical anyway so no need to check)




            // TODO: VERIFY CALIBRATION HASN'T EXPIRED


            // TODO: VERIFY PRESSURES ARE WITHIN RANGE




            if (pressure1 != null)
            {
                resultsManager.AddToLog($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {pressure1:F3} psi");
                await App.ControllerManager.SetRegPressure(1, pressure1.Value);
            }
            else
            {
                resultsManager.AddToLog($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
            }
            if (pressure2 != null)
            {
                resultsManager.AddToLog($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {pressure2:F3} psi");
                await App.ControllerManager.SetRegPressure(2, pressure2.Value);
            }
            else
            {
                resultsManager.AddToLog($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
            }
            resultsManager.AddToLog("Pressure setpoints set");
            Disp_ProcessPrg.Value = 20;




            // Move to laser sensor and collect height data

            x = motor.laser_ring.x.Value;
            t = motor.laser_ring.t.Value;
            n = motor.laser_ring_num.Value;
            d = settings.laser_delay.Value;

            resultsManager.AddToLog("Measuring magnet and concentrator height");
            resultsManager.AddToLog($"Moving to [{x}, {t}]");

            await App.ControllerManager.MoveJ(x, t);

            resultsManager.AddToLog($"Collecting {n} data points");
            string response = await App.ControllerManager.MeasureHeights(x, t, n, d);

            resultsManager.currentResults.ring_heights = App.ControllerManager.ParseHeightData(response);

            resultsManager.AddToLog("Ring data collected");
            Disp_ProcessPrg.Value = 30;


            // Process height data

            resultsManager.AddToLog("Processing height data");

            Disp_ProcessPrg.Value = 35;

            //...




            // Dispense adhesive

            resultsManager.AddToLog("Verifying pressures have reached their setpoints");
            if (pressure1 != null)
            {
                await App.ControllerManager.SetRegPressureAndWait(1, pressure1.Value, 20);
                resultsManager.AddToLog($"Pressure 1 at setpoint ({pressure1.Value:F3} psi)");
            }
            if (pressure2 != null)
            {
                await App.ControllerManager.SetRegPressureAndWait(2, pressure2.Value, 20);
                resultsManager.AddToLog($"Pressure 2 at setpoint ({pressure2.Value:F3}) psi");
            }

            resultsManager.AddToLog("Dispensing adhesive");

            float xID = motor.disp_id.x.Value;
            float tID = motor.disp_id.t.Value;
            float xOD = motor.disp_od.x.Value;
            float tOD = motor.disp_od.t.Value;
            float targetTimeID = motor.shot_settings.target_vol_id.Value / motor.shot_settings.target_flow_id.Value;
            float targetTimeOD = motor.shot_settings.target_vol_od.Value / motor.shot_settings.target_flow_od.Value;

            response = await App.ControllerManager.DispenseToRing(
                sysID,
                targetTimeID,
                xID, 
                tID, 
                sysOD, 
                targetTimeOD, 
                xOD, 
                tOD);

            Debug.Print(response);

            ResultsShotData shotData = App.ControllerManager.ParseDispenseResponse(response); // returns volumes and times
            shotData.motor_type = motorName;
            shotData.shot_result = true;
            shotData.shot_message = "";
            shotData.valve_num_id = sysID;
            shotData.valve_num_od = sysOD;
            shotData.pressure_id = pressureID;
            shotData.pressure_od = pressureOD;

            resultsManager.currentResults.shot_data = shotData;
            Disp_ProcessPrg.Value = 50;



            // Process results

            string substance_id = motor.shot_settings.sys_num_id == 1 ? settings.dispense_system.sys_1_contents : settings.dispense_system.sys_2_contents;
            string substance_od = motor.shot_settings.sys_num_od == 1 ? settings.dispense_system.sys_1_contents : settings.dispense_system.sys_2_contents;
            string tb = "  ";

            resultsManager.currentResults.shot_data = shotData;
            resultsManager.AddToLog("Dispense complete");
            resultsManager.AddToLog("Results:");
            resultsManager.AddToLog($"{tb}ID:");
            resultsManager.AddToLog($"{tb}{tb}Valve {motor.shot_settings.sys_num_id} ({substance_id})");
            resultsManager.AddToLog($"{tb}{tb}Dispense volume: {shotData.vol_id:F3} mL ({shotData.vol_id.Value * 100 / motor.shot_settings.target_vol_id.Value:F1}% of target)");
            resultsManager.AddToLog($"{tb}{tb}Dispense time: {shotData.time_id:F3} s");
            resultsManager.AddToLog($"{tb}{tb}Pressure: {shotData.pressure_id:F3} psi");
            resultsManager.AddToLog($"{tb}OD:");
            resultsManager.AddToLog($"{tb}{tb}Valve {motor.shot_settings.sys_num_id} ({substance_od})");
            resultsManager.AddToLog($"{tb}{tb}Dispense volume: {shotData.vol_od:F3} mL ({shotData.vol_od.Value * 100 / motor.shot_settings.target_vol_od.Value:F1}% of target)");
            resultsManager.AddToLog($"{tb}{tb}Dispense time: {shotData.time_od:F3} s");
            resultsManager.AddToLog($"{tb}{tb}Pressure: {shotData.pressure_od:F3} psi");


            // Start cure timer



            // Move to Hall sensor and collect polarity data

            x = motor.hall_sensor.x.Value;
            t = motor.hall_sensor.t.Value;

            resultsManager.AddToLog("Measuring magnet polarity");
            resultsManager.AddToLog($"Moving to [{x}, {t}]");

            await App.ControllerManager.MoveJ(x, t);

            //response = await App.ControllerManager.


            // Send data to Matlab and process


            // Move under top camera and take photo


            // Wait for cure timer to finish


            // Move to load position
















            // start process

            resultsManager.AddToLog("Dispense process started");
            Disp_ProcessPrg.IsIndeterminate = false;
            Disp_ProcessPrg.Value = 0;
            Dispense_GoToStep(1);



            if (doPreTopPhoto)
            {
                // take photo before process

                x = settings.ddm_common.camera_top.x.Value;
                t = settings.ddm_common.camera_top.t.Value;

                resultsManager.AddToLog("Taking photo...");
                resultsManager.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);
                //App.CameraManager.AcquireAndSave(CameraManager.CellCamera.top, null);

                resultsManager.AddToLog("Photo saved");
                Disp_ProcessPrg.Value = 20;
            }

            if (doSidePhoto)
            {
                // connect to side camera

                x = motor.camera_side.x.Value;
                t = motor.camera_side.t.Value;

                resultsManager.AddToLog("Connecting to side camera...");
                resultsManager.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);

                await Task.Delay(500);

                resultsManager.AddToLog("Camera connected");
                Disp_ProcessPrg.Value = 5;

            }
            if (doRingMeasure)
            {
                // measure magnet ring displacement

                x = motor.laser_ring.x.Value;
                t = motor.laser_ring.t.Value;
                n = motor.laser_ring_num.Value;
                d = settings.laser_delay.Value;

                resultsManager.AddToLog("Measuring ring...");
                resultsManager.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);
                string _response = await App.ControllerManager.MeasureHeights(x, t, n, d);

                resultsManager.currentResults.ring_heights = App.ControllerManager.ParseHeightData(_response);

                resultsManager.AddToLog("Ring data collected");
                Disp_ProcessPrg.Value = 30;
            }
            if (doMagMeasure)
            {
                // measure magnet (and concentrator?) displacement

                x = motor.laser_mag.x.Value;
                t = motor.laser_mag.t.Value;
                n = motor.laser_mag_num.Value;
                d = settings.laser_delay.Value;

                resultsManager.AddToLog("Measuring magnets...");
                resultsManager.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);
                string _response = await App.ControllerManager.MeasureHeights(x, t, n, d);

                resultsManager.currentResults.mag_heights = App.ControllerManager.ParseHeightData(_response);

                resultsManager.AddToLog("Magnet data collected");
                Disp_ProcessPrg.Value = 40;

            }
            if (doDispense)
            {
                // dispense cyanoacrylate


                CSMotor m = motor;
                CSShot c = motor.shot_settings;

                int valve_num_id = c.sys_num_id.Value;
                //float time_id = c.time_id.Value;
                float x_id = m.disp_id.x.Value;
                float t_id = m.disp_id.t.Value;
                //string substance_id = valve_num_id == 1 ? settings.system_1_contents : settings.system_2_contents;

                int valve_num_od = c.sys_num_od.Value;
                //float time_od = c.time_od.Value;
                float x_od = m.disp_od.x.Value;
                float t_od = m.disp_od.t.Value;
                //string substance_od = valve_num_od == 1 ? settings.system_1_contents : settings.system_2_contents;

                //resultsManager.AddToLog($"Using ID [{x_id}, {t_id}] for {time_id} seconds and OD [{x_od}, {t_od}] for {time_od} seconds");


                resultsManager.AddToLog("Dispense started");
                //string response = await App.ControllerManager.DispenseToRing(valve_num_id, time_id, x_id, t_id, valve_num_od, time_od, x_od, t_od);
                //Debug.Print(response);

                //resultsManager.currentResults.shot_data = App.ControllerManager.ParseDispenseResponse(response);

                string pressure_id_sp = await App.ControllerManager.GetRegPressureSetpoint(valve_num_id);
                string pressure_od_sp = await App.ControllerManager.GetRegPressureSetpoint(valve_num_od);

                string _tb = "  ";

                ResultsShotData data = resultsManager.currentResults.shot_data;

                resultsManager.AddToLog("Dispense complete");
                resultsManager.AddToLog("Results:");
                resultsManager.AddToLog($"{tb}ID:");
                //resultsManager.AddToLog($"{tb}{tb}Valve {valve_num_id} ({substance_id})");
                resultsManager.AddToLog($"{tb}{tb}Dispense volume: {data.vol_id} mL ({Math.Round(data.vol_id.Value * 100 / c.target_vol_id.Value, 1)}% of target)");
                resultsManager.AddToLog($"{tb}{tb}Dispense time: {data.time_id} s");
                resultsManager.AddToLog($"{tb}{tb}Pressure: {pressure_id_sp} psi");
                resultsManager.AddToLog($"{tb}OD:");
                //resultsManager.AddToLog($"{tb}{tb}Valve {valve_num_od} ({substance_od})");
                resultsManager.AddToLog($"{tb}{tb}Dispense volume: {data.vol_id} mL ({Math.Round(data.vol_od.Value * 100 / c.target_vol_od.Value, 1)}% of target)");
                resultsManager.AddToLog($"{tb}{tb}Dispense time: {data.time_od} s");
                resultsManager.AddToLog($"{tb}{tb}Pressure: {pressure_od_sp} psi");

                Disp_ProcessPrg.Value = 80;
            }
            if (doPostTopPhoto)
            {
                // take photo after process

                resultsManager.AddToLog("Taking photo...");
                resultsManager.AddToLog($"Moving to [{settings.ddm_common.camera_top.x}, {settings.ddm_common.camera_top.t}]");
                await Task.Delay(1000);
                resultsManager.AddToLog("Photo saved");
                Disp_ProcessPrg.Value = 90;
            }



            resultsManager.AddToLog("Moving back to unload position...");
            resultsManager.AddToLog($"Moving to [{settings.ddm_common.load.x}, {settings.ddm_common.load.t}]");
            Disp_ProcessPrg.Value = 100;

            x = settings.ddm_common.load.x.Value;
            t = settings.ddm_common.load.t.Value;
            await App.ControllerManager.MoveJ(x, t);

            resultsManager.AddToLog("Process complete");
            await Task.Delay(500);



            Dispense_GoToStep(2);
        }

        // ==================================================================



















        private void Dispense_GoToStep(int step)
        {
            // called when moving to the step
            tabLock = false;
            dispTabControl.SelectedIndex = step;
            currentStep = step;
            tabLock = true;

            switch (step)
            {
                case 0:

                    // config

                    break;
                case 1:

                    // process

                    break;
                case 2:

                    // results

                    break;

            }
            //processData.AddToLog($"Moved to step {step}");
        }

        private void ControllerManager_ControllerStateChanged(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void UpdateButtonLocks()
        {
            bool isConnected = App.ControllerManager.CONNECTION_STATE.isConnected;
            bool isAutoState = App.ControllerManager.CONNECTION_STATE.isAutoControllerStateRequesting;

            // generally, enable buttons when connected, disable when disconnected
            foreach (Button b in allButtons)
            {
                if (isConnected)
                {
                    b.IsEnabled = true;
                }
                else
                {
                    b.IsEnabled = false;
                }
            }

            LockRobotButtons(!isConnected);

            // set auto buttons
            //Adv_Cell_AutoStartBtn.IsEnabled = !isAutoState && isConnected;
            //Adv_Cell_AutoStopBtn.IsEnabled = isAutoState && isConnected;

            // set connect buttons
            Con_ConnectBtn.IsEnabled = !isConnected;
            Adv_Con_ConnectBtn.IsEnabled = !isConnected;
            Adv_Con_DisconnectBtn.IsEnabled = isConnected;
        }

        private int SizeIdxFromEnum(SettingsManager.DDMSize size)
        {
            switch (size)
            {
                case SettingsManager.DDMSize.ddm_57:
                    return 0;
                case SettingsManager.DDMSize.ddm_95:
                    return 1;
                case SettingsManager.DDMSize.ddm_116:
                    return 2;
                case SettingsManager.DDMSize.ddm_170:
                    return 3;
                case SettingsManager.DDMSize.ddm_170_tall:
                    return 4;
                default:
                    return -1;
            }
        }

        private SettingsManager.DDMSize SizeEnumFromIdx(int idx)
        {
            switch (idx)
            {
                case 0:
                    return SettingsManager.DDMSize.ddm_57;
                case 1:
                    return SettingsManager.DDMSize.ddm_95;
                case 2:
                    return SettingsManager.DDMSize.ddm_116;
                case 3:
                    return SettingsManager.DDMSize.ddm_170;
                case 4:
                    return SettingsManager.DDMSize.ddm_170_tall;
                default:
                    return SettingsManager.DDMSize.none;
            }
        }

        private void FormatReadout(Label label, float value)
        {
            label.Foreground = new BrushConverter().ConvertFrom("#000") as SolidColorBrush;
            label.Content = value.ToString();

        }
        private void FormatReadout(Label label, float value, string unit)
        {
            label.Foreground = new BrushConverter().ConvertFrom("#000") as SolidColorBrush;
            label.Content = value.ToString() + " " + unit;

        }
        private void FormatReadout(Label label, bool value)
        {
            label.Content = value ? "Yes" : "No";
            label.Foreground = new BrushConverter().ConvertFrom("#000") as SolidColorBrush;
            if (value)
            {
                label.Background = new BrushConverter().ConvertFrom("#ffd3ddf5") as SolidColorBrush;
            }
            else
            {
                label.Background = new BrushConverter().ConvertFrom("WhiteSmoke") as SolidColorBrush;
            }

        }
        private void FormatAllReadouts(ControllerState contState)
        {
            FormatReadout(roPowerEnabled, contState.isPowerEnabled);
            FormatReadout(roRobotHomed, contState.isRobotHomed);
            FormatReadout(roLinPos, contState.posLinear);
            FormatReadout(roRotPos, contState.posRotary);
            FormatReadout(roLinFlag1, !contState.isLinearIn1); // rail sensors are low when part present
            FormatReadout(roLinFlag2, !contState.isLinearIn2);
            FormatReadout(roLinFlag3, !contState.isLinearIn3);
            FormatReadout(roPresCmd1, contState.pressureCommand1, "psi");
            FormatReadout(roPresMeas1, contState.pressureMeasurement1, "psi");
            FormatReadout(roPresCmd2, contState.pressureCommand2, "psi");
            FormatReadout(roPresMeas2, contState.pressureMeasurement2, "psi");
            FormatReadout(roFlowVol1, contState.flowVolume1, "mL");
            FormatReadout(roFlowErr1, contState.flowError1);
            FormatReadout(roFlowVol2, contState.flowVolume2, "mL");
            FormatReadout(roFlowErr2, contState.flowError2);
            FormatReadout(roSafetyContState, contState.safetyControllerState);
            FormatReadout(roSafetyErrState, contState.safetyErrorState);
        }

        private void DisableReadout(Label label)
        {
            label.Foreground = new BrushConverter().ConvertFrom("#AAA") as SolidColorBrush;
            label.Background = new BrushConverter().ConvertFrom("WhiteSmoke") as SolidColorBrush;
        }

        private void DisableAllReadouts()
        {
            DisableReadout(roPowerEnabled);
            DisableReadout(roRobotHomed);
            DisableReadout(roLinPos);
            DisableReadout(roRotPos);
            DisableReadout(roLinFlag1);
            DisableReadout(roLinFlag2);
            DisableReadout(roLinFlag3);
            DisableReadout(roPresCmd1);
            DisableReadout(roPresMeas1);
            DisableReadout(roPresCmd2);
            DisableReadout(roPresMeas2);
            DisableReadout(roFlowVol1);
            DisableReadout(roFlowErr1);
            DisableReadout(roFlowVol2);
            DisableReadout(roFlowErr2);
        }
        private void BlankOutMotorSettings()
        {
            string blank = "-";

            Adv_Cell_MoveLoadInLbl.Content = blank;
            Adv_Cell_MoveCamTopInLbl.Content = blank;
            Adv_Cell_MoveCamSideInLbl.Content = blank;
            Adv_Cell_MoveLaserRingInLbl.Content = blank;
            Adv_Cell_MoveLaserMagInLbl.Content = blank;
            Adv_Cell_MoveDispIDInLbl.Content = blank;
            Adv_Cell_MoveDispODInLbl.Content = blank;
            Adv_Cell_MoveSpinInLbl.Content = blank;
            Adv_Cell_MeasureRingInLbl.Content = blank;
            Adv_Cell_MeasureMagInLbl.Content = blank;
            Adv_Cell_DispShotsInLbl.Content = blank;

        }
        private void DisplaySettingsToPanel()
        {
            CellSettings s = App.SettingsManager.GetAllSettings();
            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();


            if (m != null && m.IsValid())
            {
                LockRobotButtons(false);

                CSShot c = m.shot_settings;

                Adv_Cell_MoveLoadInLbl.Content = $"[{s.ddm_common.load.x}, {s.ddm_common.load.t}]";
                Adv_Cell_MoveCamTopInLbl.Content = $"[{s.ddm_common.camera_top.x}, {s.ddm_common.camera_top.t}]";
                Adv_Cell_MoveCamSideInLbl.Content = $"[{m.camera_side.x}, {m.camera_side.t}]";
                Adv_Cell_MoveLaserRingInLbl.Content = $"[{m.laser_ring.x}, {m.laser_ring.t}]";
                Adv_Cell_MoveLaserMagInLbl.Content = $"[{m.laser_mag.x}, {m.laser_mag.t}]";
                Adv_Cell_MoveDispIDInLbl.Content = $"[{m.disp_id.x}, {m.disp_id.t}]";
                Adv_Cell_MoveDispODInLbl.Content = $"[{m.disp_od.x}, {m.disp_od.t}]";
                Adv_Cell_MoveSpinInLbl.Content = $"{m.post_spin_time}s, {m.post_spin_speed}%";

                Adv_Cell_MeasureRingInLbl.Content = $"{m.laser_ring_num} places, {s.laser_delay} s each";
                Adv_Cell_MeasureMagInLbl.Content = $"{m.laser_mag_num} places, {s.laser_delay} s each";

                //Adv_Cell_DispShotsInLbl.Content = $"ID: Valve {c.valve_num_id}, x={m.disp_id.x} mm, {c.time_id} s, target {c.target_vol_id}mL\n";
                //Adv_Cell_DispShotsInLbl.Content += $"OD: Valve {c.valve_num_od}, x={m.disp_od.x} mm, {c.time_od} s, target {c.target_vol_od} mL";
            }
            else
            {
                LockRobotButtons(true);
                BlankOutMotorSettings();
            }

        }

        private void LockRobotButtons(bool state)
        {
            Adv_Cell_EnableBtn.IsEnabled = !state;
            Adv_Cell_HomeBtn.IsEnabled = !state;
            Adv_Cell_MoveLoadBtn.IsEnabled = !state;
            Adv_Cell_MoveCamTopBtn.IsEnabled = !state;
            Adv_Cell_MoveCamSideBtn.IsEnabled = !state;
            Adv_Cell_MoveLaserRingBtn.IsEnabled = !state;
            Adv_Cell_MoveLaserMagBtn.IsEnabled = !state;
            Adv_Cell_MoveDispIDBtn.IsEnabled = !state;
            Adv_Cell_MoveDispODBtn.IsEnabled = !state;
            Adv_Cell_MoveSpinBtn.IsEnabled = !state;
            Adv_Cell_MeasureRingBtn.IsEnabled = !state;
            Adv_Cell_MeasureMagBtn.IsEnabled = !state;
            Adv_Cell_SetPres1Btn.IsEnabled = !state;
            Adv_Cell_SetPres2Btn.IsEnabled = !state;
            Adv_Cell_Shot1Btn.IsEnabled = !state;
            Adv_Cell_Shot2Btn.IsEnabled = !state;
            Adv_Cell_SetZeroBothBtn.IsEnabled = !state;
            Adv_Cell_StartMeas1Btn.IsEnabled = !state;
            Adv_Cell_StopMeas1Btn.IsEnabled = !state;
            Adv_Cell_StartMeas2Btn.IsEnabled = !state;
            Adv_Cell_StopMeas2Btn.IsEnabled = !state;
            Adv_Cell_DispShotsBtn.IsEnabled = !state;
        }
        private void PopulateMotorSettings(ComboBox selection)
        {
            App.SettingsManager.selectedSize = SizeEnumFromIdx(selection.SelectedIndex);
            DisplaySettingsToPanel();

        }














        //////////////////// CUSTOM EVENTS

        public async void MainWindowSingle_OnConnected(object sender, EventArgs e)
        {
            Adv_Cell_ConnectedStatusTxt.Foreground = new BrushConverter().ConvertFrom("Black") as SolidColorBrush;
            Adv_Cell_ConnectedStatusTxt.Text = "Connected";

            string TCS = await App.ControllerManager.GetTCSVersion();
            string PAC = await App.ControllerManager.GetPACVersion();

            Adv_Cell_TCSVersionLbl.Text = TCS;
            Adv_Cell_PACVersionLbl.Text = PAC;

            Con_ConnectBtn.Content = "Connected";
            Con_ConnectBtn.IsEnabled = false;
            //Con_ErrorMsgTxb.Text = string.Empty;
            //Con_ErrorMsgTxb.Visibility = Visibility.Collapsed;

            Status_StatusTxt.Text = $"Connected ({App.ControllerManager.CONNECTION_STATE.connectedIP})";
            Status_TCSGrd.Visibility = Visibility.Visible;
            Status_TCSTxt.Text = TCS;
            Status_PACGrd.Visibility = Visibility.Visible;
            Status_PACTxt.Text = PAC;

            DispTab.IsEnabled = true;

            PopulateMotorSettings(Adv_Cell_MotorSizeCmb);
        }

        public async void MainWindowSingle_OnDisconnected(object sender, EventArgs e)
        {
            Adv_Cell_ConnectedStatusTxt.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
            Adv_Cell_ConnectedStatusTxt.Text = "Not connected";
            Adv_Cell_AutoStatusTxt.Text = "-";

            Adv_Cell_TCSVersionLbl.Text = "-";
            Adv_Cell_PACVersionLbl.Text = "-";

            Con_ConnectBtn.Content = "Connect";
            Con_ConnectBtn.IsEnabled = true;

            Status_StatusTxt.Text = "Not connected";

            Status_SimBdr.Visibility = Visibility.Collapsed;
            Status_TCSGrd.Visibility = Visibility.Collapsed;
            Status_PACGrd.Visibility = Visibility.Collapsed;

            Alert_MsgBarBdr.Visibility = Visibility.Collapsed;

            DispTab.IsEnabled = false;

            DisableAllReadouts();
            BlankOutMotorSettings();

        }

        public void MainWindowSingle_OnChangeControllerState(object sender, EventArgs e)
        {
            ControllerState contState = App.ControllerManager.CONTROLLER_STATE;

            Status_SimBdr.Visibility = Visibility.Collapsed;
            if (!contState.parseError)
            {
                if (App.ControllerManager.CONNECTION_STATE.isConnected)
                {
                    // Connected with good parse

                    switch (contState.safetyControllerState)
                    {
                        case -1:
                            Alert_MsgBarBdr.Visibility = Visibility.Visible;
                            Alert_MsgTxb.Text = "Safety thread stopped unexpectedly";
                            break;
                        case 0:
                            Alert_MsgBarBdr.Visibility = Visibility.Visible;
                            Alert_MsgTxb.Text = "Safety thread not started";
                            break;
                        case 1:
                            // Safety thread running normally
                            switch (contState.safetyErrorState)
                            {
                                case -6000:
                                    Alert_MsgBarBdr.Visibility = Visibility.Visible;
                                    Alert_MsgTxb.Text = $"{contState.safetyErrorState}: E Stop detected";
                                    break;
                                case -6001:
                                    Alert_MsgBarBdr.Visibility = Visibility.Visible;
                                    Alert_MsgTxb.Text = $"{contState.safetyErrorState}: Door opened";
                                    break;
                                case 0:
                                    Alert_MsgBarBdr.Visibility = Visibility.Collapsed;
                                    Alert_MsgTxb.Text = string.Empty;
                                    break;
                                default:
                                    Alert_MsgBarBdr.Visibility = Visibility.Visible;
                                    Alert_MsgTxb.Text = $"{contState.safetyErrorState}: Unknown error in safety thread";
                                    break;
                            }
                            break;
                    }

                    Adv_Cell_AutoStatusTxt.Foreground = new BrushConverter().ConvertFrom("Black") as SolidColorBrush;
                    Adv_Cell_AutoStatusTxt.Text = "Parse OK";
                    Status_SimBdr.Visibility = contState.isSimulated ? Visibility.Visible : Visibility.Collapsed;
                    FormatAllReadouts(contState);
                }
                else
                {
                    // Connected with bad parse
                    Adv_Cell_AutoStatusTxt.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
                    Adv_Cell_AutoStatusTxt.Text = "-";
                    DisableAllReadouts();
                }
            }
            else
            {
                // Disconnected
                Adv_Cell_AutoStatusTxt.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
                Adv_Cell_AutoStatusTxt.Text = $"Parse error: {contState.parseErrorMessage}";
                DisableAllReadouts();
            }

            if (App.ControllerManager.CONNECTION_STATE.isConnected)
            {
                //Con_StatusLbl.Content = "Connected";
            }
            else
            {
                //Con_StatusLbl.Content = "Not connected";
            }
        }

        public void MainWindowSingle_OnChangeConnectionState(object sender, EventArgs e)
        {
            UpdateButtonLocks();
        }

        public void MainWindowSingle_OnChangeConnectionLog(object sender, EventArgs e)
        {
            Con_LogTxt.Text = App.ControllerManager.GetConnectionLog();
            Con_LogTxt.ScrollToEnd();
        }

        public void MainWindowSingle_OnChangeStatusLog(object sender, EventArgs e)
        {
            Adv_Con_StatusLogTxt.Text = App.ControllerManager.GetStatusLog();
            Adv_Con_StatusLogTxt.ScrollToEnd();
        }

        public void MainWindowSingle_OnChangeRobotLog(object sender, EventArgs e)
        {
            Adv_Con_RobotLogTxt.Text = App.ControllerManager.GetRobotLog();
            Adv_Con_RobotLogTxt.ScrollToEnd();
        }

        public void MainWindowSingle_Disp_UpdateProcessLog(object sender, EventArgs e)
        {
            if (resultsManager != null)
            {
                ResultsLogLine logline = resultsManager.currentResults.process_log.Last();
                Disp_LogTxt.Text += logline.timestamp?.ToString(resultsManager.dateFormatShort) + ": " + logline.message + "\n";
                Disp_LogTxt.CaretIndex = Disp_LogTxt.Text.Length;
                Disp_LogTxt.ScrollToEnd();
            }
        }

        private void MainWindowSingle_Disp_TabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (tabLock)
            //{
            //    // gets called twice per click... maybe fix
            //    dispTabControl.SelectedIndex = currentStep;
            //}
        }










        //////////////////// COMBOBOX HANDLER

        private void Adv_Cell_MotorSizeCmb_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                PopulateMotorSettings(Adv_Cell_MotorSizeCmb);
            }
        }



        //////////////////// TEXTBOX HANDLERS

        private void Adv_Con_StatusLogTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Adv_Con_StatusSendBtn_Click(sender, e);
            }
        }

        private void Adv_Con_RobotLogTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Adv_Con_RobotSendBtn_Click(sender, e);
            }
        }




        //////////////////// BUTTON HANDLERS

        private async void Con_ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            Con_ConnectBtn.IsEnabled = false;
            Con_ConnectBtn.Content = "Connecting...";
            //Con_ErrorMsgTxb.Text = string.Empty;
            //Con_ErrorMsgTxb.Visibility = Visibility.Collapsed;
            App.LocalDataManager.localData.controller_ip = Con_IPTxt.Text;

            DeviceConnState connState = await App.ConnectionManager.ConnectToAllDevices(Con_IPTxt.Text);


            if (connState.controllerConnected)
            {
                //Con_ErrorMsgTxb.Text = string.Empty;
                //Con_ErrorMsgTxb.Visibility = Visibility.Collapsed;
            }
            else
            {
                //Con_ErrorMsgTxb.Text = "Unable to connect. Check IP address and make sure TCS is running.";
                //Con_ErrorMsgTxb.Visibility = Visibility.Visible;
            }
        }

        private async void Adv_Con_ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Con_ConnectBtn.IsEnabled = false;
            await App.ControllerManager.Connect(Adv_Con_IPTxt.Text);
            App.LocalDataManager.localData.controller_ip = Con_IPTxt.Text;
            if (App.ControllerManager.CONNECTION_STATE.isConnected)
            {
                Adv_Con_ContVersionTxt.Content = await App.ControllerManager.GetTCSVersion();
            }
        }

        private async void Adv_Con_DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Con_DisconnectBtn.IsEnabled = false;
            await App.ControllerManager.Disconnect();
            Adv_Con_ContVersionTxt.Content = "(no version info)";
        }

        private async void Adv_Con_StatusSendBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Con_StatusSendBtn.IsEnabled = false;
            string response = await App.ControllerManager.SendStatusCommand(Adv_Con_StatusMsgTxt.Text);
            UpdateButtonLocks();
        }

        private async void Adv_Con_RobotSendBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Con_RobotSendBtn.IsEnabled = false;
            string response = await App.ControllerManager.SendRobotCommand(Adv_Con_RobotMsgTxt.Text);
            UpdateButtonLocks();
        }

        private void Adv_Cell_AutoStartBtn_Click(object sender, RoutedEventArgs e)
        {
            App.ControllerManager.StartAutoControllerState();
        }

        private void Adv_Cell_AutoStopBtn_Click(object sender, RoutedEventArgs e)
        {
            App.ControllerManager.StopAutoControllerState();
        }



        private async void Disp_BeginBtn_Click(object sender, RoutedEventArgs e)
        {
            RunFullDispenseProcess();
        }

        private void Disp_SaveLogBtn_Click(object sender, RoutedEventArgs e)
        {
            if (resultsManager != null)
            {
                resultsManager.SaveDataToFile();
            }
        }

        private void Disp_ViewLogBtn_Click(object sender, RoutedEventArgs e)
        {
            if (resultsManager != null)
            {
                TextDataViewer viewer = new TextDataViewer();
                string log = resultsManager.GetLogAsString();
                if (log != null)
                {
                    viewer.Owner = this;
                    viewer.PopulateData(resultsManager.GetLogAsString(), "Process Log");
                    viewer.ShowDialog();
                }
            }
        }

        private void Disp_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (resultsManager != null)
            {
                resultsManager.OpenBrowserToDirectory();
            }
        }






        //private void Adv_Cell_OpenSettingsBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    string folderPath = App.SettingsManager.GetSettingsFilePath();
        //    System.Diagnostics.Process.Start("notepad.exe", folderPath);
        //}
        private void Adv_Cell_ReloadSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            App.SettingsManager.ReloadSettings();
            PopulateMotorSettings(Adv_Cell_MotorSizeCmb);
        }

        private async void Adv_Cell_EnableBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.EnablePower();
            Adv_Cell_EnableOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_HomeBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Cell_HomeOutLbl.Content = "(not implemented)";
        }

        private async void Adv_Cell_MoveLoadBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CellSettings s = App.SettingsManager.GetAllSettings();
            float x = s.ddm_common.load.x.Value;
            float t = s.ddm_common.load.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveLoadOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveCamTopBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CellSettings s = App.SettingsManager.GetAllSettings();
            float x = s.ddm_common.camera_top.x.Value;
            float t = s.ddm_common.camera_top.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveCamTopOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveCamSideBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.camera_side.x.Value;
            float t = m.camera_side.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveCamSideOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveLaserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.laser_ring.x.Value;
            float t = m.laser_ring.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveLaserRingOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveLaserMagBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.laser_mag.x.Value;
            float t = m.laser_mag.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveLaserMagOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveDispIDBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.disp_id.x.Value;
            float t = m.disp_id.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveDispIDOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveDispODBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.disp_od.x.Value;
            float t = m.disp_od.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveDispODOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveSpinBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float time = m.post_spin_time.Value;
            float speed = m.post_spin_speed.Value;

            string response = await App.ControllerManager.SpinInPlace(time, speed);
            Adv_Cell_MoveSpinOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MeasureRingBtn_Click(object sender, RoutedEventArgs e)
        {
            CellSettings s = App.SettingsManager.GetAllSettings();
            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float xPos = m.laser_ring.x.Value;
            float tPos = m.laser_ring.t.Value;
            int n = m.laser_ring_num.Value;
            float d = s.laser_delay.Value;

            LockRobotButtons(true);
            string response = await App.ControllerManager.MeasureHeights(xPos, tPos, n, d);
            laserRingData = App.ControllerManager.ParseHeightData(response);

            if (laserRingData.Count > 0)
            {
                Adv_Cell_MeasureRingOutLbl.Content = $"(data collected)";
            }
            else
            {
                Adv_Cell_MeasureRingOutLbl.Content = $"error: {response}";
            }

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MeasureMagBtn_Click(object sender, RoutedEventArgs e)
        {
            CellSettings s = App.SettingsManager.GetAllSettings();
            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float xPos = m.laser_mag.x.Value;
            float tPos = m.laser_mag.t.Value;
            int n = m.laser_mag_num.Value;
            float d = s.laser_delay.Value;

            LockRobotButtons(true);
            string response = await App.ControllerManager.MeasureHeights(xPos, tPos, n, d);
            laserMagData = App.ControllerManager.ParseHeightData(response);

            if (laserMagData.Count > 0)
            {
                Adv_Cell_MeasureMagOutLbl.Content = $"(data collected)";
            }
            else
            {
                Adv_Cell_MeasureMagOutLbl.Content = $"error: {response}";
            }
            LockRobotButtons(false);
        }

        private async void Adv_Cell_ShowRingBtn_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            if (laserRingData != null)
            {
                for (int i = 0; i < laserRingData.Count; i++)
                {
                    ResultsHeightMeasurement d = laserRingData[i];
                    sb.AppendLine($"{d.t}, {d.z}");
                }
                TextDataViewer viewer = new TextDataViewer();
                viewer.Owner = this;
                viewer.PopulateData(sb.ToString(), "Ring Displacement Measurements");
                viewer.Show();
            }
        }

        private async void Adv_Cell_ShowMagBtn_Click(object sender, RoutedEventArgs e)
        {
            if (laserMagData != null)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < laserMagData.Count; i++)
                {
                    ResultsHeightMeasurement d = laserMagData[i];
                    sb.AppendLine($"{d.t}, {d.z}");
                }
                TextDataViewer viewer = new TextDataViewer();
                viewer.Owner = this;
                viewer.PopulateData(sb.ToString(), "Ring Displacement Measurements");
                viewer.Show();
            }
        }

        private async void Adv_Cell_SetPres1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = Adv_Cell_SetPres1InTxt.Text;
            string response = await App.ControllerManager.SetRegPressureAndWait(1, float.Parse(input), 10);
            Adv_Cell_SetPres1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_SetPres2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = Adv_Cell_SetPres2InTxt.Text;
            string response = await App.ControllerManager.SetRegPressureAndWait(2, float.Parse(input), 10);
            Adv_Cell_SetPres2OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_Shot1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = Adv_Cell_Shot1InTxt.Text;
            string response = await App.ControllerManager.MeasureShotTimed(1, float.Parse(input));
            Adv_Cell_Shot1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_Shot2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = Adv_Cell_Shot2InTxt.Text;
            string response = await App.ControllerManager.MeasureShotTimed(2, float.Parse(input));
            Adv_Cell_Shot2OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_SetZeroBothBtn_Click(object sender, RoutedEventArgs e)
        {

            LockRobotButtons(true);
            string response = await App.ControllerManager.SetZeroShift(3.0f);
            Adv_Cell_SetZeroBothLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_StartMeas1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetShotTrigger(1, true);
            Adv_Cell_StartMeas1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_StopMeas1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetShotTrigger(1, false);
            Adv_Cell_StopMeas1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_StartMeas2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetShotTrigger(2, true);
            Adv_Cell_StartMeas2OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_StopMeas2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetShotTrigger(2, false);
            Adv_Cell_StopMeas2OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_DispShotsBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            CSShot c = m.shot_settings;

            float x_id = m.disp_id.x.Value;
            float t_id = m.disp_id.t.Value;
            //float time_id = c.time_id.Value;
            float valve_num_id = c.sys_num_id.Value;
            //float pressure_id = c.ref_pressure_1.Value;
            float target_vol_id = c.target_vol_id.Value;

            float x_od = m.disp_od.x.Value;
            float t_od = m.disp_od.t.Value;
            //float time_od = c.time_od.Value;
            float valve_num_od = c.sys_num_od.Value;
            //float pressure_od = c.ref_pressure_2.Value;
            float target_vol_od = c.target_vol_od.Value;


            //string response = await App.ControllerManager....
            string response = string.Empty;
            Adv_Cell_DispShotsOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_EStopBtn_Click(object sender, RoutedEventArgs e)
        {
            await App.ControllerManager.EStop();
        }

        private async void Adv_Cell_ECloseValvesBtn_Click(object sender, RoutedEventArgs e)
        {
            //LockStatusButtons(true);
            string response = await App.ControllerManager.CloseAllValves();
            //LockStatusButtons(false);
        }

        private async void Adv_Cam_AcqurieTopBtn_Click(object sender, RoutedEventArgs e)
        {
            acquiredImageDisplay.Source = null;
            Adv_Cam_StatusLbl.Content = "Acquiring image...";

            CameraAcquisitionResult result = new CameraAcquisitionResult();
            CameraManager.CellCamera camera = CameraManager.CellCamera.top;

            // something about Lucid driver can't be async; need to wrap
            result = await Task.Run(() => App.CameraManager.AcquireAndSave(camera, acquiredImageDisplay));

            if (result.success)
            {
                Adv_Cam_StatusLbl.Content = "Top image acquired";
                App.CameraManager.DisplayImage(acquiredImageDisplay, result.filePath);

            }
            else
            {
                Adv_Cam_StatusLbl.Content = $"Error: {result.errorMsg}";
            }
        }

        private async void Adv_Cam_AcqurieSideBtn_Click(object sender, RoutedEventArgs e)
        {
            acquiredImageDisplay.Source = null;
            Adv_Cam_StatusLbl.Content = "Acquiring image...";

            CameraAcquisitionResult result = new CameraAcquisitionResult();
            CameraManager.CellCamera camera = CameraManager.CellCamera.side;

            // something about Lucid driver can't be async; need to wrap
            result = await Task.Run(() => App.CameraManager.AcquireAndSave(camera, acquiredImageDisplay));

            if (result.success)
            {
                Adv_Cam_StatusLbl.Content = "Side image acquired";
                App.CameraManager.DisplayImage(acquiredImageDisplay, result.filePath);

            }
            else
            {
                Adv_Cam_StatusLbl.Content = $"Error: {result.errorMsg}";
            }
        }

        private void Adv_Cam_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            App.CameraManager.OpenExplorerToImages();
        }

        private void Disp_Res_FinishBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispense_GoToStep(0);
        }

        private void Adv_Res_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            App.ResultsManager.OpenBrowserToDirectory();
        }

        private void Adv_PWSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Adv_PWBox.Password == App.advancedSettingsPassword)
            {
                Adv_PWEntryBdr.Visibility = Visibility.Collapsed;
                Adv_PWMessageTxb.Visibility = Visibility.Collapsed;
                Adv_AllControlsTcl.Visibility = Visibility.Visible;
            }
            else
            {
                Adv_PWMessageTxb.Visibility = Visibility.Visible;
                Adv_PWMessageTxb.Text = "Incorrect password";
            }
        }

        private void Adv_PWBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Adv_PWSubmitBtn_Click(sender, e);
            }
        }

        private void Adv_Misc_TestMatlabBtn_Click(object sender, RoutedEventArgs e)
        {
            string exePath = @"C:\Users\areed\Documents\MATLAB\MatlabTestProject1\StandaloneDesktopApp1\output\build\MyDesktopApplication.exe";
            string filePath1 = "fake_path.jpg";
            string filePath2 = "fake_path_2.jpg";

            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = $"{filePath1} {filePath2}";
            process.Start();

            process.WaitForExit();

            string resultsFilePath = AppDomain.CurrentDomain.BaseDirectory + "results\\matlab_results.json";

            MatlabResult result = new MatlabResult();
            Debug.Print("Reading Matlab results file from: " + resultsFilePath);
            try
            {
                if (File.Exists(resultsFilePath))
                {
                    string rawJson = File.ReadAllText(resultsFilePath);
                    result = JsonSerializer.Deserialize<MatlabResult>(rawJson);
                }
                else
                {
                    Debug.Print("Matlab results file does not exist!");
                }
            }
            catch (JsonException ex)
            {
                Debug.Print("Error deserializing Matlab results file: " + ex.Message);
            }

            Debug.Print("");
            Debug.Print($"serial number detected: {result.sn_detected}");
            Debug.Print($"serial number: {result.sn}");
            Debug.Print($"file path top (in): {result.file_path_top_input}");
            Debug.Print($"file path side (in): {result.file_path_side_input}");

        }

        private void Adv_Misc_LockAdvBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_PWBox.Clear();
            Adv_PWEntryBdr.Visibility = Visibility.Visible;
            Adv_PWMessageTxb.Visibility = Visibility.Collapsed;
            Adv_AllControlsTcl.Visibility = Visibility.Collapsed;

        }

        private void Adv_DAQ_GetA0Btn_Click(object sender, RoutedEventArgs e)
        {
            //double v0 = App.DAQManager.GetVoltage();
            //Adv_DAQ_A0Txb.Text = $"{v0:F5} V";
        }
        private void Adv_DAQ_GetA0TimedBtn_Click(object sender, RoutedEventArgs e)
        {
            //App.DAQManager.GetVoltageTimed();
        }
    }
}
