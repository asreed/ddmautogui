using DDMAutoGUI.Services;
using DDMAutoGUI.Utilities;
using Microsoft.Extensions.DependencyInjection;
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
    /// Interaction logic for CalibHeightPanel.xaml
    /// </summary>
    public partial class CalibHeightPanel : UserControl
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IControllerManager _controllerManager;

        public CalibHeightPanel()
        {
            InitializeComponent();
            _settingsManager = App.Services?.GetService<ISettingsManager>();
            _controllerManager = App.Services?.GetService<IControllerManager>();
        }

        public CalibHeightPanel(ISettingsManager settingsManager, IControllerManager controllerManager) : this()
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _controllerManager = controllerManager ?? throw new ArgumentNullException(nameof(controllerManager));
        }

        public void SetupPanel()
        {
            CellSettings settings = _settingsManager.GetAllSettings();

            ddm57_a.Text = settings.laser_calib.ddm_57_coeff.A.ToString();
            ddm95_a.Text = settings.laser_calib.ddm_95_coeff.A.ToString();
            ddm116_a.Text = settings.laser_calib.ddm_116_coeff.A.ToString();
            ddm170_a.Text = settings.laser_calib.ddm_170_coeff.A.ToString();

            ddm57_phi.Text = settings.laser_calib.ddm_57_coeff.phi.ToString();
            ddm95_phi.Text = settings.laser_calib.ddm_95_coeff.phi.ToString();
            ddm116_phi.Text = settings.laser_calib.ddm_116_coeff.phi.ToString();
            ddm170_phi.Text = settings.laser_calib.ddm_170_coeff.phi.ToString();

            ddm57_r2.Text = settings.laser_calib.ddm_57_coeff.R2.ToString();
            ddm95_r2.Text = settings.laser_calib.ddm_95_coeff.R2.ToString();
            ddm116_r2.Text = settings.laser_calib.ddm_116_coeff.R2.ToString();
            ddm170_r2.Text = settings.laser_calib.ddm_170_coeff.R2.ToString();
        }

        private async void CalHeightBtn_Click(object sender, RoutedEventArgs e)
        {

            CalHeightPrg.Visibility = Visibility.Visible;

            float x, t;
            string response;
            List<ResultsHeightMeasurement> heights;
            double a, phi, r2;

            int n = 60;
            float delay = 0.05f;

            try
            {

                CSMotor motor = _settingsManager.GetMotorSettingsFromName("ddm_57");

                x = motor.laser_mag.x.Value;
                t = motor.laser_mag.t.Value;
                //response = await _controllerManager.MoveJ(x, t);
                //response = await _controllerManager.MeasureHeights(x, t, n, delay);
                //heights = _controllerManager.ParseHeightData(response);

                heights = HeightVerification.GetSampleData("ddm_57");
                HeightVerification.FitSinToData(heights, out a, out phi, out r2);
                Debug.Print($"57 fit generated:\tA = {a:f2}, phi = {phi:f4}, R^2 = {r2:f3}");


                // ...
                // ...
                // todo: send robot commands for collecting all the data
                // todo: save to settings
                // todo: find a way to use the calibration data to normalize heights for results


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during height calibration: " + ex.Message);
            }

            CalHeightPrg.Visibility = Visibility.Collapsed;
        }
    }
}
