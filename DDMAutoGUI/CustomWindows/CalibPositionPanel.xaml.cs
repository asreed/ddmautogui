using DDMAutoGUI.Services;
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
        private readonly IControllerManager _controllerManager;
        private readonly IApplicationConfiguration _applicationConfiguration;

        public CalibPositionPanel()
        {
            InitializeComponent();
        }

        public CalibPositionPanel(IControllerManager controllerManager, IApplicationConfiguration applicationConfiguration) : this()
        {
            _controllerManager = controllerManager ?? throw new ArgumentNullException(nameof(controllerManager));
            _applicationConfiguration = applicationConfiguration ?? throw new ArgumentNullException(nameof(applicationConfiguration));

            _controllerManager.ControllerStateChanged += UpdatePositionLabels;
            if (_applicationConfiguration.IsSimulationMode)
            {
                j1PosTxb.Text = "2.451 deg";
                j2PosTxb.Text = "0.005 mm";
            }
        }

        public void SetupPanel()
        {

        }

        public void UpdatePositionLabels(object sender, EventArgs e)
        {
            ControllerState contState = _controllerManager.CONTROLLER_STATE;
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
            if (_applicationConfiguration.IsSimulationMode)
            {
                j1PosTxb.Text = "2.451 deg";
                j2PosTxb.Text = "0.005 mm";
            }
        }

        private async void CalPosBtn_Click(object sender, RoutedEventArgs e)
        {
            CalPosPrg.Visibility = Visibility.Visible;

            string caption = $"Position calibration result";
            string message = String.Empty;
            try
            {
                await _controllerManager.CalibratePosition();
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
