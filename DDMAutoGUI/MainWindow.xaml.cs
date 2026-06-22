using DDMAutoGUI.CustomWindows;
using DDMAutoGUI.Services;
using DDMAutoGUI.Utilities;
using DDMAutoGUI.ViewModels;
using DDMAutoGUI.windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DDMAutoGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IControllerManager _controllerManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IApplicationConfiguration _applicationConfiguration;
        private List<Button> allButtons;
        private List<ResultsHeightMeasurement> laserRingData;
        private List<ResultsHeightMeasurement> laserMagData;

        public MainWindow()
        {
            InitializeComponent();

            // Get services from DI container
            _controllerManager = App.Services?.GetService<IControllerManager>();
            _settingsManager = App.Services?.GetService<ISettingsManager>();
            _applicationConfiguration = App.Services?.GetService<IApplicationConfiguration>();

            // Set the DataContext to the ViewModel via Dependency Injection
            var viewModel = App.Services?.GetService<MainWindowViewModel>();
            if (viewModel != null)
            {
                this.DataContext = viewModel;
            }
            else
            {
                MessageBox.Show("Error: Failed to resolve MainWindowViewModel from DI container.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            InitializeEventHandlers();

            // Initialize UI - remove static App references
            this.Title += " " + ((MainWindowViewModel)this.DataContext)?.AppTitle ?? "DDM Auto GUI";
            InitializeUI();
        }

        private void InitializeEventHandlers()
        {
            // Use injected controller manager
            if (_controllerManager == null) return;

            _controllerManager.ControllerConnected += (s, e) => HandleConnected();
            _controllerManager.ControllerDisconnected += (s, e) => HandleDisconnected();
            _controllerManager.ControllerStateChanged += (s, e) => HandleControllerStateChanged();
            _controllerManager.ConnectionLogUpdated += (s, e) => HandleConnectionLogUpdated();
            _controllerManager.StatusLogUpdated += (s, e) => HandleStatusLogUpdated();
            _controllerManager.RobotLogUpdated += (s, e) => HandleRobotLogUpdated();
            _controllerManager.ConnectionStateChanged += (s, e) => HandleConnectionStateChanged();
        }

        private void HandleConnected()
        {
            if (_controllerManager?.CONNECTION_STATE == null) return;

            string TCS = _controllerManager.CONNECTION_STATE.connectedTCS;
            string PAC = _controllerManager.CONNECTION_STATE.connectedPAC;

            Con_ConnectBtn.Content = "Connected";
            Con_ConnectBtn.IsEnabled = false;

            Status_StatusTxt.Text = $"Connected ({_controllerManager.CONNECTION_STATE.connectedIP})";
            Status_TCSGrd.Visibility = Visibility.Visible;
            Status_TCSTxt.Text = TCS;
            Status_PACGrd.Visibility = Visibility.Visible;
            Status_PACTxt.Text = PAC;

            DispTab.IsEnabled = true;
            CalibTab.IsEnabled = true;
            ServTab.IsEnabled = true;

            PopulateMotorSettings(Adv_Cell_MotorSizeCmb);
        }

        private void HandleDisconnected()
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

        private void HandleControllerStateChanged()
        {
            if (_controllerManager?.CONTROLLER_STATE == null) return;

            ControllerState contState = _controllerManager.CONTROLLER_STATE;

            Status_SimBdr.Visibility = Visibility.Collapsed;
            if (!contState.parseError && _controllerManager.CONNECTION_STATE.isConnected)
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
                DisableAllReadouts();
            }
        }

        private void HandleConnectionStateChanged()
        {
            UpdateButtonLocks();
        }

        private void HandleConnectionLogUpdated()
        {
            if (_controllerManager == null) return;

            Con_LogTxt.Text = _controllerManager.GetConnectionLog();
            Con_LogTxt.ScrollToEnd();
        }

        private void HandleStatusLogUpdated()
        {
            if (_controllerManager == null) return;

            Adv_Con_StatusLogTxt.Text = _controllerManager.GetStatusLog();
            Adv_Con_StatusLogTxt.ScrollToEnd();
        }

        private void HandleRobotLogUpdated()
        {
            if (_controllerManager == null) return;

            Adv_Con_RobotLogTxt.Text = _controllerManager.GetRobotLog();
            Adv_Con_RobotLogTxt.ScrollToEnd();
        }

        /// <summary>
        /// Initialize UI state
        /// </summary>
        private void InitializeUI()
        {
            Status_GUISimBdr.Visibility = _applicationConfiguration?.IsSimulationMode == true ? Visibility.Visible : Visibility.Collapsed;

            allButtons = new List<Button>()
            {
                Con_ConnectBtn,
                Adv_Con_ConnectBtn,
                Adv_Con_DisconnectBtn,
                Adv_Con_StatusSendBtn,
                Adv_Con_RobotSendBtn,
            };

            UpdateButtonLocks();
            Disp_BeginBtn.IsEnabled = false;

            Status_SimBdr.Visibility = Visibility.Collapsed;
            Adv_PWEntryBdr.Visibility = Visibility.Visible;
            Adv_AllControlsTcl.Visibility = Visibility.Collapsed;
            Disp_ProcessPrg.Value = 0;

            AdvTab.Visibility = Visibility.Collapsed;
        }

        #region UI State Management

        public void UpdateButtonLocks()
        {
            bool isConnected = _controllerManager?.CONNECTION_STATE?.isConnected ?? false;

            // Enable/disable buttons based on connection state
            foreach (Button b in allButtons)
            {
                b.IsEnabled = isConnected;
            }

            LockRobotButtons(!isConnected);

            // Set connect/disconnect button states
            Con_ConnectBtn.IsEnabled = !isConnected;
            Adv_Con_ConnectBtn.IsEnabled = !isConnected;
            Adv_Con_DisconnectBtn.IsEnabled = isConnected;
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
            if (_settingsManager == null) return;

            CellSettings s = _settingsManager.GetAllSettings();
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();

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
            }
            else
            {
                LockRobotButtons(true);
                BlankOutMotorSettings();
            }
        }

        private void FormatReadout(Label label, float value)
        {
            label.Foreground = new System.Windows.Media.BrushConverter().ConvertFrom("#000") as System.Windows.Media.SolidColorBrush;
            label.Content = value.ToString();
        }

        private void FormatReadout(Label label, float value, string unit)
        {
            label.Foreground = new System.Windows.Media.BrushConverter().ConvertFrom("#000") as System.Windows.Media.SolidColorBrush;
            label.Content = value.ToString() + " " + unit;
        }

        private void FormatReadout(Label label, bool value)
        {
            label.Content = value ? "Yes" : "No";
            label.Foreground = new System.Windows.Media.BrushConverter().ConvertFrom("#000") as System.Windows.Media.SolidColorBrush;
            label.Background = value
                ? new System.Windows.Media.BrushConverter().ConvertFrom("#ffd3ddf5") as System.Windows.Media.SolidColorBrush
                : new System.Windows.Media.BrushConverter().ConvertFrom("WhiteSmoke") as System.Windows.Media.SolidColorBrush;
        }

        private void FormatAllReadouts(ControllerState contState)
        {
            FormatReadout(roPowerEnabled, contState.isPowerEnabled);
            FormatReadout(roRobotHomed, contState.isRobotHomed);
            FormatReadout(roLinPos, contState.posLinear);
            FormatReadout(roRotPos, contState.posRotary);
            FormatReadout(roLinFlag1, !contState.isLinearIn1);
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
            label.Foreground = new System.Windows.Media.BrushConverter().ConvertFrom("#AAA") as System.Windows.Media.SolidColorBrush;
            label.Background = new System.Windows.Media.BrushConverter().ConvertFrom("WhiteSmoke") as System.Windows.Media.SolidColorBrush;
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

        /// <summary>
        /// Handle dispense errors - allows override if enabled in advanced options
        /// </summary>
        private void ThrowDispenseError(string message)
        {
            if (_applicationConfiguration?.AdvancedOptions?.DispenseOptions?.OverrideWarnings == true)
            {
                string cap = "Override Dispense Error?";
                string msg = $"{message}\n\nContinue anyway?\n\n'OK' will continue; 'Cancel' will end process.";
                MessageBoxResult mb = MessageBox.Show(msg, cap, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (mb == MessageBoxResult.OK) return;
            }
            throw new Exception(message);
        }

        #endregion

        #region Event Handlers - Manager Events

        public void MainWindowSingle_Disp_UpdateProcessLog(object sender, EventArgs e)
        {
            var resultsManager = App.Services?.GetService<IResultsManager>();
            if (resultsManager?.currentResults?.process_log == null) return;

            ResultsLogLine logline = resultsManager.currentResults.process_log.Last();
            Disp_LogTxt.Text += logline.timestamp?.ToString(resultsManager.DateFormatShort) + ": " + logline.message + "\n";
            Disp_LogTxt.CaretIndex = Disp_LogTxt.Text.Length;
            Disp_LogTxt.ScrollToEnd();
        }

        #endregion

        #region Event Handlers - UI Events

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tc)
            {
                switch (tc.SelectedIndex)
                {
                    case 1:
                        // Dispense Tab - no special initialization needed now
                        break;
                    case 2:
                        // Calibration Tab
                        CalPanel.SetupPanel();
                        break;
                }
            }
        }

        private void Adv_Cell_MotorSizeCmb_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                PopulateMotorSettings(Adv_Cell_MotorSizeCmb);
            }
        }

        private void PopulateMotorSettings(ComboBox selection)
        {
            if (_settingsManager == null) return;

            _settingsManager.SelectedSize = SizeEnumFromIdx(selection.SelectedIndex);
            DisplaySettingsToPanel();
        }

        #endregion

        #region Connection Button Handlers

        /// <summary>
        /// FIXED: Directly use ControllerManager instead of DeviceConnectionManager.
        /// The connection orchestration is now handled via the ControllerManager directly.
        /// </summary>
        private async void Con_ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LoadAdvancedOptions();
            Con_ConnectBtn.IsEnabled = false;
            Con_ConnectBtn.Content = "Connecting...";
            Con_ConnectPrg.Visibility = Visibility.Visible;

            // Connect directly through ControllerManager
            await _controllerManager.Connect(Con_IPTxt.Text);

            Con_ConnectPrg.Visibility = Visibility.Collapsed;
        }

        private async void Adv_Con_ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LoadAdvancedOptions();
            Adv_Con_ConnectBtn.IsEnabled = false;
            await _controllerManager.Connect(Adv_Con_IPTxt.Text);

            if (_controllerManager.CONNECTION_STATE.isConnected)
            {
                Adv_Con_ContVersionTxt.Content = await _controllerManager.GetTCSVersion();
            }
        }

        private async void Adv_Con_DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            Adv_Con_DisconnectBtn.IsEnabled = false;
            await _controllerManager.Disconnect();
            Adv_Con_ContVersionTxt.Content = "(no version info)";
        }

        private async void Adv_Con_StatusSendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            Adv_Con_StatusSendBtn.IsEnabled = false;
            await _controllerManager.SendStatusCommand(Adv_Con_StatusMsgTxt.Text);
            UpdateButtonLocks();
        }

        private async void Adv_Con_RobotSendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            Adv_Con_RobotSendBtn.IsEnabled = false;
            await _controllerManager.SendRobotCommand(Adv_Con_RobotMsgTxt.Text);
            UpdateButtonLocks();
        }

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

        #endregion

        #region Dispense Button Handlers

        private void Disp_BeginBtn_Click(object sender, RoutedEventArgs e)
        {
            // Dispense logic now delegated to ViewModel via data binding
            // Ensure MotorSerialNumber is populated and bind button to StartDispenseCommand
        }

        private void Disp_SaveLogBtn_Click(object sender, RoutedEventArgs e)
        {
            var resultsManager = App.Services?.GetService<IResultsManager>();
            if (resultsManager != null)
            {
                resultsManager.SaveDataToFile();
            }
        }

        private void Disp_ViewLogBtn_Click(object sender, RoutedEventArgs e)
        {
            var resultsManager = App.Services?.GetService<IResultsManager>();
            if (resultsManager != null)
            {
                TextDataViewer viewer = new TextDataViewer();
                string log = resultsManager.GetLogAsString();
                if (log != null)
                {
                    viewer.Owner = this;
                    viewer.PopulateData(log, "Process Log");
                    viewer.ShowDialog();
                }
            }
        }

        private void Disp_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var resultsManager = App.Services?.GetService<IResultsManager>();
            if (resultsManager != null)
            {
                resultsManager.OpenBrowserToDirectory();
            }
        }

        private void Disp_Res_FinishBtn_Click(object sender, RoutedEventArgs e)
        {
            dispTabControl.SelectedIndex = 0;
        }

        private void Disp_Res_ViewResBtn_Click(object sender, RoutedEventArgs e)
        {
            var resultsManager = App.Services?.GetService<IResultsManager>();
            if (resultsManager == null) return;

            string data_string = resultsManager.GetCurrentResultsAsString();
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
            var resultsManager = App.Services?.GetService<IResultsManager>();
            if (resultsManager != null)
            {
                resultsManager.OpenBrowserToDirectory();
            }
        }

        private void Disp_MotorSNTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                bool state = tb.Text.Length > 0;
                Disp_BeginBtn.IsEnabled = state;
                Disp_Warning_SNBox.Visibility = !state ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Robot Control Button Handlers

        private void Adv_Cell_AutoStartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager != null)
            {
                _controllerManager.StartAutoControllerState();
            }
        }

        private void Adv_Cell_AutoStopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager != null)
            {
                _controllerManager.StopAutoControllerState();
            }
        }

        private async void Adv_Cell_EnableBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.EnablePower();
            Adv_Cell_EnableOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private void Adv_Cell_HomeBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_Cell_HomeOutLbl.Content = "(not implemented)";
        }

        private async void Adv_Cell_MoveLoadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CellSettings s = _settingsManager.GetAllSettings();
            string response = await _controllerManager.MoveJ(s.ddm_common.load.x.Value, s.ddm_common.load.t.Value);
            Adv_Cell_MoveLoadOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveCamTopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CellSettings s = _settingsManager.GetAllSettings();
            string response = await _controllerManager.MoveJ(s.ddm_common.camera_top.x.Value, s.ddm_common.camera_top.t.Value);
            Adv_Cell_MoveCamTopOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveCamSideBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MoveJ(m.camera_side.x.Value, m.camera_side.t.Value);
            Adv_Cell_MoveCamSideOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveLaserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MoveJ(m.laser_ring.x.Value, m.laser_ring.t.Value);
            Adv_Cell_MoveLaserRingOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveLaserMagBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MoveJ(m.laser_mag.x.Value, m.laser_mag.t.Value);
            Adv_Cell_MoveLaserMagOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveDispIDBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MoveJ(m.id_disp.x.Value, m.id_disp.t.Value);
            Adv_Cell_MoveDispIDOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveDispODBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MoveJ(m.od_disp.x.Value, m.od_disp.t.Value);
            Adv_Cell_MoveDispODOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MoveHallBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MoveJ(m.hall_sensor.x.Value, m.hall_sensor.t.Value);
            Adv_Cell_MoveHallOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MeasureRingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MeasureHeightsContinuous(m.laser_ring.x.Value, m.laser_ring.t.Value, m.laser_ring_num.Value, 10);
            laserRingData = _controllerManager.ParseHeightData(response);
            Adv_Cell_MeasureRingOutLbl.Content = laserRingData.Count > 0 ? "(data collected)" : $"error: {response}";
            LockRobotButtons(false);
        }

        private async void Adv_Cell_MeasureMagBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MeasureHeightsContinuous(m.laser_mag.x.Value, m.laser_mag.t.Value, m.laser_mag_num.Value, 20);
            laserMagData = _controllerManager.ParseHeightData(response);
            Adv_Cell_MeasureMagOutLbl.Content = laserMagData.Count > 0 ? "(data collected)" : $"error: {response}";
            LockRobotButtons(false);
        }

        private void Adv_Cell_ShowRingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (laserRingData != null && laserRingData.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var d in laserRingData)
                {
                    sb.AppendLine($"{d.t}, {d.z}");
                }
                TextDataViewer viewer = new TextDataViewer { Owner = this };
                viewer.PopulateData(sb.ToString(), "Ring Displacement Measurements");
                viewer.Show();
            }
        }

        private void Adv_Cell_ShowMagBtn_Click(object sender, RoutedEventArgs e)
        {
            if (laserMagData != null && laserMagData.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var d in laserMagData)
                {
                    sb.AppendLine($"{d.t}, {d.z}");
                }
                TextDataViewer viewer = new TextDataViewer { Owner = this };
                viewer.PopulateData(sb.ToString(), "Magnet Displacement Measurements");
                viewer.Show();
            }
        }

        private async void Adv_Cell_MeasureSingleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.MeasureHeightSingle();
            Adv_Cell_MeasureSingleOutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_SetPres1Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.SetRegPressureAndWait(1, float.Parse(Adv_Cell_SetPres1InTxt.Text), 10);
            Adv_Cell_SetPres1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_SetPres2Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.SetRegPressureAndWait(2, float.Parse(Adv_Cell_SetPres2InTxt.Text), 10);
            Adv_Cell_SetPres2OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_Shot1Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.MeasureShotTimed(1, float.Parse(Adv_Cell_Shot1InTxt.Text));
            Adv_Cell_Shot1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_Shot2Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.MeasureShotTimed(2, float.Parse(Adv_Cell_Shot2InTxt.Text));
            Adv_Cell_Shot2OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_SetZeroBothBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.SetZeroShift(3.0f);
            Adv_Cell_SetZeroBothLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_StartMeas1Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.SetShotTrigger(1, true);
            Adv_Cell_StartMeas1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_StopMeas1Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.SetShotTrigger(1, false);
            Adv_Cell_StopMeas1OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_StartMeas2Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.SetShotTrigger(2, true);
            Adv_Cell_StartMeas2OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private async void Adv_Cell_StopMeas2Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            LockRobotButtons(true);
            string response = await _controllerManager.SetShotTrigger(2, false);
            Adv_Cell_StopMeas2OutLbl.Content = response;
            LockRobotButtons(false);
        }

        private void Adv_Cell_DispShotsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            LockRobotButtons(true);

            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            CSShot c = m.shot_settings;

            float x_id = m.id_disp.x.Value;
            float t_id = m.id_disp.t.Value;
            float valve_num_id = c.id_sys_num.Value;
            float target_vol_id = c.id_target_vol.Value;

            float x_od = m.od_disp.x.Value;
            float t_od = m.od_disp.t.Value;
            float valve_num_od = c.od_sys_num.Value;
            float target_vol_od = c.od_target_vol.Value;

            string response = string.Empty;
            Adv_Cell_DispShotsOutLbl.Content = response;

            LockRobotButtons(false);
        }

        private async void Adv_Cell_EStopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager != null)
            {
                await _controllerManager.EStop();
            }
        }

        private async void Adv_Cell_ECloseValvesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager != null)
            {
                await _controllerManager.CloseAllValves();
            }
        }

        #endregion

        #region Camera Button Handlers

        private async void Adv_Cam_AcquireTopBtn_Click(object sender, RoutedEventArgs e)
        {
            var cameraManager = App.Services?.GetService<ICameraManager>();
            if (cameraManager == null) return;

            acquiredImageDisplay.Source = null;
            Adv_Cam_StatusLbl.Content = "Acquiring image...";

            CameraAcquisitionResult result = await cameraManager.AcquireAndSave(CameraManager.CellCamera.top, acquiredImageDisplay);

            if (result.success)
            {
                Adv_Cam_StatusLbl.Content = "Top image acquired";
                cameraManager.DisplayImage(acquiredImageDisplay, result.filePath);
            }
            else
            {
                Adv_Cam_StatusLbl.Content = $"Error: {result.errorMsg}";
            }
        }

        private async void Adv_Cam_AcquireSideBtn_Click(object sender, RoutedEventArgs e)
        {
            var cameraManager = App.Services?.GetService<ICameraManager>();
            if (cameraManager == null) return;

            acquiredImageDisplay.Source = null;
            Adv_Cam_StatusLbl.Content = "Acquiring image...";

            CameraAcquisitionResult result = await cameraManager.AcquireAndSave(CameraManager.CellCamera.side, acquiredImageDisplay);

            if (result.success)
            {
                Adv_Cam_StatusLbl.Content = "Side image acquired";
                cameraManager.DisplayImage(acquiredImageDisplay, result.filePath);
            }
            else
            {
                Adv_Cam_StatusLbl.Content = $"Error: {result.errorMsg}";
            }
        }

        /// <summary>
        /// FIXED: Use static OCRManager class with RunOCRAsync method instead of deprecated OCRUtilities.
        /// </summary>
        private async void Adv_Cam_RunOCR_Click(object sender, RoutedEventArgs e)
        {
            Adv_Cam_OCRPrg.Visibility = Visibility.Visible;

            OCRData data = await OCRManager.RunOCRAsync(Adv_Cam_OCRPathTxb.Text);
            Adv_Cam_OCRResTxb.Text = data != null
                ? JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true })
                : "Error reading OCR results";

            Adv_Cam_OCRPrg.Visibility = Visibility.Collapsed;
        }

        private void Adv_Cam_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var cameraManager = App.Services?.GetService<ICameraManager>();
            if (cameraManager != null)
            {
                cameraManager.OpenExplorerToImages();
            }
        }

        #endregion

        #region Settings & Options

        private void LoadAdvancedOptions()
        {
            if (_applicationConfiguration == null) return;

            _applicationConfiguration.AdvancedOptions.ConnectionOptions.Controller = Adv_Opt_Con_ControllerChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.ConnectionOptions.IoLinkDevices = Adv_Opt_Con_IOLinkChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.ConnectionOptions.TopCamera = Adv_Opt_Con_TopCamChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.ConnectionOptions.SideCamera = Adv_Opt_Con_SideCamChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.ConnectionOptions.LaserSensor = Adv_Opt_Con_LaserChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.ConnectionOptions.DaqDevice = Adv_Opt_Con_DAQChk.IsChecked ?? false;

            _applicationConfiguration.AdvancedOptions.DispenseOptions.CheckHealth = Adv_Opt_Disp_HealthChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.PhotoTop = Adv_Opt_Disp_TopPhotoChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.PhotoSide = Adv_Opt_Disp_SidePhotoChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.RunOCR = Adv_Opt_Disp_RunOCRChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.MeasureHeights = Adv_Opt_Disp_RingHeightChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.Dispense = Adv_Opt_Disp_DispChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.Autocalibrate = Adv_Opt_Disp_AutoCalibChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.CheckPolarity = Adv_Opt_Disp_MagPolChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.PhotoTopAfter = Adv_Opt_Disp_TopPhotoAfterChk.IsChecked ?? false;
            _applicationConfiguration.AdvancedOptions.DispenseOptions.OverrideWarnings = Adv_Opt_Disp_OverrideChk.IsChecked ?? false;
        }

        private void Adv_Cell_ReloadSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsManager == null) return;

            _settingsManager.ReloadSettings();
            PopulateMotorSettings(Adv_Cell_MotorSizeCmb);
        }

        private int SizeIdxFromEnum(SettingsManager.DDMSize size) => size switch
        {
            SettingsManager.DDMSize.ddm_57 => 0,
            SettingsManager.DDMSize.ddm_95 => 1,
            SettingsManager.DDMSize.ddm_116 => 2,
            SettingsManager.DDMSize.ddm_170 => 3,
            SettingsManager.DDMSize.ddm_170_tall => 4,
            _ => -1
        };

        private SettingsManager.DDMSize SizeEnumFromIdx(int idx) => idx switch
        {
            0 => SettingsManager.DDMSize.ddm_57,
            1 => SettingsManager.DDMSize.ddm_95,
            2 => SettingsManager.DDMSize.ddm_116,
            3 => SettingsManager.DDMSize.ddm_170,
            4 => SettingsManager.DDMSize.ddm_170_tall,
            _ => SettingsManager.DDMSize.none
        };

        #endregion

        #region Advanced Settings

        private void Adv_PWSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_applicationConfiguration == null) return;

            if (Adv_PWBox.Password == _applicationConfiguration.AdvancedSettingsPassword)
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

        private void Adv_Misc_LockAdvBtn_Click(object sender, RoutedEventArgs e)
        {
            Adv_PWBox.Clear();
            Adv_PWEntryBdr.Visibility = Visibility.Visible;
            Adv_PWMessageTxb.Visibility = Visibility.Collapsed;
            Adv_AllControlsTcl.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// FIXED: Use static DAQUtilities class instead of deprecated IDAQManager interface.
        /// </summary>
        private async void Adv_Misc_TestMatlabBtn_Click(object sender, RoutedEventArgs e)
        {
            await DAQUtilities.CollectDataAndProcessML("ddm_116");
        }

        #endregion

        #region Developer Utilities

        private void Dev_Btn_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 5;
            Adv_PWBox.Focus();
        }

        private void Adv_DAQ_GetA0Btn_Click(object sender, RoutedEventArgs e)
        {
            // DAQ voltage reading - implement as needed
        }

        private void Adv_DAQ_GetA0TimedBtn_Click(object sender, RoutedEventArgs e)
        {
            // DAQ timed reading - implement as needed
        }

        private void Adv_Opt_Disp_Force57Chk_Checked(object sender, RoutedEventArgs e) => Disp_Motor57.IsEnabled = true;
        private void Adv_Opt_Disp_Force57Chk_Unchecked(object sender, RoutedEventArgs e) => Disp_Motor57.IsEnabled = false;
        private void Adv_Opt_Disp_Force95Chk_Checked(object sender, RoutedEventArgs e) => Disp_Motor95.IsEnabled = true;
        private void Adv_Opt_Disp_Force95Chk_Unchecked(object sender, RoutedEventArgs e) => Disp_Motor95.IsEnabled = false;
        private void Adv_Opt_Disp_Force116Chk_Checked(object sender, RoutedEventArgs e) => Disp_Motor116.IsEnabled = true;
        private void Adv_Opt_Disp_Force116Chk_Unchecked(object sender, RoutedEventArgs e) => Disp_Motor116.IsEnabled = false;
        private void Adv_Opt_Disp_Force170Chk_Checked(object sender, RoutedEventArgs e) => Disp_Motor170.IsEnabled = true;
        private void Adv_Opt_Disp_Force170Chk_Unchecked(object sender, RoutedEventArgs e) => Disp_Motor170.IsEnabled = false;
        private void Adv_Opt_Disp_Force170TChk_Checked(object sender, RoutedEventArgs e) => Disp_Motor170Tall.IsEnabled = true;
        private void Adv_Opt_Disp_Force170TChk_Unchecked(object sender, RoutedEventArgs e) => Disp_Motor170Tall.IsEnabled = false;

        #endregion
    }
}
