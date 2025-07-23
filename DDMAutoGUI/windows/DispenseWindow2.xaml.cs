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

        private DispenseProcessData processData;
        private DDMSettings settings;



        public DispenseWindow2()
        {
            InitializeComponent();

            processData = new DispenseProcessData();
            processData.AddToLog("Process window opened");
            processData.UpdateProcessLog += ProcessData_UpdateProcessLog;

            settings = new DDMSettings();
            settings = SettingsManager.Instance.GetSettings();
            processData.AddToLog($"Settings loaded (last saved {settings.last_saved})");

            GoToStep(0);
        }

        private async void beginButton_Click(object sender, RoutedEventArgs e)
        {

            // pull data from config (validate?)

            string sn = snTextBox.Text.Trim();
            int motorSelection = motorSizeComboBox.SelectedIndex;
            bool doPrePhoto = prePhotoCheckBox.IsChecked ?? false;
            bool doRingMeasure = measureRingCheckBox.IsChecked ?? false;
            bool doMagMeasure = measureMagCheckBox.IsChecked ?? false;
            bool doDispense = dispenseCheckBox.IsChecked ?? false;
            bool doPostPhoto = postPhotoCheckBox.IsChecked ?? false;

            // pull data from settings

            DDMSettingsOneSize motor = new DDMSettingsOneSize();

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

            processData.AddToLog($"Disp ID: [{motor.disp_id.x}, {motor.disp_id.th}]");
            processData.AddToLog($"Disp OD: [{motor.disp_od.x} ,  {motor.disp_od.th}]");


            // start process

            processData.AddToLog("Dispense process started");
            processProgressBar.IsIndeterminate = false;
            processProgressBar.Value = 0;
            GoToStep(1);

            if (doPrePhoto || doPostPhoto)
            {
                // connect to camera

                processData.AddToLog("Connecting to cameras...");
                await Task.Delay(1000);
                processData.AddToLog("Cameras connected");
                processProgressBar.Value = 10;

            }

            if (doPrePhoto)
            {
                processData.AddToLog("Taking photo...");
                processData.AddToLog($"Moving to [{settings.all_sizes.camera.x}, {settings.all_sizes.camera.th}]");
                await Task.Delay(1000);
                processData.AddToLog("Photo saved");
                processProgressBar.Value = 20;
            }
            if (doRingMeasure)
            {
                processData.AddToLog("Measuring ring...");
                processData.AddToLog($"Moving to [{motor.laser_ring.x}, {motor.laser_ring.th}]");
                await Task.Delay(1000);
                processData.AddToLog("Data collected");
                processProgressBar.Value = 30;
            }
            if (doMagMeasure)
            {
                processData.AddToLog("Measuring magnets...");
                processData.AddToLog($"Moving to [{motor.laser_mag.x}, {motor.laser_mag.th}]");
                await Task.Delay(1000);
                processData.AddToLog("Data collected");
                processProgressBar.Value = 40;

            }
            if (doDispense)
            {
                processData.AddToLog("Dispensing...");
                processData.AddToLog($"Using ID [{motor.disp_id.x}, {motor.disp_id.th}] and OD [{motor.disp_od.x}, {motor.disp_od.th}]");
                await Task.Delay(1000);
                processData.AddToLog("...");
                await Task.Delay(1000);
                processData.AddToLog("...");
                await Task.Delay(1000);
                processData.AddToLog("Dispense complete");
                processProgressBar.Value = 80;
            }
            if (doPostPhoto)
            {
                processData.AddToLog("Taking photo...");
                processData.AddToLog($"Moving to [{settings.all_sizes.camera.x}, {settings.all_sizes.camera.th}]");
                await Task.Delay(1000);
                processData.AddToLog("Photo saved");
                processProgressBar.Value = 90;
            }



            processData.AddToLog("Moving back to unload position...");
            processData.AddToLog($"Moving to [{settings.all_sizes.load.x}, {settings.all_sizes.load.th}]");
            processProgressBar.Value = 100;
            await Task.Delay(1000);

            processData.AddToLog("Process complete");
            await Task.Delay(1000);



            GoToStep(2);

        }






        public void ProcessData_UpdateProcessLog(object sender, EventArgs e)
        {
            logTextBox.Text = processData.processLog;
            logTextBox.CaretIndex = logTextBox.Text.Length;
            logTextBox.ScrollToEnd();
        }






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

        private void logBtn_Click(object sender, RoutedEventArgs e)
        {
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(processData.processLog, "Process Log");
            viewer.ShowDialog();
        }
    }
}
