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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for CalibHeightPanel.xaml
    /// </summary>
    public partial class CalibHeightPanel : UserControl
    {
        public CalibHeightPanel()
        {
            InitializeComponent();

        }

        public void SetupTable()
        {
            CellSettings settings = App.SettingsManager.GetAllSettings();

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

        private async void CalPosBtn_Click(object sender, RoutedEventArgs e)
        {
            
            CalPosPrg.Visibility = Visibility.Visible;

            try
            {







            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during height calibration: " + ex.Message);
            }

            CalPosPrg.Visibility = Visibility.Collapsed;
        }
    }
