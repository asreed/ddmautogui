using ArenaNET;
using DDMAutoGUI.utilities;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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

        public List<DDMResultsSingleHeight> laserRingData;
        public List<DDMResultsSingleHeight> laserMagData;


        public ControlPanelWindow()
        {
            InitializeComponent();

            App.ControllerManager.ControllerConnected += debugWindow_OnControllerConnected;
            App.ControllerManager.ControllerDisconnected += debugWindow_OnControllerDisconnected;
            App.ControllerManager.ControllerStateChanged += debugWindow_OnUpdateAutoControllerState;

            versionManagerLabel.Content = App.UIManager.GetAppVersionString();

            if (App.UIManager.UI_STATE.isConnected)
            {
                debugWindow_OnControllerConnected(this, EventArgs.Empty);
            }
            else
            {
                debugWindow_OnControllerDisconnected(this, EventArgs.Empty);
            }

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

            LockRobotButtons(false);
            LockStatusButtons(false);
            motorSizeSelect.IsEnabled = true;
            PopulateMotorSettings(motorSizeSelect);

            eStopBtn.IsEnabled = true;
            eCloseValvesBtn.IsEnabled = true;

            App.ControllerManager.StartAutoControllerState();
        }

        private void debugWindow_OnControllerDisconnected(object sender, EventArgs e)
        {

            Debug.Print("Disconnected event called");

            connectedBox.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
            connectedBox.Text = "Not connected";
            statusBox.Text = "-";

            versionTCSLabel.Content = "-";
            versionConfigLabel.Content = "-";

            LockRobotButtons(true);
            LockStatusButtons(true);
            motorSizeSelect.IsEnabled = false;
            DisableAllReadouts();
            BlankOutMotorSettings();

            eStopBtn.IsEnabled = false;
            eCloseValvesBtn.IsEnabled = false;

            App.ControllerManager.StopAutoControllerState();
        }

        private void debugWindow_OnUpdateAutoControllerState(object sender, EventArgs e)
        {

            ControllerState contState = App.ControllerManager.CONTROLLER_STATE;

            if (!contState.parseError)
            {
                if (App.UIManager.UI_STATE.isConnected)
                {
                    statusBox.Foreground = new BrushConverter().ConvertFrom("Black") as SolidColorBrush;
                    statusBox.Text = "Parse OK";
                    FormatAllReadouts(contState);
                }
                else
                {
                    statusBox.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
                    statusBox.Text = "-";
                    DisableAllReadouts();
                }
            }
            else
            {
                statusBox.Foreground = new BrushConverter().ConvertFrom("Red") as SolidColorBrush;
                statusBox.Text = $"Parse error: {contState.parseErrorMessage}";
                DisableAllReadouts();
            }

        }

        private void FormatReadout(Label label, float value)
        {
            label.Content = value.ToString();

        }
        private void FormatReadout(Label label, float value, string unit)
        {
            label.Content = value.ToString() + " " + unit;

        }
        private void FormatReadout(Label label, bool value)
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

        private void FormatAllReadouts(ControllerState contState)
        {
            FormatReadout(roPowerEnabled, contState.isPowerEnabled);
            FormatReadout(roRobotHomed, contState.isRobotHomed);
            FormatReadout(roLinPos, contState.posLinear);
            FormatReadout(roRotPos, contState.posRotary);
            FormatReadout(roLinFlag1, !contState.isLinearIn1); // rail sensors are low when part present
            FormatReadout(roLinFlag2, !contState.isLinearIn2);
            FormatReadout(roLinFlag3, !contState.isLinearIn3);
            FormatReadout(roPresCmd1, contState.pressureCommand1, "psi");
            FormatReadout(roPresMeas1, contState.pressureMeasurement1, "psi");
            FormatReadout(roPresCmd2, contState.pressureCommand2, "psi");
            FormatReadout(roPresMeas2, contState.pressureMeasurement2, "psi");
            FormatReadout(roFlowVol1, contState.flowVolume1, "mL");
            FormatReadout(roFlowErr1, contState.flowError1);
            FormatReadout(roFlowVol2, contState.flowVolume2, "mL");
            FormatReadout(roFlowErr2, contState.flowError2);
        }

        private void DisableReadout(Label label)
        {
            label.Foreground = new BrushConverter().ConvertFrom("#AAA") as SolidColorBrush;
            label.Background = new BrushConverter().ConvertFrom("WhiteSmoke") as SolidColorBrush;
        }

        private void DisableAllReadouts()
        {
            DisableReadout(roPowerEnabled);
            DisableReadout(roRobotHomed);
            DisableReadout(roLinPos);
            DisableReadout(roRotPos);
            DisableReadout(roLinFlag1);
            DisableReadout(roLinFlag2);
            DisableReadout(roLinFlag3);
            DisableReadout(roPresCmd1);
            DisableReadout(roPresMeas1);
            DisableReadout(roPresCmd2);
            DisableReadout(roPresMeas2);
            DisableReadout(roFlowVol1);
            DisableReadout(roFlowErr1);
            DisableReadout(roFlowVol2);
            DisableReadout(roFlowErr2);
        }

        private void LockRobotButtons(bool state)
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

            laserRingBtn.IsEnabled = !state;
            laserMagBtn.IsEnabled = !state;

            setPressure1Btn.IsEnabled = !state;
            setPressure2Btn.IsEnabled = !state;
            shot1Btn.IsEnabled = !state;
            shot2Btn.IsEnabled = !state;

            setZeroBothBtn.IsEnabled = !state;
            startMeasure1Btn.IsEnabled = !state;
            stopMeasure1Btn.IsEnabled = !state;
            startMeasure2Btn.IsEnabled = !state;
            stopMeasure2Btn.IsEnabled = !state;

            dispShotsBtn.IsEnabled = !state;

            Debug.Print($"All buttons stated changed {state.ToString()}");
        }

        private void BlankOutMotorSettings()
        {
            string blank = "-";

            moveLoadInput.Content = blank;
            moveCamTopInput.Content = blank;
            moveCamSideInput.Content = blank;
            moveLaserRingInput.Content = blank;
            moveLaserMagInput.Content = blank;
            moveDispIDInput.Content = blank;
            moveDispODInput.Content = blank;
            moveSpinInput.Content = blank;

            laserRingInput.Content = blank;
            laserMagInput.Content = blank;

            dispShotsInput.Content = blank;
            dispShotsInput.Content += blank;

        }

        private void DisplaySettingsToPanel()
        {
            DDMSettings s = App.SettingsManager.SETTINGS;
            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();


            if (m != null && m.IsValid())
            {
                LockRobotButtons(false);

                DDMSettingShotCalibration c = m.shot_calibration;

                moveLoadInput.Content = $"[{s.common.load.x}, {s.common.load.t}]";
                moveCamTopInput.Content = $"[{s.common.camera_top.x}, {s.common.camera_top.t}]";
                moveCamSideInput.Content = $"[{m.camera_side.x}, {m.camera_side.t}]";
                moveLaserRingInput.Content = $"[{m.laser_ring.x}, {m.laser_ring.t}]";
                moveLaserMagInput.Content = $"[{m.laser_mag.x}, {m.laser_mag.t}]";
                moveDispIDInput.Content = $"[{m.disp_id.x}, {m.disp_id.t}]";
                moveDispODInput.Content = $"[{m.disp_od.x}, {m.disp_od.t}]";
                moveSpinInput.Content = $"{c.spin_time}s, {c.spin_speed}%";

                laserRingInput.Content = $"{m.laser_ring_num} places";
                laserMagInput.Content = $"{m.laser_mag_num} places";

                dispShotsInput.Content = $"ID: Valve {c.valve_num_id}, x={m.disp_id.x} mm, {c.time_id} s, target {c.target_vol_id }mL\n";
                dispShotsInput.Content += $"OD: Valve {c.valve_num_od}, x={m.disp_od.x} mm, {c.time_od} s, target {c.target_vol_od} mL";
            }
            else
            {
                LockRobotButtons(true);
                BlankOutMotorSettings();
            }

        }

        private void LockStatusButtons(bool state)
        {
            //
        }

        private void PopulateMotorSettings(ComboBox selection)
        {
            App.SettingsManager.selectedSize = SizeEnumFromIdx(selection.SelectedIndex);
            DisplaySettingsToPanel();

        }











        private void motorSizeSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                PopulateMotorSettings(motorSizeSelect);
            }
        }









        // =========== BUTTON HANDLERS

        private async void enablePowerBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.EnablePower();
            enablePowerOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void homeBtn_Click(object sender, RoutedEventArgs e)
        {
            homeOutput.Content = "(not implemented)";
        }


        private async void moveLoadBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettings s = App.SettingsManager.SETTINGS;
            float x = s.common.load.x.Value;
            float th = s.common.load.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveLoadOutput.Content = response;

            LockRobotButtons(false);
        }


        private async void moveCamTopBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettings s = App.SettingsManager.SETTINGS;
            float x = s.common.camera_top.x.Value;
            float th = s.common.camera_top.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveCamTopOutput.Content = response;

            LockRobotButtons(false);
        }

        private async void moveCamSideBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.camera_side.x.Value;
            float th = m.camera_side.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveCamSideOutput.Content = response;

            LockRobotButtons(false);
        }

        private async void moveLaserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.laser_ring.x.Value;
            float th = m.laser_ring.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveLaserRingOutput.Content = response;

            LockRobotButtons(false);
        }
        private async void moveLaserMagBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.laser_mag.x.Value;
            float th = m.laser_mag.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveLaserMagOutput.Content = response;

            LockRobotButtons(false);
        }
        private async void moveDispIDBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.disp_id.x.Value;
            float th = m.disp_id.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveDispIDOutput.Content = response;

            LockRobotButtons(false);
        }
        private async void moveDispODBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float x = m.disp_od.x.Value;
            float th = m.disp_od.t.Value;

            string response = await App.ControllerManager.MoveJ(x, th);
            moveDispODOutput.Content = response;

            LockRobotButtons(false);
        }

        private async void moveSpinBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float time = m.shot_calibration.spin_time.Value;
            float speed = m.shot_calibration.spin_speed.Value;

            string response = await App.ControllerManager.SpinInPlace(time, speed);
            moveDispIDOutput.Content = response;

            LockRobotButtons(false);
        }

        private async void eCloseAllValvesBtn_Click(object sender, RoutedEventArgs e)
        {
            LockStatusButtons(true);
            string response = await App.ControllerManager.CloseAllValves();
            LockStatusButtons(false);
        }

        private async void setZeroBothBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetZeroShift(3.0f);
            setZeroBothOutput.Content = response;
            LockRobotButtons(false);
        }

        private async void startMeasure1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetShotTrigger(1, true);
            startMeasure1Output.Content = response;
            LockRobotButtons(false);
        }

        private async void startMeasure2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetShotTrigger(2, true);
            startMeasure1Output.Content = response;
            LockRobotButtons(false);
        }

        private async void stopMeasure1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetShotTrigger(1, false);
            startMeasure1Output.Content = response;
            LockRobotButtons(false);
        }

        private async void stopMeasure2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string response = await App.ControllerManager.SetShotTrigger(2, false);
            startMeasure1Output.Content = response;
            LockRobotButtons(false);
        }

        private async void setPressure1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = setPressure1Input.Text;
            string response = await App.ControllerManager.SetRegPressure(1, float.Parse(input));
            startMeasure1Output.Content = response;
            LockRobotButtons(false);
        }

        private async void setPressure2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = setPressure2Input.Text;
            string response = await App.ControllerManager.SetRegPressure(2, float.Parse(input));
            setPressure2Output.Content = response;
            LockRobotButtons(false);
        }

        private async void shot1Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = shot1Input.Text;
            string response = await App.ControllerManager.MeasureShotTimed(1, float.Parse(input));
            shot1Output.Content = response;
            LockRobotButtons(false);
        }

        private async void shot2Btn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);
            string input = shot2Input.Text;
            string response = await App.ControllerManager.MeasureShotTimed(2, float.Parse(input));
            shot1Output.Content = response;
            LockRobotButtons(false);
        }

        private async void laserRingBtn_Click(object sender, RoutedEventArgs e)
        {
            DDMSettings s = App.SettingsManager.SETTINGS;
            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float xPos = m.laser_ring.x.Value;
            float tPos = m.laser_ring.t.Value;
            int n = m.laser_ring_num.Value;
            float d = s.common.laser_delay.Value;

            LockRobotButtons(true);
            string response = await App.ControllerManager.MeasureHeights(xPos, tPos, n, d);
            laserRingData = App.ControllerManager.ParseHeightData(response);

            if (laserRingData.Count > 0)
            {
                laserRingOutput.Content = $"(data collected)";
            }
            else
            {
                laserRingOutput.Content = $"error: {response}";
            }

            LockRobotButtons(false);
        }

        private async void laserMagBtn_Click(object sender, RoutedEventArgs e)
        {
            DDMSettings s = App.SettingsManager.SETTINGS;
            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            float xPos = m.laser_mag.x.Value;
            float tPos = m.laser_mag.t.Value;
            int n = m.laser_mag_num.Value;
            float d = s.common.laser_delay.Value;

            LockRobotButtons(true);
            string response = await App.ControllerManager.MeasureHeights(xPos, tPos, n, d);
            laserMagData = App.ControllerManager.ParseHeightData(response);

            if (laserMagData.Count > 0)
            {
                laserMagOutput.Content = $"(data collected)";
            }
            else
            {
                laserMagOutput.Content = $"error: {response}";
            }
            LockRobotButtons(false);
        }


        private void showRingBtn_Click(object sender, RoutedEventArgs e)
        {
            //TextDataViewer viewer = new TextDataViewer();
            //viewer.Owner = this;
            //viewer.PopulateData(laserRingData, "Ring Displacement Measurements");
            //viewer.Show();
        }

        private void showMagBtn_Click(object sender, RoutedEventArgs e)
        {
            //TextDataViewer viewer = new TextDataViewer();
            //viewer.Owner = this;
            //viewer.PopulateData(laserMagData, "Magnet Displacement Measurements");
            //viewer.Show();
        }


        private async void dispShotsBtn_Click(object sender, RoutedEventArgs e)
        {
            LockRobotButtons(true);

            DDMSettingsSingleSize m = App.SettingsManager.GetSettingsForSelectedSize();
            DDMSettingShotCalibration c = m.shot_calibration;

            float x_id = m.disp_id.x.Value;
            float t_id = m.disp_id.t.Value;
            float time_id = c.time_id.Value;
            float valve_num_id = c.valve_num_id.Value;
            float pressure_id = c.pressure_1.Value;
            float target_vol_id = c.target_vol_id.Value;

            float x_od = m.disp_od.x.Value;
            float t_od = m.disp_od.t.Value;
            float time_od = c.time_od.Value;
            float valve_num_od = c.valve_num_od.Value;
            float pressure_od = c.pressure_2.Value;
            float target_vol_od = c.target_vol_od.Value;


            //string response = await App.ControllerManager.SpinInPlace(time, speed);
            string response = string.Empty;
            dispShotsOutput.Content = response;

            LockRobotButtons(false);
        }

        private async void eStopBtn_Click(object sender, RoutedEventArgs e)
        {
            await App.ControllerManager.EStop();
        }
    }
}
