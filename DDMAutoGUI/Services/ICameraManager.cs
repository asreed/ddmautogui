using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DDMAutoGUI.Services
{
    /// <summary>
    /// Interface for the camera manager service.
    /// Handles camera acquisition and image management.
    /// </summary>
    public interface ICameraManager
    {
        Task<CameraAcquisitionResult> AcquireAndSave(
            CameraManager.CellCamera cellCamera,
            Image displayElement);

        Task<CameraAcquisitionResult> AcquireAndSave(
            CameraManager.CellCamera cellCamera,
            Image displayElement,
            bool skipSave);

        Task<CameraAcquisitionResult> AcquireAndSave(
            CameraManager.CellCamera cellCamera,
            Image displayElement,
            CameraManager.CellImageFormat imgFormat,
            bool skipSave);

        void DisplayImage(Image displayElement, string filePath);
        void OpenExplorerToImages();
        Task<bool> TestCameraConnection(CameraManager.CellCamera cellCamera);
    }
}