using DDMAutoGUI.utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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

namespace DDMAutoGUI.windows
{
    /// <summary>
    /// Interaction logic for DispenseWindow2.xaml
    /// </summary>
    public partial class DispenseWindow2 : Window
    {


        private int currentStep = 0;
        private bool tabLock = true; // prevent user from clicking tabs directly
        private bool abortPreConfirmed = false;

        private ProcessResults processData;
        private DDMSettings settings;



        public DispenseWindow2()
        {
            InitializeComponent();

            processData = new ProcessResults();
            processData.AddToLog("Process window opened");
            processData.UpdateProcessLog += ProcessData_UpdateProcessLog;

            App.SettingsManager.ReadSettingsFile();
            settings = App.SettingsManager.SETTINGS;

            this.processData.AddToLog($"Settings loaded (last saved {settings.last_saved})");
            logTextBox.Text = "";

            GoToStep(0);
        }


        // ================ Main dispense process routine

        private async void DoProcess()
        {
            // pull data from user config (validate?)

            string sn = "TEST-SN-123456789";
            int motorSelection = motorSizeComboBox.SelectedIndex;

            bool doSNPhoto = snPhotoCheckBox.IsChecked ?? false;
            bool doPrePhoto = prePhotoCheckBox.IsChecked ?? false;
            bool doRingMeasure = measureRingCheckBox.IsChecked ?? false;
            bool doMagMeasure = measureMagCheckBox.IsChecked ?? false;
            bool doDispense = dispenseCheckBox.IsChecked ?? false;
            bool doPostPhoto = postPhotoCheckBox.IsChecked ?? false;

            // store relevant data in processData object

            processData.results.ring_sn = sn;

            // pull data from settings

            DDMSettingsSingleSize motor = new DDMSettingsSingleSize();

            switch (motorSelection)
            {
                case 0: // DDM 57
                    motor = settings.ddm_57;
                    break;

                case 1: // DDM 95
                    motor = settings.ddm_95;
                    break;

                case 2: // DDM 116
                    motor = settings.ddm_116;
                    break;

                case 3: // DDM 170
                    motor = settings.ddm_170;
                    break;

                case 4: // DDM 170 Tall
                    motor = settings.ddm_170_tall;
                    break;
            }

            processData.AddToLog($"Serial number: {sn}");

            // start process

            processData.AddToLog("Dispense process started");
            processProgressBar.IsIndeterminate = false;
            processProgressBar.Value = 0;
            GoToStep(1);

            // verify all sensors are online

            processData.AddToLog("Verifying sensors...");
            await Task.Delay(500);
            processData.AddToLog("Sensors verified");

            if (doSNPhoto)
            {
                // connect to side camera

                processData.AddToLog("Connecting to side camera...");
                await Task.Delay(500);
                processData.AddToLog("Camera connected");
                processProgressBar.Value = 5;

            }

            if (doPrePhoto || doPostPhoto)
            {
                // connect to top camera

                processData.AddToLog("Connecting to top camera...");
                await Task.Delay(500);
                processData.AddToLog("Camera connected");
                processProgressBar.Value = 10;

            }

            if (doPrePhoto)
            {
                // take photo before process

                processData.AddToLog("Taking photo...");
                processData.AddToLog($"Moving to [{settings.common.camera_top.x}, {settings.common.camera_top.t}]");
                await Task.Delay(1000);
                processData.AddToLog("Photo saved");
                processProgressBar.Value = 20;
            }
            if (doRingMeasure)
            {
                // measure magnet ring displacement

                processData.AddToLog("Measuring ring...");
                processData.AddToLog($"Moving to [{motor.laser_ring.x}, {motor.laser_ring.t}]");
                await Task.Delay(1000);

                string fakeResponse = "0 0.00,0.10;0.10,0.11;0.20,0.09;0.30,0.10";
                processData.results.ring_heights = App.ControllerManager.ParseHeightData(fakeResponse);

                processData.AddToLog("Data collected");
                processProgressBar.Value = 30;
            }
            if (doMagMeasure)
            {
                // measure magnet (and concentrator?) displacement

                processData.AddToLog("Measuring magnets...");
                processData.AddToLog($"Moving to [{motor.laser_mag.x}, {motor.laser_mag.t}]");
                await Task.Delay(1000);

                string fakeResponse = "0 0.00,0.10;0.10,0.11;0.20,0.09;0.30,0.10;0.40,0.12;0.50,0.11";
                processData.results.mag_heights = App.ControllerManager.ParseHeightData(fakeResponse);

                processData.AddToLog("Data collected");
                processProgressBar.Value = 40;

            }
            if (doDispense)
            {
                // dispense cyanoacrylate

                processData.AddToLog("Dispensing...");
                processData.AddToLog($"Using ID [{motor.disp_id.x}, {motor.disp_id.t}] and OD [{motor.disp_od.x}, {motor.disp_od.t}]");
                await Task.Delay(1000);
                processData.AddToLog("...");
                await Task.Delay(1000);
                processData.AddToLog("...");
                await Task.Delay(1000);

                processData.results.shot_id = new DDMResultsShot()
                {
                    valve_num = motor.shot_calibration.valve_num_id,
                    vol = 0.005f,
                    time = motor.shot_calibration.time_id,
                    pressure = motor.shot_calibration.pressure_id
                };
                processData.results.shot_od = new DDMResultsShot()
                {
                    valve_num = motor.shot_calibration.valve_num_od,
                    vol = 0.006f,
                    time = motor.shot_calibration.time_od,
                    pressure = motor.shot_calibration.pressure_od
                };
                processData.AddToLog("Dispense complete");
                processProgressBar.Value = 80;
            }
            if (doPostPhoto)
            {
                // take photo after process

                processData.AddToLog("Taking photo...");
                processData.AddToLog($"Moving to [{settings.common.camera_top.x}, {settings.common.camera_top.t}]");
                await Task.Delay(1000);
                processData.AddToLog("Photo saved");
                processProgressBar.Value = 90;
            }



            processData.AddToLog("Moving back to unload position...");
            processData.AddToLog($"Moving to [{settings.common.load.x}, {settings.common.load.t}]");
            processProgressBar.Value = 100;
            await Task.Delay(1000);

            processData.AddToLog("Process complete");
            await Task.Delay(1000);



            GoToStep(2);
        }


















