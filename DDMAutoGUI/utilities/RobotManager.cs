using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Web;



namespace DDMAutoGUI.utilities
{

    // Manages connection to PCR robot controller running TCS
    // Also handles events for robot state so GUI can be locked/unlocked accordingly




    public class UIState
    {
        public bool isConnected = false;
        public bool isAutoStateRequesting = false;

    }

    public class ControllerState
    {
        public bool parseError = false;
        public string parseErrorMessage = string.Empty;

        public bool isPowerEnabled = false;
        public bool isRobotHomed = false;

        public float posLinear = 0;
        public float posRotary = 0;

        public bool isLinearIn1 = false;
        public bool isLinearIn2 = false;
        public bool isLinearIn3 = false;

        public float pressureCommand1 = 0;
        public float pressureMeasurement1 = 0;
        public float pressureCommand2 = 0;
        public float pressureMeasurement2 = 0;

        public float flowVolume1 = 0;
        public float flowError1 = 0;
        public float flowVolume2 = 0;
        public float flowError2 = 0;

    }



    public sealed class RobotManager
    {
        // singleton pattern (maybe not the best idea?)
        private static readonly Lazy<RobotManager> lazy =
            new Lazy<RobotManager>(() => new RobotManager());
        public static RobotManager Instance { get { return lazy.Value; } }




        public const string CORRECT_TCS_VERSION = "Tcs_ddm_cell_1_1_4"; // ???? ?????????????


        private string statusLog = string.Empty;
        private string robotLog = string.Empty;

        private double autoStatusInterval = 1.0; //sec
        private DispatcherTimer _timer;

        private string term = "\n";
        private int sendTimeout = 2000;
        private int receiveTimeout = 2000;

        private Socket statusClient;
        private Socket robotClient;

        public event EventHandler UpdateUIState;
        public event EventHandler ReceiveStatus;
        public event EventHandler ChangeStatusLog;
        public event EventHandler ChangeRobotLog;
        public event EventHandler UpdateAutoStatus;

        private UIState UI_STATE = new UIState();
        private ControllerState CONTROLLER_STATE = new ControllerState();

        public RobotManager()
        {
            //


        }









        // ==================================================================
        // Autoload TCS (port 23)

