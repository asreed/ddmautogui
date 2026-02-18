using DDMAutoGUI.utilities;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for CalibPositionPanel.xaml
    /// </summary>



    public partial class CalibPositionPanel : UserControl
    {
        public CalibPositionPanel()
        {
            InitializeComponent();
            App.ControllerManager.ControllerStateChanged += UpdatePositionLabels;
        }

        public void SetupPanel()
        {

        }

        public void UpdatePositionLabels(object sender, EventArgs e)
        {
            ControllerState contState = App.ControllerManager.CONTROLLER_STATE;
            if (!contState.parseError)
            {
                j1PosTxb.Text = contState.posRotary.ToString("F2") + " deg";
                j2PosTxb.Text = contState.posLinear.ToString("F2") + " mm";
            }
            else
            {
                j1PosTxb.Text = "-";
                j2PosTxb.Text = "-";
            }
        }

        private async void CalPosBtn_Click(object sender, RoutedEventArgs e)
        {
            CalPosPrg.Visibility = Visibility.Visible;

            string caption = $"Position calibration result";
            string message = String.Empty;
            try
            {
                await App.ControllerManager.CalibratePosition();
                message = "Position calibration successful";
                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.Print("Error during position calibration: " + ex.Message);
                message = "Error during position calibration: " + ex.Message;
                MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            }


            CalPosPrg.Visibility = Visibility.Collapsed;
        }
    }
}
