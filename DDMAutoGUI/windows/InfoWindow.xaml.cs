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
    /// Interaction logic for InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();

            string releaseDate = "July 17, 2025";
            string testNotes = "Intended for Gen 1 Phase 1a. ENGINEERING TESTING ONLY.";

            string formattedVersion = App.UIManager.GetAppVersionString();

            StringBuilder infoString = new StringBuilder();

            infoString.Append($"UI Version: {formattedVersion}\n");
            infoString.Append($"Release date: {releaseDate}\n");
            infoString.Append($"Notes: {testNotes}\n");

            infoTextBox.Text = infoString.ToString();

        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
