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
    /// Interaction logic for AbortProcessConfirmation.xaml
    /// </summary>
    public partial class AbortProcessConfirmation : Window
    {
        public bool confirm = false;

        public AbortProcessConfirmation()
        {
            InitializeComponent();
        }

        private void abortBtn_Click(object sender, RoutedEventArgs e)
        {
            confirm = true;
            this.Close();
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            confirm = false;
            this.Close();
        }
    }
}
