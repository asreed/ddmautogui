using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    public partial class CalibFlowSizePanel : UserControl
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalDataService _localDataService;
        private readonly IFlowCalibrationService _flowCalibrationService;
        private readonly IControllerService _controllerService;

        /// <summary>The motor size this panel calibrates, e.g. "ddm_116".</summary>
        public string MotorName { get; set; }

        /// <summary>Friendly label used on the calibrate button, e.g. "DDM 116".</summary>
        public string DisplayName { get; set; }

        public CalibFlowSizePanel()
        {
            InitializeComponent();
            RunPrg.Visibility = Visibility.Collapsed;

            _settingsService = App.Services?.GetService<ISettingsService>();
            _localDataService = App.Services?.GetService<ILocalDataService>();
            _flowCalibrationService = App.Services?.GetService<IFlowCalibrationService>();
            _controllerService = App.Services?.GetService<IControllerService>();
        }

        public void SetupPanel()
        {
            if (string.IsNullOrEmpty(MotorName))
            {
                return;
            }

            if (_controllerService.CONNECTION_STATE == null || !_controllerService.CONNECTION_STATE.isConnected)
            {
                return;
            }

            try
            {
                RunBtn.Content = $"Calibrate {DisplayName ?? MotorName} flow rate";

                CellSettings settings = _settingsService.GetAllSettings();
                LocalData localData = _localDataService.GetLocalData();

                CSMotor motor = _settingsService.GetMotorSettingsFromName(MotorName);
                LDMotorCalib calib = _localDataService.GetCalibFromMotorName(localData, MotorName);
                CSDefaultCalib refCalib = _settingsService.GetDefaultPressuresFromName(MotorName);

                float sys1RefPres = refCalib.sys_1_pressure.Value;
                float sys2RefPres = refCalib.sys_2_pressure.Value;
                float sys1CalPres = calib.sys_1_pressure.Value;
                float sys2CalPres = calib.sys_2_pressure.Value;

                float sys1Dev = (sys1CalPres - sys1RefPres) / sys1RefPres * 100;
                float sys2Dev = (sys2CalPres - sys2RefPres) / sys2RefPres * 100;

                S1_RefPresTxb.Text = $"{sys1RefPres:F2} psi";
                S2_RefPresTxb.Text = $"{sys2RefPres:F2} psi";
                S1_CalPresTxb.Text = $"{sys1CalPres:F2} psi";
                S2_CalPresTxb.Text = $"{sys2CalPres:F2} psi";
                S1_CalPresDevTxb.Text = $"{sys1Dev:F2}%";
                S2_CalPresDevTxb.Text = $"{sys2Dev:F2}%";
            }
            catch (Exception ex)
            {
                Debug.Print($"Error populating flow calibration data for {MotorName}: {ex.Message}");
            }
        }

        private async void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            RunBtn.IsEnabled = false;
            RunPrg.Visibility = Visibility.Visible;

            try
            {
                bool rerun;
                do
                {
                    rerun = await RunOnceAsync();
                }
                while (rerun);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error during flow calibration for {MotorName}: {ex.Message}");
            }
            finally
            {
                RunBtn.IsEnabled = true;
                RunPrg.Visibility = Visibility.Collapsed;
                SetupPanel();
            }
        }

        /// <summary>
        /// Runs one calibration attempt and prompts the user.
        /// Returns true if the user chose to re-run.
        /// </summary>
        private async Task<bool> RunOnceAsync()
        {
            CellSettings settings = _settingsService.GetAllSettings();
            LocalData localData = _localDataService.GetLocalData();
            LDMotorCalib calib = _localDataService.GetCalibFromMotorName(localData, MotorName);

            RunCalibResult result =
                await _flowCalibrationService.RunDispenseForManualCalibration(settings, localData, MotorName);

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
                _flowCalibrationService.SetPressuresFromCalibration(settings, localData, MotorName);
                return false;
            }

            return userInput == MessageBoxResult.No; // No = re-run, Cancel = stop
        }
    }
}