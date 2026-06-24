using ArenaNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DDMAutoGUI.Services
{

    public class CameraAcquisitionResult
    {
        public bool success = false;
        public string errorMsg = "";
        public string filePath = "";
        public string fileName = "";
    }



    public class CameraManager : ICameraManager, IDisposable
    {
        private static string TB = "..";

        private const EPfncFormat PIXEL_FORMAT = EPfncFormat.BGR8;
        private string acqFilePath = string.Empty;
        private string acqFilePrefix = "acq_img";
        private string acqFileSuffixPNG = ".png";
        private string acqFileSuffixJPG = ".jpg";
        private string acqFileDirectory = AppDomain.CurrentDomain.BaseDirectory + "acquisitions\\";
        private CellImageFormat defaultImageFormat = CellImageFormat.JPG;

        private readonly ILightController _lightController;
        private readonly Func<CellSettings> _getSettings;
        private ISystem _system; // Arena system singleton

        public enum CellCamera
        {
            top,
            side
        }

        public enum CellImageFormat
        {
            PNG,
            JPG
        }

        public CameraManager(
            ILightController lightController,
            ISettingsManager settingsManager)
        {
            _lightController = lightController ?? throw new ArgumentNullException(nameof(lightController));
            _getSettings = () => settingsManager.GetAllSettings();

            // Initialize Arena SDK once
            try
            {
                _system = ArenaNET.Arena.OpenSystem();
                Debug.Print("Camera manager initialized (Arena SDK opened)");
            }
            catch (Exception ex)
            {
                Debug.Print($"Warning: Failed to initialize Arena SDK: {ex.Message}");
                _system = null;
            }
        }

        public void Dispose()
        {
            if (_system != null)
            {
                try
                {
                    ArenaNET.Arena.CloseSystem(_system);
                    Debug.Print("Arena SDK closed");
                }
                catch (Exception ex)
                {
                    Debug.Print($"Error closing Arena SDK: {ex.Message}");
                }
                _system = null;
            }
        }

        public async Task<bool> TestCameraConnection(CellCamera cellCamera)
        {
            CameraAcquisitionResult result = await AcquireAndSave(cellCamera, null, true);
            return result.success;
        }

        public void OpenExplorerToImages()
        {
            Process.Start("explorer.exe", acqFileDirectory);
        }

        public async Task<CameraAcquisitionResult> AcquireAndSave(CellCamera cellCamera, Image displayElement)
        {
            return await AcquireAndSave(cellCamera, displayElement, defaultImageFormat, false);
        }

        public async Task<CameraAcquisitionResult> AcquireAndSave(CellCamera cellCamera, Image displayElement, bool skipSave)
        {
            return await AcquireAndSave(cellCamera, displayElement, defaultImageFormat, skipSave);
        }

        public async Task<CameraAcquisitionResult> AcquireAndSave(CellCamera cellCamera, Image displayElement, CellImageFormat imgFormat, bool skipSave)
        {
            string sfx = imgFormat switch
            {
                CellImageFormat.PNG => acqFileSuffixPNG,
                CellImageFormat.JPG => acqFileSuffixJPG,
                _ => acqFileSuffixJPG
            };
            acqFilePath = acqFileDirectory + acqFilePrefix + GetTimestamp() + sfx;

            CameraAcquisitionResult result = new CameraAcquisitionResult
            {
                success = false,
                filePath = acqFilePath,
                fileName = acqFilePrefix + GetTimestamp() + sfx
            };

            if (_system == null)
            {
                result.errorMsg = "Arena SDK not initialized";
                return result;
            }

            IDevice device = null;

            try
            {
                // Get camera serial numbers from settings on-demand
                CellSettings settings = _getSettings();
                string cameraTopSN = settings?.camera_top_sn;
                string cameraSideSN = settings?.camera_side_sn;

                // Update device list
                _system.UpdateDevices(100);
                if (_system.Devices.Count == 0)
                {
                    Debug.Print("\nNo camera connected\nAborting");
                    throw new Exception("No cameras detected");
                }

                IDeviceInfo selectedDeviceInfo = null;

                for (int i = 0; i < _system.Devices.Count; i++)
                {
                    if (_system.Devices[i].SerialNumber == cameraTopSN && cellCamera == CellCamera.top)
                    {
                        selectedDeviceInfo = _system.Devices[i];
                        Debug.Print($"{TB}Selected top camera with SN {cameraTopSN}");
                        break;
                    }
                    else if (_system.Devices[i].SerialNumber == cameraSideSN && cellCamera == CellCamera.side)
                    {
                        selectedDeviceInfo = _system.Devices[i];
                        Debug.Print($"{TB}Selected side camera with SN {cameraSideSN}");
                        break;
                    }
                }

                if (selectedDeviceInfo == null)
                {
                    Debug.Print($"\nNo matching camera connected for {(cellCamera == CellCamera.top ? "top" : "side")}\nAborting");
                    result.errorMsg = $"No matching camera connected for {(cellCamera == CellCamera.top ? "top" : "side")}";
                    result.success = false;
                    return result;
                }

                device = _system.CreateDevice(selectedDeviceInfo);

                // enable stream auto negotiate packet size
                var streamAutoNegotiatePacketSizeNode = (IBoolean)device.TLStreamNodeMap.GetNode("StreamAutoNegotiatePacketSize");
                streamAutoNegotiatePacketSizeNode.Value = true;

                // enable stream packet resend
                var streamPacketResendEnableNode = (IBoolean)device.TLStreamNodeMap.GetNode("StreamPacketResendEnable");
                streamPacketResendEnableNode.Value = true;

                // turn lights on
                if (skipSave == false)
                {
                    await _lightController.LightsOn();
                }

                // get image
                device.StartStream();
                IImage image = device.GetImage(2000);

                // turn lights off
                if (skipSave == false)
                {
                    await _lightController.LightsOff();
                }

                // save image
                if (!skipSave)
                {
                    switch (imgFormat)
                    {
                        case CellImageFormat.PNG:
                            SaveImagePNG(image, acqFilePath);
                            break;
                        case CellImageFormat.JPG:
                            SaveImageJPG(image, acqFilePath);
                            break;
                    }
                }

                // clean up
                device.RequeueBuffer(image);
                device.StopStream();
                _system.DestroyDevice(device);

                result.success = true;
                return result;

            }
            catch (Exception ex)
            {
                await _lightController.LightsOff();
                Debug.Print($"\nException thrown: {ex.Message}");
                result.errorMsg = ex.Message;
                result.success = false;
                
                if (device != null)
                {
                    try
                    {
                        _system.DestroyDevice(device);
                    }
                    catch { }
                }
                
                return result;
            }
        }

        static void SaveImagePNG(IImage image, string filePath)
        {

            // convert image
            Debug.Print($"{TB}{TB}Convert image to {PIXEL_FORMAT}");

            IImage converted = ImageFactory.Convert(image, PIXEL_FORMAT);

            // prepare image parameters
            Debug.Print($"{TB}{TB}Prepare image parameters");

            SaveNET.ImageParams parameters = new SaveNET.ImageParams(
                converted.Width,
                converted.Height,
                converted.BitsPerPixel,
                true);

            // prepare image writer
            Debug.Print($"{TB}{TB}Prepare image writer");

            SaveNET.ImageWriter writer = new SaveNET.ImageWriter(parameters, filePath);

            // Set image writer to PNG
            //   Set the output file format of the image writer to PNG.
            //   The writer saves the image file as PNG file even without
            //	 the extension in the file name. Aside from this setting, 
            //   compression level can be set between 0 to 9 and the image
            //   can be created using interlacing by changing the parameters. 

            Debug.Print($"{TB}{TB}Set image writer to PNG");

            writer.SetPng(".png", 0, false);

            // save image
            Debug.Print($"{TB}{TB}Save image");

            writer.Save(converted.DataArray, true);

            // destroy converted image
            ImageFactory.Destroy(converted);
        }

        static void SaveImageJPG(IImage image, string filePath)
        {
            // convert image
            Debug.Print($"{TB}{TB}Convert image to {PIXEL_FORMAT}");

            IImage converted = ImageFactory.Convert(image, PIXEL_FORMAT);

            // prepare image parameters
            Debug.Print($"{TB}{TB}Prepare image parameters");

            SaveNET.ImageParams parameters = new SaveNET.ImageParams(
                    converted.Width,
                    converted.Height,
                    converted.BitsPerPixel,
                    true);

            // prepare image writer
            Debug.Print($"{TB}{TB}Prepare image writer");

            SaveNET.ImageWriter writer = new SaveNET.ImageWriter(parameters, filePath);

            // Set image writer to JPEG
            //   Set the output file format of the image writer to JPEG.
            //   The writer saves the image file as JPEG file even without
            //       the extension in the file name. Aside from this setting,
            //   quality can be set between 1 to 100, the image can be set
            //   as progressive, subsampling can be set, and optimal Huffman
            //   Tables can be calculated by changing the parameters.
            writer.SetJpeg(".jpg", 95, false, SaveNET.EJpegSubsampling.NoSubsampling, false);

            // save
            Debug.Print($"{TB}{TB}Save image");

            writer.Save(converted.DataArray, true);

            // destroy converted image
            ImageFactory.Destroy(converted);
        }

        public void DisplayImage(Image displayElement, string filePath)
        {
            // for convenience
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(acqFilePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            displayElement.Source = bitmap;
        }

        public string GetTimestamp()
        {
            return DateTime.Now.ToString("_yyMMdd_HHmmss");
        }
    }
}
