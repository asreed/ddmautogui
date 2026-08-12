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
        private readonly IControllerService _controllerService;
        private readonly IApplicationConfiguration _applicationConfiguration;

        public MainWindow()
        {
            InitializeComponent();

            // Get services from DI container
            _controllerService = App.Services?.GetService<IControllerService>();
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
            this.Title = ((MainWindowViewModel)this.DataContext)?.AppTitle;
            InitializeUI();
        }

        private void InitializeEventHandlers()
        {
            // Use injected controller manager
            if (_controllerService == null) return;

            _controllerService.ControllerConnected += (s, e) => HandleConnected();
            _controllerService.ControllerDisconnected += (s, e) => HandleDisconnected();
            _controllerService.ControllerStateChanged += (s, e) => HandleControllerStateChanged();
        }

        private void HandleConnected()
        {
            if (_controllerService?.CONNECTION_STATE == null) return;

            DispTab.IsEnabled = true;
            CalibTab.IsEnabled = true;
            ServTab.IsEnabled = true;

            CellControlPanel.RefreshMotorSettings();
        }

        private void HandleDisconnected()
        {
            Alert_MsgBarBdr.Visibility = Visibility.Collapsed;

            DispTab.IsEnabled = false;
            CalibTab.IsEnabled = false;
            ServTab.IsEnabled = false;

            CellControlPanel.ClearMotorSettings();
        }

        private void HandleControllerStateChanged()
        {
            if (_controllerService?.CONTROLLER_STATE == null) return;

            ControllerState contState = _controllerService.CONTROLLER_STATE;

            if (!contState.parseError && _controllerService.CONNECTION_STATE.isConnected)
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
            }
        }



        /// <summary>
        /// Initialize UI state
        /// </summary>
        private void InitializeUI()
        {
            //Status_GUISimBdr.Visibility = _applicationConfiguration?.IsSimulationMode == true ? Visibility.Visible : Visibility.Collapsed;

            //Status_SimBdr.Visibility = Visibility.Collapsed;
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
            if (_applicationConfiguration?.AdvancedOptions?.PartCycleOptions?.OverrideWarnings == true)
            {
                string cap = "Override Dispense Error?";
                string msg = $"{message}\n\nContinue anyway?\n\n'OK' will continue; 'Cancel' will end process.";
                MessageBoxResult mb = MessageBox.Show(msg, cap, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (mb == MessageBoxResult.OK) return;
            }
            throw new Exception(message);
        }

        #endregion



        #region Event Handlers - UI Events

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tc)
            {
                // Re-lock the Calibration and Service areas on every navigation so they
                // require the password again next time. The Advanced/dev area is
                // intentionally excluded — it stays unlocked until manually locked.
                Lock(Calib_PWBox, Calib_PWEntryBdr, Calib_PWMessageTxb, Calib_Panel);
                Lock(Serv_PWBox, Serv_PWEntryBdr, Serv_PWMessageTxb, Serv_Panel);

                switch (tc.SelectedIndex)
                {
                    case 1:
                        // Dispense Tab - no special initialization needed now
                        break;
                    case 2:
                        // Calibration Tab
                        Calib_Panel.SetupPanel();
                        break;
                }
            }
        }
        

        #endregion


        private void Serv_PWSubmitBtn_Click(object sender, RoutedEventArgs e)
            => TryUnlock(Serv_PWBox, _applicationConfiguration?.ServicePassword,
                         Serv_PWEntryBdr, Serv_PWMessageTxb, Serv_Panel);

        private void Calib_PWSubmitBtn_Click(object sender, RoutedEventArgs e)
            => TryUnlock(Calib_PWBox, _applicationConfiguration?.CalibrationPassword,
                         Calib_PWEntryBdr, Calib_PWMessageTxb, Calib_Panel);
        private void Adv_PWSubmitBtn_Click(object sender, RoutedEventArgs e)
            => TryUnlock(Adv_PWBox, _applicationConfiguration?.AdvancedSettingsPassword,
                         Adv_PWEntryBdr, Adv_PWMessageTxb, Adv_AllControlsTcl);

        private void Adv_Misc_LockAdvBtn_Click(object sender, RoutedEventArgs e)
            => Lock(Adv_PWBox, Adv_PWEntryBdr, Adv_PWMessageTxb, Adv_AllControlsTcl);


        /// <summary>
        /// FIXED: Use static DAQUtilities class instead of deprecated IDAQManager interface.
        /// </summary>
        private async void Adv_Misc_TestMatlabBtn_Click(object sender, RoutedEventArgs e)
        {
            await DAQUtilities.CollectDataAndProcessML("ddm_116");
        }



        #region Developer Utilities

        private void Dev_Btn_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 4;
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

        #endregion



        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Release the ViewModel's subscriptions to the long-lived singleton services.
            (DataContext as IDisposable)?.Dispose();
        }

        /// <summary>
        /// Validates a password entry against the expected value and toggles the
        /// associated lock/content visibility. Centralizes the identical logic used
        /// by the Service, Calibration, and Advanced Settings gates.
        /// </summary>
        private void TryUnlock(
            PasswordBox passwordBox,
            string expectedPassword,
            UIElement entryBorder,
            TextBlock messageText,
            UIElement protectedContent)
        {
            if (_applicationConfiguration == null) return;

            if (passwordBox.Password == expectedPassword)
            {
                entryBorder.Visibility = Visibility.Collapsed;
                messageText.Visibility = Visibility.Collapsed;
                protectedContent.Visibility = Visibility.Visible;
            }
            else
            {
                messageText.Visibility = Visibility.Visible;
                messageText.Text = "Incorrect password";
            }
        }

        /// <summary>
        /// Resets a password gate to its locked state: clears the entry, shows the
        /// lock prompt, and hides the protected content. Symmetric counterpart to
        /// <see cref="TryUnlock"/>.
        /// </summary>
        private void Lock(
            PasswordBox passwordBox,
            UIElement entryBorder,
            TextBlock messageText,
            UIElement protectedContent)
        {
            passwordBox.Clear();
            entryBorder.Visibility = Visibility.Visible;
            messageText.Visibility = Visibility.Collapsed;
            protectedContent.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            if (sender == Serv_PWBox)       Serv_PWSubmitBtn_Click(sender, e);
            else if (sender == Calib_PWBox) Calib_PWSubmitBtn_Click(sender, e);
            else if (sender == Adv_PWBox)   Adv_PWSubmitBtn_Click(sender, e);
        }

        private async void Adv_DAQ_TestConnectionBtn_Click(object sender, RoutedEventArgs e)
        {
            var daqService = App.Services?.GetService<IDaqService>();
            if (daqService == null)
            {
                SetDaqResult("DAQ service unavailable");
                return;
            }

            Button btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            SetDaqResult("Testing connection...");

            try
            {
                DaqConnectionResult result = await daqService.TestDaqConnection();

                SetDaqResult(result.success
                    ? $"Connected - device {result.device_id}"
                    : $"Connection failed - {result.error_code}: {result.error_message}");
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async void Adv_DAQ_TestSignal_Click(object sender, RoutedEventArgs e)
        {
            var daqService = App.Services?.GetService<IDaqService>();
            if (daqService == null)
            {
                SetDaqResult("DAQ service unavailable");
                return;
            }

            Button btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            SetDaqResult("Testing signal...");

            try
            {
                DaqConnectionResult result = await daqService.TestDaqSignal();

                string amplitude = result.signal_amplitude.HasValue
                    ? $"{result.signal_amplitude.Value:F3} Vpp"
                    : "n/a";

                SetDaqResult(result.success
                    ? $"Signal OK - {amplitude} on {result.device_id}"
                    : $"Signal test failed ({amplitude}) - {result.error_code}: {result.error_message}");
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async void Adv_DAQ_TestSingleMeas_Click(object sender, RoutedEventArgs e)
        {
            var daqService = App.Services?.GetService<IDaqService>();
            if (daqService == null)
            {
                SetDaqResult("DAQ service unavailable");
                return;
            }

            Button btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                DaqSingleReadResult result = await daqService.ReadSingleValue();

                SetDaqResult(result.success
                    ? $"ai0 = {result.voltage,8:F4} V"
                    : $"Read failed - {result.error_code}: {result.error_message}");
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }
        private async void Adv_DAQ_AcquireHallBtn_Click(object sender, RoutedEventArgs e)
        {
            var daqService = App.Services?.GetService<IDaqService>();
            if (daqService == null)
            {
                SetDaqResult("DAQ service unavailable");
                return;
            }

            Button btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            SetDaqResult("Acquiring Hall data...");

            try
            {
                HallAcquisitionResult result = await daqService.AcquireHallData();

                if (!result.success)
                {
                    SetDaqResult($"Acquisition failed - {result.error_code}: {result.error_message}");
                    return;
                }

                SetDaqResult($"Acquired {result.signal.Length} samples");

                // Build the two-column table off the UI thread - 2020 rows of
                // string formatting is enough to be visible as a hitch otherwise.
                string data = await Task.Run(() => FormatHallData(result));

                TextDataViewer viewer = new TextDataViewer();
                viewer.Owner = this;
                viewer.PopulateData(data, "Hall Data (time, signal)");
                viewer.ShowDialog();
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Formats a Hall acquisition as a two-column time/voltage table with a
        /// short summary header, for display in the TextDataViewer.
        /// </summary>
        private static string FormatHallData(HallAcquisitionResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Samples:   {result.signal.Length}");
            sb.AppendLine($"Duration:  {result.time[result.time.Length - 1]:F4} s");
            sb.AppendLine($"Min:       {result.signal.Min():F4} V");
            sb.AppendLine($"Max:       {result.signal.Max():F4} V");
            sb.AppendLine($"Peak-peak: {result.signal.Max() - result.signal.Min():F4} V");
            sb.AppendLine();
            sb.AppendLine($"{"time (s)"},{"signal (V)"}");

            for (int i = 0; i < result.signal.Length; i++)
            {
                sb.AppendLine($"{result.time[i]:F5},{result.signal[i]:F5}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Writes a timestamped line to the DAQ test output on the Advanced tab.
        /// </summary>
        private void SetDaqResult(string message)
        {
            Adv_DAQ_ResultTxb.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }

        /// <summary>
        /// Composes the DAQ acquisition and the polarity algorithm at the call
        /// site, keeping DaqService free of processing logic and
        /// PolarityVerification free of hardware dependencies.
        /// </summary>
        private async void Adv_DAQ_VerifyPolarityBtn_Click(object sender, RoutedEventArgs e)
        {
            var daqService = App.Services?.GetService<IDaqService>();
            var settingsService = App.Services?.GetService<ISettingsService>();

            if (daqService == null || settingsService == null)
            {
                SetDaqResult("DAQ or settings service unavailable");
                return;
            }

            //CSMotor motor = settingsService.GetSettingsForSelectedSize();
            //if (motor == null)
            //{
            //    SetDaqResult("No motor settings loaded - connect to the work cell first");
            //    return;
            //}

            // ============================ TEST ONLY
            // Use DDM 116 for test
            CSMotor motor = new CSMotor
            {
                pol_expected_wavelength = 0.0125f,
                pol_expected_magnets = 80
            };
            // ============================


            Button btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;
            SetDaqResult("Acquiring Hall data...");

            try
            {
                HallAcquisitionResult acquisition = await daqService.AcquireHallData();

                if (!acquisition.success)
                {
                    SetDaqResult($"Acquisition failed - {acquisition.error_code}: {acquisition.error_message}");
                    return;
                }

                SetDaqResult($"Acquired {acquisition.signal.Length} samples - verifying...");

                // Filtering and peak detection over ~2020 samples is CPU-bound;
                // keep it off the UI thread.
                PolarityVerificationResult verification = await Task.Run(() =>
                    PolarityVerification.VerifyPolarityData(
                        acquisition.time,
                        acquisition.signal,
                        DaqService.SampleRate,
                        motor));

                SetDaqResult(verification.passed
                    ? $"PASS - {verification.message}"
                    : $"FAIL - {verification.message.Replace(Environment.NewLine, " ")}");

                string data = await Task.Run(() => FormatPolarityResult(acquisition, verification));

                TextDataViewer viewer = new TextDataViewer();
                viewer.Owner = this;
                viewer.PopulateData(data, "Polarity Verification");
                viewer.ShowDialog();
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Formats a polarity verification for the TextDataViewer: summary,
        /// detected wavelengths, then the raw and filtered sample table.
        /// </summary>
        private static string FormatPolarityResult(
            HallAcquisitionResult acquisition,
            PolarityVerificationResult v)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Result:            {(v.passed ? "PASS" : "FAIL")}");
            sb.AppendLine($"Message:           {v.message.Trim()}");
            sb.AppendLine();
            sb.AppendLine($"Peaks detected:    {v.numPeaks}");
            sb.AppendLine($"Expected magnets:  {v.expectedMagnets}");
            sb.AppendLine($"Expected lambda:   {v.expectedWavelength:F5} s");
            sb.AppendLine($"Long wavelengths:  {v.numLongWavelengths}");
            sb.AppendLine($"Short wavelengths: {v.numShortWavelengths}");
            sb.AppendLine();
            sb.AppendLine($"Samples:           {acquisition.signal.Length}");
            sb.AppendLine($"Min / Max:         {acquisition.signal.Min():F4} / {acquisition.signal.Max():F4} V");
            sb.AppendLine($"Peak-peak:         {acquisition.signal.Max() - acquisition.signal.Min():F4} V");

            // Wavelength list makes an out-of-range gap easy to spot by eye.
            if (v.wavelengths != null && v.wavelengths.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Wavelengths (s), * = long, ! = short:");
                for (int i = 0; i < v.wavelengths.Length; i++)
                {
                    double w = v.wavelengths[i];
                    string flag = w > 1.8 * v.expectedWavelength ? " *"
                                : w < 0.65 * v.expectedWavelength ? " !"
                                : "";
                    sb.AppendLine($"  {i,4}: {w,10:F5}{flag}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"{"time (s)",12}  {"raw (V)",12}  {"filtered (V)",13}  peak");
            sb.AppendLine(new string('-', 48));

            var extrema = new HashSet<int>(v.extremaIndices ?? Array.Empty<int>());

            for (int i = 0; i < acquisition.signal.Length; i++)
            {
                string filtered = v.filteredSignal != null && i < v.filteredSignal.Length
                    ? $"{v.filteredSignal[i],13:F5}"
                    : new string(' ', 13);

                sb.AppendLine(
                    $"{acquisition.time[i],12:F5}  " +
                    $"{acquisition.signal[i],12:F5}  " +
                    $"{filtered}  " +
                    $"{(extrema.Contains(i) ? "<--" : "")}");
            }

            return sb.ToString();
        }
    }
}