        // ================ Helpers

        private void GoToStep(int step)
        {
            // called when moving to the step
            tabLock = false;
            dispTabControl.SelectedIndex = step;
            currentStep = step;
            tabLock = true;

            switch (step)
            {
                case 0:

                    // config

                    break;
                case 1:

                    // process

                    break;
                case 2:

                    // results

                    break;

            }
            processData.AddToLog($"Moved to step {step}");
        }

        private bool ConfirmAbortAndClose()
        {
            AbortProcessConfirmation abortWindow = new AbortProcessConfirmation();
            abortWindow.Owner = this;
            abortWindow.ShowDialog();
            if (abortWindow.confirm)
            {
                // do any clean up logic
                // save log?

                //UIState state = new UIState();
                //state = RobotManager.Instance.GetUIState();
                //state.isDispenseWizardActive = false;
                //RobotManager.Instance.SetUIState(state);

                return true;
            }
            return false;
        }






        // ================ Button clicks

        private async void beginButton_Click(object sender, RoutedEventArgs e)
        {
            DoProcess();
        }

        private void logBtn_Click(object sender, RoutedEventArgs e)
        {
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(processData.GetLogAsString(), "Process Log");
            viewer.ShowDialog();
        }

        private void saveLogBtn_Click(object sender, RoutedEventArgs e)
        {
            processData.SaveDataToFile();
        }

        private void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            processData.OpenBrowserToDirectory();
        }





        // ================ Other events

        public void ProcessData_UpdateProcessLog(object sender, EventArgs e)
        {
            DDMResultsLogLine logline = processData.results.process_log.Last();
            logTextBox.Text += logline.date?.ToString(processData.dateFormatShort) + ": " + logline.message + "\n";
            logTextBox.CaretIndex = logTextBox.Text.Length;
            logTextBox.ScrollToEnd();
        }

        private void dispTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabLock)
            {
                // gets called twice per click... maybe fix
                dispTabControl.SelectedIndex = currentStep;
            }
        }
        private void DispenseWindow_WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!abortPreConfirmed)
            // skip logic if the abort button was pressed
            {
                bool confirmation = ConfirmAbortAndClose();
                if (!confirmation)
                {
                    // abort cancelled, back to wizard
                    e.Cancel = true;
                }
            }
        }
    }
}
