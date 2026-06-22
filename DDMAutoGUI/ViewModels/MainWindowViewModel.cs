using DDMAutoGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DDMAutoGUI.ViewModels
{
    /// <summary>
    /// ViewModel for MainWindow. Handles all business logic and state management
    /// for the application including connection, dispense process, and UI coordination.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IControllerManager _controllerManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IResultsManager _resultsManager;
        private readonly ICameraManager _cameraManager;
        private readonly IReleaseInfoManager _releaseInfoManager;
        private readonly ILocalDataManager _localDataManager;
        private readonly IApplicationConfiguration _appConfig;
        private readonly DispenseProcessService _dispenseProcessService;

        private string _appTitle;
        private bool _isConnected;
        private string _connectionStatus;
        private string _processLog;
        private string _connectionLog;
        private string _statusLog;
        private string _robotLog;
        private double _processProgress;
        private bool _isProcessing;
        private string _motorSerialNumber;
        private string _controllerIpAddress;
        private string _selectedMotorType;
        private bool _isDispenseProcessRunning;
        private int _selectedMotorSizeIndex;
        private ObservableCollection<string> _motorSizes;

        public MainWindowViewModel(
            IControllerManager controllerManager,
            ISettingsManager settingsManager,
            IResultsManager resultsManager,
            ICameraManager cameraManager,
            IReleaseInfoManager releaseInfoManager,
            ILocalDataManager localDataManager,
            IApplicationConfiguration appConfig,
            DispenseProcessService dispenseProcessService)
        {
            _controllerManager = controllerManager ?? throw new ArgumentNullException(nameof(controllerManager));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _resultsManager = resultsManager ?? throw new ArgumentNullException(nameof(resultsManager));
            _cameraManager = cameraManager ?? throw new ArgumentNullException(nameof(cameraManager));
            _releaseInfoManager = releaseInfoManager ?? throw new ArgumentNullException(nameof(releaseInfoManager));
            _localDataManager = localDataManager ?? throw new ArgumentNullException(nameof(localDataManager));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _dispenseProcessService = dispenseProcessService ?? throw new ArgumentNullException(nameof(dispenseProcessService));

            _controllerIpAddress = "192.168.1.1";

            InitializeCommands();
            InitializeEventHandlers();
            InitializeAppTitle();
            InitializeMotorSizes();
        }

        #region Properties

        public string AppTitle
        {
            get => _appTitle;
            set => SetProperty(ref _appTitle, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public string ProcessLog
        {
            get => _processLog;
            set => SetProperty(ref _processLog, value);
        }

        public string ConnectionLog
        {
            get => _connectionLog;
            set => SetProperty(ref _connectionLog, value);
        }

        public string StatusLog
        {
            get => _statusLog;
            set => SetProperty(ref _statusLog, value);
        }

        public string RobotLog
        {
            get => _robotLog;
            set => SetProperty(ref _robotLog, value);
        }

        public double ProcessProgress
        {
            get => _processProgress;
            set => SetProperty(ref _processProgress, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string MotorSerialNumber
        {
            get => _motorSerialNumber;
            set => SetProperty(ref _motorSerialNumber, value);
        }

        public string ControllerIpAddress
        {
            get => _controllerIpAddress;
            set => SetProperty(ref _controllerIpAddress, value);
        }

        public string SelectedMotorType
        {
            get => _selectedMotorType;
            set => SetProperty(ref _selectedMotorType, value);
        }

        public bool IsDispenseProcessRunning
        {
            get => _isDispenseProcessRunning;
            set => SetProperty(ref _isDispenseProcessRunning, value);
        }

        public int SelectedMotorSizeIndex
        {
            get => _selectedMotorSizeIndex;
            set => SetProperty(ref _selectedMotorSizeIndex, value);
        }

        public ObservableCollection<string> MotorSizes
        {
            get => _motorSizes;
            set => SetProperty(ref _motorSizes, value);
        }

        /// <summary>
        /// Gets whether the application is running in simulation mode.
        /// Reads directly from application configuration.
        /// </summary>
        public bool IsSimulationMode => _appConfig.IsSimulationMode;

        // For displaying/managing logged content
        public string ConnectionLogText { get; set; }
        public string StatusLogText { get; set; }
        public string RobotLogText { get; set; }
        public string ProcessLogText { get; set; }

        // For controller state display
        public bool IsPowerEnabled { get; set; }
        public bool IsRobotHomed { get; set; }
        public float LinearPosition { get; set; }
        public float RotaryPosition { get; set; }
        // ... etc for all readouts

        // For motor settings display
        public string MotorSettingsDisplay { get; set; }

        // For advanced options
        public bool AdvancedOptionsConnectController { get; set; }
        // ... for each checkbox

        // For robot measurement data
        public List<ResultsHeightMeasurement> LaserRingData { get; set; }
        public List<ResultsHeightMeasurement> LaserMagData { get; set; }

        #endregion

        #region Commands

        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand StartDispenseCommand { get; private set; }
        public ICommand CancelDispenseCommand { get; private set; }
        public ICommand ViewResultsCommand { get; private set; }
        public ICommand OpenResultsDirectoryCommand { get; private set; }
        public ICommand AcquireTopCommand { get; private set; }
        public ICommand AcquireSideCommand { get; private set; }
        public ICommand OpenResultsFolderCommand { get; private set; }
        public ICommand LockAdvancedTabCommand { get; private set; }

        private void InitializeCommands()
        {
            ConnectCommand = new AsyncRelayCommand<string>(ExecuteConnect, parameter => CanConnect(parameter));
            DisconnectCommand = new AsyncRelayCommand(ExecuteDisconnect, parameter => CanDisconnect(parameter));
            StartDispenseCommand = new AsyncRelayCommand(ExecuteStartDispense, parameter => CanStartDispense(parameter));
            CancelDispenseCommand = new RelayCommand(ExecuteCancelDispense, parameter => CanCancelDispense(parameter));
            ViewResultsCommand = new RelayCommand(ExecuteViewResults);
            OpenResultsDirectoryCommand = new RelayCommand(ExecuteOpenResultsDirectory);
            AcquireTopCommand = new AsyncRelayCommand(ExecuteAcquireTop, CanAcquireImage);
            AcquireSideCommand = new AsyncRelayCommand(ExecuteAcquireSide, CanAcquireImage);
            OpenResultsFolderCommand = new RelayCommand(_ => ExecuteOpenResultsFolder());
            LockAdvancedTabCommand = new RelayCommand(_ => ExecuteLockAdvancedTab());
        }

        #endregion

        #region Command Execution

        private async Task ExecuteConnect(string ipAddress)
        {
            try
            {
                IsProcessing = true;
                ConnectionStatus = "Connecting...";

                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = ControllerIpAddress;
                }

                bool connected = await _controllerManager.Connect(ipAddress);
                IsConnected = connected;
                ConnectionStatus = connected ? "Connected" : "Connection failed";
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Error: {ex.Message}";
                Debug.Print($"Connection error: {ex}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanConnect(string parameter) => !IsProcessing && !IsConnected;

        private async Task ExecuteDisconnect(object parameter)
        {
            try
            {
                IsProcessing = true;
                await _controllerManager.Disconnect();
                IsConnected = false;
                ConnectionStatus = "Disconnected";
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Disconnect error: {ex.Message}";
                Debug.Print($"Disconnect error: {ex}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanDisconnect(object parameter) 
            => IsConnected && !IsProcessing;

        private async Task ExecuteStartDispense(object parameter)
        {
            try
            {
                IsProcessing = true;
                IsDispenseProcessRunning = true;
                ProcessProgress = 0;
                ProcessLog = "";

                if (string.IsNullOrEmpty(MotorSerialNumber))
                {
                    ProcessLog = "Error: Motor serial number is required";
                    return;
                }

                if (!IsConnected)
                {
                    ProcessLog = "Error: Controller not connected";
                    return;
                }

                // Use _appConfig.AdvancedOptions
                var result = await _dispenseProcessService.ExecuteFullDispenseProcessAsync(
                    SelectedMotorType,
                    MotorSerialNumber,
                    _appConfig.AdvancedOptions);

                if (result.Success)
                {
                    ProcessLog += $"\n\n=== PROCESS COMPLETED ===\n";
                    ProcessLog += $"Result: {(result.Pass ? "PASS" : "FAIL")}\n";
                    ProcessLog += $"Message: {result.Message}\n";
                    ProcessLog += $"Results saved to: {result.ResultsPath}";
                }
                else
                {
                    ProcessLog += $"\n\n=== PROCESS FAILED ===\n";
                    ProcessLog += $"Error: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                ProcessLog += $"\n\nUnexpected error: {ex.Message}";
                Debug.Print($"Dispense process error: {ex}");
            }
            finally
            {
                IsProcessing = false;
                IsDispenseProcessRunning = false;
            }
        }

        private bool CanStartDispense(object parameter) 
            => IsConnected && !IsProcessing && !string.IsNullOrEmpty(MotorSerialNumber) && !string.IsNullOrEmpty(SelectedMotorType);

        private void ExecuteCancelDispense(object parameter)
        {
            IsProcessing = false;
            IsDispenseProcessRunning = false;
            ProcessLog += "\n\n=== PROCESS CANCELLED BY USER ===";
        }

        private bool CanCancelDispense(object parameter) 
            => IsDispenseProcessRunning;

        private void ExecuteViewResults(object parameter)
        {
            string resultsJson = _resultsManager.GetCurrentResultsAsString();
            if (!string.IsNullOrEmpty(resultsJson))
            {
                Debug.Print(resultsJson);
            }
        }

        private void ExecuteOpenResultsDirectory(object parameter)
        {
            _resultsManager.OpenBrowserToDirectory();
        }

        private async Task ExecuteAcquireTop(object parameter)
        {
            // Delegate to camera manager
        }

        private async Task ExecuteAcquireSide(object parameter)
        {
            // Delegate to camera manager
        }

        private void ExecuteOpenResultsFolder()
        {
            _resultsManager.OpenBrowserToDirectory();
        }

        private void ExecuteLockAdvancedTab()
        {
            // Lock logic here
        }

        private bool CanAcquireImage(object parameter) => IsConnected && !IsProcessing;

        #endregion

        #region Event Handlers

        private void InitializeEventHandlers()
        {
            _controllerManager.ControllerConnected += ControllerManager_Connected;
            _controllerManager.ControllerDisconnected += ControllerManager_Disconnected;
            _controllerManager.ConnectionLogUpdated += ControllerManager_ConnectionLogUpdated;
            _controllerManager.StatusLogUpdated += ControllerManager_StatusLogUpdated;
            _controllerManager.RobotLogUpdated += ControllerManager_RobotLogUpdated;
            _resultsManager.UpdateProcessLog += ResultsManager_UpdateProcessLog;
        }

        private void ControllerManager_Connected(object sender, EventArgs e)
        {
            IsConnected = true;
            ConnectionStatus = "Connected";
        }

        private void ControllerManager_Disconnected(object sender, EventArgs e)
        {
            IsConnected = false;
            ConnectionStatus = "Disconnected";
        }

        private void ControllerManager_ConnectionLogUpdated(object sender, EventArgs e)
        {
            ConnectionLog = _controllerManager.GetConnectionLog();
        }

        private void ControllerManager_StatusLogUpdated(object sender, EventArgs e)
        {
            StatusLog = _controllerManager.GetStatusLog();
        }

        private void ControllerManager_RobotLogUpdated(object sender, EventArgs e)
        {
            RobotLog = _controllerManager.GetRobotLog();
        }

        private void ResultsManager_UpdateProcessLog(object sender, EventArgs e)
        {
            ProcessLog = _resultsManager.GetLogAsString();
        }

        private void DispenseService_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProcessProgress = e.Percentage;
        }

        #endregion

        #region Helper Methods

        private void InitializeAppTitle()
        {
            string version = _releaseInfoManager.GetCurrentVersion();
            AppTitle = $"DDM Auto GUI - {version}";
        }

        private void InitializeMotorSizes()
        {
            MotorSizes = new ObservableCollection<string>
            {
                "ddm_57",
                "ddm_95",
                "ddm_116",
                "ddm_170",
                "ddm_170_tall"
            };
            SelectedMotorSizeIndex = 2; // Default to ddm_116
        }

        #endregion
    }
}