using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the camera manager service.
    /// Handles camera acquisition and image management.
    /// </summary>
    public interface ICameraService : IDisposable
    {
        Task<CameraAcquisitionResult> AcquireAndSave(
            CameraService.CellCamera cellCamera,
            Image displayElement);

        Task<CameraAcquisitionResult> AcquireAndSave(
            CameraService.CellCamera cellCamera,
            Image displayElement,
            bool skipSave);

        Task<CameraAcquisitionResult> AcquireAndSave(
            CameraService.CellCamera cellCamera,
            Image displayElement,
            CameraService.CellImageFormat imgFormat,
            bool skipSave);

        void DisplayImage(Image displayElement, string filePath);
        void OpenExplorerToImages();
        Task<bool> TestCameraConnection(CameraService.CellCamera cellCamera);
    }
}