using DDMAutoGUI.utilities;
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
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Automation.Provider;
using System.Reflection;

namespace DDMAutoGUI.windows
{
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();
            stopAutoStatusBtn.IsEnabled = false;
            RobotManager.Instance.UpdateControllerState += debugWindow_OnUpdateAutoStatus;
            debugWindow_OnUpdateAutoStatus(this, EventArgs.Empty);
            startAutoStatusBtn_Click(this, null);
            UpdateVersion();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stopAutoStatusBtn_Click(this, null);
        }

        private async void UpdateVersion()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            managerVersionLabel.Content = $"Manager UI version: {v.Major}.{v.Minor}.{v.Build}";

            string response = await RobotManager.Instance.GetControllerSoftwareVersionAsync();
            tcsVersionLabel.Content = $"Controller TCS version: {response}";

            response = await RobotManager.Instance.GetControllerConfigVersionAsync();
            configVersionLabel.Content = $"Controller config version: {response}";
        }

        private async void statusBtn_Click(object sender, RoutedEventArgs e)
        {
            LockStatusButtons(true);
            string response = await RobotManager.Instance.SendStatusCommandAsync("systemstatus");
            statusOutput.Content = response;
            LockStatusButtons(false);
        }

        private async void closeValvesBtn_Click(object sender, RoutedEventArgs e)
        {
            LockStatusButtons(true);
            string response = await RobotManager.Instance.SendStatusCommandAsync("closeallvalves");
            closeValvesOutput.Content = response;
            LockStatusButtons(false);
        }



        private void startAutoStatusBtn_Click(object sender, RoutedEventArgs e)
        {
            startAutoStatusBtn.IsEnabled = false;
            stopAutoStatusBtn.IsEnabled = true;
            RobotManager.Instance.StartAutoControllerState();
        }
        private void stopAutoStatusBtn_Click(object sender, RoutedEventArgs e)
        {
            startAutoStatusBtn.IsEnabled = true;
            stopAutoStatusBtn.IsEnabled = false;
            RobotManager.Instance.StopAutoControllerState();
        }


        /*
        private async void spinBySpeedBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = spinBySpeedInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"spinbyspeed {input}");
            spinBySpeedOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void spinByTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = spinByTimeInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"spinbytime {input}");
            spinByTimeOutput.Content = response;
            LockRobotButtons(false);
        }
        */



        private async void enableBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response;
            int timeout = 0;

            response = await RobotManager.Instance.SendRobotCommandAsync("hp 1");
            while (true)
            {
                response = await RobotManager.Instance.SendRobotCommandAsync("hp");
                if (response == "0 1")
                {
                    break;
                }
                timeout++;
                if (timeout > 20)
                {
                    response = "Timeout";
                    break;
                }
                Thread.Sleep(500);
            }
            enableOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void init1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = init1Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"init1 {input}");
            init1Output.Content = response;
            LockRobotButtons(false);
        }

        private async void init2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = init2Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"init2 {input}");
            init2Output.Content = response;
            LockRobotButtons(false);
        }
        private async void initBothBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = initBothInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"initBoth {input}");
            initBothOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void dispenseBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = dispenseInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"dispense {input}");
            dispenseOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void spinOnlyBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = spinOnlyInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"spinonly {input}");
            spinOnlyOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void moveDispBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = moveDispInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"moveToDispense {input}");
            moveDispOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void moveLoadBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = moveLoadInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"moveToLoad {input}");
            moveLoadOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void movePosBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = movePosInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"moveToPosition {input}");
            movePosOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void spinToPosBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = spinToPosInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"spinToPosition {input}");
            spinToPosOutput.Content = response;
            LockRobotButtons(false);
        }

        /*
        private async void spinOnlyBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = spinOnlyInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"spinonly {input}");
            spinOnlyOutput.Content = response;
            LockRobotButtons(false);
        }
        */

        public void debugWindow_OnUpdateAutoStatus(object sender, EventArgs e)
        {
            //Debug.Print("updating debug window with new status");
            //ControllerState newStatus = RobotManager.Instance.GetControllerState();
            //FormatIndicator(powerEnabledLabel, newStatus.isPowerEnabled);
            //FormatIndicator(dispensingLabel, newStatus.isDispensing);
            //FormatIndicator(vmAlarmLabel, newStatus.isVMAlarm);
            //FormatIndicator(vmEOCLabel, newStatus.isVMEOC);

            //FormatIndicator(linearIn1, newStatus.isLinearIn1);
            //FormatIndicator(linearIn2, newStatus.isLinearIn2);
            //FormatIndicator(linearIn3, newStatus.isLinearIn3);

            //FormatIndicator(posLinearText, false);
            //FormatIndicator(posRotaryText, false);
            //posLinearText.Text = newStatus.posLinear.ToString("F2");
            //posRotaryText.Text = newStatus.posRotary.ToString("F2");

        }
        
        private void FormatIndicator(TextBlock ind, bool value)
        {
            Border border = ind.Parent as Border;
            if (value)
            {
                border.Background = new BrushConverter().ConvertFrom("#ffd3ddf5") as SolidColorBrush;
                border.BorderBrush = new BrushConverter().ConvertFrom("#ffb2c5f2") as SolidColorBrush;
            }
            else
            {
                border.Background = new SolidColorBrush(Colors.WhiteSmoke);
                border.BorderBrush = new BrushConverter().ConvertFrom("#CCC") as SolidColorBrush;
            }
        }


        private void LockStatusButtons(bool state)
        {
            statusBtn.IsEnabled = !state;
        }
        private void LockRobotButtons(bool state)
        {
            enableBtn.IsEnabled = !state;
            init1Btn.IsEnabled = !state;
            init2Btn.IsEnabled = !state;
            initBothBtn.IsEnabled = !state;
            dispenseBtn.IsEnabled = !state;
            spinOnlyBtn.IsEnabled = !state;
            moveDispBtn.IsEnabled = !state;
            moveLoadBtn.IsEnabled = !state;
            movePosBtn.IsEnabled = !state;
            spinToPosBtn.IsEnabled = !state;
        }

        private void dispenseInput_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
