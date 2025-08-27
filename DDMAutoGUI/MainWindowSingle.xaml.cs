using DDMAutoGUI.utilities;
using DDMAutoGUI.windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DDMAutoGUI
{
    /// <summary>
    /// Interaction logic for MainWindowSingle.xaml
    /// </summary>
   
    
    
    
    
    
    
    
    public partial class MainWindowSingle : Window
    {

        private List<Button> allButtons;

        private List<DDMResultsSingleHeight> laserRingData;
        private List<DDMResultsSingleHeight> laserMagData;


        private ProcessResults processData;
        private int currentStep = 0;
        private bool tabLock = true; // prevent user from clicking tabs directly


        public MainWindowSingle()
        {

            App.ControllerManager.ControllerConnected       += MainWindowSingle_OnConnected;
            App.ControllerManager.ControllerDisconnected    += MainWindowSingle_OnDisconnected;
            App.ControllerManager.ControllerStateChanged    += MainWindowSingle_OnChangeControllerState;
            App.ControllerManager.StatusLogUpdated          += MainWindowSingle_OnChangeStatusLog;
            App.ControllerManager.RobotLogUpdated           += MainWindowSingle_OnChangeRobotLog;

            App.UIManager.UIStateChanged += MainWindowSingle_OnChangeUIState;



            InitializeComponent();
            this.Title += " " + App.UIManager.GetAppVersionString();

            allButtons = new List<Button>()
            {
                Con_ConnectBtn,
                Adv_Con_ConnectBtn,
                Adv_Con_DisconnectBtn,
                Adv_Con_StatusSendBtn,
                Adv_Con_RobotSendBtn,
                Adv_Cell_AutoStartBtn,
                Adv_Cell_AutoStopBtn,
                Adv_Cell_EStopBtn,
                Adv_Cell_ECloseValvesBtn
            };
        }








        // ================ Main dispense process routine

        private async void DoProcess()
        {
            // pull data from user config (validate?)


            processData = new ProcessResults();
            processData.UpdateProcessLog += MainWindowSingle_Disp_UpdateProcessLog;

            string sn = "TEST-SN-123456789";
            int motorSelection = Disp_MotorSizeCmb.SelectedIndex;

            bool doSNPhoto = Disp_SNPhotoChk.IsChecked ?? false;
            bool doPrePhoto = Disp_PrePhotoChk.IsChecked ?? false;
            bool doRingMeasure = Disp_MeasureRingChk.IsChecked ?? false;
            bool doMagMeasure = Disp_MeasureMagChk.IsChecked ?? false;
            bool doDispense = Disp_DispenseChk.IsChecked ?? false;
            bool doPostPhoto = Disp_PostPhotoChk.IsChecked ?? false;

            DDMSettings settings = App.SettingsManager.SETTINGS;

            // store relevant data in processData object

            processData.results.ring_sn = sn;

            // pull data from settings

            DDMSettingsSingleSize motor = new DDMSettingsSingleSize();

            float x, t, d;
            int n;

            switch (motorSelection)
            {
                case 0: // DDM 57
                    motor = settings.ddm_57;
                    break;

                case 1: // DDM 95
                    motor = settings.ddm_95;
                    break;

                case 2: // DDM 116
                    motor = settings.ddm_116;
                    break;

                case 3: // DDM 170
                    motor = settings.ddm_170;
                    break;

                case 4: // DDM 170 Tall
                    motor = settings.ddm_170_tall;
                    break;
            }

            processData.AddToLog($"Serial number: {sn}");

            // start process

            processData.AddToLog("Dispense process started");
            Disp_ProcessPrg.IsIndeterminate = false;
            Disp_ProcessPrg.Value = 0;
            GoToStep(1);

            // verify all sensors are online

            processData.AddToLog("Verifying sensors...");
            await Task.Delay(500);
            processData.AddToLog("Sensors verified");

            if (doSNPhoto)
            {
                // connect to side camera


                x = motor.camera_side.x.Value;
                t = motor.camera_side.t.Value;

                processData.AddToLog("Connecting to side camera...");
                processData.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);

                await Task.Delay(500);

                processData.AddToLog("Camera connected");
                Disp_ProcessPrg.Value = 5;

            }

            if (doPrePhoto || doPostPhoto)
            {
                // connect to top camera

                processData.AddToLog("Connecting to top camera...");
                await Task.Delay(500);
                processData.AddToLog("Camera connected");
                Disp_ProcessPrg.Value = 10;

            }

            if (doPrePhoto)
            {
                // take photo before process

                x = settings.common.camera_top.x.Value;
                t = settings.common.camera_top.t.Value;

                processData.AddToLog("Taking photo...");
                processData.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);

                await Task.Delay(1000); 

                processData.AddToLog("Photo saved");
                Disp_ProcessPrg.Value = 20;
            }
            if (doRingMeasure)
            {
                // measure magnet ring displacement
                
                x = motor.laser_ring.x.Value;
                t = motor.laser_ring.t.Value;
                n = settings.common.laser_ring_num.Value;
                d = settings.common.laser_delay.Value;

                processData.AddToLog("Measuring ring...");
                processData.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);
                string response = await App.ControllerManager.MeasureHeights(x, t, n, d);

                processData.results.ring_heights = App.ControllerManager.ParseHeightData(response);

                processData.AddToLog("Ring data collected");
                Disp_ProcessPrg.Value = 30;
            }
            if (doMagMeasure)
            {
                // measure magnet (and concentrator?) displacement

                x = motor.laser_mag.x.Value;
                t = motor.laser_mag.t.Value;
                n = settings.common.laser_mag_num.Value;
                d = settings.common.laser_delay.Value;

                processData.AddToLog("Measuring magnets...");
                processData.AddToLog($"Moving to [{x}, {t}]");

                await App.ControllerManager.MoveJ(x, t);
                string response = await App.ControllerManager.MeasureHeights(x, t, n, d);

                processData.results.mag_heights = App.ControllerManager.ParseHeightData(response);

                processData.AddToLog("Magnet data collected");
                Disp_ProcessPrg.Value = 40;

            }
            if (doDispense)
            {
                // dispense cyanoacrylate


                DDMSettingsSingleSize m = motor;
                DDMSettingShotCalibration c = motor.shot_calibration;

                int id_valve = c.valve_num_id.Value;
                float id_time = c.time_id.Value;
                float id_x = m.disp_id.x.Value;
                float id_t = m.disp_id.t.Value;
                float id_pressure = c.pressure_1.Value;
                float id_targetVol = c.target_vol_id.Value;

                int od_valve = c.valve_num_od.Value;
                float od_time = c.time_od.Value;
                float od_x = m.disp_od.x.Value;
                float od_t = m.disp_od.t.Value;
                float od_pressure = c.pressure_2.Value;
                float od_targetVol = c.target_vol_od.Value;

                processData.AddToLog($"Using ID [{id_x}, {id_t}] for {id_time} seconds and OD [{od_x}, {od_t}] for {od_time} seconds");


                processData.AddToLog("Dispensing...");
                string response = await App.ControllerManager.DispenseToRing(id_valve, id_time, id_x, id_t, od_valve, od_time, od_x, od_t);
                Debug.Print(response);


                processData.results.shot_id = new DDMResultsShot()
                {
                    valve_num = motor.shot_calibration.valve_num_id,
                    vol = 0.005f,
                    time = motor.shot_calibration.time_id,
                    pressure = motor.shot_calibration.pressure_1
                };
                processData.results.shot_od = new DDMResultsShot()
                {
                    valve_num = motor.shot_calibration.valve_num_od,
                    vol = 0.006f,
                    time = motor.shot_calibration.time_od,
                    pressure = motor.shot_calibration.pressure_2
                };
                processData.AddToLog("Dispense complete");
                Disp_ProcessPrg.Value = 80;
            }
            if (doPostPhoto)
            {
                // take photo after process

                processData.AddToLog("Taking photo...");
                processData.AddToLog($"Moving to [{settings.common.camera_top.x}, {settings.common.camera_top.t}]");
                await Task.Delay(1000);
                processData.AddToLog("Photo saved");
                Disp_ProcessPrg.Value = 90;
            }



            processData.AddToLog("Moving back to unload position...");
            processData.AddToLog($"Moving to [{settings.common.load.x}, {settings.common.load.t}]");
            Disp_ProcessPrg.Value = 100;

            x = settings.common.load.x.Value;
            t = settings.common.load.t.Value;
            await App.ControllerManager.MoveJ(x, t);

            processData.AddToLog("Process complete");
            await Task.Delay(500);



            GoToStep(2);
        }

        private void GoToStep(int step)
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
            processData.AddToLog($"Moved to step {step}");
        }























        private void ControllerManager_ControllerStateChanged(object? sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void UpdateButtonLocks()
        {
            bool isConnected = App.UIManager.UI_STATE.isConnected;
            bool isAutoState = App.UIManager.UI_STATE.isAutoControllerStateRequesting;

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

            // set auto buttons
            Adv_Cell_AutoStartBtn.IsEnabled = !isAutoState && isConnected;
            Adv_Cell_AutoStopBtn.IsEnabled = isAutoState && isConnected;

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
            label.Content = value.ToString();

        }
        private void FormatReadout(Label label, float value, string unit)
        {
            label.Content = value.ToString() + " " + unit;

        }
        private void FormatReadout(Label label, bool value)
        {
            label.Content = value ? "Yes" : "No";
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
            DDMSettings s = App.SettingsManager.SETTINGS;
            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();


            if (m != null && m.IsValid())
            {
                LockRobotButtons(false);

                DDMSettingShotCalibration c = m.shot_calibration;

                Adv_Cell_MoveLoadInLbl.Content = $"[{s.common.load.x}, {s.common.load.t}]";
                Adv_Cell_MoveCamTopInLbl.Content = $"[{s.common.camera_top.x}, {s.common.camera_top.t}]";
                Adv_Cell_MoveCamSideInLbl.Content = $"[{m.camera_side.x}, {m.camera_side.t}]";
                Adv_Cell_MoveLaserRingInLbl.Content = $"[{m.laser_ring.x}, {m.laser_ring.t}]";
                Adv_Cell_MoveLaserMagInLbl.Content = $"[{m.laser_mag.x}, {m.laser_mag.t}]";
                Adv_Cell_MoveDispIDInLbl.Content = $"[{m.disp_id.x}, {m.disp_id.t}]";
                Adv_Cell_MoveDispODInLbl.Content = $"[{m.disp_od.x}, {m.disp_od.t}]";
                Adv_Cell_MoveSpinInLbl.Content = $"{c.spin_time}s, {c.spin_speed}%";

                Adv_Cell_MeasureRingInLbl.Content = $"{s.common.laser_ring_num} places, {s.common.laser_delay} s each";
                Adv_Cell_MeasureMagInLbl.Content = $"{s.common.laser_mag_num} places, {s.common.laser_delay} s each";

                Adv_Cell_DispShotsInLbl.Content = $"ID: Valve {c.valve_num_id}, x={m.disp_id.x} mm, {c.time_id} s, target {c.target_vol_id}mL\n";
                Adv_Cell_DispShotsInLbl.Content += $"OD: Valve {c.valve_num_od}, x={m.disp_od.x} mm, {c.time_od} s, target {c.target_vol_od} mL";
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

            Adv_Cell_TCSVersionLbl.Content = await App.ControllerManager.GetTCSVersion();
            Adv_Cell_PACVersionLbl.Content = await App.ControllerManager.GetPACVersion();

            PopulateMotorSettings(Adv_Cell_MotorSizeCmb);
        }

        public async void MainWindowSingle_OnDisconnected(object sender, EventArgs e)
        {
            Adv_Cell_ConnectedStatusTxt.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
            Adv_Cell_ConnectedStatusTxt.Text = "Not connected";
            Adv_Cell_AutoStatusTxt.Text = "-";

            Adv_Cell_TCSVersionLbl.Content = "-";
            Adv_Cell_PACVersionLbl.Content = "-";

            DisableAllReadouts();
            BlankOutMotorSettings();

        }

        public void MainWindowSingle_OnChangeControllerState(object sender, EventArgs e)
        {
            ControllerState contState = App.ControllerManager.CONTROLLER_STATE;

            if (!contState.parseError)
            {
                if (App.UIManager.UI_STATE.isConnected)
                {
                    Adv_Cell_AutoStatusTxt.Foreground = new BrushConverter().ConvertFrom("Black") as SolidColorBrush;
                    Adv_Cell_AutoStatusTxt.Text = "Parse OK";
                    FormatAllReadouts(contState);
                }
                else
                {
                    Adv_Cell_AutoStatusTxt.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
                    Adv_Cell_AutoStatusTxt.Text = "-";
                    DisableAllReadouts();
                }
            }
            else
            {
                Adv_Cell_AutoStatusTxt.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
                Adv_Cell_AutoStatusTxt.Text = $"Parse error: {contState.parseErrorMessage}";
                DisableAllReadouts();
            }
        }

        public void MainWindowSingle_OnChangeUIState(object sender, EventArgs e)
        {
            UpdateButtonLocks();
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
            DDMResultsLogLine logline = processData.results.process_log.Last();
            Disp_LogTxt.Text += logline.date?.ToString(processData.dateFormatShort) + ": " + logline.message + "\n";
            Disp_LogTxt.CaretIndex = Disp_LogTxt.Text.Length;
            Disp_LogTxt.ScrollToEnd();
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

            await App.ControllerManager.ConnectAsync(Con_IPTxt.Text);
            if (App.UIManager.UI_STATE.isConnected)
            {
                Con_ConnectBtn.Content = "Connected";
            }
            else
            {
                Con_ConnectBtn.IsEnabled = true;
            }
        }

        private async void Adv_Con_ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Con_ConnectBtn.IsEnabled = false;
            await App.ControllerManager.ConnectAsync(Adv_Con_IPTxt.Text);
            if (App.UIManager.UI_STATE.isConnected)
            {
                Adv_Con_ContVersionTxt.Content = await App.ControllerManager.GetTCSVersion();
            }
        }

        private async void Adv_Con_DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Con_DisconnectBtn.IsEnabled = false;
            await App.ControllerManager.DisconnectAsync();
            Adv_Con_ContVersionTxt.Content = "(no version info)";
        }

        private async void Adv_Con_StatusSendBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Con_StatusSendBtn.IsEnabled = false;
            string response = await App.ControllerManager.SendStatusCommandAsync(Adv_Con_StatusMsgTxt.Text);
            UpdateButtonLocks();
        }

        private async void Adv_Con_RobotSendBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Con_RobotSendBtn.IsEnabled = false;
            string response = await App.ControllerManager.SendRobotCommandAsync(Adv_Con_RobotMsgTxt.Text);
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
            DoProcess();
        }

        private void Disp_SaveLogBtn_Click(object sender, RoutedEventArgs e)
        {
            processData.SaveDataToFile();
        }

        private void Disp_ViewLogBtn_Click(object sender, RoutedEventArgs e)
        {
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(processData.GetLogAsString(), "Process Log");
            viewer.ShowDialog();
        }

        private void Disp_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            processData.OpenBrowserToDirectory();
        }






        private void Adv_Cell_OpenSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = App.SettingsManager.GetSettingsFilePath();
            System.Diagnostics.Process.Start("notepad.exe", folderPath);
        }
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

            DDMSettings s = App.SettingsManager.SETTINGS;
            float x = s.common.load.x.Value;
            float t = s.common.load.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveLoadOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveCamTopBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettings s = App.SettingsManager.SETTINGS;
            float x = s.common.camera_top.x.Value;
            float t = s.common.camera_top.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveCamTopOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveCamSideBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.camera_side.x.Value;
            float t = m.camera_side.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveCamSideOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveLaserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.laser_ring.x.Value;
            float t = m.laser_ring.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveLaserRingOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveLaserMagBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.laser_mag.x.Value;
            float t = m.laser_mag.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveLaserMagOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveDispIDBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.disp_id.x.Value;
            float t = m.disp_id.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveDispIDOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveDispODBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.disp_od.x.Value;
            float t = m.disp_od.t.Value;

            string response = await App.ControllerManager.MoveJ(x, t);
            Adv_Cell_MoveDispODOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveSpinBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float time = m.shot_calibration.spin_time.Value;
            float speed = m.shot_calibration.spin_speed.Value;

            string response = await App.ControllerManager.SpinInPlace(time, speed);
            Adv_Cell_MoveSpinOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_MeasureRingBtn_Click(object sender, RoutedEventArgs e)
        {
            DDMSettings s = App.SettingsManager.SETTINGS;
            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float xPos = m.laser_ring.x.Value;
            float tPos = m.laser_ring.t.Value;
            int n = s.common.laser_ring_num.Value;
            float d = s.common.laser_delay.Value;

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
            DDMSettings s = App.SettingsManager.SETTINGS;
            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float xPos = m.laser_mag.x.Value;
            float tPos = m.laser_mag.t.Value;
            int n = s.common.laser_mag_num.Value;
            float d = s.common.laser_delay.Value;

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
            for (int i = 0; i < laserRingData.Count; i++)
            {
                DDMResultsSingleHeight d = laserRingData[i];
                sb.AppendLine($"{d.t}, {d.z}");
            }
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(sb.ToString(), "Ring Displacement Measurements");
            viewer.Show();
        }

        private async void Adv_Cell_ShowMagBtn_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < laserMagData.Count; i++)
            {
                DDMResultsSingleHeight d = laserMagData[i];
                sb.AppendLine($"{d.t}, {d.z}");
            }
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(sb.ToString(), "Ring Displacement Measurements");
            viewer.Show();
        }

        private async void Adv_Cell_SetPres1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = Adv_Cell_SetPres1InTxt.Text;
            string response = await App.ControllerManager.SetRegPressure(1, float.Parse(input));
            Adv_Cell_SetPres1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_SetPres2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = Adv_Cell_SetPres2InTxt.Text;
            string response = await App.ControllerManager.SetRegPressure(2, float.Parse(input));
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

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            DDMSettingShotCalibration c = m.shot_calibration;

            float x_id = m.disp_id.x.Value;
            float t_id = m.disp_id.t.Value;
            float time_id = c.time_id.Value;
            float valve_num_id = c.valve_num_id.Value;
            float pressure_id = c.pressure_1.Value;
            float target_vol_id = c.target_vol_id.Value;

            float x_od = m.disp_od.x.Value;
            float t_od = m.disp_od.t.Value;
            float time_od = c.time_od.Value;
            float valve_num_od = c.valve_num_od.Value;
            float pressure_od = c.pressure_2.Value;
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
            result = await Task.Run(() => App.CameraManager.AcquireAndSave(acquiredImageDisplay));

            if (result.success)
            {
                Adv_Cam_StatusLbl.Content = "Image acquired";
                App.CameraManager.DisplayImage(acquiredImageDisplay, result.filePath);

            }
            else
            {
                Adv_Cam_StatusLbl.Content = $"Error: {result.errorMsg}";
            }
        }

        private void Adv_Cam_AcqurieSideBtn_Click(object sender, RoutedEventArgs e)
        {
            App.CameraManager.OpenExplorerToImages();
        }
    }
}
