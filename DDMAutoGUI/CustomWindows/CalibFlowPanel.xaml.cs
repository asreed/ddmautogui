using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for CalibFlowPanel.xaml
    /// </summary>
    public partial class CalibFlowPanel : UserControl
    {
        private readonly ISettingsManager _settingsManager;
        private readonly ILocalDataManager _localDataManager;
        private readonly IFlowCalibrationManager _flowCalibrationManager;

        private RunCalibResult runCalibResult = new RunCalibResult();

        public CalibFlowPanel()
        {
            InitializeComponent();
            Calib_116_RunPrg.Visibility = Visibility.Collapsed;

            // Resolve services from DI container when instantiated via XAML
            _settingsManager = App.Services?.GetService<ISettingsManager>();
            _localDataManager = App.Services?.GetService<ILocalDataManager>();
            _flowCalibrationManager = App.Services?.GetService<IFlowCalibrationManager>();
        }

        public CalibFlowPanel(
            ISettingsManager settingsManager,
            ILocalDataManager localDataManager,
            IFlowCalibrationManager flowCalibrationManager) : this()
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _localDataManager = localDataManager ?? throw new ArgumentNullException(nameof(localDataManager));
            _flowCalibrationManager = flowCalibrationManager ?? throw new ArgumentNullException(nameof(flowCalibrationManager));
        }

        public void SetupPanel()
        {
            try
            {
                CellSettings settings = _settingsManager.GetAllSettings();
                LocalData localData = _localDataManager.GetLocalData();

                Calib_LastCalTxb.Text = localData.calib_data.last_calib.Value.ToString();
                Calib_LastMotorTxb.Text = localData.calib_data.last_size;

                int sysID = settings.ddm_116.shot_settings.id_sys_num.Value;
                int sysOD = settings.ddm_116.shot_settings.od_sys_num.Value;

                float sys1RefPres = settings.dispense_system.default_pressures.ddm_116.sys_1_pressure.Value;
                float sys2RefPres = settings.dispense_system.default_pressures.ddm_116.sys_2_pressure.Value;
                float sys1CalPres = localData.calib_data.ddm_116.sys_1_pressure.Value;
                float sys2CalPres = localData.calib_data.ddm_116.sys_2_pressure.Value;

                float sys1Dev = (sys1CalPres - sys1RefPres) / sys1RefPres * 100;
                float sys2Dev = (sys2CalPres - sys2RefPres) / sys2RefPres * 100;

                Calib_116_S1_RefPresTxb.Text = sys1RefPres.ToString("F2") + " psi";
                Calib_116_S2_RefPresTxb.Text = sys2RefPres.ToString("F2") + " psi";
                Calib_116_S1_CalPresTxb.Text = sys1CalPres.ToString("F2") + " psi";
                Calib_116_S2_CalPresTxb.Text = sys2CalPres.ToString("F2") + " psi";
                Calib_116_S1_CalPresDevTxb.Text = sys1Dev.ToString("F2") + "%";
                Calib_116_S2_CalPresDevTxb.Text = sys2Dev.ToString("F2") + "%";

                //float refFlowID = settings.ddm_116.shot_settings.id_target_flow.Value;
                //float refFlowOD = settings.ddm_116.shot_settings.od_target_flow.Value;

                //Calib_116_ID_RefFlowTxb.Text = refFlowID.ToString("F2") + " mL/s";
                //Calib_116_OD_RefFlowTxb.Text = refFlowOD.ToString("F2") + " mL/s";

            }
            catch (Exception ex)
            {
                Debug.Print("Error populating flow calibration data: " + ex.Message);
            }
        }

        private async void Calib_116_RunBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Calib_116_RunBtn.IsEnabled = false;
                Calib_116_RunPrg.Visibility = Visibility.Visible;

                CellSettings settings = _settingsManager.GetAllSettings();
                LocalData localData = _localDataManager.GetLocalData();

                // do the dispense, get a preliminary calibration result back
                // display calibration result for user confirmation
                // if user accepts, saves and completes
                // otherwise, recursively re-runs until user accepts or cancels

                RunCalibResult result = await _flowCalibrationManager.RunDispenseForManualCalibration(settings, localData, "ddm_116");

                float newSys1Pres = localData.calib_data.ddm_116.sys_1_pressure.Value * result.sf1;
                float newSys2Pres = localData.calib_data.ddm_116.sys_2_pressure.Value * result.sf2;

                string caption = $"Accept new calibration?";
                string message = "";
                message += $"Calibration scale factors:\n\n";
                message += $"SF1: {result.sf1:F2}\n";
                message += $"SF2: {result.sf2:F2}\n\n";
                message += $"New calculated pressures:\n\n";
                message += $"Sys 1: {newSys1Pres:F2} psi\n";
                message += $"Sys 2: {newSys2Pres:F2} psi\n\n";
                message += $"Accept results? \"No\" will re-run procedure.";

                MessageBoxResult userInput = MessageBox.Show(message, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (userInput == MessageBoxResult.Yes)
                {
                    // if OK, save, set new pressures, and reset UI

                    _flowCalibrationManager.GenerateAndSaveCalibration(result);
                    _flowCalibrationManager.SetPressuresFromCalibration(settings, localData, "ddm_116");
                    Calib_116_RunBtn.IsEnabled = true;
                    Calib_116_RunPrg.Visibility = Visibility.Collapsed;
                }
                else if (userInput == MessageBoxResult.No)
                {
                    Calib_116_RunBtn_Click(sender, e);
                }
                else
                {
                    Calib_116_RunBtn.IsEnabled = true;
                    Calib_116_RunPrg.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error during flow calibration: {ex.Message}");
                Calib_116_RunBtn.IsEnabled = true;
                Calib_116_RunPrg.Visibility = Visibility.Collapsed;
            }

            SetupPanel();
        }
    }
}
