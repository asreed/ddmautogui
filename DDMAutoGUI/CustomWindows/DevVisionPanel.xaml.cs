using DDMAutoGUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace DDMAutoGUI.CustomWindows
{
    /// <summary>
    /// Interaction logic for DevVisionPanel.xaml
    /// </summary>
    public partial class DevVisionPanel : UserControl
    {
        public DevVisionPanel()
        {
            InitializeComponent();
        }

        private async void AcquireTopCommand(object sender, RoutedEventArgs e)
        {
            var cameraManager = App.Services?.GetService<ICameraManager>();
            if (cameraManager == null) return;

            acquiredImageDisplay.Source = null;
            Adv_Cam_StatusLbl.Content = "Acquiring image...";

            CameraAcquisitionResult result = await cameraManager.AcquireAndSave(CameraManager.CellCamera.top, acquiredImageDisplay);

            if (result.success)
            {
                Adv_Cam_StatusLbl.Content = "Top image acquired";
                cameraManager.DisplayImage(acquiredImageDisplay, result.filePath);
            }
            else
            {
                Adv_Cam_StatusLbl.Content = $"Error: {result.errorMsg}";
            }
        }

        private async void AcquireSideCommand(object sender, RoutedEventArgs e)
        {
            var cameraManager = App.Services?.GetService<ICameraManager>();
            if (cameraManager == null) return;

            acquiredImageDisplay.Source = null;
            Adv_Cam_StatusLbl.Content = "Acquiring image...";

            CameraAcquisitionResult result = await cameraManager.AcquireAndSave(CameraManager.CellCamera.side, acquiredImageDisplay);

            if (result.success)
            {
                Adv_Cam_StatusLbl.Content = "Side image acquired";
                cameraManager.DisplayImage(acquiredImageDisplay, result.filePath);
            }
            else
            {
                Adv_Cam_StatusLbl.Content = $"Error: {result.errorMsg}";
            }
        }

        private void Adv_Cam_OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var cameraManager = App.Services?.GetService<ICameraManager>();
            if (cameraManager != null)
            {
                cameraManager.OpenExplorerToImages();
            }
        }
    }
}