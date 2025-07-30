using ArenaNET;
using DDMAutoGUI.utilities;
using Microsoft.VisualBasic;
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
    public partial class ControlPanelWindow : Window
    {

        public string laserRingData;
        public string laserMagData;
        public string laserCustomData;


        public ControlPanelWindow()
        {
            InitializeComponent();

            UIManager.Instance.UIStateChanged += debugWindow_OnUpdateUIState;
            ControllerManager.Instance.ControllerStateChanged += debugWindow_OnUpdateAutoControllerState;

            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            versionManagerLabel.Content = $"{v.Major}.{v.Minor}.{v.Build}";

            if (UIManager.Instance.UI_STATE.isConnected)
            {
                debugWindow_OnControllerConnected(this, EventArgs.Empty);
                debugWindow_OnUpdateAutoControllerState(this, EventArgs.Empty);
            }
            else
            {
                debugWindow_OnControllerDisconnected(this, EventArgs.Empty);
                debugWindow_OnUpdateAutoControllerState(this, EventArgs.Empty);
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ControllerManager.Instance.StopAutoControllerState();
        }




        private async void debugWindow_OnControllerConnected(object sender, EventArgs e)
        {
            connectedBox.Foreground = new BrushConverter().ConvertFrom("Black") as SolidColorBrush;
            connectedBox.Text = "Connected";

            versionTCSLabel.Content = await ControllerManager.Instance.GetTCSVersion();
            versionConfigLabel.Content = await ControllerManager.Instance.GetPACVersion();

            lockRobotButtons(false);
            lockStatusButtons(false);

            eStopBtn.IsEnabled = true;
            eCloseValvesBtn.IsEnabled = true;

            ControllerManager.Instance.StartAutoControllerState();
        }

        private void debugWindow_OnControllerDisconnected(object sender, EventArgs e)
        {
            connectedBox.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
            connectedBox.Text = "Not connected";
            statusBox.Text = "-";

            versionTCSLabel.Content = "-";
            versionConfigLabel.Content = "-";

            lockRobotButtons(true);
            lockStatusButtons(true);
            disableAllReadouts();

            eStopBtn.IsEnabled = false;
            eCloseValvesBtn.IsEnabled = false;

            ControllerManager.Instance.StopAutoControllerState();
        }

        private async void debugWindow_OnUpdateUIState(object sender, EventArgs e)
        {
            


            // ... ?
        }

        private void debugWindow_OnUpdateAutoControllerState(object sender, EventArgs e)
        {

            ControllerState contState = ControllerManager.Instance.CONTROLLER_STATE;

            if (!contState.parseError)
            {
                statusBox.Foreground = new BrushConverter().ConvertFrom("Black") as SolidColorBrush;
                statusBox.Text = "Parse OK";
                formatAllReadouts(contState);
            }
            else
            {
                statusBox.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
                statusBox.Text = $"Parse error: {contState.parseErrorMessage}";
                disableAllReadouts();
            }

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

        private void formatAllReadouts(ControllerState contState)
        {
            formatReadout(roPowerEnabled, contState.isPowerEnabled);
            formatReadout(roRobotHomed, contState.isRobotHomed);
            formatReadout(roLinPos, contState.posLinear);
            formatReadout(roRotPos, contState.posRotary);
            formatReadout(roLinFlag1, !contState.isLinearIn1); // rail sensors are low when part present
            formatReadout(roLinFlag2, !contState.isLinearIn2);
            formatReadout(roLinFlag3, !contState.isLinearIn3);
            formatReadout(roPresCmd1, contState.pressureCommand1, "psi");
            formatReadout(roPresMeas1, contState.pressureMeasurement1, "psi");
            formatReadout(roPresCmd2, contState.pressureCommand2, "psi");
            formatReadout(roPresMeas2, contState.pressureMeasurement2, "psi");
            formatReadout(roFlowVol1, contState.flowVolume1, "mL");
            formatReadout(roFlowErr1, contState.flowError1);
            formatReadout(roFlowVol2, contState.flowVolume2, "mL");
            formatReadout(roFlowErr2, contState.flowError2);
        }

        private void disableReadout(Label label)
        {
            label.Foreground = new BrushConverter().ConvertFrom("#AAA") as SolidColorBrush;
            label.Background = new BrushConverter().ConvertFrom("WhiteSmoke") as SolidColorBrush;
        }

        private void disableAllReadouts()
        {
            disableReadout(roPowerEnabled);
            disableReadout(roRobotHomed);
            disableReadout(roLinPos);
            disableReadout(roRotPos);
            disableReadout(roLinFlag1);
            disableReadout(roLinFlag2);
            disableReadout(roLinFlag3);
            disableReadout(roPresCmd1);
            disableReadout(roPresMeas1);
            disableReadout(roPresCmd2);
            disableReadout(roPresMeas2);
            disableReadout(roFlowVol1);
            disableReadout(roFlowErr1);
            disableReadout(roFlowVol2);
            disableReadout(roFlowErr2);
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
            string response = await ControllerManager.Instance.EnablePower();
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
            //string input = moveLinearLoadInput.Text;
            string response = await ControllerManager.Instance.MoveOneAxis(1, 100); //
            moveLinearLoadOutput.Content = response;
            lockRobotButtons(false);
        }



        

        private async void moveLinearCamBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = moveLinearCamInput.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"moveToCamera {input}");
            //moveLinearCamOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void moveLinearDisp1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = moveLinearDisp1Input.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"moveToDispense1 {input}");
            //moveLinearDisp1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void moveLinearDisp2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = moveLinearDisp2Input.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"moveToDispense2 {input}");
            //moveLinearDisp2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void spinTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = spinTimeInput.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"spinonly {input}");
            //spinTimeOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void openValve1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = openValve1Input.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"init1 {input}");
            //openValve1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void openValve2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = openValve2Input.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"init2 {input}");
            //openValve2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void closeValvesBtn_Click(object sender, RoutedEventArgs e)
        {
            lockStatusButtons(true);
            //string response = await ControllerManager.Instance.SendStatusCommandAsync("closeallvalves");
            ////closeValvesOutput.Content = response;
            lockStatusButtons(false);
        }

        private async void setZeroBothBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync("zeroshiftboth");
            //setZeroBothOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void startMeasure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync("startmeasureflow1");
            //startMeasure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void startMeasure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync("startmeasureflow2");
            //startMeasure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void stopMeasure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync("stopmeasureflow1");
            //stopMeasure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void stopMeasure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync("stopmeasureflow2");
            //stopMeasure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void setPressure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = setPressure1Input.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"setpressure1 {input}");
            //setPressure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void setPressure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = setPressure2Input.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"setpressure2 {input}");
            //setPressure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void shot1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = shot1Input.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"measuredShot1 {input}");
            //shot1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void shot2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = shot2Input.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"measuredShot2 {input}");
            //shot2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void connectLaserBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"connectToLaser");
            //connectLaserOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void disconnectLaserBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"disconnectFromLaser");
            //disconnectLaserBtn.Content = response;
            lockRobotButtons(false);
        }

        private async void getLaserSingleBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"getLaserSingleMeasurement");
            //getLaserSingleOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void getLaserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"getLaserRingMeasurements");
            //if (response.Split(" ").Length > 1)
            //{
            //    laserRingData = response.Split(" ")[1].Replace(";", "\n");
            //    getLaserRingOutput.Content = "(data collected)";
            //}
            //else
            //{
            //    getLaserRingOutput.Content = $"error: {response}";
            //}
            lockRobotButtons(false);
        }

        private async void getLaserMagBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"getLaserMagMeasurements");
            //if (response.Split(" ").Length > 1)
            //{
            //    laserMagData = response.Split(" ")[1].Replace(";", "\n");
            //    getLaserMagOutput.Content = "(data collected)";
            //}
            //else
            //{
            //    getLaserMagOutput.Content = $"error: {response}";
            //}
            lockRobotButtons(false);
        }

        private async void getLaserCustomBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = getLaserCustomInput.Text;
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"getLaserCustomMeasurements {input}");
            //if (response.Split(" ").Length > 1)
            //{
            //    laserCustomData = response.Split(" ")[1].Replace(";", "\n");
            //    getLaserCustomOutput.Content = "(data collected)";
            //}
            //else
            //{
            //    getLaserCustomOutput.Content = $"error: {response}";
            //}
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
            //string response = await ControllerManager.Instance.SendRobotCommandAsync($"dispenseShotsToRing {input}");
            //dispShotsOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void eStopBtn_Click(object sender, RoutedEventArgs e)
        {
            //string response = await ControllerManager.Instance.SendStatusCommandAsync($"halt");
            //response = await ControllerManager.Instance.SendStatusCommandAsync($"hp 0");
        }
    }
}
