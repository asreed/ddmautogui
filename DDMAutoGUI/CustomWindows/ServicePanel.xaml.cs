using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using DDMAutoGUI.utilities;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for ServicePanel.xaml
    /// </summary>


    public partial class ServicePanel : UserControl
    {
        public ServicePanel()
        {
            InitializeComponent();
        }

        private void DisableButtons()
        {
            S1_FillBtn.IsEnabled = false;
            S1_FlushBtn.IsEnabled = false;
            S1_DepresBtn.IsEnabled = false;
            S2_FillBtn.IsEnabled = false;
            S2_FlushBtn.IsEnabled = false;
            S2_DepresBtn.IsEnabled = false;
        }
        private void EnableButtons()
        {
            S1_FillBtn.IsEnabled = true;
            S1_FlushBtn.IsEnabled = true;
            S1_DepresBtn.IsEnabled = true;
            S2_FillBtn.IsEnabled = true;
            S2_FlushBtn.IsEnabled = true;
            S2_DepresBtn.IsEnabled = true;

            S1_DepresPrg.Visibility = Visibility.Collapsed;
            S1_FlushPrg.Visibility = Visibility.Collapsed;
            S2_FillPrg.Visibility = Visibility.Collapsed;
            S2_DepresPrg.Visibility = Visibility.Collapsed;
            S2_FlushPrg.Visibility = Visibility.Collapsed;
            S2_FillPrg.Visibility = Visibility.Collapsed;
        }
        private async void S1_DepresBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons();
            S1_DepresPrg.Visibility = Visibility.Visible;
            await App.ControllerManager.SetBothRegPressureAndWait(0, 0, 20);
            EnableButtons();
        }

        private async void S1_FlushBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons();
            S1_FlushPrg.Visibility = Visibility.Visible;

            int sys = 1;
            try
            {
                CellSettings settings = App.SettingsManager.GetAllSettings();
                float pressure = settings.dispense_system.sys_1_flush_pressure.Value;
                float time = settings.dispense_system.sys_1_flush_time.Value;

                await App.ControllerManager.SetRegPressureAndWait(sys, pressure, 20);
                await App.ControllerManager.OpenValveTimed(sys, time);

            }
            catch (Exception ex)
            {
                Debug.Print($"Error flushing system {sys}: {ex.Message}");
            }

            EnableButtons();
        }
        private async void S1_FillBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons();
            S1_FillPrg.Visibility = Visibility.Visible;

            int sys = 1;
            try
            {
                // Use default DDM 116 pressure for fill for now

                CellSettings settings = App.SettingsManager.GetAllSettings();
                float pressure = settings.dispense_system.default_pressures.ddm_116.sys_1_pressure.Value;
                float time = settings.dispense_system.sys_1_fill_time.Value;

                await App.ControllerManager.SetRegPressureAndWait(sys, pressure, 20);
                await App.ControllerManager.OpenValveTimed(sys, time);

            }
            catch (Exception ex)
            {
                Debug.Print($"Error filling system {sys}: {ex.Message}");
            }

            EnableButtons();
        }


        private async void S2_DepresBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons();
            S2_DepresPrg.Visibility = Visibility.Visible;
            await App.ControllerManager.SetBothRegPressureAndWait(0, 0, 20);
            EnableButtons();
        }

        private async void S2_FlushBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons();
            S2_FlushPrg.Visibility = Visibility.Visible;

            int sys = 2;
            try
            {
                CellSettings settings = App.SettingsManager.GetAllSettings();
                float pressure = settings.dispense_system.sys_2_flush_pressure.Value;
                float time = settings.dispense_system.sys_2_flush_time.Value;

                await App.ControllerManager.SetRegPressureAndWait(sys, pressure, 20);
                await App.ControllerManager.OpenValveTimed(sys, time);

            }
            catch (Exception ex)
            {
                Debug.Print($"Error flushing system {sys}: {ex.Message}");
            }

            EnableButtons();
        }

        private async void S2_FillBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableButtons();
            S2_FillPrg.Visibility = Visibility.Visible;
            int sys = 2;

            try
            {
                // Use default DDM 116 pressure for fill for now

                CellSettings settings = App.SettingsManager.GetAllSettings();
                float pressure = settings.dispense_system.default_pressures.ddm_116.sys_2_pressure.Value;
                float time = settings.dispense_system.sys_2_fill_time.Value;

                await App.ControllerManager.SetRegPressureAndWait(sys, pressure, 20);
                await App.ControllerManager.OpenValveTimed(sys, time);

            }
            catch (Exception ex)
            {
                Debug.Print($"Error filling system {sys}: {ex.Message}");
            }

            EnableButtons();
        }
    }
}
