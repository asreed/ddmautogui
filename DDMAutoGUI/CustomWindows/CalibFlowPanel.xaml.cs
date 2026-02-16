using DDMAutoGUI.utilities;
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
        public CalibFlowPanel()
        {
            InitializeComponent();

            Calib_116_RunPrg.Visibility = Visibility.Collapsed;
            Calib_116_DecideGrd.Visibility = Visibility.Collapsed;
        }

        public void SetupPanel()
        {
            try
            {
                // Fill in flow calibration info
                CellSettings settings = App.SettingsManager.GetAllSettings();
                LocalData localData = App.LocalDataManager.GetLocalData();

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

                float refFlowID = settings.ddm_116.shot_settings.id_target_flow.Value;
                float refFlowOD = settings.ddm_116.shot_settings.od_target_flow.Value;

                Calib_116_ID_RefFlowTxb.Text = refFlowID.ToString("F2") + " mL/s";
                Calib_116_OD_RefFlowTxb.Text = refFlowOD.ToString("F2") + " mL/s";



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

                CellSettings settings = App.SettingsManager.GetAllSettings();
                LocalData localData = App.LocalDataManager.GetLocalData();
                RunCalibResult result = await App.FlowCalibrationManager.RunCalibrationRoutineOnce(settings, localData, "ddm_116");

                if (result != null)
                {
                    if (result.success)
                    {
                        // successful calib. display options and wait for user input
                        Debug.Print("Single calib run successful");
                        Calib_116_DecideGrd.Visibility = Visibility.Visible;

                        Calib_116_S1_CalPresTxb.Text = result.sf1.ToString("F2") + " psi";
                    }
                    else
                    {
                        // unsuccessful calib. reset
                        throw new Exception($"{result.message}");
                    }
                }
                else
                {
                    // null result. check logic to make sure result is not null
                    Debug.Print("Null result from single calib run (?)");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error during flow calibration: {ex.Message}");
                Calib_116_RunBtn.IsEnabled = true;
                Calib_116_RunPrg.Visibility = Visibility.Collapsed;
            }
        }

        private void Calib_116_AcceptBtn_Click(object sender, RoutedEventArgs e)
        {
            // if OK, simply reset UI. calib already saved.
            Calib_116_RunBtn.IsEnabled = true;
            Calib_116_RunPrg.Visibility = Visibility.Collapsed;
            Calib_116_DecideGrd.Visibility = Visibility.Collapsed;
        }

        private void Calib_116_RejectBtn_Click(object sender, RoutedEventArgs e)
        {
            // if not OK, run calib again
            Calib_116_DecideGrd.Visibility = Visibility.Collapsed;
            Calib_116_RunBtn_Click(sender, e);
        }

        private void Calib_116_CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            // data is already saved... what to do...
            Calib_116_RunBtn.IsEnabled = true;
            Calib_116_RunPrg.Visibility = Visibility.Collapsed;
            Calib_116_DecideGrd.Visibility = Visibility.Collapsed;

        }
    }
}
