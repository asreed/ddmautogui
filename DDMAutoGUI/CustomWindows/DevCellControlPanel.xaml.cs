using DDMAutoGUI.Services;
using DDMAutoGUI.windows;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for DevCellControlPanel.xaml.
    /// Hosts the developer-only manual cell/robot console: live System State readouts,
    /// motor-size selection, and the per-axis move/measure/dispense commands. State
    /// readouts and button enablement come from the inherited MainWindowViewModel; this
    /// control owns the manual command actions and the motor-settings label display.
    /// </summary>
    public partial class DevCellControlPanel : UserControl
    {
        private readonly IControllerService _controllerManager;
        private readonly ISettingsService _settingsManager;

        private List<ResultsHeightMeasurement> laserRingData;
        private List<ResultsHeightMeasurement> laserMagData;

        public DevCellControlPanel()
        {
            InitializeComponent();

            _controllerManager = App.Services?.GetService<IControllerService>();
            _settingsManager = App.Services?.GetService<ISettingsService>();
        }

        // ==================================================================
        // Motor settings display
        // MainWindow drives these on connect/disconnect, preserving the original
        // HandleConnected/HandleDisconnected behavior now that the controls live here.

        /// <summary>
        /// Loads the settings for the currently selected motor size into the readout labels.
        /// </summary>
        public void RefreshMotorSettings()
        {
            if (_settingsManager == null) return;

            _settingsManager.SelectedSize = SizeEnumFromIdx(Adv_Cell_MotorSizeCmb.SelectedIndex);
            DisplaySettingsToPanel();
        }

        /// <summary>
        /// Resets the motor-settings readout labels to the blank placeholder.
        /// </summary>
        public void ClearMotorSettings()
        {
            BlankOutMotorSettings();
        }

        private void DisplaySettingsToPanel()
        {
            if (_settingsManager == null) return;

            CellSettings s = _settingsManager.GetAllSettings();
            CSMotor m = _settingsManager.GetSettingsForSelectedSize();

            if (m != null && m.IsValid())
            {
                Adv_Cell_MoveLoadInLbl.Content = $"[{s.ddm_common.pos_load.x}, {s.ddm_common.pos_load.t}]";
                Adv_Cell_MoveCamTopInLbl.Content = $"[{s.ddm_common.pos_camera_top.x}, {s.ddm_common.pos_camera_top.t}]";
                Adv_Cell_MoveCamSideInLbl.Content = $"[{m.pos_camera_side.x}, {m.pos_camera_side.t}]";
                Adv_Cell_MoveLaserRingInLbl.Content = $"[{m.pos_laser_ring.x}, {m.pos_laser_ring.t}]";
                Adv_Cell_MoveLaserMagInLbl.Content = $"[{m.pos_laser_mag.x}, {m.pos_laser_mag.t}]";
                Adv_Cell_MoveDispCAP1Lbl.Content = $"[{m.pos_ca_p1.x}, {m.pos_ca_p1.t}]";
                Adv_Cell_MoveDispCAP2Lbl.Content = $"[{m.pos_ca_p2.x}, {m.pos_ca_p2.t}]";
                Adv_Cell_MoveDispCAP3Lbl.Content = $"[{m.pos_ca_p3.x}, {m.pos_ca_p3.t}]";
                Adv_Cell_MoveDispCAP4Lbl.Content = $"[{m.pos_ca_p4.x}, {m.pos_ca_p4.t}]";
                Adv_Cell_MoveDispUVP1Lbl.Content = $"[{m.pos_uv_p1.x}, {m.pos_uv_p1.t}]";
                Adv_Cell_MoveDispUVCureLbl.Content = $"[{m.pos_uv_cure.x}, {m.pos_uv_cure.t}]";
                Adv_Cell_MoveHallInLbl.Content = $"[{m.pos_hall_sensor.x}, {m.pos_hall_sensor.t}]";
                Adv_Cell_MeasureRingInLbl.Content = $"{m.laser_ring_num} places, {s.laser_delay} s each";
                Adv_Cell_MeasureMagInLbl.Content = $"{m.laser_mag_num} places, {s.laser_delay} s each";
            }
            else
            {
                BlankOutMotorSettings();
            }
        }

        private void BlankOutMotorSettings()
        {
            string blank = "-";
            Adv_Cell_MoveLoadInLbl.Content = blank;
            Adv_Cell_MoveCamTopInLbl.Content = blank;
            Adv_Cell_MoveCamSideInLbl.Content = blank;
            Adv_Cell_MoveLaserRingInLbl.Content = blank;
            Adv_Cell_MoveLaserMagInLbl.Content = blank;
            Adv_Cell_MoveDispCAP1Lbl.Content = blank;
            Adv_Cell_MoveDispCAP2Lbl.Content = blank;
            Adv_Cell_MoveDispCAP3Lbl.Content = blank;
            Adv_Cell_MoveDispCAP4Lbl.Content = blank;
            Adv_Cell_MoveDispUVP1Lbl.Content = blank;
            Adv_Cell_MoveDispUVCureLbl.Content = blank;
            Adv_Cell_MoveHallInLbl.Content = blank;
            Adv_Cell_MeasureRingInLbl.Content = blank;
            Adv_Cell_MeasureMagInLbl.Content = blank;
        }

        private SettingsService.DDMSize SizeEnumFromIdx(int idx) => idx switch
        {
            0 => SettingsService.DDMSize.ddm_57,
            1 => SettingsService.DDMSize.ddm_95,
            2 => SettingsService.DDMSize.ddm_116,
            3 => SettingsService.DDMSize.ddm_170,
            4 => SettingsService.DDMSize.ddm_170_tall,
            _ => SettingsService.DDMSize.none
        };

        private void Adv_Cell_MotorSizeCmb_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                RefreshMotorSettings();
            }
        }

        private void Adv_Cell_ReloadSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsManager == null) return;

            _settingsManager.ReloadSettings();
            RefreshMotorSettings();
        }

        // ==================================================================
        // Pendant

        private async void Adv_Cell_EnableBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            string response = await _controllerManager.EnablePower();
            Adv_Cell_EnableOutLbl.Content = response;
        }

        // ==================================================================
        // Movement

        private async void Adv_Cell_MoveLoadBtn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetAllSettings()?.ddm_common?.pos_load, Adv_Cell_MoveLoadOutLbl);

        private async void Adv_Cell_MoveCamTopBtn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetAllSettings()?.ddm_common?.pos_camera_top, Adv_Cell_MoveCamTopOutLbl);

        private async void Adv_Cell_MoveCamSideBtn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_camera_side, Adv_Cell_MoveCamSideOutLbl);

        private async void Adv_Cell_MoveLaserRingBtn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_laser_ring, Adv_Cell_MoveLaserRingOutLbl);

        private async void Adv_Cell_MoveLaserMagBtn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_laser_mag, Adv_Cell_MoveLaserMagOutLbl);

        //private async void Adv_Cell_MoveDispIDBtn_Click(object sender, RoutedEventArgs e)
        //    => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.id_disp, Adv_Cell_MoveDispIDOutLbl);

        //private async void Adv_Cell_MoveDispODBtn_Click(object sender, RoutedEventArgs e)
        //    => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.od_disp, Adv_Cell_MoveDispODOutLbl);

        private async void Adv_Cell_MoveHallBtn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_hall_sensor, Adv_Cell_MoveHallOutLbl);

        /// <summary>
        /// Shared move helper: performs a MoveJ to the given position and writes the
        /// controller response to the supplied output label.
        /// </summary>
        private async Task MoveToAsync(CSLocation? position, Label output)
        {
            if (_controllerManager == null || position?.x == null || position?.t == null) return;

            output.Content = await _controllerManager.MoveJ(position.x.Value, position.t.Value);
        }

        // ==================================================================
        // Height sensor

        private async void Adv_Cell_MeasureRingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MeasureHeightsContinuous(m.pos_laser_ring.x.Value, m.pos_laser_ring.t.Value, m.laser_ring_num.Value, 10);
            laserRingData = _controllerManager.ParseHeightData(response);
            Adv_Cell_MeasureRingOutLbl.Content = laserRingData.Count > 0 ? "(data collected)" : $"error: {response}";
        }

        private async void Adv_Cell_MeasureMagBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null || _settingsManager == null) return;

            CSMotor m = _settingsManager.GetSettingsForSelectedSize();
            string response = await _controllerManager.MeasureHeightsContinuous(m.pos_laser_mag.x.Value, m.pos_laser_mag.t.Value, m.laser_mag_num.Value, 20);
            laserMagData = _controllerManager.ParseHeightData(response);
            Adv_Cell_MeasureMagOutLbl.Content = laserMagData.Count > 0 ? "(data collected)" : $"error: {response}";
        }

        private void Adv_Cell_ShowRingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (laserRingData != null && laserRingData.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var d in laserRingData)
                {
                    sb.AppendLine($"{d.t}, {d.z}");
                }
                TextDataViewer viewer = new TextDataViewer { Owner = Window.GetWindow(this) };
                viewer.PopulateData(sb.ToString(), "Ring Displacement Measurements");
                viewer.Show();
            }
        }

        private void Adv_Cell_ShowMagBtn_Click(object sender, RoutedEventArgs e)
        {
            if (laserMagData != null && laserMagData.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var d in laserMagData)
                {
                    sb.AppendLine($"{d.t}, {d.z}");
                }
                TextDataViewer viewer = new TextDataViewer { Owner = Window.GetWindow(this) };
                viewer.PopulateData(sb.ToString(), "Magnet Displacement Measurements");
                viewer.Show();
            }
        }

        private async void Adv_Cell_MeasureSingleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            string response = await _controllerManager.MeasureHeightSingle();
            Adv_Cell_MeasureSingleOutLbl.Content = response;
        }

        // ==================================================================
        // Dispense setup

        private async void Adv_Cell_SetPres1Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            string response = await _controllerManager.SetRegPressureAndWait(1, float.Parse(Adv_Cell_SetPres1InTxt.Text), 10);
            Adv_Cell_SetPres1OutLbl.Content = response;
        }

        private async void Adv_Cell_SetPres2Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            string response = await _controllerManager.SetRegPressureAndWait(2, float.Parse(Adv_Cell_SetPres2InTxt.Text), 10);
            Adv_Cell_SetPres2OutLbl.Content = response;
        }

        private async void Adv_Cell_Shot1Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            string response = await _controllerManager.MeasureShotTimed(1, float.Parse(Adv_Cell_Shot1InTxt.Text));
            Adv_Cell_Shot1OutLbl.Content = response;
        }

        private async void Adv_Cell_Shot2Btn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            string response = await _controllerManager.MeasureShotTimed(2, float.Parse(Adv_Cell_Shot2InTxt.Text));
            Adv_Cell_Shot2OutLbl.Content = response;
        }

        private async void Adv_Cell_SetZeroBothBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            string response = await _controllerManager.SetZeroShift(3.0f);
            Adv_Cell_SetZeroBothLbl.Content = response;
        }

        //private async void Adv_Cell_StartMeas1Btn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_controllerManager == null) return;

        //    string response = await _controllerManager.SetShotTrigger(1, true);
        //    Adv_Cell_StartMeas1OutLbl.Content = response;
        //}

        //private async void Adv_Cell_StopMeas1Btn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_controllerManager == null) return;

        //    string response = await _controllerManager.SetShotTrigger(1, false);
        //    Adv_Cell_StopMeas1OutLbl.Content = response;
        //}

        //private async void Adv_Cell_StartMeas2Btn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_controllerManager == null) return;

        //    string response = await _controllerManager.SetShotTrigger(2, true);
        //    Adv_Cell_StartMeas2OutLbl.Content = response;
        //}

        //private async void Adv_Cell_StopMeas2Btn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (_controllerManager == null) return;

        //    string response = await _controllerManager.SetShotTrigger(2, false);
        //    Adv_Cell_StopMeas2OutLbl.Content = response;
        //}

        // ==================================================================
        // Dispense

        private async void Dev_ST_DispBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_controllerManager == null) return;

            int valveNum = int.Parse(Dev_ST_ValveNumberTxb.Text);
            float time = float.Parse(Dev_ST_ShotTimeTxb.Text);
            float x = float.Parse(Dev_ST_StartXPosTxb.Text);
            float t = float.Parse(Dev_ST_StartThetaPosTxb.Text);
            int dir = 1;

            if (valveNum != 1 && valveNum != 2)
            {
                Dev_ST_DispOutputLbl.Content = "Error: Valve number must be 1 or 2";
                return;
            }

            if (time <= 0)
            {
                Dev_ST_DispOutputLbl.Content = "Error: Shot time must be positive";
                return;
            }

            string response = await _controllerManager.DispenseSingleTrackToRing(valveNum, time, x, t, dir);
            Dev_ST_DispOutputLbl.Content = response;
        }

        private async void Adv_Cell_MoveDispCAP1Btn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_ca_p1, Adv_Cell_MoveDispCAP1OutLbl);

        private async void Adv_Cell_MoveDispCAP2Btn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_ca_p2, Adv_Cell_MoveDispCAP2OutLbl);

        private async void Adv_Cell_MoveDispCAP3Btn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_ca_p3, Adv_Cell_MoveDispCAP3OutLbl);

        private async void Adv_Cell_MoveDispCAP4Btn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_ca_p4, Adv_Cell_MoveDispCAP4OutLbl);

        private async void Adv_Cell_MoveDispUVP1Btn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_uv_p1, Adv_Cell_MoveDispUVP1OutLbl);

        private async void Adv_Cell_MoveDispUVCureBtn_Click(object sender, RoutedEventArgs e)
            => await MoveToAsync(_settingsManager?.GetSettingsForSelectedSize()?.pos_uv_cure, Adv_Cell_MoveDispUVCureOutLbl);
    }
}