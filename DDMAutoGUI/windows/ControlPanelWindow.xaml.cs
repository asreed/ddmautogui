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

            App.UIManager.UIStateChanged += debugWindow_OnUpdateUIState;
            App.ControllerManager.ControllerStateChanged += debugWindow_OnUpdateAutoControllerState;

            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            versionManagerLabel.Content = $"{v.Major}.{v.Minor}.{v.Build}";

            if (App.UIManager.UI_STATE.isConnected)
            {
                debugWindow_OnControllerConnected(this, EventArgs.Empty);
                debugWindow_OnUpdateAutoControllerState(this, EventArgs.Empty);
            }
            else
            {
                debugWindow_OnControllerDisconnected(this, EventArgs.Empty);
                debugWindow_OnUpdateAutoControllerState(this, EventArgs.Empty);
            }

            motorSizeSelect.SelectedIndex = SizeIdxFromEnum(App.SettingsManager.selectedSize);
            DisplaySettingsToPanel();

        }
 
        private int SizeIdxFromEnum(SettingsManager.DDMSize size)
        {
            switch (size)
            {
                case SettingsManager.DDMSize.ddm_57:
                    return 0;
                case SettingsManager.DDMSize.ddm_95:
                    return 1;
                case SettingsManager.DDMSize.ddm_116:
                    return 2;
                case SettingsManager.DDMSize.ddm_170:
                    return 3;
                case SettingsManager.DDMSize.ddm_170_tall:
                    return 4;
                default:
                    return -1;
            }
        }

        private SettingsManager.DDMSize SizeEnumFromIdx(int idx)
        {
            switch (idx)
            {
                case 0:
                    return SettingsManager.DDMSize.ddm_57;
                case 1:
                    return SettingsManager.DDMSize.ddm_95;
                case 2:
                    return SettingsManager.DDMSize.ddm_116;
                case 3:
                    return SettingsManager.DDMSize.ddm_170;
                case 4:
                    return SettingsManager.DDMSize.ddm_170_tall;
                default:
                    return SettingsManager.DDMSize.none;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.ControllerManager.StopAutoControllerState();
        }




        private async void debugWindow_OnControllerConnected(object sender, EventArgs e)
        {
            connectedBox.Foreground = new BrushConverter().ConvertFrom("Black") as SolidColorBrush;
            connectedBox.Text = "Connected";

            versionTCSLabel.Content = await App.ControllerManager.GetTCSVersion();
            versionConfigLabel.Content = await App.ControllerManager.GetPACVersion();

            lockRobotButtons(false);
            lockStatusButtons(false);

            eStopBtn.IsEnabled = true;
            eCloseValvesBtn.IsEnabled = true;

            App.ControllerManager.StartAutoControllerState();
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

            App.ControllerManager.StopAutoControllerState();
        }

        private async void debugWindow_OnUpdateUIState(object sender, EventArgs e)
        {
            


            // ... ?
        }

        private void debugWindow_OnUpdateAutoControllerState(object sender, EventArgs e)
        {

            ControllerState contState = App.ControllerManager.CONTROLLER_STATE;

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
            moveLoadBtn.IsEnabled = !state;
            moveCamTopBtn.IsEnabled = !state;
            moveCamSideBtn.IsEnabled = !state;
            moveLaserRingBtn.IsEnabled = !state;
            moveLaserMagBtn.IsEnabled = !state;
            moveDispIDBtn.IsEnabled = !state;
            moveDispODBtn.IsEnabled = !state;
            moveSpinBtn.IsEnabled = !state;

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

        private void DisplaySettingsToPanel()
        {
            DDMSettings s = App.SettingsManager.SETTINGS;
            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();

            if (m != null)
            {
                moveLoadInput.Content = $"[{s.common.load.x}, {s.common.load.t}]";
                moveCamTopInput.Content = $"[{s.common.camera_top.x}, {s.common.camera_top.t}]";
                moveCamSideInput.Content = $"[{m.camera_side.x}, {m.camera_side.t}]";
                moveLaserRingInput.Content = $"[{m.laser_ring.x}, {m.laser_ring.t}]";
                moveLaserMagInput.Content = $"[{m.laser_mag.x}, {m.laser_mag.t}]";
                moveDispIDInput.Content = $"[{m.disp_id.x}, {m.disp_id.t}]";
                moveDispODInput.Content = $"[{m.disp_od.x}, {m.disp_od.t}]";
            }

        }

        private void lockStatusButtons(bool state)
        {
            //
        }











        private void motorSizeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.SettingsManager.selectedSize = SizeEnumFromIdx(motorSizeSelect.SelectedIndex);
            DisplaySettingsToPanel();
        }


        // =========== BUTTON HANDLERS

        private async void enablePowerBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string response = await App.ControllerManager.EnablePower();
            enablePowerOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            homeOutput.Content = "(not implemented)";
        }


        private async void moveLoadBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);

            DDMSettings s = App.SettingsManager.SETTINGS;
            float x = s.common.load.x.Value;
            float th = s.common.load.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveLoadOutput.Content = response;

            lockRobotButtons(false);
        }


        private async void moveCamTopBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);

            DDMSettings s = App.SettingsManager.SETTINGS;
            float x = s.common.camera_top.x.Value;
            float th = s.common.camera_top.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveCamTopOutput.Content = response;

            lockRobotButtons(false);
        }

        private async void moveCamSideBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.camera_side.x.Value;
            float th = m.camera_side.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveCamSideOutput.Content = response;

            lockRobotButtons(false);
        }

        private async void moveLaserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.laser_ring.x.Value;
            float th = m.laser_ring.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveLaserRingOutput.Content = response;

            lockRobotButtons(false);
        }
        private async void moveLaserMagBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.laser_mag.x.Value;
            float th = m.laser_mag.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveLaserMagOutput.Content = response;

            lockRobotButtons(false);
        }
        private async void moveDispIDBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.disp_id.x.Value;
            float th = m.disp_id.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveDispIDOutput.Content = response;

            lockRobotButtons(false);
        }
        private async void moveDispODBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.disp_od.x.Value;
            float th = m.disp_od.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveDispODOutput.Content = response;

            lockRobotButtons(false);
        }

        private async void moveSpinBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float time = 4;
            float speed = 150;

            string response = await App.ControllerManager.SpinInPlace(time, speed);
            moveDispIDOutput.Content = response;

            lockRobotButtons(false);
        }

        private async void openValve1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = openValve1Input.Text;
            //string response = await App.ControllerManager.SendRobotCommandAsync($"init1 {input}");
            //openValve1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void openValve2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = openValve2Input.Text;
            //string response = await App.ControllerManager.SendRobotCommandAsync($"init2 {input}");
            //openValve2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void closeValvesBtn_Click(object sender, RoutedEventArgs e)
        {
            lockStatusButtons(true);
            //string response = await App.ControllerManager.SendStatusCommandAsync("closeallvalves");
            ////closeValvesOutput.Content = response;
            lockStatusButtons(false);
        }

        private async void setZeroBothBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync("zeroshiftboth");
            //setZeroBothOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void startMeasure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync("startmeasureflow1");
            //startMeasure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void startMeasure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync("startmeasureflow2");
            //startMeasure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void stopMeasure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync("stopmeasureflow1");
            //stopMeasure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void stopMeasure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync("stopmeasureflow2");
            //stopMeasure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void setPressure1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = setPressure1Input.Text;
            //string response = await App.ControllerManager.SendRobotCommandAsync($"setpressure1 {input}");
            //setPressure1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void setPressure2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = setPressure2Input.Text;
            //string response = await App.ControllerManager.SendRobotCommandAsync($"setpressure2 {input}");
            //setPressure2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void shot1Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = shot1Input.Text;
            //string response = await App.ControllerManager.SendRobotCommandAsync($"measuredShot1 {input}");
            //shot1Output.Content = response;
            lockRobotButtons(false);
        }

        private async void shot2Btn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            string input = shot2Input.Text;
            //string response = await App.ControllerManager.SendRobotCommandAsync($"measuredShot2 {input}");
            //shot2Output.Content = response;
            lockRobotButtons(false);
        }

        private async void connectLaserBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync($"connectToLaser");
            //connectLaserOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void disconnectLaserBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync($"disconnectFromLaser");
            //disconnectLaserBtn.Content = response;
            lockRobotButtons(false);
        }

        private async void getLaserSingleBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync($"getLaserSingleMeasurement");
            //getLaserSingleOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void getLaserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            lockRobotButtons(true);
            //string response = await App.ControllerManager.SendRobotCommandAsync($"getLaserRingMeasurements");
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
            //string response = await App.ControllerManager.SendRobotCommandAsync($"getLaserMagMeasurements");
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
            //string response = await App.ControllerManager.SendRobotCommandAsync($"getLaserCustomMeasurements {input}");
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
            //string response = await App.ControllerManager.SendRobotCommandAsync($"dispenseShotsToRing {input}");
            //dispShotsOutput.Content = response;
            lockRobotButtons(false);
        }

        private async void eStopBtn_Click(object sender, RoutedEventArgs e)
        {
            //string response = await App.ControllerManager.SendStatusCommandAsync($"halt");
            //response = await App.ControllerManager.SendStatusCommandAsync($"hp 0");
        }
    }
}
