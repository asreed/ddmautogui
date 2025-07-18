using DDMAutoGUI.dispenseSteps;
using DDMAutoGUI.utilities;
using DDMAutoGUI.windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace DDMAutoGUI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class DispenseWindow : Window
    {

        dispenseStep1 s1;
        dispenseStep2 s2;
        dispenseStep3 s3;

        private int currentStep = 0;
        private bool tabLock = true; // prevent user from clicking tabs directly
        
        public  DispenseProcessData processData;

        private bool abortPreConfirmed = false;


        public DispenseWindow()
        {
            InitializeComponent();

            // load in steps
            s1 = new dispenseStep1(this);
            s2 = new dispenseStep2();
            s3 = new dispenseStep3();
            tabContainer1.Children.Add(s1);
            tabContainer2.Children.Add(s2);
            tabContainer3.Children.Add(s3);


            //RobotManager.Instance.UpdateUIState += dispenseWindow_OnUpdateUIState;
            processData = new DispenseProcessData();
            processData.AddToLog("Process started");
            abortPreConfirmed = false;
            GoToStep(0);
        }








        private void GetStepData(int step)
        {
            // called before moving away from the step
            switch (step)
            {
                case 0:

                    string sn = s1.GetSN();
                    processData.ringSN = sn;
                    processData.AddToLog($"Ring SN entered: {sn}");

                    break;
                case 1:


                    break;
            }
        }

        private void GoToStep(int step)
        {
            // called when moving to the step
            tabLock = false;
            dispTabControl.SelectedIndex = step;
            tabLock = true;


            switch (step)
            {
                case 0:



                    break;
                case 1:



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

                UIState state = new UIState();
                state = RobotManager.Instance.GetUIState();
                state.isDispenseWizardActive = false;
                RobotManager.Instance.SetUIState(state);
                return true;
            }
            return false;
        }









        // ============== button handlers


        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            GetStepData(currentStep);
            currentStep++;
            GoToStep(currentStep);
        }

        private void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            currentStep--;
            GoToStep(currentStep);
        }

        private void showLog_Click(object sender, RoutedEventArgs e)
        {
            TextDataViewer viewer = new TextDataViewer();
            viewer.Owner = this;
            viewer.PopulateData(processData.processLog, "Process Log");
            viewer.ShowDialog();
        }

        private void abortBtn_Click(object sender, RoutedEventArgs e)
        {
            bool confirmation = ConfirmAbortAndClose();
            if (confirmation)
            {
                // abort confirmed, close window
                abortPreConfirmed = true;
                this.Close();
            }
        }
    }
}
