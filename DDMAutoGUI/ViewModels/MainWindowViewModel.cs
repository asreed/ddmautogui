using DDMAutoGUI.Services;
using DDMAutoGUI.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DDMAutoGUI.ViewModels
{
    /// <summary>
    /// ViewModel for MainWindow. Handles all business logic and state management
    /// for the application including connection, dispense process, and UI coordination.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly IControllerManager _controllerManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IResultsManager _resultsManager;
        private readonly ICameraManager _cameraManager;
        private readonly ILocalDataManager _localDataManager;
        private readonly IApplicationConfiguration _appConfig;
        private readonly IPartCycleService _partCycleService;

        private string _appTitle;
        private bool _isConnected;
        private string _connectionStatus;
        private string _processLog;
        private string _connectionLog;
        private string _statusLog;
        private string _robotLog;
        private double _processProgress;
        private string _currentStep;
        private bool _isProcessing;
        private string _motorSerialNumber;
        private string _controllerIpAddress;
        private string _selectedMotorType;
        private bool _isDispenseProcessRunning;
        private int _selectedMotorSizeIndex;
        private ObservableCollection<string> _motorSizes;

        private bool _isPowerEnabled;
        private bool _isRobotHomed;
        private float _linearPosition;
        private float _rotaryPosition;
        private bool _linearFlag1;
        private bool _linearFlag2;
        private bool _linearFlag3;
        private float _pressureCommand1;
        private float _pressureMeasurement1;
        private float _pressureCommand2;
        private float _pressureMeasurement2;
        private float _flowVolume1;
        private int _flowError1;
        private float _flowVolume2;
        private int _flowError2;
        private float _sysPressure;
        private int _safetyControllerState;
        private int _safetyErrorState;
        private bool _isSimulated;

        private DispatcherTimer _calibrationWatchTimer;

        private BitmapSource _acquiredImageSource;
        private string _cameraStatus;

        private DispenseResultStatus _resultStatus;
        private string _resultMessage;
        private string _resultRingSerial;
        private string _resultToolSerial;
        private string _resultDispenseVolumeId;
        private string _resultDispenseVolumeOd;
        private string _resultStepTopPhoto;
        private string _resultStepSidePhoto;
        private string _resultStepSerialNumbers;
        private string _resultStepMagnetPolarity;
        private string _resultStepMCHeight;
        private string _resultStepDispense;
        private string _resultStepTopPostPhoto;
        private string _resultMaxMCHeight;

        public MainWindowViewModel(
            IControllerManager controllerManager,
            ISettingsManager settingsManager,
            IResultsManager resultsManager,
            ICameraManager cameraManager,
            ILocalDataManager localDataManager,
            IApplicationConfiguration appConfig,
            IPartCycleService dispenseProcessService)
        {
            _controllerManager = controllerManager ?? throw new ArgumentNullException(nameof(controllerManager));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _resultsManager = resultsManager ?? throw new ArgumentNullException(nameof(resultsManager));
            _cameraManager = cameraManager ?? throw new ArgumentNullException(nameof(cameraManager));
            _localDataManager = localDataManager ?? throw new ArgumentNullException(nameof(localDataManager));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _partCycleService = dispenseProcessService ?? throw new ArgumentNullException(nameof(dispenseProcessService));

            _controllerIpAddress = "192.168.0.1";

            InitializeCommands();
            InitializeEventHandlers();
            InitializeAppTitle();
            InitializeMotorSizes();
            InitializeCalibrationWatch();

            _selectedMotorType = "ddm_116";
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
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    OnPropertyChanged(nameof(IsDisconnected));
                    OnPropertyChanged(nameof(IsRobotControlEnabled));
                    OnPropertyChanged(nameof(IsCalibrationExpired));
                }
            }
        }

        /// <summary>True when no controller is connected. Drives the Connect button.</summary>
        public bool IsDisconnected => !_isConnected;

        /// <summary>
        /// Gates the manual cell/robot command buttons. Requires a live connection and
        /// no robot command in flight, since robot commands run one-at-a-time and can
        /// take a second or two — we lock the controls until they return.
        /// </summary>
        public bool IsRobotControlEnabled => IsConnected && !_controllerManager.IsRobotBusy;

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

        public string CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string MotorSerialNumber
        {
            get => _motorSerialNumber;
            set
            {
                if (SetProperty(ref _motorSerialNumber, value))
                {
                    OnPropertyChanged(nameof(IsSerialNumberMissing));
                    Application.Current?.Dispatcher.BeginInvoke(
                        CommandManager.InvalidateRequerySuggested);
                }
            }
        }

        public string ControllerIpAddress
        {
            get => _controllerIpAddress;
            set => SetProperty(ref _controllerIpAddress, value);
        }

        public string SelectedMotorType
        {
            get => _selectedMotorType;
            set
            {
                if (SetProperty(ref _selectedMotorType, value))
                {
                    Application.Current?.Dispatcher.BeginInvoke(
                        CommandManager.InvalidateRequerySuggested);
                }
            }
        }

        public bool IsDispenseProcessRunning
        {
            get => _isDispenseProcessRunning;
            set => SetProperty(ref _isDispenseProcessRunning, value);
        }

        public int SelectedMotorSizeIndex
        {
            get => _selectedMotorSizeIndex;
            set
            {
                if (SetProperty(ref _selectedMotorSizeIndex, value))
                {
                    SelectedMotorType = (MotorSizes != null && value >= 0 && value < MotorSizes.Count)
                        ? MotorSizes[value]
                        : null;
                }
            }
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
        public bool IsPowerEnabled { get => _isPowerEnabled; set => SetProperty(ref _isPowerEnabled, value); }
        public bool IsRobotHomed { get => _isRobotHomed; set => SetProperty(ref _isRobotHomed, value); }
        public float LinearPosition { get => _linearPosition; set => SetProperty(ref _linearPosition, value); }
        public float RotaryPosition { get => _rotaryPosition; set => SetProperty(ref _rotaryPosition, value); }
        public bool LinearFlag1 { get => _linearFlag1; set => SetProperty(ref _linearFlag1, value); }
        public bool LinearFlag2 { get => _linearFlag2; set => SetProperty(ref _linearFlag2, value); }
        public bool LinearFlag3 { get => _linearFlag3; set => SetProperty(ref _linearFlag3, value); }
        public float PressureCommand1 { get => _pressureCommand1; set => SetProperty(ref _pressureCommand1, value); }
        public float PressureMeasurement1 { get => _pressureMeasurement1; set => SetProperty(ref _pressureMeasurement1, value); }
        public float PressureCommand2 { get => _pressureCommand2; set => SetProperty(ref _pressureCommand2, value); }
        public float PressureMeasurement2 { get => _pressureMeasurement2; set => SetProperty(ref _pressureMeasurement2, value); }
        public float FlowVolume1 { get => _flowVolume1; set => SetProperty(ref _flowVolume1, value); }
        public int FlowError1 { get => _flowError1; set => SetProperty(ref _flowError1, value); }
        public float FlowVolume2 { get => _flowVolume2; set => SetProperty(ref _flowVolume2, value); }
        public int FlowError2 { get => _flowError2; set => SetProperty(ref _flowError2, value); }
        public float SysPressure { get => _sysPressure; set => SetProperty(ref _sysPressure, value); }
        public int SafetyControllerState { get => _safetyControllerState; set => SetProperty(ref _safetyControllerState, value); }
        public int SafetyErrorState { get => _safetyErrorState; set => SetProperty(ref _safetyErrorState, value); }
        public bool IsSimulated { get => _isSimulated; set => SetProperty(ref _isSimulated, value); }

        // For motor settings display
        public string MotorSettingsDisplay { get; set; }

        // For advanced options
        public bool AdvancedOptionsConnectController { get; set; }
        // Connection Options - write-through to _appConfig.AdvancedOptions.ConnectionOptions
        public bool ConnectController
        {
            get => _appConfig.AdvancedOptions.ConnectionOptions.Controller;
            set
            {
                if (_appConfig.AdvancedOptions.ConnectionOptions.Controller != value)
                {
                    _appConfig.AdvancedOptions.ConnectionOptions.Controller = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ConnectIoLinkDevices
        {
            get => _appConfig.AdvancedOptions.ConnectionOptions.IoLinkDevices;
            set
            {
                if (_appConfig.AdvancedOptions.ConnectionOptions.IoLinkDevices != value)
                {
                    _appConfig.AdvancedOptions.ConnectionOptions.IoLinkDevices = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ConnectTopCamera
        {
            get => _appConfig.AdvancedOptions.ConnectionOptions.TopCamera;
            set
            {
                if (_appConfig.AdvancedOptions.ConnectionOptions.TopCamera != value)
                {
                    _appConfig.AdvancedOptions.ConnectionOptions.TopCamera = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ConnectSideCamera
        {
            get => _appConfig.AdvancedOptions.ConnectionOptions.SideCamera;
            set
            {
                if (_appConfig.AdvancedOptions.ConnectionOptions.SideCamera != value)
                {
                    _appConfig.AdvancedOptions.ConnectionOptions.SideCamera = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ConnectLaserSensor
        {
            get => _appConfig.AdvancedOptions.ConnectionOptions.LaserSensor;
            set
            {
                if (_appConfig.AdvancedOptions.ConnectionOptions.LaserSensor != value)
                {
                    _appConfig.AdvancedOptions.ConnectionOptions.LaserSensor = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ConnectDaqDevice
        {
            get => _appConfig.AdvancedOptions.ConnectionOptions.DaqDevice;
            set
            {
                if (_appConfig.AdvancedOptions.ConnectionOptions.DaqDevice != value)
                {
                    _appConfig.AdvancedOptions.ConnectionOptions.DaqDevice = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispCheckHealth
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.CheckHealth;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.CheckHealth != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.CheckHealth = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispPhotoTop
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.PhotoTop;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.PhotoTop != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.PhotoTop = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispPhotoSide
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.PhotoSide;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.PhotoSide != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.PhotoSide = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispRunOCR
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.RunOCR;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.RunOCR != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.RunOCR = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispCheckPolarity
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.CheckPolarity;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.CheckPolarity != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.CheckPolarity = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispMeasureHeights
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.MeasureHeights;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.MeasureHeights != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.MeasureHeights = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispDispense
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.Dispense;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.Dispense != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.Dispense = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispAutocalibrate
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.Autocalibrate;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.Autocalibrate != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.Autocalibrate = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispPhotoTopAfter
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.PhotoTopAfter;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.PhotoTopAfter != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.PhotoTopAfter = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DispOverrideWarnings
        {
            get => _appConfig.AdvancedOptions.PartCycleOptions.OverrideWarnings;
            set
            {
                if (_appConfig.AdvancedOptions.PartCycleOptions.OverrideWarnings != value)
                {
                    _appConfig.AdvancedOptions.PartCycleOptions.OverrideWarnings = value;
                    OnPropertyChanged();
                }
            }
        }

        // Image acquisition properties
        public BitmapSource AcquiredImageSource
        {
            get => _acquiredImageSource;
            set => SetProperty(ref _acquiredImageSource, value);
        }

        public string CameraStatus
        {
            get => _cameraStatus;
            set => SetProperty(ref _cameraStatus, value);
        }

        /// <summary>
        /// Overall outcome shown on the Review Results tab. Setting this also refreshes
        /// the three convenience flags the status borders bind their visibility to.
        /// </summary>
        public DispenseResultStatus ResultStatus
        {
            get => _resultStatus;
            set
            {
                if (SetProperty(ref _resultStatus, value))
                {
                    OnPropertyChanged(nameof(IsResultPass));
                    OnPropertyChanged(nameof(IsResultFail));
                    OnPropertyChanged(nameof(IsResultIncomplete));
                }
            }
        }

        public bool IsResultPass => _resultStatus == DispenseResultStatus.Pass;
        public bool IsResultFail => _resultStatus == DispenseResultStatus.Fail;
        public bool IsResultIncomplete => _resultStatus == DispenseResultStatus.Incomplete;

        public string ResultMessage
        {
            get => _resultMessage;
            set => SetProperty(ref _resultMessage, value);
        }

        public string ResultRingSerial
        {
            get => _resultRingSerial;
            set => SetProperty(ref _resultRingSerial, value);
        }

        public string ResultToolSerial
        {
            get => _resultToolSerial;
            set => SetProperty(ref _resultToolSerial, value);
        }

        public string ResultDispenseVolumeId
        {
            get => _resultDispenseVolumeId;
            set => SetProperty(ref _resultDispenseVolumeId, value);
        }

        public string ResultDispenseVolumeOd
        {
            get => _resultDispenseVolumeOd;
            set => SetProperty(ref _resultDispenseVolumeOd, value);
        }

        public string ResultStepTopPhoto
        {
            get => _resultStepTopPhoto;
            set => SetProperty(ref _resultStepTopPhoto, value);
        }

        public string ResultStepSidePhoto
        {
            get => _resultStepSidePhoto;
            set => SetProperty(ref _resultStepSidePhoto, value);
        }

        public string ResultStepSerialNumbers
        {
            get => _resultStepSerialNumbers;
            set => SetProperty(ref _resultStepSerialNumbers, value);
        }

        public string ResultStepMagnetPolarity
        {
            get => _resultStepMagnetPolarity;
            set => SetProperty(ref _resultStepMagnetPolarity, value);
        }

        public string ResultStepMCHeight
        {
            get => _resultStepMCHeight;
            set => SetProperty(ref _resultStepMCHeight, value);
        }

        public string ResultStepDispense
        {
            get => _resultStepDispense;
            set => SetProperty(ref _resultStepDispense, value);
        }

        public string ResultStepTopPostPhoto
        {
            get => _resultStepTopPostPhoto;
            set => SetProperty(ref _resultStepTopPostPhoto, value);
        }

        public string ResultMaxMCHeight
        {
            get => _resultMaxMCHeight;
            set => SetProperty(ref _resultMaxMCHeight, value);
        }

        // Tab indices for dispTabControl (inside the Operate tab) so the workflow
        // can drive navigation without scattering magic numbers at the call site.
        private const int SelectMotorTabIndex = 0;
        private const int MonitorProcessTabIndex = 1;
        private const int ReviewResultsTabIndex = 2;

        private int _selectedDispenseTabIndex;

        /// <summary>
        /// Two-way bound to dispTabControl.SelectedIndex so the dispense workflow can
        /// advance the operator from "Select Motor" to "Monitor Process" and finally
        /// to "Review Results".
        /// </summary>
        public int SelectedDispenseTabIndex
        {
            get => _selectedDispenseTabIndex;
            set => SetProperty(ref _selectedDispenseTabIndex, value);
        }

        private bool _readoutsEnabled;
        public bool ReadoutsEnabled { get => _readoutsEnabled; set => SetProperty(ref _readoutsEnabled, value); }

        /// <summary>
        /// True when no ring serial number has been entered. Drives the "No SN"
        /// warning box and contributes to the dispense command gate.
        /// </summary>
        public bool IsSerialNumberMissing => string.IsNullOrEmpty(MotorSerialNumber);

        /// <summary>
        /// True when flow calibration is missing or older than the configured expiry
        /// window. Drives the "Calibration Timeout" warning box and the dispense gate.
        /// Re-evaluated on a timer so it can flip on its own as time elapses.
        /// </summary>
        public bool IsCalibrationExpired
        {
            get
            {
                // The expiry threshold lives in controller settings, which are only
                // loaded while connected (and cleared to null on disconnect). Treat an
                // unknown state as "not expired" so we never surface a misleading
                // warning; CanStartDispense gates on IsConnected independently.
                if (!IsConnected)
                    return false;

                float? expHours = _settingsManager.GetAllSettings()?.dispense_system?.calib_exp_hours;
                if (expHours == null)
                    return false;

                DateTime? lastCalib = _localDataManager.GetLocalData()?.calib_data?.last_calib;
                if (lastCalib == null)
                    return true;

                return (DateTime.Now - lastCalib.Value).TotalHours > expHours.Value;
            }
        }

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
                CurrentStep = "Starting dispense process...";
                ProcessLog = "";

                ProcessLog += $"=== PROCESS STARTED ===\n";

                // Bring the operator to the live progress/log view as the run begins.
                SelectedDispenseTabIndex = MonitorProcessTabIndex;

                // Use _appConfig.AdvancedOptions
                var result = await _partCycleService.ExecutePartCycleAsync(
                    SelectedMotorType,
                    MotorSerialNumber,
                    _appConfig.AdvancedOptions);

                if (result.Success)
                {
                    if (result.Pass) {
                        ProcessLog += $"\n\n=== PROCESS PASSED ===\n";
                    }
                    else
                    {
                        ProcessLog += $"\n\n=== PROCESS FAILED ===\n";
                    }
                }
                else
                {
                    ProcessLog += $"\n\n=== PROCESS INCOMPLETE ===\n";
                }

                // Fill the Review Results tab and bring the operator to it, regardless of
                // outcome, so pass/fail/incomplete is always visible.
                PopulateResultsDisplay(result);
                SelectedDispenseTabIndex = ReviewResultsTabIndex;
            }
            catch (Exception ex)
            {
                ProcessLog += $"\n\nUnexpected error: {ex.Message}";
                Debug.Print($"Dispense process error: {ex}");
            }
            finally
            {
                // Advance to the results view now that the run has finished.
                SelectedDispenseTabIndex = ReviewResultsTabIndex;

                IsProcessing = false;
                IsDispenseProcessRunning = false;
            }
        }

        private bool CanStartDispense(object parameter) 
        {
            // Must be connected to controller
            if (!IsConnected)
                return false;

            // Cannot start while another process is running
            if (IsProcessing)
                return false;

            // Motor serial number is required
            if (IsSerialNumberMissing)
                return false;

            // Motor type must be selected
            if (string.IsNullOrEmpty(SelectedMotorType))
                return false;

            // Calibration must be present and within the configured expiry window
            if (IsCalibrationExpired)
                return false;

            return true;
        }

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
            await ExecuteAcquireCamera(CameraManager.CellCamera.top, "Top image acquired");
        }

        private async Task ExecuteAcquireSide(object parameter)
        {
            await ExecuteAcquireCamera(CameraManager.CellCamera.side, "Side image acquired");
        }

        private async Task ExecuteAcquireCamera(CameraManager.CellCamera camera, string successMessage)
        {
            try
            {
                IsProcessing = true;
                CameraStatus = "Acquiring image...";
                AcquiredImageSource = null;

                CameraAcquisitionResult result = await _cameraManager.AcquireAndSave(camera, null);

                if (result.success)
                {
                    CameraStatus = successMessage;
                    if (!string.IsNullOrEmpty(result.filePath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(result.filePath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Safe to use across threads
                        AcquiredImageSource = bitmap;
                    }
                }
                else
                {
                    CameraStatus = $"Error: {result.errorMsg}";
                }
            }
            catch (Exception ex)
            {
                CameraStatus = $"Error: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
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
            _controllerManager.ControllerStateChanged += ControllerManager_StateChanged;

            // Wire dispense progress reporting to the bound ProcessProgress property.
            // This subscription was lost in the MVVM/DI refactor, which is why the
            // Disp_ProcessPrg bar stopped advancing during a run.
            _partCycleService.ProgressChanged += DispenseProcessService_ProgressChanged;
            _controllerManager.RobotBusyChanged += ControllerManager_RobotBusyChanged;

            _localDataManager.LocalDataChanged += LocalDataManager_LocalDataChanged;
        }

        private void ControllerManager_Connected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = true;
                ConnectionStatus = $"Connected ({_controllerManager.CONNECTION_STATE?.connectedIP})";
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void ControllerManager_Disconnected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = false;
                ReadoutsEnabled = false;
                ConnectionStatus = "Not connected";
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void ControllerManager_ConnectionLogUpdated(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                ConnectionLog = _controllerManager.GetConnectionLog());
        }

        private void ControllerManager_StatusLogUpdated(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                StatusLog = _controllerManager.GetStatusLog());
        }

        private void ControllerManager_RobotLogUpdated(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                RobotLog = _controllerManager.GetRobotLog());
        }

        private void ResultsManager_UpdateProcessLog(object sender, EventArgs e)
        {
            ProcessLog = _resultsManager.GetLogAsString();
        }

        private void DispenseProcessService_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // The workflow awaits controller I/O and may resume on a worker thread,
            // so marshal the update to the UI thread to keep the OneWay bindings valid.
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                ProcessProgress = e.Percentage;
                if (!string.IsNullOrWhiteSpace(e.Step))
                {
                    CurrentStep = e.Step;
                }
            });
        }

        private void ControllerManager_StateChanged(object sender, EventArgs e)
        {
            ControllerState state = _controllerManager.CONTROLLER_STATE;
            if (state == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                IsPowerEnabled = state.isPowerEnabled;
                IsRobotHomed = state.isRobotHomed;
                LinearPosition = state.posLinear;
                RotaryPosition = state.posRotary;
                LinearFlag1 = !state.isLinearIn1;
                LinearFlag2 = !state.isLinearIn2;
                LinearFlag3 = !state.isLinearIn3;
                PressureCommand1 = state.pressureCommand1;
                PressureMeasurement1 = state.pressureMeasurement1;
                PressureCommand2 = state.pressureCommand2;
                PressureMeasurement2 = state.pressureMeasurement2;
                FlowVolume1 = state.flowVolume1;
                FlowError1 = state.flowError1;
                FlowVolume2 = state.flowVolume2;
                FlowError2 = state.flowError2;
                SysPressure = state.systemPressure;
                SafetyControllerState = state.safetyControllerState;
                SafetyErrorState = state.safetyErrorState;
                IsSimulated = state.isSimulated;

                ReadoutsEnabled = !state.parseError && (_controllerManager.CONNECTION_STATE?.isConnected ?? false);
            });
        }

        private void ControllerManager_RobotBusyChanged(object sender, EventArgs e)
        {
            // Raised on the UI thread from SendRobotCommand; marshal defensively in case a
            // future caller offloads robot I/O the way Connect() does for the camera/FTP work.
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(IsRobotControlEnabled));
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void LocalDataManager_LocalDataChanged(object sender, EventArgs e)
        {
            // last_calib is rewritten here when a flow calibration completes (from the
            // Calibrate tab, outside this ViewModel). Marshal to the UI thread and refresh
            // the calibration-expiry warning + dispense gate immediately, rather than
            // waiting for the periodic timer tick to notice the new timestamp.
            Application.Current?.Dispatcher.BeginInvoke(RefreshCalibrationStatus);
        }

        #endregion

        #region Helper Methods

        private void InitializeAppTitle()
        {
            string version = ReleaseInfo.GetCurrentVersion();
            AppTitle = $"{version}";
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

        /// <summary>
        /// Copies the subset of completed-run data shown on the Review Results tab into
        /// the bound properties. The tri-state status and message come from the returned
        /// summary; the detail fields come from the authoritative results record.
        /// </summary>
        private void PopulateResultsDisplay(PartCycleResult result)
        {
            ResultStatus = !result.Success
                ? DispenseResultStatus.Incomplete
                : result.Pass ? DispenseResultStatus.Pass : DispenseResultStatus.Fail;

            ResultMessage = result.Message;

            var data = _resultsManager.currentResults;
            ResultToolSerial = string.IsNullOrWhiteSpace(data?.tool_sn) ? "-" : data.tool_sn;
            ResultRingSerial = string.IsNullOrWhiteSpace(data?.ring_sn) ? "-" : data.ring_sn;


            float idFrac = data?.reference_data?.id_target_vol is float idTarget and not 0f
                ? (data?.shot_data?.id_vol ?? 0f) / idTarget * 100
                : 0f;
            float odFrac = data?.reference_data?.od_target_vol is float odTarget and not 0f
                ? (data?.shot_data?.od_vol ?? 0f) / odTarget * 100
                : 0f;
            ResultDispenseVolumeId = data?.shot_data?.id_vol is float idVol ? $"{idFrac:F1}% ({idVol:0.000})" : "-";
            ResultDispenseVolumeOd = data?.shot_data?.od_vol is float odVol ? $"{odFrac:F1}% ({odVol:0.000})" : "-";

            // Process step statuses
            string folder = _resultsManager.currentResultsFolderPath;
            ResultStepTopPhoto = PhotoSavedStatus(folder, "Top");
            ResultStepSidePhoto = PhotoSavedStatus(folder, "Side");
            ResultStepTopPostPhoto = PhotoSavedStatus(folder, "TopPost");

            ResultStepSerialNumbers = !string.IsNullOrWhiteSpace(data?.tool_sn) && !string.IsNullOrWhiteSpace(data?.ring_sn)
                ? "Detected"
                : "Not Detected";

            ResultStepMagnetPolarity = data?.daq_matlab_results?.result is int polarityResult
                ? (polarityResult == 1 ? "Passed" : "Failed")
                : "-";

            ResultStepMCHeight = data?.height_verification_result?.passed is bool heightPassed
                ? (heightPassed ? "Passed" : "Failed")
                : "-";

            ResultStepDispense = data?.shot_data?.shot_result is bool shotResult
                ? (shotResult ? "Completed" : "Incomplete")
                : "-";

            // Process result detail values
            ResultMaxMCHeight = data?.height_verification_result?.normMaxHeight is double maxHeight
                ? $"{maxHeight:F2} um"
                : "-";
        }

        private static string PhotoSavedStatus(string folder, string fileName)
            => !string.IsNullOrEmpty(folder) && System.IO.File.Exists(System.IO.Path.Combine(folder, fileName + ".jpg"))
                ? "Saved"
                : "Not Saved";

        /// <summary>
        /// Starts a low-frequency timer that re-evaluates the time-based calibration
        /// expiry so the warning surfaces (and the dispense gate updates) without
        /// requiring any operator interaction. DispatcherTimer ticks on the UI thread,
        /// so raising PropertyChanged here is safe.
        /// </summary>
        private void InitializeCalibrationWatch()
        {
            _calibrationWatchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _calibrationWatchTimer.Tick += (_, _) => RefreshCalibrationStatus();
            _calibrationWatchTimer.Start();
        }

        /// <summary>
        /// Re-evaluates the calibration-expiry warning and the dispense command gate.
        /// Call this from the timer, and after any fresh calibration completes, so the
        /// warning clears immediately rather than at the next tick.
        /// </summary>
        public void RefreshCalibrationStatus()
        {
            OnPropertyChanged(nameof(IsCalibrationExpired));
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        /// <summary>
        /// Detaches every manager/service event subscription. Called when the owning
        /// window closes so the ViewModel does not remain rooted by the long-lived
        /// singleton services through their event handlers.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _controllerManager.ControllerConnected -= ControllerManager_Connected;
            _controllerManager.ControllerDisconnected -= ControllerManager_Disconnected;
            _controllerManager.ConnectionLogUpdated -= ControllerManager_ConnectionLogUpdated;
            _controllerManager.StatusLogUpdated -= ControllerManager_StatusLogUpdated;
            _controllerManager.RobotLogUpdated -= ControllerManager_RobotLogUpdated;
            _controllerManager.ControllerStateChanged -= ControllerManager_StateChanged;
            _resultsManager.UpdateProcessLog -= ResultsManager_UpdateProcessLog;
            _partCycleService.ProgressChanged -= DispenseProcessService_ProgressChanged;
            _controllerManager.ControllerStateChanged -= ControllerManager_StateChanged;
            _controllerManager.RobotBusyChanged -= ControllerManager_RobotBusyChanged;
            _localDataManager.LocalDataChanged -= LocalDataManager_LocalDataChanged;

            _calibrationWatchTimer?.Stop();

            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// Display outcome for the Review Results tab. "Incomplete" represents a run that
    /// never reached the pass/fail determination (error, abort, or unexpected stop).
    /// </summary>
    public enum DispenseResultStatus
    {
        None,
        Pass,
        Fail,
        Incomplete
    }
}
