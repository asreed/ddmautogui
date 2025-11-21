using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DDMAutoGUI.Utilities
{

    public class DeviceConnState
    {
        public bool controllerConnected;
        public bool ioLinkDevicesConnected;
        public bool camerasConnected;
        public bool daqConnected;
    }


    public class DeviceConnectionManager
    {

        public DeviceConnectionManager() { 
        
        }


        public async Task<DeviceConnState> ConnectToAllDevices(string controllerIP)
        {

            DeviceConnState connState = new DeviceConnState
            {
                controllerConnected = false,
                ioLinkDevicesConnected = false,
                camerasConnected = false,
                daqConnected = false
            };

            App.LocalDataManager.localData.controller_ip = controllerIP;

            // Connect to controller
            connState.controllerConnected = await App.ControllerManager.Connect(controllerIP);

            // Ask controller for IO Link device status

            // Load settings

            // Connect to cameras

            // Connect to DAQ


            return connState;


        }
    }
}
