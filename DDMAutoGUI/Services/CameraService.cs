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



    public class CameraService : ICameraService, IDisposable
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

        public CameraService(
            ILightController lightController,
            ISettingsService settingsService)
        {
            _lightController = lightController ?? throw new ArgumentNullException(nameof(lightController));
            _getSettings = () => settingsService.GetAllSettings();

            // Initialize Arena SDK once
            try
            {
                _system = ArenaNET.Arena.OpenSystem();
                Debug.Print("Camera service initialized (Arena SDK opened)");
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

            _deviceLock.Dispose();
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

            // Compute the timestamp once so filePath and fileName always agree,
            // and keep it local so overlapping acquisitions can't stomp each other.
            string timestamp = GetTimestamp();
            string fileName = acqFilePrefix + timestamp + sfx;
            string filePath = acqFileDirectory + fileName;
            acqFilePath = filePath;

            CameraAcquisitionResult result = new CameraAcquisitionResult
            {
                success = false,
                filePath = filePath,
                fileName = fileName
            };

            if (_system == null)
            {
                result.errorMsg = "Arena SDK not initialized";
                return result;
            }

            // Read settings on the calling thread before offloading SDK work.
            CellSettings settings = _getSettings();
            string targetSN = cellCamera == CellCamera.top
                ? settings?.camera_top_sn
                : settings?.camera_side_sn;

            // Serialize all access to the shared Arena ISystem.
            await _deviceLock.WaitAsync();
            try
            {
                // One try/catch around the WHOLE operation — light control AND
                // acquisition — so no exception (including the re-thrown
                // device-creation / "no matching camera" ones) can escape and crash.
                try
                {
                    if (!skipSave)
                    {
                        await _lightController.LightsOn();
                    }

                    await Task.Run(() =>
                    {
                        IDevice device = null;
                        bool streaming = false;
                        try
                        {
                            // A failed device creation surfaces as an exception. Catch it
                            // here so the failure is attributed clearly and we never fall
                            // through to use a null device below.
                            try
                            {
                                device = CreateDeviceWithRetry(targetSN, cellCamera);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Device creation failed for {cellCamera} camera (SN {targetSN}): {ex.Message}", ex);
                            }

                            ((IBoolean)device.TLStreamNodeMap.GetNode("StreamAutoNegotiatePacketSize")).Value = true;
                            ((IBoolean)device.TLStreamNodeMap.GetNode("StreamPacketResendEnable")).Value = true;

                            // Lower GigE heartbeat so a stale control channel is released quickly.
                            var heartbeat = (IInteger)device.NodeMap.GetNode("GevHeartbeatTimeout");
                            heartbeat.Value = 1000; // ms

                            device.StartStream();
                            streaming = true;

                            IImage image = device.GetImage(2000);

                            if (!skipSave)
                            {
                                switch (imgFormat)
                                {
                                    case CellImageFormat.PNG:
                                        SaveImagePNG(image, filePath);
                                        break;
                                    case CellImageFormat.JPG:
                                        SaveImageJPG(image, filePath);
                                        break;
                                }
                            }

                            device.RequeueBuffer(image);
                        }
                        finally
                        {
                            // Always release the device so the next open starts clean —
                            // this is what prevents the NEXT IFOpenDevice failure. Stop the
                            // stream first if it was started, otherwise a mid-acquisition
                            // failure could leave it running.
                            if (device != null)
                            {
                                if (streaming)
                                {
                                    try { device.StopStream(); } catch { }
                                }
                                try { _system.DestroyDevice(device); } catch { }
                            }
                        }
                    });

                    result.success = true;
                }
                finally
                {
                    // Lights always go off, even if acquisition threw.
                    if (!skipSave)
                    {
                        await _lightController.LightsOff();
                    }
                }
            }
            catch (Exception ex)
            {
                // Single safety net: light control OR acquisition failures are
                // reported via the result instead of crashing the program.
                Debug.Print($"\nException thrown: {ex.Message}");
                result.errorMsg = ex.Message;
                result.success = false;
            }
            finally
            {
                _deviceLock.Release();
            }

            return result;
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

        // Serializes access to the shared Arena ISystem. UpdateDevices / CreateDevice /
        // DestroyDevice all mutate shared state and are NOT safe to call concurrently.
        private readonly SemaphoreSlim _deviceLock = new SemaphoreSlim(1, 1);



        // Opens the device, retrying transient IFOpenDevice failures. These occur when
        // the camera's exclusive control channel from a previous session hasn't been
        // released yet (GigE heartbeat). A short, increasing backoff lets it free up.
        // Runs synchronously by design — it is only ever called from inside Task.Run.
        private IDevice CreateDeviceWithRetry(string targetSN, CellCamera cellCamera, int maxAttempts = 3)
        {
            Exception lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // Refresh the list every attempt — a stale IDeviceInfo is itself a
                // common cause of open failures.
                _system.UpdateDevices(100);

                IDeviceInfo selectedDeviceInfo = null;
                for (int i = 0; i < _system.Devices.Count; i++)
                {
                    if (_system.Devices[i].SerialNumber == targetSN)
                    {
                        selectedDeviceInfo = _system.Devices[i];
                        break;
                    }
                }

                if (selectedDeviceInfo == null)
                {
                    // Not a transient open failure — the camera simply isn't present.
                    throw new Exception($"No matching camera connected for {cellCamera} (SN {targetSN})");
                }

                try
                {
                    IDevice device = _system.CreateDevice(selectedDeviceInfo);
                    if (attempt > 1)
                    {
                        Debug.Print($"{TB}CreateDevice succeeded on attempt {attempt}");
                    }
                    return device;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Debug.Print($"{TB}CreateDevice attempt {attempt}/{maxAttempts} failed: {ex.Message}");

                    if (attempt < maxAttempts)
                    {
                        // 300ms, 600ms... gives the camera time to release the
                        // previous control channel.
                        Thread.Sleep(300 * attempt);
                    }
                }
            }

            throw new Exception(
                $"Failed to open {cellCamera} camera (SN {targetSN}) after {maxAttempts} attempts: {lastError?.Message}",
                lastError);
        }
    }
}
