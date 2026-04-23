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
            //S1_FillBtn.IsEnabled = false;
            //S1_FlushBtn.IsEnabled = false;
            //S1_DepresBtn.IsEnabled = false;
            //S2_FillBtn.IsEnabled = false;
            //S2_FlushBtn.IsEnabled = false;
            //S2_DepresBtn.IsEnabled = false;
        }
        private void EnableButtons()
        {
            //S1_FillBtn.IsEnabled = true;
            //S1_FlushBtn.IsEnabled = true;
            //S1_DepresBtn.IsEnabled = true;
            //S2_FillBtn.IsEnabled = true;
            //S2_FlushBtn.IsEnabled = true;
            //S2_DepresBtn.IsEnabled = true;

            //Flush_DepresPrg.Visibility = Visibility.Collapsed;
            //Flush_S1_LowPresPrg.Visibility = Visibility.Collapsed;
            //Flush_S1_PurgePrg.Visibility = Visibility.Collapsed;
            //Flush_S2_LowPresPrg.Visibility = Visibility.Collapsed;
            //Flush_S2_PurgePrg.Visibility = Visibility.Collapsed;
        }

        public async Task Depressurize(ProgressBar prg)
        {
            DisableButtons();
            prg.Visibility = Visibility.Visible;
            await App.ControllerManager.SetBothRegPressureAndWait(0, 0, 30);
            prg.Visibility = Visibility.Collapsed;
            EnableButtons();
        }

        public async Task SetLowPressure(int sys, ProgressBar prg) {

            DisableButtons();
            prg.Visibility = Visibility.Visible;

            try
            {
                CellSettings settings = App.SettingsManager.GetAllSettings();
                float pressure = 0;
                switch (sys)
                {
                    case 1:
                        pressure = settings.dispense_system.sys_1_flush_pressure.Value;
                        break;
                    case 2:
                        pressure = settings.dispense_system.sys_2_flush_pressure.Value;
                        break;
                }
                await App.ControllerManager.SetRegPressureAndWait(sys, pressure, 20);

            }
            catch (Exception ex)
            {
                Debug.Print($"Error setting low pressure, system {sys}: {ex.Message}");
            }

            prg.Visibility = Visibility.Collapsed;
            EnableButtons();

        }

        public async Task SetDefPressure(int sys, ProgressBar prg)
        {

            DisableButtons();
            prg.Visibility = Visibility.Visible;

            float pressure = 0;
            try
            {
                CellSettings settings = App.SettingsManager.GetAllSettings();
                switch (sys)
                {
                    case 1:
                        pressure = settings.dispense_system.default_pressures.ddm_116.sys_1_pressure.Value;
                        break;
                    case 2:
                        pressure = settings.dispense_system.default_pressures.ddm_116.sys_2_pressure.Value;
                        break;
                }
                await App.ControllerManager.SetRegPressureAndWait(sys, pressure, 20);

            }
            catch (Exception ex)
            {
                Debug.Print($"Error setting default pressure {pressure}, system {sys}: {ex.Message}");
            }

            prg.Visibility = Visibility.Collapsed;
            EnableButtons();

        }

        public async Task Purge(int sys, ProgressBar prg)
        {
            DisableButtons();
            prg.Visibility = Visibility.Visible;

            try
            {
                CellSettings settings = App.SettingsManager.GetAllSettings();
                float time = 0;
                switch (sys) {
                    case 1:
                        time = settings.dispense_system.sys_1_flush_time.Value;
                        break;
                    case 2:
                        time = settings.dispense_system.sys_2_flush_time.Value;
                        break;
                }
                await App.ControllerManager.OpenValveTimed(sys, time);

            }
            catch (Exception ex)
            {
                Debug.Print($"Error during timed purge, system {sys}: {ex.Message}");
            }

            prg.Visibility = Visibility.Collapsed;
            EnableButtons();
        }


        public async Task Fill(int sys, ProgressBar prg)
        {
            DisableButtons();
            prg.Visibility = Visibility.Visible;

            try
            {
                CellSettings settings = App.SettingsManager.GetAllSettings();
                float time = 0;
                switch (sys)
                {
                    case 1:
                        time = settings.dispense_system.sys_1_fill_time.Value;
                        break;
                    case 2:
                        time = settings.dispense_system.sys_2_fill_time.Value;
                        break;
                }
                await App.ControllerManager.OpenValveTimed(sys, time);

            }
            catch (Exception ex)
            {
                Debug.Print($"Error during timed fill, system {sys}: {ex.Message}");
            }

            prg.Visibility = Visibility.Collapsed;
            EnableButtons();
        }





        private async void Flush_DepresBtn_Click(object sender, RoutedEventArgs e)
        {
            await Depressurize(Flush_DepresPrg);
        }
        private async void Flush_S1_LowPresBtn_Click(object sender, RoutedEventArgs e)
        {
            await SetLowPressure(1, Flush_S1_LowPresPrg);
        }
        private async void Flush_S2_LowPresBtn_Click(object sender, RoutedEventArgs e)
        {
            await SetLowPressure(2, Flush_S2_LowPresPrg);
        }
        private async void Flush_S1_PurgeBtn_Click(object sender, RoutedEventArgs e)
        {
            await Purge(1, Flush_S1_PurgePrg);
        }
        private async void Flush_S2_PurgeBtn_Click(object sender, RoutedEventArgs e)
        {
            await Purge(2, Flush_S2_PurgePrg);
        }



        private async void Fill_DepresBtn_Click(object sender, RoutedEventArgs e)
        {
            await Depressurize(Fill_DepresPrg);
        }
        private async void Fill_S1_DefPresBtn_Click(object sender, RoutedEventArgs e)
        {
            await SetDefPressure(1, Fill_S1_DefPresPrg);
        }
        private async void Fill_S2_DefPresBtn_Click(object sender, RoutedEventArgs e)
        {
            await SetDefPressure(2, Fill_S2_DefPresPrg);
        }
        private async void Fill_S1_FillBtn_Click(object sender, RoutedEventArgs e)
        {
            await Fill(1, Fill_S1_FillPrg);
        }
        private async void Fill_S2_FillBtn_Click(object sender, RoutedEventArgs e)
        {
            await Fill(2, Fill_S2_FillPrg);
        }



    }
}
