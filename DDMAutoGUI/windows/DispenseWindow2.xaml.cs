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

        public DispenseProcessData processData;



        public DispenseWindow2()
        {
            InitializeComponent();
            processData = new DispenseProcessData();
            processData.AddToLog("Process window opened");
            processData.UpdateProcessLog += ProcessData_UpdateProcessLog;

            GoToStep(0);
        }

        private async void beginButton_Click(object sender, RoutedEventArgs e)
        {

            // pull data from config (validate?)

            string sn = snTextBox.Text.Trim();
            bool doPrePhoto = prePhotoCheckBox.IsChecked ?? false;
            bool doRingMeasure = measureRingCheckBox.IsChecked ?? false;
            bool doMagMeasure = measureMagCheckBox.IsChecked ?? false;
            bool doDispense = dispenseCheckBox.IsChecked ?? false;
            bool doPostPhoto = postPhotoCheckBox.IsChecked ?? false;

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
                await Task.Delay(1000);
                processData.AddToLog("Photo saved");
                processProgressBar.Value = 20;
            }
            if (doRingMeasure)
            {
                processData.AddToLog("Measuring ring...");
                await Task.Delay(1000);
                processData.AddToLog("Data collected");
                processProgressBar.Value = 30;
            }
            if (doMagMeasure)
            {
                processData.AddToLog("Measuring magnets...");
                await Task.Delay(1000);
                processData.AddToLog("Data collected");
                processProgressBar.Value = 40;

            }
            if (doDispense)
            {
                processData.AddToLog("Dispensing...");
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
                await Task.Delay(1000);
                processData.AddToLog("Photo saved");
                processProgressBar.Value = 90;
            }


            processData.AddToLog("Process complete");
            processProgressBar.Value = 100;
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
    }
}
