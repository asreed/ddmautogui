using ArenaNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DDMAutoGUI.utilities
{

    public class CameraAcquisitionResult
    {
        public bool success = false;
        public string errorMsg = "";
        public string filePath = "";
        public string fileName = "";
    }



    public class CameraManager
    {
        private const ArenaNET.EPfncFormat PIXEL_FORMAT = ArenaNET.EPfncFormat.BGR8;
        private string acqFilePath = string.Empty;
        private string acqFilePrefix = "acq_img";
        private string acqFileSuffixPNG = ".png";
        private string acqFileSuffixJPG = ".jpg";
        private string acqFileDirectory = AppDomain.CurrentDomain.BaseDirectory + "acquisitions\\";
        private string cameraTopSN, cameraSideSN = "";
        private CellImageFormat defaultImageFormat = CellImageFormat.JPG;


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


        public CameraManager()
        {
            App.ControllerManager.ControllerConnected += CameraManager_OnConnected;
            App.ControllerManager.ControllerDisconnected += CameraManager_OnDisconnected;
            Debug.Print("Camera manager initialized");
        }

        public async void CameraManager_OnConnected(object sender, EventArgs e)
        {
            CellSettings settings = App.SettingsManager.GetAllSettings();
            cameraTopSN = settings.camera_top_sn;
            cameraSideSN = settings.camera_side_sn;
        }

        public void CameraManager_OnDisconnected(object sender, EventArgs e)
        {
            cameraTopSN = null;
            cameraSideSN = null;
        }



        public void OpenExplorerToImages()
        {
            Process.Start("explorer.exe", acqFileDirectory);
        }


        public CameraAcquisitionResult AcquireAndSave(CellCamera cellCamera, Image displayElement)
        {
            return AcquireAndSave(cellCamera, displayElement, defaultImageFormat);
        }

        public CameraAcquisitionResult AcquireAndSave(CellCamera cellCamera, Image displayElement, CellImageFormat imgFormat)
        {
            string sfx = string.Empty;
            switch (imgFormat)
            {
                case CellImageFormat.PNG:
                    sfx = acqFileSuffixPNG;
                    break;
                case CellImageFormat.JPG:
                    sfx = acqFileSuffixJPG;
                    break;
            }
            acqFilePath = acqFileDirectory + acqFilePrefix + GetTimestamp() + sfx;

            CameraAcquisitionResult result = new CameraAcquisitionResult();
            result.success = false;
            result.filePath = acqFilePath;
            result.fileName = acqFilePrefix + GetTimestamp() + acqFileSuffixJPG;

            ArenaNET.ISystem system = null;

            try
            {
                // prepare
                system = ArenaNET.Arena.OpenSystem();
                system.UpdateDevices(100);
                if (system.Devices.Count == 0)
                {
                    Debug.Print("\nNo camera connected\nAborting");
                    throw new Exception("No cameras detected");
                }

                Debug.Print($"Camera top SN from settings: {cameraTopSN}");
                Debug.Print($"Camera side SN from settings: {cameraSideSN}");

                Debug.Print($"Number of devices: {system.Devices.Count}");
                for (int i = 0; i < system.Devices.Count; i++)
                {
                    Debug.Print($"Device {i} SN: {system.Devices[i].SerialNumber}");
                }

                ArenaNET.IDeviceInfo selectedDeviceInfo = null;

                for (int i = 0; i < system.Devices.Count; i++)
                {
                    if (system.Devices[i].SerialNumber == cameraTopSN && cellCamera == CellCamera.top)
                    {
                        selectedDeviceInfo = system.Devices[i];
                        Debug.Print($"Selected top camera with SN {cameraTopSN}");
                        break;
                    }
                    else if (system.Devices[i].SerialNumber == cameraSideSN && cellCamera == CellCamera.side)
                    {
                        selectedDeviceInfo = system.Devices[i];
                        Debug.Print($"Selected side camera with SN {cameraSideSN}");
                        break;
                    }
                }

                if (selectedDeviceInfo == null)
                {
                    Debug.Print($"\nNo matching camera connected for {(cellCamera == CellCamera.top ? "top" : "side")}\nAborting");
                    result.errorMsg = $"No matching camera connected for {(cellCamera == CellCamera.top ? "top" : "side")}";
                    result.success = false;
                    ArenaNET.Arena.CloseSystem(system);
                    return result;
                }


                ArenaNET.IDevice device = system.CreateDevice(selectedDeviceInfo);

                // enable stream auto negotiate packet size
                var streamAutoNegotiatePacketSizeNode = (ArenaNET.IBoolean)device.TLStreamNodeMap.GetNode("StreamAutoNegotiatePacketSize");
                streamAutoNegotiatePacketSizeNode.Value = true;

                // enable stream packet resend
                var streamPacketResendEnableNode = (ArenaNET.IBoolean)device.TLStreamNodeMap.GetNode("StreamPacketResendEnable");
                streamPacketResendEnableNode.Value = true;

                // get image
                device.StartStream();
                ArenaNET.IImage image = device.GetImage(2000);

                // save image
                switch (imgFormat)
                {
                    case CellImageFormat.PNG:
                        SaveImagePNG(image, acqFilePath);
                        break;
                    case CellImageFormat.JPG:
                        SaveImageJPG(image, acqFilePath);
                        break;
                }

                // clean up
                device.RequeueBuffer(image);
                device.StopStream();
                system.DestroyDevice(device);
                ArenaNET.Arena.CloseSystem(system);

                result.success = true;
                return result;

            }
            catch (Exception ex)
            {

                Debug.Print("\nException thrown: {0}", ex.Message);
                result.errorMsg = ex.Message;
                result.success = false;
                if (system != null) { 
                    ArenaNET.Arena.CloseSystem(system);
                }
                return result;
            }

        }

        static void SaveImagePNG(ArenaNET.IImage image, String filePath)
        {
            // convert image
            Debug.Print($"...Convert image to {PIXEL_FORMAT}");

            ArenaNET.IImage converted = ArenaNET.ImageFactory.Convert(image, PIXEL_FORMAT);

            // prepare image parameters
            Debug.Print("...Prepare image parameters");

            SaveNET.ImageParams parameters = new SaveNET.ImageParams(
                converted.Width,
                converted.Height,
                converted.BitsPerPixel,
                true);

            // prepare image writer
            Debug.Print("...Prepare image writer");

            SaveNET.ImageWriter writer = new SaveNET.ImageWriter(parameters, filePath);

            // Set image writer to PNG
            //   Set the output file format of the image writer to PNG.
            //   The writer saves the image file as PNG file even without
            //	 the extension in the file name. Aside from this setting, 
            //   compression level can be set between 0 to 9 and the image
            //   can be created using interlacing by changing the parameters. 

            Debug.Print("...Set image writer to PNG");

            writer.SetPng(".png", 0, false);

            // save image
            Debug.Print("...Save image");

            writer.Save(converted.DataArray, true);

            // destroy converted image
            ArenaNET.ImageFactory.Destroy(converted);
        }




        static void SaveImageJPG(ArenaNET.IImage image, String filePath)
        {
            // convert image
            Debug.Print($"...Convert image to {PIXEL_FORMAT}");

            ArenaNET.IImage converted = ArenaNET.ImageFactory.Convert(image, PIXEL_FORMAT);

            // prepare image parameters
            Debug.Print("...Prepare image parameters");

            SaveNET.ImageParams parameters = new SaveNET.ImageParams(
                    converted.Width,
                    converted.Height,
                    converted.BitsPerPixel,
                    true);

            // prepare image writer
            Debug.Print("...Prepare image writer");

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
            Debug.Print("...Save image");

            writer.Save(converted.DataArray, true);

            // destroy converted image
            ArenaNET.ImageFactory.Destroy(converted);
        }

        public void DisplayImage(Image displayElement, String filePath)
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
