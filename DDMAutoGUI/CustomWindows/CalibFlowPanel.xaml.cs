using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for CalibFlowPanel.xaml.
    /// Runs the manual flow calibration routine for the motor size selected in the
    /// ComboBox and displays the most recent calibration result.
    /// </summary>
    public partial class CalibFlowPanel : UserControl
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalDataService _localDataService;
        private readonly IFlowCalibrationService _flowCalibrationService;
        private readonly IControllerService _controllerService;

        public CalibFlowPanel()
        {
            InitializeComponent();
            RunPrg.Visibility = Visibility.Collapsed;

            // Resolve services from DI container when instantiated via XAML.
            _settingsService = App.Services?.GetService<ISettingsService>();
            _localDataService = App.Services?.GetService<ILocalDataService>();
            _flowCalibrationService = App.Services?.GetService<IFlowCalibrationService>();
            _controllerService = App.Services?.GetService<IControllerService>();
        }

        /// <summary>The motor size currently selected in the ComboBox, e.g. "ddm_116".</summary>
        private string SelectedMotorName => MotorSizeCmb.SelectedItem as string;

        public void SetupPanel()
        {
            LocalData localData = _localDataService?.GetLocalData();
            if (localData?.calib_data != null)
            {
                Calib_LastCalTxb.Text = localData.calib_data.last_calib?.ToString() ?? "-";
                Calib_LastMotorTxb.Text = localData.calib_data.last_size ?? "-";
            }
        }

        private void MotorSizeCmb_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Refresh the calibrate button label to reflect the newly selected size.
            string motorName = SelectedMotorName;
            RunBtn.Content = string.IsNullOrEmpty(motorName)
                ? "Calibrate flow rate"
                : $"Calibrate {motorName} flow rate";
        }

        private async void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            string motorName = SelectedMotorName;
            if (string.IsNullOrEmpty(motorName))
            {
                return;
            }

            // Calibration drives the robot, so require a live controller connection.
            if (_controllerService?.CONNECTION_STATE == null || !_controllerService.CONNECTION_STATE.isConnected)
            {
                Debug.Print("Flow calibration requested while not connected; ignoring.");
                return;
            }

            RunBtn.IsEnabled = false;
            RunPrg.Visibility = Visibility.Visible;

            try
            {
                bool rerun;
                do
                {
                    rerun = await RunOnceAsync(motorName);
                }
                while (rerun);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error during flow calibration for {motorName}: {ex.Message}");
            }
            finally
            {
                RunBtn.IsEnabled = true;
                RunPrg.Visibility = Visibility.Collapsed;
                SetupPanel();
            }
        }

        /// <summary>
        /// Runs one calibration attempt for <paramref name="motorName"/> and prompts the
        /// user. Returns true if the user chose to re-run.
        /// </summary>
        private async Task<bool> RunOnceAsync(string motorName)
        {
            CellSettings settings = _settingsService.GetAllSettings();
            LocalData localData = _localDataService.GetLocalData();
            LDMotorCalib calib = _localDataService.GetCalibFromMotorName(localData, motorName);

            RunCalibResult result =
                await _flowCalibrationService.RunDispenseForManualCalibration(settings, localData, motorName);

            float newSys1Pres = calib.sys_1_pressure.Value * result.sf1;
            float newSys2Pres = calib.sys_2_pressure.Value * result.sf2;

            string caption = "Accept new calibration?";
            string message =
                $"Calibration scale factors:\n\n" +
                $"SF1: {result.sf1:F2}\n" +
                $"SF2: {result.sf2:F2}\n\n" +
                $"New calculated pressures:\n\n" +
                $"Sys 1: {newSys1Pres:F2} psi\n" +
                $"Sys 2: {newSys2Pres:F2} psi\n\n" +
                $"Accept results? \"No\" will re-run procedure.";

            MessageBoxResult userInput =
                MessageBox.Show(message, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (userInput == MessageBoxResult.Yes)
            {
                _flowCalibrationService.GenerateAndSaveCalibration(result);
                _flowCalibrationService.SetPressuresFromCalibration(settings, localData, motorName);
                return false;
            }

            return userInput == MessageBoxResult.No; // No = re-run, Cancel = stop
        }
    }
}
