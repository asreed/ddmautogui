using ArenaNET;
using DDMAutoGUI.utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DDMAutoGUI.windows
{
    /// <summary>
    /// Interaction logic for DebugWindow2.xaml
    /// </summary>
    public partial class DebugWindow2 : Window
    {

        public string laserRingData;
        public string laserMagData;
        public string laserCustomData;


        public DebugWindow2()
        {
            InitializeComponent();
            debugWindow_OnUpdateAutoStatus2(this, EventArgs.Empty);
            UpdateVersion();
            RobotManager.Instance.UpdateAutoStatus += debugWindow_OnUpdateAutoStatus2;
            RobotManager.Instance.StartAutoStatus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RobotManager.Instance.StopAutoStatus();
        }

        private async void UpdateVersion()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            versionManagerLabel.Content = $"{v.Major}.{v.Minor}.{v.Build}";

            string response = await RobotManager.Instance.GetControllerSoftwareVersionAsync();
            versionTCSLabel.Content = response;

            response = await RobotManager.Instance.GetControllerConfigVersionAsync();
            versionConfigLabel.Content = response;
        }

        private void debugWindow_OnUpdateAutoStatus2(object sender, EventArgs e)
        {
            RobotState newStatus = RobotManager.Instance.GetControllerState();
            formatReadout(roPowerEnabled, newStatus.isPowerEnabled);
            formatReadout(roRobotHomed, newStatus.isRobotHomed);
            formatReadout(roLinPos, newStatus.posLinear);
            formatReadout(roRotPos, newStatus.posRotary);
            formatReadout(roLinFlag1, !newStatus.isLinearIn1); // rail sensors are low when part present
            formatReadout(roLinFlag2, !newStatus.isLinearIn2);
            formatReadout(roLinFlag3, !newStatus.isLinearIn3);
            formatReadout(roPresCmd1, newStatus.pressureCommand1, "psi");
            formatReadout(roPresMeas1, newStatus.pressureMeasurement1, "psi");
            formatReadout(roPresCmd2, newStatus.pressureCommand2, "psi");
            formatReadout(roPresMeas2, newStatus.pressureMeasurement2, "psi");
            formatReadout(roFlowVol1, newStatus.flowVolume1, "mL");
            formatReadout(roFlowErr1, newStatus.flowError1);
            formatReadout(roFlowVol2, newStatus.flowVolume2, "mL");
            formatReadout(roFlowErr2, newStatus.flowError2);

        }

        private void formatReadout(Label label, float value)
        {
            label.Content = value.ToString();

        }
        private void formatReadout(Label label, float value, string unit)
        {
            label.Content = value.ToString() + " " + unit;

        }
        private void formatReadout(Label label, bool value)
        {
            label.Content = value ? "Yes" : "No";
            if (value)
            {
                label.Background = new BrushConverter().ConvertFrom("#ffd3ddf5") as SolidColorBrush;
            } else
            {
                label.Background = new BrushConverter().ConvertFrom("WhiteSmoke") as SolidColorBrush;
            }

        }

        private void lockRobotButtons(bool state)
        {
            enablePowerBtn.IsEnabled = !state;
            homeBtn.IsEnabled = !state;
            moveLinearLoadBtn.IsEnabled = !state;
            moveLinearCamBtn.IsEnabled = !state;
            moveLinearDisp1Btn.IsEnabled = !state;
            moveLinearDisp2Btn.IsEnabled = !state;
            spinTimeBtn.IsEnabled = !state;

            connectLaserBtn.IsEnabled = !state;
            disconnectLaserBtn.IsEnabled= !state;
            getLaserSingleBtn.IsEnabled = !state;
            getLaserRingBtn.IsEnabled = !state;
            getLaserMagBtn.IsEnabled = !state;

            setPressure1Btn.IsEnabled = !state;
            setPressure2Btn.IsEnabled = !state;
            shot1Btn.IsEnabled = !state;
            shot2Btn.IsEnabled = !state;
            openValve1Btn.IsEnabled = !state;
            openValve2Btn.IsEnabled = !state;

            setZeroBothBtn.IsEnabled = !state;
            startMeasure1Btn.IsEnabled = !state;
            stopMeasure1Btn.IsEnabled = !state;
            startMeasure2Btn.IsEnabled = !state;
            stopMeasure2Btn.IsEnabled = !state;

            dispShotsBtn.IsEnabled = !state;
        }

        private void lockStatusButtons(bool state)
        {
            //
        }







        // =========== BUTTON HANDLERS

        private async void enablePowerBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.EnablePower();
            enablePowerOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            homeOutput.Content = "(not implemented)";
        }

        //private async void moveLinearBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    lockRobotButtons(true);
        //    string input = moveLinearInput.Text;
        //    string response = await RobotManager.Instance.SendRobotCommandAsync($"moveToPosition {input}");
        //    moveLinearOutput.Content = response;
        //    lockRobotButtons(false);
        //}

        //private async void moveRotaryBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    lockRobotButtons(true);
        //    string input = moveRotaryInput.Text;
        //    string response = await RobotManager.Instance.SendRobotCommandAsync($"spinToPosition {input}");
        //    moveRotaryOutput.Content = response;
        //    lockRobotButtons(false);
        //}

        private async void moveLinearLoadBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = moveLinearLoadInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"moveToLoad {input}");
            moveLinearLoadOutput.Content = response;
            lockRobotButtons(false);
        }



        

        private async void moveLinearCamBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = moveLinearCamInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"moveToCamera {input}");
            moveLinearCamOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void moveLinearDisp1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = moveLinearDisp1Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"moveToDispense1 {input}");
            moveLinearDisp1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void moveLinearDisp2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = moveLinearDisp2Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"moveToDispense2 {input}");
            moveLinearDisp2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void spinTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = spinTimeInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"spinonly {input}");
            spinTimeOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void openValve1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = openValve1Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"init1 {input}");
            openValve1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void openValve2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = openValve2Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"init2 {input}");
            openValve2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void closeValvesBtn_Click(object sender, RoutedEventArgs e)
        {
            lockStatusButtons(true);
            string response = await RobotManager.Instance.SendStatusCommandAsync("closeallvalves");
            //closeValvesOutput.Content = response;
            lockStatusButtons(false);
        }

        private async void setZeroBothBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync("zeroshiftboth");
            setZeroBothOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void startMeasure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync("startmeasureflow1");
            startMeasure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void startMeasure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync("startmeasureflow2");
            startMeasure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void stopMeasure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync("stopmeasureflow1");
            stopMeasure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void stopMeasure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync("stopmeasureflow2");
            stopMeasure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void setPressure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = setPressure1Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"setpressure1 {input}");
            setPressure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void setPressure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = setPressure2Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"setpressure2 {input}");
            setPressure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void shot1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = shot1Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"measuredShot1 {input}");
            shot1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void shot2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = shot2Input.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"measuredShot2 {input}");
            shot2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void connectLaserBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync($"connectToLaser");
            connectLaserOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void disconnectLaserBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync($"disconnectFromLaser");
            disconnectLaserBtn.Content = response;
            lockRobotButtons(false);
        }

        private async void getLaserSingleBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync($"getLaserSingleMeasurement");
            getLaserSingleOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void getLaserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync($"getLaserRingMeasurements");
            if (response.Split(" ").Length > 1)
            {
                laserRingData = response.Split(" ")[1].Replace(";", "\n");
                getLaserRingOutput.Content = "(data collected)";
            }
            else
            {
                getLaserRingOutput.Content = $"error: {response}";
            }
            lockRobotButtons(false);
        }

        private async void getLaserMagBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await RobotManager.Instance.SendRobotCommandAsync($"getLaserMagMeasurements");
            if (response.Split(" ").Length > 1)
            {
                laserMagData = response.Split(" ")[1].Replace(";", "\n");
                getLaserMagOutput.Content = "(data collected)";
            }
            else
            {
                getLaserMagOutput.Content = $"error: {response}";
            }
            lockRobotButtons(false);
        }

        private async void getLaserCustomBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = getLaserCustomInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"getLaserCustomMeasurements {input}");
            if (response.Split(" ").Length > 1)
            {
                laserCustomData = response.Split(" ")[1].Replace(";", "\n");
                getLaserCustomOutput.Content = "(data collected)";
            }
            else
            {
                getLaserCustomOutput.Content = $"error: {response}";
            }
            lockRobotButtons(false);
        }

        private void showRingBtn_Click(object sender, RoutedEventArgs e)
        {
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(laserRingData, "Ring Displacement Measurements");
            viewer.Show();
        }

        private void showMagBtn_Click(object sender, RoutedEventArgs e)
        {
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(laserMagData, "Magnet Displacement Measurements");
            viewer.Show();
        }

        private void showCustomBtn_Click(object sender, RoutedEventArgs e)
        {
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(laserCustomData, "Custom Displacement Measurements");
            viewer.Show();
        }

        private async void dispShotsBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = dispShotsInput.Text;
            string response = await RobotManager.Instance.SendRobotCommandAsync($"dispenseShotsToRing {input}");
            dispShotsOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void eStopBtn_Click(object sender, RoutedEventArgs e)
        {
            string response = await RobotManager.Instance.SendStatusCommandAsync($"halt");
            response = await RobotManager.Instance.SendStatusCommandAsync($"hp 0");
        }
    }
}
