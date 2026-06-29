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

            CellControlPanel.RefreshMotorSettings();
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

            CellControlPanel.ClearMotorSettings();
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
            }
        }

        private void HandleConnectionLogUpdated()
        {
            if (_controllerManager == null) return;

            Con_LogTxt.Text = _controllerManager.GetConnectionLog();
            Con_LogTxt.ScrollToEnd();
        }

        /// <summary>
        /// Initialize UI state
        /// </summary>
        private void InitializeUI()
        {
            Status_GUISimBdr.Visibility = _applicationConfiguration?.IsSimulationMode == true ? Visibility.Visible : Visibility.Collapsed;


            Status_SimBdr.Visibility = Visibility.Collapsed;
            Adv_PWEntryBdr.Visibility = Visibility.Visible;
            Adv_AllControlsTcl.Visibility = Visibility.Collapsed;
            AdvTab.Visibility = Visibility.Collapsed;
        }

        #region UI State Management



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

        #endregion

        #region Connection Button Handlers

        /// <summary>
        /// FIXED: Directly use ControllerManager instead of DeviceConnectionManager.
        /// The connection orchestration is now handled via the ControllerManager directly.
        /// </summary>
        private async void Con_ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            Con_ConnectBtn.IsEnabled = false;
            Con_ConnectBtn.Content = "Connecting...";
            Con_ConnectPrg.Visibility = Visibility.Visible;

            // Connect directly through ControllerManager
            await _controllerManager.Connect(Con_IPTxt.Text);

            Con_ConnectPrg.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Dispense Button Handlers

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

        #endregion

        #region Robot Control Button Handlers

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
        




        private void Serv_PWSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_applicationConfiguration == null) return;

            if (Serv_PWBox.Password == _applicationConfiguration.ServicePassword)
            {
                Serv_PWEntryBdr.Visibility = Visibility.Collapsed;
                Serv_PWMessageTxb.Visibility = Visibility.Collapsed;
                Serv_Grid.Visibility = Visibility.Visible;
            }
            else
            {
                Serv_PWMessageTxb.Visibility = Visibility.Visible;
                Serv_PWMessageTxb.Text = "Incorrect password";
            }
        }

        private void Serv_PWBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Serv_PWSubmitBtn_Click(sender, e);
            }
        }





        private void Calib_PWSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_applicationConfiguration == null) return;

            if (Calib_PWBox.Password == _applicationConfiguration.CalibrationPassword)
            {
                Calib_PWEntryBdr.Visibility = Visibility.Collapsed;
                Calib_PWMessageTxb.Visibility = Visibility.Collapsed;
                Calib_Grid.Visibility = Visibility.Visible;
            }
            else
            {
                Calib_PWMessageTxb.Visibility = Visibility.Visible;
                Calib_PWMessageTxb.Text = "Incorrect password";
            }
        }

        private void Calib_PWBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Calib_PWSubmitBtn_Click(sender, e);
            }
        }





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

        private void MotorSizeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && DataContext is MainWindowViewModel vm)
            {
                vm.SelectedMotorType = rb.Tag as string;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Release the ViewModel's subscriptions to the long-lived singleton services.
            (DataContext as IDisposable)?.Dispose();
        }
    }
}
