using DDMAutoGUI.CustomWindows;
using DDMAutoGUI.utilities;
using DDMAutoGUI.Utilities;
using DDMAutoGUI.windows;
using NationalInstruments.Restricted;
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
            LoadAdvancedOptions();

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

            };

            UpdateButtonLocks();

            Status_SimBdr.Visibility = Visibility.Collapsed;
            Adv_PWEntryBdr.Visibility = Visibility.Visible;
            Adv_AllControlsTcl.Visibility = Visibility.Collapsed;
            Disp_ProcessPrg.Value = 0;

            AdvTab.Visibility = Visibility.Collapsed;

        }

















        // ==================================================================
        // Main dispense process routine

        private async void RunFullDispenseProcess()
        {

            float x, t, d;
            int n;
            string response;

            string resultsPath;

            string topImagePath = string.Empty;
            string sideImagePath = string.Empty;
            string topAfterImagePath = string.Empty;

            bool errorEncountered = false;
            string errorMessage = string.Empty;
            string displayMessage = string.Empty;
            bool saveResults = true; // save results no matter what (for now)

            int sysID = 0;
            int sysOD = 0;

            float pressureID = 0f;
            float pressureOD = 0f;

            string tb = "  "; // for log formatting
            string overrideMsg = "Error encountered during dispense. Continue?";
            string overrideCap = "Override error?";



            LoadAdvancedOptions();

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


            App.ResultsManager.UpdateProcessLog += MainWindowSingle_Disp_UpdateProcessLog;
            App.ResultsManager.ClearCurrentResults();
            App.ResultsManager.CreateNewResults();
            Disp_LogTxt.Text = "";

            resultsPath = App.ResultsManager.CreateResultsFolder(); // create folder with no SN. rename later if SN detected
            App.ResultsManager.currentResults.ring_sn = Disp_MotorSNTxt.Text.Trim(); // until we can figure out SN from camera



            Disp_ProcessPrg.Value = 0;
            Disp_BusyPrg.Visibility = Visibility.Visible;
            Dispense_GoToStep(1);

            try
            {

                App.ResultsManager.AddToLog($"Dispense process started for motor {motorName}");


                if (App.advancedOptions.dispenseOptions.checkHealth)
                {
                    App.ResultsManager.AddToLog("Checking system health...");
                    HealthResult healthResult = await App.ControllerManager.CheckSystemHealth();
                    if (healthResult.isHealthy == false)
                    {
                        App.ResultsManager.AddToLog("Issues found:");
                        StringBuilder sb = new StringBuilder();
                        foreach (string issue in healthResult.issues)
                        {
                            App.ResultsManager.AddToLog($"{tb}{issue}");
                        }
                        throw new Exception("System health check failed");
                    }
                    else
                    {
                        App.ResultsManager.AddToLog("System OK");
                    }

                    response = await App.ControllerManager.SetZeroShift(3);
                    response = await App.ControllerManager.WaitBothRegPressures(5);
                }
                Disp_ProcessPrg.Value = 5;



                App.ResultsManager.AddToLog("Enabling power...");
                response = await App.ControllerManager.EnablePower();
                if (response != "1")
                {
                    throw new Exception("Failed to enable power");
                }
                else
                {
                    App.ResultsManager.AddToLog("Power enabled");
                }

                App.ResultsManager.AddToLog("Homing...");
                response = await App.ControllerManager.Home();
                if (response != "0")
                {
                    throw new Exception("Failed to home");
                }
                else
                {
                    App.ResultsManager.AddToLog("Homed");
                }
                Disp_ProcessPrg.Value = 10;


                // ALWAYS CHECK CLEARANCE
                App.ResultsManager.AddToLog("Checking clearance on center screw...");
                x = settings.ddm_common.clearance_check.x.Value;
                t = settings.ddm_common.clearance_check.t.Value;
                response = await App.ControllerManager.MoveJ(x, t);
                response = await App.ControllerManager.MeasureHeightSingle();
                float height = float.Parse(response.Split(" ")[1]);
                float min = settings.clearance_check_min.Value;
                float max = settings.clearance_check_max.Value;
                if (height > max || height < min)
                {
                    App.ResultsManager.AddToLog($"Clearance check failed: measured height {height} um outside of range ({min} - {max} um)");
                    
                    string err = "Clearance check failed";
                    MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                    if (mb != MessageBoxResult.OK) throw new Exception(err);

                }
                else
                {
                    App.ResultsManager.AddToLog($"Clearance check passed: {height} um within range ({min} - {max} um)");
                }
                Disp_ProcessPrg.Value = 15;



                if (App.advancedOptions.dispenseOptions.dispense)
                {
                    App.ResultsManager.AddToLog($"Setting dispense system pressures for {motorName}...");

                    LDMotorCalib calib = App.LocalDataManager.GetCalibFromMotorName(motorName);
                    float? _pressure1 = calib.sys_1_pressure;
                    float? _pressure2 = calib.sys_2_pressure;

                    if (_pressure1 != null)
                    {
                        App.ResultsManager.AddToLog($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {_pressure1.Value:F3} psi");
                        response = await App.ControllerManager.SetRegPressure(1, _pressure1.Value);
                    }
                    else
                    {
                        App.ResultsManager.AddToLog($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
                    }
                    if (_pressure2 != null)
                    {
                        App.ResultsManager.AddToLog($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {_pressure2.Value:F3} psi");
                        response = await App.ControllerManager.SetRegPressure(2, _pressure2.Value);
                    }
                    else
                    {
                        App.ResultsManager.AddToLog($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
                    }

                    App.ResultsManager.AddToLog("Waiting for pressures to settle...");
                    response = await App.ControllerManager.WaitBothRegPressures(10);
                    await Task.Delay(1000);
                    App.ResultsManager.AddToLog("Pressures settled");
                    App.ResultsManager.AddToLog("Zeroing flow sensors...");
                    response = await App.ControllerManager.SetZeroShift(3);
                    App.ResultsManager.AddToLog("Flow sensors zeroed");

                    App.ResultsManager.AddToLog("Pressures set");


                }
                Disp_ProcessPrg.Value = 20;



                if (App.advancedOptions.dispenseOptions.photoTop)
                {
                    App.ResultsManager.AddToLog("Acquiring preprocess top photo...");
                    x = settings.ddm_common.camera_top.x.Value;
                    t = settings.ddm_common.camera_top.t.Value;
                    response = await App.ControllerManager.MoveJ(x, t);

                    CameraAcquisitionResult camResult = await App.CameraManager.AcquireAndSave(CameraManager.CellCamera.top, null);

                    if (!camResult.success)
                    {
                        string err = $"Preprocess top camera acquisition failed: {camResult.errorMsg}";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);
                    }
                    else
                    {
                        topImagePath = camResult.filePath;
                        if (topImagePath != String.Empty)
                        {
                            App.ResultsManager.CopyPhotoToResultsFolder(topImagePath, "Top");
                        }
                        App.ResultsManager.AddToLog($"Preprocess top photo acquired");
                    }
                }
                Disp_ProcessPrg.Value = 25;



                if (App.advancedOptions.dispenseOptions.photoSide)
                {
                    App.ResultsManager.AddToLog("Acquiring side photo...");
                    x = motor.camera_side.x.Value;
                    t = motor.camera_side.t.Value;
                    response = await App.ControllerManager.MoveJ(x, t);

                    CameraAcquisitionResult camResult = await App.CameraManager.AcquireAndSave(CameraManager.CellCamera.side, null);

                    if (!camResult.success)
                    {
                        string err = $"Side camera acquisition failed: {camResult.errorMsg}";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);
                    }
                    else
                    {
                        sideImagePath = camResult.filePath;
                        if (sideImagePath != String.Empty)
                        {
                            App.ResultsManager.CopyPhotoToResultsFolder(sideImagePath, "Side");
                        }
                        App.ResultsManager.AddToLog($"Side photo acquired");
                    }
                }
                Disp_ProcessPrg.Value = 30;


                if (App.advancedOptions.dispenseOptions.runOCR)
                {
                    App.ResultsManager.AddToLog("Processing images...");
                    OCRData ocrData = await App.OCRManager.RunOCR(resultsPath);

                    string toolType = OCRManagerExtensions.GetToolType(ocrData, "Top.jpg");
                    if (toolType == null)
                    {
                        App.ResultsManager.AddToLog($"Unable to determine tool type from image");
                    }
                    else
                    {
                        if (toolType != motorName)
                        {
                            App.ResultsManager.AddToLog($"Tool type detected from image ({toolType}) does not match expected motor type ({motorName})");
                            
                            string err = "Tool type mismatch";
                            MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                            if (mb != MessageBoxResult.OK) throw new Exception(err);
                        }
                        else
                        {
                            string toolSN = OCRManagerExtensions.GetToolSN(ocrData, motorName, "Top.jpg");
                            App.ResultsManager.currentResults.tool_sn = toolSN;
                            App.ResultsManager.AddToLog($"Tool SN found: {toolSN}");
                        }
                    }

                    string ringSN = OCRManagerExtensions.GetRingSN(ocrData, motorName, "Side.jpg");
                    if (ringSN == null)
                    {
                        App.ResultsManager.AddToLog($"Unable to determine ring SN from image");

                        string err = "Ring SN not found";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);

                        // If no serial number found, use the one entered
                        ringSN = Disp_MotorSNTxt.Text;
                        App.ResultsManager.currentResults.ring_sn = ringSN;
                        App.ResultsManager.AddToLog($"No ring SN detected. User entered: {ringSN}");

                        // todo is find better logic. Should we compare the user entered text to the detected text?
                    }
                    else
                    {
                        App.ResultsManager.currentResults.ring_sn = ringSN;
                        App.ResultsManager.AddToLog($"Ring SN detected: {ringSN}");
                    }

                    App.ResultsManager.AddToLog($"Images processed");
                }
                Disp_ProcessPrg.Value = 40;





                if (App.advancedOptions.dispenseOptions.checkPolarity)
                {
                    App.ResultsManager.AddToLog("Checking magnet polarity...");
                    x = motor.hall_sensor.x.Value;
                    t = motor.hall_sensor.t.Value;
                    response = await App.ControllerManager.MoveJ(x, t);

                    float hallTime = settings.hall_spin_time.Value;
                    float hallSpeed = settings.hall_spin_speed.Value;

                    // start matlab exe running
                    Task<DAQMatlabResults> matlabTask = App.DAQManager.CollectDataAndProcessML("ddm_116");

                    // wait a few second for load
                    await Task.Delay(7000);

                    // start spin
                    Task spinTask = App.ControllerManager.SpinInPlace(hallTime, hallSpeed);

                    // now wait for data 
                    DAQMatlabResults result = await matlabTask;

                    App.ResultsManager.currentResults.daq_matlab_results = result;
                    App.ResultsManager.CopyPolarityPlotToResultsFolder(result.results_directory + "plot.png", "PolarityPlot");

                    if (result.result == 1)
                    {
                        App.ResultsManager.AddToLog($"Magnet polarity OK");
                    }
                    else if (result.result == 0)
                    {
                        App.ResultsManager.AddToLog($"Magnet polarity failed: {result.error_code} {result.error_message}");

                        string err = "Magnet polarity check failed";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);
                    }
                    else if (result.result == -1)
                    {
                        App.ResultsManager.AddToLog($"Magnet polarity check did not complete: {result.error_code} {result.error_message}");

                        string err = "Magnet polarity check failed";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);
                    }
                    else
                    {
                        App.ResultsManager.AddToLog($"Unexpected result from magnet polarity check: {result.result}");

                        string err = "Magnet polarity check failed";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);
                    }

                    // now wait for spin to finish
                    await spinTask;

                }
                Disp_ProcessPrg.Value = 50;



                if (App.advancedOptions.dispenseOptions.measureHeights)
                {
                    App.ResultsManager.AddToLog("Collecting ring height data...");
                    x = motor.laser_ring.x.Value;
                    t = motor.laser_ring.t.Value;
                    response = await App.ControllerManager.MoveJ(x, t);

                    n = motor.laser_ring_num.Value;
                    d = settings.laser_delay.Value;
                    response = await App.ControllerManager.MeasureHeights(x, t, n, d);


                    App.ResultsManager.currentResults.ring_heights = App.ControllerManager.ParseHeightData(response);
                    App.ResultsManager.AddToLog("Ring height data collected");
                    Disp_ProcessPrg.Value = 35;

                    App.ResultsManager.AddToLog("Collecting magnet/concentrator height data...");
                    x = motor.laser_mag.x.Value;
                    t = motor.laser_mag.t.Value;
                    response = await App.ControllerManager.MoveJ(x, t);

                    n = motor.laser_mag_num.Value;
                    d = settings.laser_delay.Value;
                    response = await App.ControllerManager.MeasureHeights(x, t, n, d);

                    App.ResultsManager.currentResults.mag_heights = App.ControllerManager.ParseHeightData(response);
                    App.ResultsManager.AddToLog("Magnet/concentrator height data collected");

                }
                Disp_ProcessPrg.Value = 60;



                if (App.advancedOptions.dispenseOptions.dispense)
                {
                    sysID = motor.shot_settings.id_sys_num.Value;
                    sysOD = motor.shot_settings.od_sys_num.Value;

                    float xID = motor.id_disp.x.Value;
                    float tID = motor.id_disp.t.Value;
                    float xOD = motor.od_disp.x.Value;
                    float tOD = motor.od_disp.t.Value;
                    float targetTimeID = motor.shot_settings.id_target_vol.Value / motor.shot_settings.id_target_flow.Value;
                    float targetTimeOD = motor.shot_settings.od_target_vol.Value / motor.shot_settings.od_target_flow.Value;

                    App.ResultsManager.AddToLog("Waiting for pressures to stabilize...");
                    response = await App.ControllerManager.WaitBothRegPressures(5);
                    App.ResultsManager.AddToLog("Pressures stabilized");

                    pressureID = float.Parse(await App.ControllerManager.GetRegPressureSetpoint(sysID));
                    pressureOD = float.Parse(await App.ControllerManager.GetRegPressureSetpoint(sysOD));

                    App.ResultsManager.AddToLog("Dispensing cyanoacrylate...");
                    response = await App.ControllerManager.DispenseToRing(
                        sysID,
                        targetTimeID,
                        xID,
                        tID,
                        sysOD,
                        targetTimeOD,
                        xOD,
                        tOD);


                    // If the dispense command has been called, save results.
                    // Need to save if there's a possibility of any liquid at all on the ring, even if the process fails.
                    //saveResults = true;

                    Debug.Print(response);

                    ResultsShotData shotData = App.ControllerManager.ParseDispenseResponse(response); // sets volumes, times, result pass/fail, message
                    shotData.motor_type = motorName;
                    shotData.id_valve_num = sysID;
                    shotData.od_valve_num = sysOD;
                    shotData.id_pressure = pressureID;
                    shotData.od_pressure = pressureOD;

                    string substance_id = motor.shot_settings.id_sys_num == 1 ? settings.dispense_system.sys_1_contents : settings.dispense_system.sys_2_contents;
                    string substance_od = motor.shot_settings.od_sys_num == 1 ? settings.dispense_system.sys_1_contents : settings.dispense_system.sys_2_contents;

                    ResultsReferenceData referenceData = new ResultsReferenceData
                    {
                        id_substance = substance_id,
                        od_substance = substance_od,
                        id_target_vol = motor.shot_settings.id_target_vol,
                        od_target_vol = motor.shot_settings.od_target_vol,
                        id_target_flow = motor.shot_settings.id_target_flow,
                        od_target_flow = motor.shot_settings.od_target_flow,
                        id_calib_pressure = App.LocalDataManager.GetPressureFromMotorName(motorName, sysID),
                        od_calib_pressure = App.LocalDataManager.GetPressureFromMotorName(motorName, sysOD)
                    };

                    App.ResultsManager.currentResults.shot_data = shotData;
                    App.ResultsManager.currentResults.reference_data = referenceData;

                    if (shotData.shot_result == true)
                    {
                        App.ResultsManager.AddToLog("Dispense successful");
                        App.ResultsManager.AddToLog("Results:");
                        App.ResultsManager.AddToLog($"{tb}ID:");
                        App.ResultsManager.AddToLog($"{tb}{tb}Valve {motor.shot_settings.id_sys_num} ({substance_id})");
                        App.ResultsManager.AddToLog($"{tb}{tb}Dispense volume: {shotData.id_vol:F3} mL ({shotData.id_vol.Value * 100 / motor.shot_settings.id_target_vol.Value:F1}% of target)");
                        App.ResultsManager.AddToLog($"{tb}{tb}Dispense time: {shotData.id_time:F3} s");
                        App.ResultsManager.AddToLog($"{tb}{tb}Pressure: {shotData.id_pressure:F3} psi");
                        App.ResultsManager.AddToLog($"{tb}OD:");
                        App.ResultsManager.AddToLog($"{tb}{tb}Valve {motor.shot_settings.id_sys_num} ({substance_od})");
                        App.ResultsManager.AddToLog($"{tb}{tb}Dispense volume: {shotData.od_vol:F3} mL ({shotData.od_vol.Value * 100 / motor.shot_settings.od_target_vol.Value:F1}% of target)");
                        App.ResultsManager.AddToLog($"{tb}{tb}Dispense time: {shotData.od_time:F3} s");
                        App.ResultsManager.AddToLog($"{tb}{tb}Pressure: {shotData.od_pressure:F3} psi");
                    }
                    else
                    {
                        App.ResultsManager.AddToLog($"Dispense failed: {shotData.shot_message}");

                        string err = "Dispense failed";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.Yes) throw new Exception(err);
                    }

                }
                Disp_ProcessPrg.Value = 70;



                if (App.advancedOptions.dispenseOptions.autocalibrate)
                {

                    App.ResultsManager.AddToLog("Autocalibrating pressures...");

                    if (App.ResultsManager.currentResults.shot_data == null)
                    {
                        App.ResultsManager.AddToLog($"Autocalibration failed: no results data loaded (???)");

                        string err = "Autocalibration failed";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);
                    }

                    bool calibSuccess;
                    string calibMessage;
                    float sf1, sf2;

                    FlowCalibrationManager.CalculateNewScaleFactors(
                        App.ResultsManager.currentResults.shot_data,
                        App.SettingsManager.GetAllSettings(),
                        App.LocalDataManager.GetLocalData(),
                        out calibSuccess,
                        out calibMessage,
                        out sf1,
                        out sf2);

                    if (!calibSuccess)
                    {
                        string err = "Calibration calculation failed: {calibMessage}";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);
                    }

                    App.ResultsManager.AddToLog($"Calibration calculation succeeded.");
                    App.ResultsManager.AddToLog($"Saving new calibration to local file...");

                    // create dummy comtainer to pass along
                    RunCalibResult resultContainer = new RunCalibResult
                    {
                        success = calibSuccess,
                        message = calibMessage,
                        time = DateTime.Now,
                        motorName = motorName,
                        sf1 = sf1,
                        sf2 = sf2
                    };
                    App.FlowCalibrationManager.GenerateAndSaveCalibration(resultContainer);
                    App.ResultsManager.AddToLog($"Calibration saved to local file");

                    App.ResultsManager.currentResults.reference_data.sys_1_autocal_sf = sf1;
                    App.ResultsManager.currentResults.reference_data.sys_2_autocal_sf = sf2;

                    LDMotorCalib calib = App.LocalDataManager.GetCalibFromMotorName(motorName);
                    float? _pressure1 = calib.sys_1_pressure;
                    float? _pressure2 = calib.sys_2_pressure;

                    App.ResultsManager.AddToLog($"Autocalibration succeeded");
                    if (_pressure1 != null)
                    {
                        App.ResultsManager.AddToLog($"{tb}System 1:");
                        App.ResultsManager.AddToLog($"{tb}{tb}SF: {sf1:F3}");
                        App.ResultsManager.AddToLog($"{tb}{tb}New pressure: {_pressure1:F3}");
                    }
                    if (_pressure2 != null)
                    {
                        App.ResultsManager.AddToLog($"{tb}System 2:");
                        App.ResultsManager.AddToLog($"{tb}{tb}SF: {sf2:F3}");
                        App.ResultsManager.AddToLog($"{tb}{tb}New pressure: {_pressure2:F3}");
                    }

                    App.ResultsManager.AddToLog("Adjusting dispense system pressures...");

                    if (_pressure1 != null)
                    {
                        App.ResultsManager.AddToLog($"Setting pressure for system 1 ({settings.dispense_system.sys_1_contents}) to {_pressure1.Value:F3} psi");
                        response = await App.ControllerManager.SetRegPressure(1, _pressure1.Value);
                    }
                    else
                    {
                        App.ResultsManager.AddToLog($"No pressure change for system 1 ({settings.dispense_system.sys_1_contents})");
                    }
                    if (_pressure2 != null)
                    {
                        App.ResultsManager.AddToLog($"Setting pressure for system 2 ({settings.dispense_system.sys_2_contents}) to {_pressure2.Value:F3} psi");
                        response = await App.ControllerManager.SetRegPressure(2, _pressure2.Value);
                    }
                    else
                    {
                        App.ResultsManager.AddToLog($"No pressure change for system 2 ({settings.dispense_system.sys_2_contents})");
                    }
                    App.ResultsManager.AddToLog("System pressures adjusted");
                }





                if (App.advancedOptions.dispenseOptions.photoTopAfter)
                {
                    App.ResultsManager.AddToLog("Acquiring postprocess top photo...");
                    x = settings.ddm_common.camera_top.x.Value;
                    t = settings.ddm_common.camera_top.t.Value;
                    response = await App.ControllerManager.MoveJ(x, t);

                    CameraAcquisitionResult camResult = await App.CameraManager.AcquireAndSave(CameraManager.CellCamera.top, null);

                    if (!camResult.success)
                    {
                        string err = $"Top camera acquisition failed: {camResult.errorMsg}";
                        MessageBoxResult mb = MessageBox.Show($"{overrideMsg}\n{err}", overrideCap, MessageBoxButton.OKCancel);
                        if (mb != MessageBoxResult.OK) throw new Exception(err);
                    }
                    else
                    {
                        topAfterImagePath = camResult.filePath;
                        App.ResultsManager.AddToLog($"Postprocess top photo acquired");
                    }
                }
                Disp_ProcessPrg.Value = 90;


                App.ResultsManager.AddToLog("Moving to unload...");
                x = settings.ddm_common.load.x.Value;
                t = settings.ddm_common.load.t.Value;
                response = await App.ControllerManager.MoveJ(x, t);






            }
            catch (Exception ex)
            {
                App.ResultsManager.AddToLog($"Process failed: {ex.Message}");
                errorEncountered = true;

                Disp_ProcessPrg.IsIndeterminate = true;
                try
                {
                    App.ResultsManager.AddToLog("Attempting to move to unload position...");
                    x = settings.ddm_common.load.x.Value;
                    t = settings.ddm_common.load.t.Value;
                    App.ControllerManager.MoveJ(x, t);
                }
                catch (Exception ex2)
                {
                    App.ResultsManager.AddToLog($"Failed to move to unload position: {ex2.Message}");
                }
                Disp_ProcessPrg.IsIndeterminate = false;
            }

            Disp_ProcessPrg.Value = 95;




            // Determine pass/fail

            bool pass = false;
            string msg = string.Empty;
            if (errorEncountered)
            {
                pass = false;
                msg = errorMessage;
                displayMessage = msg;
            }
            else
            {
                App.ResultsManager.DeterminePassFail(
                    App.ResultsManager.currentResults,
                    settings,
                    motor,
                    out pass,
                    out msg);
                App.ResultsManager.currentResults.overall_process_result = pass;
                App.ResultsManager.currentResults.overall_proces_message = msg;
                displayMessage = msg;
            }

            App.ResultsManager.AddToLog("Process complete");
            Disp_ProcessPrg.Value = 100;

            // Save results to file
            if (saveResults)
            {
                //if (topImagePath != String.Empty || sideImagePath != String.Empty)
                //{
                //    App.ResultsManager.AddToLog("Saving photos to results folder");
                //}
                //if (topImagePath != String.Empty)
                //{
                //    App.ResultsManager.CopyPhotoToResultsFolder(topImagePath, "Top");
                //}
                //if (sideImagePath != String.Empty)
                //{
                //    App.ResultsManager.CopyPhotoToResultsFolder(sideImagePath, "Side");
                //}
                if (topAfterImagePath != String.Empty)
                {
                    App.ResultsManager.CopyPhotoToResultsFolder(topAfterImagePath, "TopPost");
                }
                App.ResultsManager.AddToLog("Saving settings to results folder");
                App.SettingsManager.SaveSettingsCopyToLocal(settings, resultsPath);
                App.ResultsManager.AddToLog("Saving all results data to results folder");
                App.ResultsManager.SaveDataToFile();
            }





            // Prepare and display results page

            Results res = App.ResultsManager.currentResults;
            if (pass)
            {
                Disp_Res_PassBdr.Visibility = Visibility.Visible;
                Disp_Res_FailBdr.Visibility = Visibility.Collapsed;
            }
            else
            {
                Disp_Res_PassBdr.Visibility = Visibility.Collapsed;
                Disp_Res_FailBdr.Visibility = Visibility.Visible;
            }
            Disp_Res_ResMessageTxb.Text = displayMessage;


            Disp_Res_SNTxb.Text = App.ResultsManager.currentResults.ring_sn;
            var data = App.ResultsManager.currentResults.shot_data;
            if (data != null && data.id_vol != null && data.od_vol != null)
            {
                Disp_Res_VolIDTxb.Text = $"{data.id_vol:F3} mL ({Math.Round(data.id_vol.Value * 100 / motor.shot_settings.id_target_vol.Value, 1):F1}% of target)";
                Disp_Res_VolODTxb.Text = $"{data.od_vol:F3} mL ({Math.Round(data.od_vol.Value * 100 / motor.shot_settings.od_target_vol.Value, 1):F1}% of target)";
            }
            Dispense_GoToStep(2);

            // Clean up
            Disp_BusyPrg.Visibility = Visibility.Collapsed;
            App.ResultsManager.UpdateProcessLog -= MainWindowSingle_Disp_UpdateProcessLog;

            return;

        }




















        private void Dispense_GoToStep(int step)
        {
            dispTabControl.SelectedIndex = step;
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
            FormatReadout(roSysPressure, contState.systemPressure, "psi");
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
            Adv_Cell_MoveHallInLbl.Content = blank;
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
                Adv_Cell_MoveDispIDInLbl.Content = $"[{m.id_disp.x}, {m.id_disp.t}]";
                Adv_Cell_MoveDispODInLbl.Content = $"[{m.od_disp.x}, {m.od_disp.t}]";
                Adv_Cell_MoveHallInLbl.Content = $"[{m.hall_sensor.x}, {m.hall_sensor.t}]";

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
            Adv_Cell_MoveHallBtn.IsEnabled = !state;
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

        private void LoadAdvancedOptions()
        {
            App.advancedOptions.connectionOptions.controller = Adv_Opt_Con_ControllerChk.IsChecked ?? false;
            App.advancedOptions.connectionOptions.ioLinkDevices = Adv_Opt_Con_IOLinkChk.IsChecked ?? false;
            App.advancedOptions.connectionOptions.topCamera = Adv_Opt_Con_TopCamChk.IsChecked ?? false;
            App.advancedOptions.connectionOptions.sideCamera = Adv_Opt_Con_SideCamChk.IsChecked ?? false;
            App.advancedOptions.connectionOptions.laserSensor = Adv_Opt_Con_LaserChk.IsChecked ?? false;
            App.advancedOptions.connectionOptions.daqDevice = Adv_Opt_Con_DAQChk.IsChecked ?? false;

            App.advancedOptions.dispenseOptions.checkHealth = Adv_Opt_Disp_HealthChk.IsChecked ?? false;
            App.advancedOptions.dispenseOptions.photoTop = Adv_Opt_Disp_TopPhotoChk.IsChecked ?? false;
            App.advancedOptions.dispenseOptions.photoSide = Adv_Opt_Disp_SidePhotoChk.IsChecked ?? false;
            App.advancedOptions.dispenseOptions.runOCR = Adv_Opt_Disp_RunOCRChk.IsChecked ?? false;
            App.advancedOptions.dispenseOptions.measureHeights = Adv_Opt_Disp_RingHeightChk.IsChecked ?? false;
            App.advancedOptions.dispenseOptions.dispense = Adv_Opt_Disp_DispChk.IsChecked ?? false;
            App.advancedOptions.dispenseOptions.autocalibrate = Adv_Opt_Disp_AutoCalibChk.IsChecked ?? false;
            App.advancedOptions.dispenseOptions.checkPolarity = Adv_Opt_Disp_MagPolChk.IsChecked ?? false;
            App.advancedOptions.dispenseOptions.photoTopAfter = Adv_Opt_Disp_TopPhotoAfterChk.IsChecked ?? false;
        }













        //////////////////// CUSTOM EVENTS

        public async void MainWindowSingle_OnConnected(object sender, EventArgs e)
        {

            string TCS = App.ControllerManager.CONNECTION_STATE.connectedTCS;
            string PAC = App.ControllerManager.CONNECTION_STATE.connectedPAC;

            Con_ConnectBtn.Content = "Connected";
            Con_ConnectBtn.IsEnabled = false;

            Status_StatusTxt.Text = $"Connected ({App.ControllerManager.CONNECTION_STATE.connectedIP})";
            Status_TCSGrd.Visibility = Visibility.Visible;
            Status_TCSTxt.Text = TCS;
            Status_PACGrd.Visibility = Visibility.Visible;
            Status_PACTxt.Text = PAC;

            DispTab.IsEnabled = true;
            CalibTab.IsEnabled = true;
            ServTab.IsEnabled = true;

            PopulateMotorSettings(Adv_Cell_MotorSizeCmb);
        }

        public async void MainWindowSingle_OnDisconnected(object sender, EventArgs e)
        {

            Con_ConnectBtn.Content = "Connect";
            Con_ConnectBtn.IsEnabled = true;

            Status_StatusTxt.Text = "Not connected";

            Status_SimBdr.Visibility = Visibility.Collapsed;
            Status_TCSGrd.Visibility = Visibility.Collapsed;
            Status_PACGrd.Visibility = Visibility.Collapsed;

            Alert_MsgBarBdr.Visibility = Visibility.Collapsed;

            DispTab.IsEnabled = false;
            CalibTab.IsEnabled = false;
            ServTab.IsEnabled = false;

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

                    Status_SimBdr.Visibility = contState.isSimulated ? Visibility.Visible : Visibility.Collapsed;
                    FormatAllReadouts(contState);
                }
                else
                {
                    // Connected with bad parse
                    DisableAllReadouts();
                }
            }
            else
            {
                // Disconnected
                DisableAllReadouts();
            }

            if (App.ControllerManager.CONNECTION_STATE.isConnected)
            {
            }
            else
            {
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
            if (App.ResultsManager != null)
            {
                ResultsLogLine logline = App.ResultsManager.currentResults.process_log.Last();
                Disp_LogTxt.Text += logline.timestamp?.ToString(App.ResultsManager.dateFormatShort) + ": " + logline.message + "\n";
                Disp_LogTxt.CaretIndex = Disp_LogTxt.Text.Length;
                Disp_LogTxt.ScrollToEnd();
            }
        }

        private void MainWindowSingle_Disp_TabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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
            LoadAdvancedOptions();
            Con_ConnectBtn.IsEnabled = false;
            Con_ConnectBtn.Content = "Connecting...";
            Con_ConnectPrg.Visibility = Visibility.Visible;
            //App.LocalDataManager.localData.controller_ip = Con_IPTxt.Text;

            DeviceConnState connState = await App.ConnectionManager.ConnectToAllDevices(Con_IPTxt.Text);

            Con_ConnectPrg.Visibility = Visibility.Collapsed;

            if (connState.controllerConnected)
            {
            }
            else
            {
            }
        }

        private async void Adv_Con_ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadAdvancedOptions();
            Adv_Con_ConnectBtn.IsEnabled = false;
            await App.ControllerManager.Connect(Adv_Con_IPTxt.Text);
            //App.LocalDataManager.localData.controller_ip = Con_IPTxt.Text;
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
            if (App.ResultsManager != null)
            {
                App.ResultsManager.SaveDataToFile();
            }
        }

        private void Disp_ViewLogBtn_Click(object sender, RoutedEventArgs e)
        {
            if (App.ResultsManager != null)
            {
                TextDataViewer viewer = new TextDataViewer();
                string log = App.ResultsManager.GetLogAsString();
                if (log != null)
                {
                    viewer.Owner = this;
                    viewer.PopulateData(App.ResultsManager.GetLogAsString(), "Process Log");
                    viewer.ShowDialog();
                }
            }
        }

        private void Disp_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (App.ResultsManager != null)
            {
                App.ResultsManager.OpenBrowserToDirectory();
            }
        }



        //private async void Cal_CalPosBtn_Click(object sender, RoutedEventArgs e)
        //{

        //    Cal_PosPrg.Visibility = Visibility.Visible;
        //    string response = await App.ControllerManager.CalibratePosition();
        //    Con_ConnectPrg.Visibility = Visibility.Collapsed;

        //    if (response == "0")
        //    {
        //        Cal_PosResultTxb.Text = "Success";
        //    }
        //    else
        //    {
        //        Cal_PosResultTxb.Text = response;
        //    }
        //}

        //private void Cal_PosUpdateBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    Cal_PosJ1Txb.Text = $"{App.ControllerManager.CONTROLLER_STATE.posRotary}";
        //    Cal_PosJ2Txb.Text = $"{App.ControllerManager.CONTROLLER_STATE.posLinear}";
        //}






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
            float x = m.id_disp.x.Value;
            float t = m.id_disp.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveDispIDOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveDispODBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.od_disp.x.Value;
            float t = m.od_disp.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveDispODOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveHallBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.hall_sensor.x.Value;
            float t = m.hall_sensor.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveHallOutLbl.Content = response;

            LockRobotButtons(false);
        }

        //private async void Adv_Cell_MoveSpinBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    LockRobotButtons(true);

        //    CSMotor m = App.SettingsManager.GetSettingsForSelectedSize();
        //    float time = m.post_spin_time.Value;
        //    float speed = m.post_spin_speed.Value;

        //    string response = await App.ControllerManager.SpinInPlace(time, speed);
        //    //Adv_Cell_MoveSpinOutLbl.Content = response;

        //    LockRobotButtons(false);
        //}

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

        private async void Adv_Cell_MeasureSingleBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.MeasureHeightSingle();
            Adv_Cell_MeasureSingleOutLbl.Content = response;
            LockRobotButtons(false);
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

            float x_id = m.id_disp.x.Value;
            float t_id = m.id_disp.t.Value;
            //float time_id = c.time_id.Value;
            float valve_num_id = c.id_sys_num.Value;
            //float pressure_id = c.ref_pressure_1.Value;
            float target_vol_id = c.id_target_vol.Value;

            float x_od = m.od_disp.x.Value;
            float t_od = m.od_disp.t.Value;
            //float time_od = c.time_od.Value;
            float valve_num_od = c.od_sys_num.Value;
            //float pressure_od = c.ref_pressure_2.Value;
            float target_vol_od = c.od_target_vol.Value;


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
            result = await App.CameraManager.AcquireAndSave(camera, acquiredImageDisplay);

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
            result = await App.CameraManager.AcquireAndSave(camera, acquiredImageDisplay);

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

        //private void Cal_PWSubmitBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (Cal_PWBox.Password == App.calibrationPassword)
        //    {
        //        Cal_PWEntryBdr.Visibility = Visibility.Collapsed;
        //        Cal_PWMessageTxb.Visibility = Visibility.Collapsed;
        //        Cal_AllControlsTcl.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        Cal_PWMessageTxb.Visibility = Visibility.Visible;
        //        Cal_PWMessageTxb.Text = "Incorrect password";
        //    }
        //}

        //private void Cal_PWBox_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Enter)
        //    {
        //        Cal_PWSubmitBtn_Click(sender, e);
        //    }
        //}
        private async void Adv_Misc_TestMatlabBtn_Click(object sender, RoutedEventArgs e)
        {

            DAQMatlabResults result = await App.DAQManager.CollectDataAndProcessML("ddm_116");

            //string exePath = @"C:\Users\areed\Documents\MATLAB\MatlabTestProject1\StandaloneDesktopApp1\output\build\MyDesktopApplication.exe";
            //string filePath1 = "fake_path.jpg";
            //string filePath2 = "fake_path_2.jpg";

            //Process process = new Process();
            //process.StartInfo.FileName = exePath;
            //process.StartInfo.Arguments = $"{filePath1} {filePath2}";
            //process.Start();

            //process.WaitForExit();

            //string resultsFilePath = AppDomain.CurrentDomain.BaseDirectory + "results\\matlab_results.json";

            //MatlabResult result = new MatlabResult();
            //Debug.Print("Reading Matlab results file from: " + resultsFilePath);
            //try
            //{
            //    if (File.Exists(resultsFilePath))
            //    {
            //        string rawJson = File.ReadAllText(resultsFilePath);
            //        result = JsonSerializer.Deserialize<MatlabResult>(rawJson);
            //    }
            //    else
            //    {
            //        Debug.Print("Matlab results file does not exist!");
            //    }
            //}
            //catch (JsonException ex)
            //{
            //    Debug.Print("Error deserializing Matlab results file: " + ex.Message);
            //}

            //Debug.Print("");
            //Debug.Print($"serial number detected: {result.sn_detected}");
            //Debug.Print($"serial number: {result.sn}");
            //Debug.Print($"file path top (in): {result.file_path_top_input}");
            //Debug.Print($"file path side (in): {result.file_path_side_input}");

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

        private void Dev_Btn_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 5;
            Adv_PWBox.Focus();
        }

        private void Disp_Res_ViewResBtn_Click(object sender, RoutedEventArgs e)
        {
            string data_string = App.ResultsManager.GetCurrentResultsAsString();

            TextDataViewer viewer = new TextDataViewer();
            if (data_string != null)
            {
                viewer.Owner = this;
                viewer.PopulateData(data_string, "Results Data");
                viewer.ShowDialog();
            }
        }

        private void Disp_Res_OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            App.ResultsManager.OpenBrowserToDirectory();
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                TabControl tc = (TabControl)sender;
                switch (tc.SelectedIndex)
                {
                    case 0:
                        // Connection Tab

                        break;

                    case 1:
                        // Dispense Tab

                        break;

                    case 2:
                        // Calibration Tab

                        CalPanel.SetupPanel();

                        break;

                    case 3:


                        break;
                }
            }
        }


        private async void Adv_Cam_RunOCR_Click(object sender, RoutedEventArgs e)
        {
            string name = Adv_Cam_OCRPathTxb.Text;
            Adv_Cam_OCRPrg.Visibility = Visibility.Visible;

            OCRData data = await App.OCRManager.RunOCR(name);
            if (data != null)
            {
                Adv_Cam_OCRResTxb.Text = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                Adv_Cam_OCRResTxb.Text = "Error reading OCR results";
            }

            Adv_Cam_OCRPrg.Visibility = Visibility.Collapsed;
        }
    }
}
