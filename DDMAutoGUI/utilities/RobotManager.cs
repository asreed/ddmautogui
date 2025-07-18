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



namespace DDMAutoGUI.utilities
{

    // Manages connection to PCR robot controller running TCS
    // Also handles events for robot state so GUI can be locked/unlocked accordingly




    public class UIState
    {
        public bool isConnected = false;
        public bool isDispenseWizardActive = false;

        public bool isAutoStateRequesting = false;
    }

    public class RobotState
    {
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


        public const string CORRECT_TCS_VERSION = "Tcs_ddm_cell_1_1_3"; // ???? ?????????????


        private string statusLog = string.Empty;
        private string robotLog = string.Empty;





        private double autoStatusInterval = 1.0; //sec

        private bool debug = true;
        private string term = "\n";
        private int sendTimeout = 5000;
        private int receiveTimeout = 5000;

        private Socket statusClient;
        private Socket robotClient;

        public event EventHandler UpdateUIState;
        public event EventHandler ReceiveStatus;
        public event EventHandler ChangeStatusLog;
        public event EventHandler ChangeRobotLog;
        public event EventHandler UpdateAutoStatus;


        private UIState UI_STATE = new UIState();
        private RobotState CONTROLLER_STATE = new RobotState();

        public RobotManager()
        {
            //


        }



        // Process-specific commands

        public async void BeginDispense()
        {
            UIState newState = UI_STATE;
            //newState.isDispensing = true;
            //SetUIState(newState);

            //await SendRobotCommandAsync("dispense");

            //newState = UI_STATE;
            //newState.isDispensing = false;
            SetUIState(newState);
        }





        // Load TCS

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




        // General TCS messaging

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

            UpdateRobotLog("Connecting...");
            try
            {
                await statusClient.ConnectAsync(statusEP);
                await robotClient.ConnectAsync(robotEP);


                UpdateRobotLog("Connected");
                UIState newState = UI_STATE;
                newState.isConnected = true;
                SetUIState(newState);
                return true;

            }
            catch (SocketException e)
            {


                UpdateRobotLog("Connection error");
                UpdateRobotLog($"{e.ErrorCode}: {e.Message}");
                statusClient.Close();
                robotClient.Close();

                UIState newState = UI_STATE;
                newState.isConnected = false;
                SetUIState(newState);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            StopAutoStatus();
            await SendStatusCommandAsync("exit");
            await SendRobotCommandAsync("exit");
            statusClient.Shutdown(SocketShutdown.Both);
            statusClient.Close();
            robotClient.Shutdown(SocketShutdown.Both);
            robotClient.Close();

            UIState newState = UI_STATE;
            newState.isConnected = false;
            SetUIState(newState);

        }

        public async Task<string> SendRobotCommandAsync(string command)
        {
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
            }
            return response.ToString().Trim();
        }


        public async Task<string> SendStatusCommandAsync(string command)
        {
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
            }
            return response;
        }

        public async Task<string> GetControllerSoftwareVersionAsync()
        {
            string response = await SendStatusCommandAsync("getversion");
            string version = response.Split(" ")[1];
            return version;
        }

        public async Task<string> GetControllerConfigVersionAsync()
        {
            string response = await SendStatusCommandAsync("getconfigversion");
            string[] fullversion = response.Split(" ");
            string version = string.Join(" ", fullversion.Skip(1));
            return version;
        }


        private RobotState ParseControllerStatus(string newStatusString)
        {
            RobotState newStatus = new RobotState();
            string[] status = newStatusString.Split(" ");

            newStatus.isPowerEnabled        = status[1] != "0";
            newStatus.isRobotHomed          = status[2] != "0";

            newStatus.posLinear             = float.Parse(status[3]);
            newStatus.posRotary             = float.Parse(status[4]);

            newStatus.isLinearIn1           = status[5] != "0";
            newStatus.isLinearIn2           = status[6] != "0";
            newStatus.isLinearIn3           = status[7] != "0";

            newStatus.pressureCommand1      = float.Parse(status[8]);
            newStatus.pressureMeasurement1  = float.Parse(status[9]);
            newStatus.pressureCommand2      = float.Parse(status[10]);
            newStatus.pressureMeasurement2  = float.Parse(status[11]);

            newStatus.flowVolume1           = float.Parse(status[12]);
            newStatus.flowError1            = int.Parse(status[13]);
            newStatus.flowVolume2           = float.Parse(status[14]);
            newStatus.flowError2            = int.Parse(status[15]);

            return newStatus;
        }

        private void UpdateStatusLog(string logLine)
        {
            statusLog += logLine + "\n";
            ChangeStatusLog?.Invoke(this, EventArgs.Empty);
            //Debug.Print($"S: {logLine}");
        }

        private void UpdateRobotLog(string logLine)
        {
            robotLog += logLine + "\n";
            ChangeRobotLog?.Invoke(this, EventArgs.Empty);
            //Debug.Print($"R: {logLine}");

        }



        private DispatcherTimer _timer;

        public void StartAutoStatus()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(autoStatusInterval);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        public void StopAutoStatus()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }

        public void Timer_Tick(object sender, EventArgs e)
        {
            GetStatusTaskAsync();
        }

        private async void GetStatusTaskAsync()
        {
            string response = await SendStatusCommandAsync("systemstatus");
            CONTROLLER_STATE = ParseControllerStatus(response);
            UpdateAutoStatus?.Invoke(this, EventArgs.Empty);
        }








        public UIState GetUIState()
        {
            return UI_STATE;
        }
        public RobotState GetControllerState()
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
        // Robot routines

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