        public async Task<string> ReceiveConsoleHeader(Socket client)
        {

            StringBuilder response = new StringBuilder();
            try
            {
                Debug.Print("attempting to receive header");
                byte[] buffer = new byte[1024];
                int bytesRead;

                while (true)
                {
                    bytesRead = await client.ReceiveAsync(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    Debug.Print($"building response: {response.ToString()}");
                    if (response.ToString().Trim().EndsWith(":") || response.ToString().Trim().EndsWith("..."))
                    {
                        break;
                    }

                }
                Debug.Print($"full response: {response.ToString()}");
            }
            catch (SocketException e)
            {
                Debug.Print("Send console command failed");
                Debug.Print($"{e.ErrorCode}: {e.Message}");
                response.Append("Error?");
            }
            return response.ToString();
        }

        public async Task<string> SendConsoleCmd(Socket client, string command)
        {
            byte[] commandBytes = Encoding.ASCII.GetBytes(command + term);
            StringBuilder response = new StringBuilder();
            try
            {
                Debug.Print($"c >> {command}");
                await client.SendAsync(commandBytes);
                byte[] buffer = new byte[1024];
                int bytesRead;

                while (true)
                {
                    bytesRead = await client.ReceiveAsync(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    //Debug.Print($"building response: {response.ToString()}");
                    if (response.ToString().Trim().EndsWith(":"))
                    {
                        break;
                    }
                }
                Debug.Print($"c << {response.ToString()}");
            }
            catch (SocketException e)
            {
                Debug.Print("Send console command failed");
                Debug.Print($"{e.ErrorCode}: {e.Message}");
                response.Clear().Append("Failed");
            }
            return response.ToString();
        }

        public async Task<string> AttemptLoadTCS(string ip)
        {
            string reply;
            string response;
            IPEndPoint controllerEP = new IPEndPoint(IPAddress.Parse(ip), 23);
            Socket controllerClient = new Socket(controllerEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            controllerClient.SendTimeout = sendTimeout;
            controllerClient.ReceiveTimeout = receiveTimeout;
            try
            {
                await controllerClient.ConnectAsync(controllerEP);
                await ReceiveConsoleHeader(controllerClient);
                await SendConsoleCmd(controllerClient, "Help");
                response = await SendConsoleCmd(controllerClient, "show thread");
                if (response.Contains(CORRECT_TCS_VERSION))
                {
                    // correct TCS already running
                    Debug.Print("correct TCS running");
                    reply = "Correct TCS version already running";
                }
                else
                {
                    Debug.Print("incorrect or no TCS running. Attempting to load.");
                    response = await SendConsoleCmd(controllerClient, $"load flash/projects/{CORRECT_TCS_VERSION} -start");
                    if (response.Contains("-508"))
                    {
                        Debug.Print("correct TCS is not installed on controller");
                        reply = "Correct TCS NOT INSTALLED";
                    }
                    else if (response.Contains("-745"))
                    {
                        // project already loaded
                        response = await SendConsoleCmd(controllerClient, $"start {CORRECT_TCS_VERSION} -compile");
                        response = await SendConsoleCmd(controllerClient, "show thread");
                        if (response.Contains(CORRECT_TCS_VERSION))
                        {
                            // correct TCS is now running
                            reply = "Success. Correct TCS is running";



                        }
                        else
                        {
                            // tried to load program and now it's not running?
                            reply = "Attempted to load and run; not running?";
                        }

                    }
                    else if (response.Contains("Compile successful"))
                    {
                        // project apparently running
                        response = await SendConsoleCmd(controllerClient, "show thread");
                        if (response.Contains(CORRECT_TCS_VERSION))
                        {
                            // correct TCS is now running
                            reply = "Success. Correct TCS is running";



                        }
                        else
                        {
                            // tried to load program and now it's not running?
                            reply = "Attempted to load and run; not running?";
                        }
                    }
                    else
                    {
                        // unexpected response from compile command
                        reply = "Unexpected respones to compile request";
                    }
                }
                await SendConsoleCmd(controllerClient, "quit");
                controllerClient.Close();
                Debug.Print("end of attempt reached");
            }
            catch (SocketException e)
            {
                Debug.Print(($"{e.ErrorCode}: {e.Message}"));
                controllerClient.Close();
                reply = "Connection failure";
            }
            return reply;
        }




        // ==================================================================
        // General TCS messaging (port 10000 and 10100)

        public async Task<bool> ConnectAsync(string ip)
        {
            IPEndPoint statusEP = new IPEndPoint(IPAddress.Parse(ip), 10000);
            IPEndPoint robotEP = new IPEndPoint(IPAddress.Parse(ip), 10100);

            statusClient = new Socket(statusEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            robotClient = new Socket(robotEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            statusClient.SendTimeout = sendTimeout;
            statusClient.ReceiveTimeout = receiveTimeout;

            robotClient.SendTimeout = sendTimeout;
            robotClient.ReceiveTimeout = receiveTimeout;

            UpdateBothLogs($"Connecting to {ip}...");
            try
            {
                await statusClient.ConnectAsync(statusEP);
                await robotClient.ConnectAsync(robotEP);

                // It seems to be possible that the connection succeeds even for some
                // apparently random IP addresses. Need to send test command.

                if (await TestStatusConnection() != "0")
                {
                    SocketException ex = new SocketException(-1, "Failed test command");
                    throw ex;
                }

                UpdateBothLogs("Connection succeeded");

                UIState newState = UI_STATE;
                newState.isConnected = true;
                SetUIState(newState);

                return true;

            }
            catch (SocketException e)
            {

                statusClient.Close();
                robotClient.Close();
                UpdateBothLogs("Connection failed");
                UpdateBothLogs($"{e.ErrorCode}: {e.Message}");

                UIState newState = UI_STATE;
                newState.isConnected = false;
                SetUIState(newState);

                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            UpdateBothLogs("Disconnecting...");
            try
            {

                StopAutoStatus();
                await SendStatusCommandAsync("exit");
                await SendRobotCommandAsync("exit");
                statusClient.Shutdown(SocketShutdown.Both);
                statusClient.Close();
                robotClient.Shutdown(SocketShutdown.Both);
                robotClient.Close();

            }
            catch (SocketException e)
            {
                UpdateBothLogs("Disconnection failed");
                UpdateBothLogs($"{e.ErrorCode}: {e.Message}");
            }

            UIState newState = UI_STATE;
            newState.isConnected = false;
            SetUIState(newState);

        }

        public async Task<string> SendRobotCommandAsync(string command)
        {
            if (!UI_STATE.isConnected)
            {
                return "-100";
            }

            UpdateRobotLog($">> {command}");

            byte[] commandBytes = Encoding.ASCII.GetBytes(command + term); //don't forget termination char
            StringBuilder response = new StringBuilder();

            try
            {
                await robotClient.SendAsync(commandBytes);

                byte[] buffer = new byte[1024];
                int bytesRead;
                int i = 0;

                while (true)
                {
                    bytesRead = await robotClient.ReceiveAsync(buffer);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    string partialResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    response.Append(partialResponse);
                    if (partialResponse.Contains(term))
                    {
                        break;
                    }
                    i++;
                }

                UpdateRobotLog($"<< {response.ToString().Trim()}");
            }
            catch (SocketException e)
            {
                UpdateRobotLog("Send failed");
                UpdateRobotLog($"{e.ErrorCode}: {e.Message}");
                response = new StringBuilder();

                UIState newState = UI_STATE;
                newState.isConnected = false;
                SetUIState(newState);

            }
            return response.ToString().Trim();
        }


        public async Task<string> SendStatusCommandAsync(string command)
        {
            if (!UI_STATE.isConnected)
            {
                return "-100";
            }

            UpdateStatusLog($">> {command}");

            byte[] commandBytes = Encoding.ASCII.GetBytes(command + term); //don't forget termination char
            string response = string.Empty;

            try
            {
                await statusClient.SendAsync(commandBytes);
                byte[] buffer = new byte[1024];
                int receivedLength = await statusClient.ReceiveAsync(buffer);
                response = Encoding.ASCII.GetString(buffer, 0, receivedLength).Trim();
                UpdateStatusLog($"<< {response}");
            }
            catch (SocketException e)
            {
                UpdateStatusLog("Send failed");
                UpdateStatusLog($"{e.ErrorCode}: {e.Message}");
                response = string.Empty;

                UIState newState = UI_STATE;
                newState.isConnected = false;
                SetUIState(newState);
            }
            return response;
        }












        // ==================================================================
        // Private helpers

        private async Task<string> TestStatusConnection()
        {
            string response = await SendStatusCommandAsync("nop");
            return response.Trim();
        }

        private ControllerState ParseControllerStatus(string newStatusString)
        {
            ControllerState newStatus = new ControllerState();
            string[] status = newStatusString.Split(" ");
            if (status.Length > 1)
            {
                try
                {
                    newStatus.isPowerEnabled = status[1] != "0";
                    newStatus.isRobotHomed = status[2] != "0";

                    newStatus.posLinear = float.Parse(status[3]);
                    newStatus.posRotary = float.Parse(status[4]);

                    newStatus.isLinearIn1 = status[5] != "0";
                    newStatus.isLinearIn2 = status[6] != "0";
                    newStatus.isLinearIn3 = status[7] != "0";

                    newStatus.pressureCommand1 = float.Parse(status[8]);
                    newStatus.pressureMeasurement1 = float.Parse(status[9]);
                    newStatus.pressureCommand2 = float.Parse(status[10]);
                    newStatus.pressureMeasurement2 = float.Parse(status[11]);

                    newStatus.flowVolume1 = float.Parse(status[12]);
                    newStatus.flowError1 = int.Parse(status[13]);
                    newStatus.flowVolume2 = float.Parse(status[14]);
                    newStatus.flowError2 = int.Parse(status[15]);

                    newStatus.parseError = false;
                }
                catch
                {
                    // likely version mismatch

                    newStatus.parseError = true;
                    newStatus.parseErrorMessage = "Could not parse data into structure";
                }
            }
            else
            {
                // likely error from controller

                newStatus.parseError = true;
                newStatus.parseErrorMessage = status[0];
            }
            return newStatus;

        }

        private void UpdateStatusLog(string logLine)
        {
            statusLog += logLine + "\n";
            ChangeStatusLog?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateRobotLog(string logLine)
        {
            robotLog += logLine + "\n";
            ChangeRobotLog?.Invoke(this, EventArgs.Empty);

        }

        private void UpdateBothLogs(string logLine)
        {
            UpdateStatusLog(logLine);
            UpdateRobotLog(logLine);
        }





        // ==================================================================
        // Auto status start/stop

        public void StartAutoStatus()
        {
            if (UI_STATE.isConnected)
            {
                if (UI_STATE.isAutoStateRequesting == false)
                {
                    _timer = new DispatcherTimer();
                    _timer.Interval = TimeSpan.FromSeconds(autoStatusInterval);
                    _timer.Tick += Timer_Tick;
                    _timer.Start();

                    UIState newState = UI_STATE;
                    newState.isAutoStateRequesting = true;
                    SetUIState(newState);
                }
            }
        }

        public void StopAutoStatus()
        {
            if (UI_STATE.isConnected)
            {
                if (UI_STATE.isAutoStateRequesting == true)
                {
                    if (_timer != null)
                    {
                        _timer.Stop();
                        _timer.Tick -= Timer_Tick;
                        _timer = null;

                        UIState newState = UI_STATE;
                        newState.isAutoStateRequesting = false;
                        SetUIState(newState);
                    }
                }
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            string response = await SendStatusCommandAsync("systemstatus");

            ControllerState newState = ParseControllerStatus(response);
            if (newState != null)
            {
                CONTROLLER_STATE = newState;
                UpdateAutoStatus?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // ?
            }
        }








        // ==================================================================
        // Public state set/get methods

        public UIState GetUIState()
        {
            return UI_STATE;
        }
        public ControllerState GetControllerState()
        {
            return CONTROLLER_STATE;
        }

        public void SetUIState(UIState state)
        {
            UI_STATE = state;
            UpdateUIState?.Invoke(this, EventArgs.Empty);
        }

        public string GetStatusLog()
        {
            return statusLog;
        }

        public string GetRobotLog()
        {
            return robotLog;
        }

        public string GetCorrectTCSVersion()
        {
            return CORRECT_TCS_VERSION;
        }








        // ==================================================================
        // Public robot routines

        public async Task<string> GetControllerSoftwareVersionAsync()
        {
            string response = await SendStatusCommandAsync("getversion");
            string version = string.Empty;
            if (response.Split(" ").Length > 1)
            {
                version = response.Split(" ")[1];
            }
            return version;
        }

        public async Task<string> GetControllerConfigVersionAsync()
        {
            string response = await SendStatusCommandAsync("getconfigversion");
            string[] fullversion = response.Split(" ");
            string version = string.Empty;
            if (fullversion.Length > 1)
            {
                version = string.Join(" ", fullversion.Skip(1));
            }
            return version;
        }

        public async Task<string> EnablePower()
        {
            string response;
            int timeout = 0;

            response = await SendRobotCommandAsync("hp 1");
            while (true)
            {
                response = await SendRobotCommandAsync("hp");
                if (response == "0 1")
                {
                    break;
                }
                timeout++;
                if (timeout > 20)
                {
                    response = "Timeout";
                    break;
                }
                await Task.Delay(500);
            }
            return response;
        }

        public async Task<string> Home()
        {
            string response = "not implemented yet";
            return response;
        }

        public async Task<string> MoveOneAxis(int axisNumber, float position)
        {
            string input = $"DDM_MoveOneAxis {axisNumber} {position}";
            string response = await SendRobotCommandAsync(input);
            return response;
        }

        public async Task<string> MoveJ(float xPosition, float thPosition)
        {
            string input = $"DDM_MoveJ {xPosition} {thPosition}";
            string response = await SendRobotCommandAsync(input);
            return response;
        }

        public async Task<string> SpinInPlace(float spinTime, float spinSpeed)
        {
            string input = $"DDM_SpinInPlace {spinTime} {spinSpeed}";
            string response = await SendRobotCommandAsync(input);
            return response;
        }

    }
}
