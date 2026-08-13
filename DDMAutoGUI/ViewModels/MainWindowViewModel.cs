using DDMAutoGUI.Constants;
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
        private readonly IControllerService _controllerService;
        private readonly ISettingsService _settingsService;
        private readonly IResultsService _resultsService;
        private readonly ICameraService _cameraService;
        private readonly ILocalDataService _localDataService;
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

        private int _selectedFlowCalibSizeIndex;
        private string _selectedFlowCalibMotorType;

        private string _connectedTcsVersion = "-";
        private string _connectedPacVersion = "-";

        public MainWindowViewModel(
            IControllerService controllerService,
            ISettingsService settingsService,
            IResultsService resultsService,
            ICameraService cameraService,
            ILocalDataService localDataService,
            IApplicationConfiguration appConfig,
            IPartCycleService dispenseProcessService)
        {
            _controllerService = controllerService ?? throw new ArgumentNullException(nameof(controllerService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _resultsService = resultsService ?? throw new ArgumentNullException(nameof(resultsService));
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _localDataService = localDataService ?? throw new ArgumentNullException(nameof(localDataService));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _partCycleService = dispenseProcessService ?? throw new ArgumentNullException(nameof(dispenseProcessService));

            InitializeCommands();
            InitializeEventHandlers();
            InitializeAppTitle();
            InitializeMotorSizes();
            InitializeCalibrationWatch();

            _controllerIpAddress = _appConfig.DefaultControllerIPAddress;
            _connectionStatus = ConnectionStatusText.NotConnected;
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
                    OnPropertyChanged(nameof(IsCalibrationMismatched));
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
        public bool IsRobotControlEnabled => IsConnected && !_controllerService.IsRobotBusy;

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
                    OnPropertyChanged(nameof(IsCalibrationMismatched));
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

        /// <summary>The motor name (e.g. "ddm_116") the Calibrate Flow panel operates on.</summary>
        public string SelectedFlowCalibMotorType
        {
            get => _selectedFlowCalibMotorType;
            set => SetProperty(ref _selectedFlowCalibMotorType, value);
        }

        /// <summary>
        /// Motor size selected on the Calibrate Flow panel. Kept separate from
        /// <see cref="SelectedMotorSizeIndex"/> (the Operate tab's selection) so the two
        /// workflows don't drive each other.
        /// </summary>
        public int SelectedFlowCalibSizeIndex
        {
            get => _selectedFlowCalibSizeIndex;
            set
            {
                if (SetProperty(ref _selectedFlowCalibSizeIndex, value))
                {
                    SelectedFlowCalibMotorType = (MotorSizes != null && value >= 0 && value < MotorSizes.Count)
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

        private bool _dispOverridePCLocks;

        /// <summary>
        /// Dev/debug option. When enabled, bypasses the serial-number and calibration
        /// gates in <see cref="CanExecutePartCycle"/> so a part cycle can be started for
        /// testing without a scanned SN or valid/matching flow calibration. The
        /// connection and "already running" gates are always enforced.
        /// </summary>
        public bool DispOverridePCLocks
        {
            get => _dispOverridePCLocks;
            set
            {
                if (SetProperty(ref _dispOverridePCLocks, value))
                {
                    Application.Current?.Dispatcher.BeginInvoke(
                        CommandManager.InvalidateRequerySuggested);
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

                float? expHours = _settingsService.GetAllSettings()?.dispense_system?.calib_exp_hours;
                if (expHours == null)
                    return false;

                DateTime? lastCalib = _localDataService.GetLocalData()?.calib_data?.last_calib;
                if (lastCalib == null)
                    return true;

                return (DateTime.Now - lastCalib.Value).TotalHours > expHours.Value;
            }
        }

        /// <summary>
        /// True when the most recent flow calibration was performed for the currently
        /// selected motor type. Contributes to the dispense gate so an operator can't
        /// dispense against a calibration captured for a different size. Returns false
        /// while disconnected, since calibration/settings are only known when connected.
        /// </summary>
        public bool IsCalibrationMismatched
        {
            get
            {
                if (!IsConnected)
                    return false;

                string? lastSize = _localDataService.GetLocalData()?.calib_data?.last_size;
                return !string.IsNullOrEmpty(lastSize)
                    && !lastSize.Equals(SelectedMotorType, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>TCS firmware version reported by the controller, or "-" when unknown.</summary>
        public string ConnectedTcsVersion
        {
            get => _connectedTcsVersion;
            set => SetProperty(ref _connectedTcsVersion, value);
        }

        /// <summary>PAC firmware version reported by the controller, or "-" when unknown.</summary>
        public string ConnectedPacVersion
        {
            get => _connectedPacVersion;
            set => SetProperty(ref _connectedPacVersion, value);
        }

        /// <summary>
        /// Path to the results server share (UNC preferred over a mapped drive letter).
        /// Write-through to AdvancedOptions.ResultsStorageOptions.ServerPath.
        /// </summary>
        public string ServerPathText
        {
            get => _appConfig.AdvancedOptions.ResultsStorageOptions.ServerPath;
            set
            {
                if (_appConfig.AdvancedOptions.ResultsStorageOptions.ServerPath != value)
                {
                    _appConfig.AdvancedOptions.ResultsStorageOptions.ServerPath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Whether the connect routine verifies reachability of the results server share.</summary>
        public bool DispVerifyServer
        {
            get => _appConfig.AdvancedOptions.ResultsStorageOptions.VerifyServerOnConnect;
            set
            {
                if (_appConfig.AdvancedOptions.ResultsStorageOptions.VerifyServerOnConnect != value)
                {
                    _appConfig.AdvancedOptions.ResultsStorageOptions.VerifyServerOnConnect = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>When true, results are written locally only and never copied to the server (dev/debug).</summary>
        public bool DispSaveLocalOnly
        {
            get => _appConfig.AdvancedOptions.ResultsStorageOptions.SaveLocalOnly;
            set
            {
                if (_appConfig.AdvancedOptions.ResultsStorageOptions.SaveLocalOnly != value)
                {
                    _appConfig.AdvancedOptions.ResultsStorageOptions.SaveLocalOnly = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }
        public ICommand StartDispenseCommand { get; private set; }
        public ICommand ViewResultsCommand { get; private set; }
        public ICommand OpenResultsDirectoryCommand { get; private set; }
        public ICommand AcquireTopCommand { get; private set; }
        public ICommand AcquireSideCommand { get; private set; }
        public ICommand OpenResultsFolderCommand { get; private set; }

        private void InitializeCommands()
        {
            ConnectCommand = new AsyncRelayCommand<string>(ExecuteConnect, parameter => CanConnect(parameter));
            DisconnectCommand = new AsyncRelayCommand(ExecuteDisconnect, parameter => CanDisconnect(parameter));
            StartDispenseCommand = new AsyncRelayCommand(ExecutePartCycle, parameter => CanExecutePartCycle(parameter));
            ViewResultsCommand = new RelayCommand(ExecuteViewResults);
            OpenResultsDirectoryCommand = new RelayCommand(ExecuteOpenResultsDirectory);
            AcquireTopCommand = new AsyncRelayCommand(ExecuteAcquireTop, CanAcquireImage);
            AcquireSideCommand = new AsyncRelayCommand(ExecuteAcquireSide, CanAcquireImage);
            OpenResultsFolderCommand = new RelayCommand(_ => ExecuteOpenResultsFolder());
        }

        #endregion

        #region Command Execution

        private async Task ExecuteConnect(string ipAddress)
        {
            try
            {
                IsProcessing = true;
                ConnectionStatus = ConnectionStatusText.Connecting;

                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = ControllerIpAddress;
                }

                bool connected = await _controllerService.Connect(ipAddress);
                IsConnected = connected;
                ConnectionStatus = connected
                    ? ConnectionStatusText.ConnectedTo(ipAddress)
                    : ConnectionStatusText.ConnectionFailed;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionStatusText.Error(ex.Message);
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
                await _controllerService.Disconnect();
                IsConnected = false;
                ConnectionStatus = ConnectionStatusText.Disconnected;
            }
            catch (Exception ex)
            {
                ConnectionStatus = ConnectionStatusText.Error(ex.Message);
                Debug.Print($"Disconnect error: {ex}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanDisconnect(object parameter) 
            => IsConnected && !IsProcessing;

        private async Task ExecutePartCycle(object parameter)
        {
            try
            {
                IsProcessing = true;
                IsDispenseProcessRunning = true;
                ProcessProgress = 0;
                CurrentStep = "Starting part cycle process...";

                // Bring the operator to the live progress/log view as the run begins.
                SelectedDispenseTabIndex = MonitorProcessTabIndex;

                // Use _appConfig.AdvancedOptions
                var result = await _partCycleService.ExecutePartCycleAsync(
                    SelectedMotorType,
                    MotorSerialNumber,
                    _appConfig.AdvancedOptions);

                //if (result.Success)
                //{
                //    if (result.Pass) {
                //        //
                //    }
                //    else
                //    {
                //        //
                //    }
                //}
                //else
                //{
                //    //
                //}

                // Fill the Review Results tab and bring the operator to it, regardless of
                // outcome, so pass/fail/incomplete is always visible.
                PopulateResultsDisplay(result);
                SelectedDispenseTabIndex = ReviewResultsTabIndex;
            }
            catch (Exception ex)
            {
                Debug.Print($"Dispense process error: {ex}");
                ResultStatus = DispenseResultStatus.Incomplete;
                ResultMessage = $"Run failed: {ex.Message}";
            }
            finally
            {
                SelectedDispenseTabIndex = ReviewResultsTabIndex;
                IsProcessing = false;
                IsDispenseProcessRunning = false;
            }
        }

        private bool CanExecutePartCycle(object parameter)
        {
            // Must be connected to controller (always enforced)
            if (!IsConnected)
                return false;

            // Cannot start while another process is running (always enforced)
            if (IsProcessing)
                return false;

            // Motor type must be selected (always enforced)
            if (string.IsNullOrEmpty(SelectedMotorType))
                return false;

            // Dev/debug override bypasses the operator-facing safety gates below.
            if (DispOverridePCLocks)
                return true;

            // Motor serial number is required
            if (IsSerialNumberMissing)
                return false;

            // Calibration must be present and within the configured expiry window
            if (IsCalibrationExpired)
                return false;

            // Calibration must match the selected motor type
            if (IsCalibrationMismatched)
                return false;

            return true;
        }

        private void ExecuteCancelDispense(object parameter)
        {
            IsProcessing = false;
            IsDispenseProcessRunning = false;
            ProcessLog += "\n\n=== PART CYCLE CANCELLED BY USER ===";
        }

        private bool CanCancelDispense(object parameter) 
            => IsDispenseProcessRunning;

        private void ExecuteViewResults(object parameter)
        {
            string resultsJson = _resultsService.GetCurrentResultsAsString();
            if (!string.IsNullOrEmpty(resultsJson))
            {
                Debug.Print(resultsJson);
            }
        }

        private void ExecuteOpenResultsDirectory(object parameter)
        {
            _resultsService.OpenBrowserToDirectory();
        }

        private async Task ExecuteAcquireTop(object parameter)
        {
            await ExecuteAcquireCamera(CameraService.CellCamera.top, "Top image acquired");
        }

        private async Task ExecuteAcquireSide(object parameter)
        {
            await ExecuteAcquireCamera(CameraService.CellCamera.side, "Side image acquired");
        }

        private async Task ExecuteAcquireCamera(CameraService.CellCamera camera, string successMessage)
        {
            try
            {
                IsProcessing = true;
                CameraStatus = "Acquiring image...";
                AcquiredImageSource = null;

                CameraAcquisitionResult result = await _cameraService.AcquireAndSave(camera, null);

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
            _resultsService.OpenBrowserToDirectory();
        }

        private bool CanAcquireImage(object parameter) => IsConnected && !IsProcessing;

        #endregion

        #region Event Handlers

        private void InitializeEventHandlers()
        {
            _controllerService.ControllerConnected += ControllerService_Connected;
            _controllerService.ControllerDisconnected += ControllerService_Disconnected;
            _controllerService.ConnectionLogUpdated += ControllerService_ConnectionLogUpdated;
            _controllerService.StatusLogUpdated += ControllerService_StatusLogUpdated;
            _controllerService.RobotLogUpdated += ControllerService_RobotLogUpdated;
            _resultsService.UpdateProcessLog += ResultsService_UpdateProcessLog;
            _controllerService.ControllerStateChanged += ControllerService_StateChanged;

            // Wire dispense progress reporting to the bound ProcessProgress property.
            // This subscription was lost in the MVVM/DI refactor, which is why the
            // Disp_ProcessPrg bar stopped advancing during a run.
            _partCycleService.ProgressChanged += DispenseProcessService_ProgressChanged;
            _controllerService.RobotBusyChanged += ControllerService_RobotBusyChanged;

            _localDataService.LocalDataChanged += LocalDataService_LocalDataChanged;
        }

        private void ControllerService_Connected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = true;
                ConnectionStatus = ConnectionStatusText.ConnectedTo(_controllerService.CONNECTION_STATE?.connectedIP);
                ConnectedTcsVersion = _controllerService.CONNECTION_STATE?.connectedTCS ?? "-";
                ConnectedPacVersion = _controllerService.CONNECTION_STATE?.connectedPAC ?? "-";
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void ControllerService_Disconnected(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = false;
                ReadoutsEnabled = false;
                ConnectionStatus = ConnectionStatusText.Disconnected;
                ConnectedTcsVersion = "-";
                ConnectedPacVersion = "-";
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void ControllerService_ConnectionLogUpdated(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                ConnectionLog = _controllerService.GetConnectionLog());
        }

        private void ControllerService_StatusLogUpdated(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                StatusLog = _controllerService.GetStatusLog());
        }

        private void ControllerService_RobotLogUpdated(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                RobotLog = _controllerService.GetRobotLog());
        }

        private void ResultsService_UpdateProcessLog(object sender, EventArgs e)
        {
            ProcessLog = _resultsService.GetLogAsString();
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

        private void ControllerService_StateChanged(object sender, EventArgs e)
        {
            ControllerState state = _controllerService.CONTROLLER_STATE;
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

                ReadoutsEnabled = !state.parseError && (_controllerService.CONNECTION_STATE?.isConnected ?? false);
            });
        }

        private void ControllerService_RobotBusyChanged(object sender, EventArgs e)
        {
            // Raised on the UI thread from SendRobotCommand; marshal defensively in case a
            // future caller offloads robot I/O the way Connect() does for the camera/FTP work.
            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(IsRobotControlEnabled));
                CommandManager.InvalidateRequerySuggested();
            });
        }

        private void LocalDataService_LocalDataChanged(object sender, EventArgs e)
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
            AppTitle = $"{_appConfig.DisplayTitle} {version}";
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
            SelectedFlowCalibSizeIndex = 2; // Default to ddm_116
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

            var data = _resultsService.currentResults;
            ResultToolSerial = string.IsNullOrWhiteSpace(data?.tool_sn_detected) ? "-" : data.tool_sn_detected;
            ResultRingSerial = string.IsNullOrWhiteSpace(data?.ring_sn_detected) ? "-" : data.ring_sn_detected;


            float idFrac = data?.reference_data?.id_target_vol is float idTarget and not 0f
                ? (data?.shot_data?.id_vol ?? 0f) / idTarget * 100
                : 0f;
            float odFrac = data?.reference_data?.od_target_vol is float odTarget and not 0f
                ? (data?.shot_data?.od_vol ?? 0f) / odTarget * 100
                : 0f;
            ResultDispenseVolumeId = data?.shot_data?.id_vol is float idVol ? $"{idFrac:F1}% ({idVol:0.000})" : "-";
            ResultDispenseVolumeOd = data?.shot_data?.od_vol is float odVol ? $"{odFrac:F1}% ({odVol:0.000})" : "-";

            // Process step statuses
            string folder = _resultsService.currentResultsFolderPath;
            ResultStepTopPhoto = PhotoSavedStatus(folder, "Top");
            ResultStepSidePhoto = PhotoSavedStatus(folder, "Side");
            ResultStepTopPostPhoto = PhotoSavedStatus(folder, "TopPost");

            ResultStepSerialNumbers = !string.IsNullOrWhiteSpace(data?.tool_sn_detected) && !string.IsNullOrWhiteSpace(data?.ring_sn_detected)
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
            OnPropertyChanged(nameof(IsCalibrationMismatched));
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

            _controllerService.ControllerConnected -= ControllerService_Connected;
            _controllerService.ControllerDisconnected -= ControllerService_Disconnected;
            _controllerService.ConnectionLogUpdated -= ControllerService_ConnectionLogUpdated;
            _controllerService.StatusLogUpdated -= ControllerService_StatusLogUpdated;
            _controllerService.RobotLogUpdated -= ControllerService_RobotLogUpdated;
            _controllerService.ControllerStateChanged -= ControllerService_StateChanged;
            _resultsService.UpdateProcessLog -= ResultsService_UpdateProcessLog;
            _partCycleService.ProgressChanged -= DispenseProcessService_ProgressChanged;
            _controllerService.RobotBusyChanged -= ControllerService_RobotBusyChanged;
            _localDataService.LocalDataChanged -= LocalDataService_LocalDataChanged;

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
