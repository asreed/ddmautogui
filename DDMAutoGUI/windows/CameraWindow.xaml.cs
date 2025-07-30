using DDMAutoGUI.utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for CameraWindow.xaml
    /// </summary>
    public partial class CameraWindow : Window
    {


        public CameraWindow()
        {
            InitializeComponent();
        }

        private async void acquireBtn_Click(object sender, RoutedEventArgs e)
        {
            acquiredImageDisplay.Source = null;
            displayLabel.Content = "Acquiring image...";

            CameraAcquisitionResult result = new CameraAcquisitionResult();
            result = await Task.Run(() => CameraManager.Instance.AcquireAndSave(acquiredImageDisplay));

            if (result.success)
            {
                displayLabel.Content = "Image acquired";
                CameraManager.Instance.DisplayImage(acquiredImageDisplay, result.filePath);

            } else
            {
                displayLabel.Content = $"Error: {result.errorMsg}";
            }
        }

        private void openFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            CameraManager.Instance.OpenExplorerToImages();
        }

    }
}
