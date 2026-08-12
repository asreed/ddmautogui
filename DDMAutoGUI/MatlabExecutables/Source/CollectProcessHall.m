function CollectProcessHall(arg1, arg2, arg3)
% Main_HallFnct - Acquires Hall sensor data via DAQ, analyzes signal for 
% motor integrity
%
% Inputs:
%   arg1 - Motor size identifier ("ddm_57", "ddm_95", "ddm_116", "ddm_170", 
%          or "ddm_170_tall")
%
%   arg2 - Full path to results directory (e.g. "C:\...\MatlabResults\")
%
%   arg3 - File name for results (e.g. "PolarityResults.json")

% Setup globals
VERSION = "1.0.0";
EC_NON = 0;
EM_NON = "No error";
EC_NOC = -6100;
EM_NOC = "Could not connect to DAQ";
EC_FAI = -6101;
EM_FAI = "Failed to add input channel";
EC_UMS = -6102;
EM_UMS = "Unknown motor size";
EC_DAF = -6103;
EM_DAF = "Data acquisition failed";
EC_DME = -6104;
EM_DME = "Data matrix empty";
EC_INP = -6105;
EM_INP = "Incorrect number of peaks";
EC_POL = -6106;
EM_POL = "Polarity Error";
% ...
EC_UKE = -6199;
EM_UKE = "Unknown error";

% DAQ and Data Process Parameters
sampleRate = 2000;       % Hz
sampleTime = 1.01;        % seconds
cutoffFreq = 100;         % Hz for low-pass filter
filterOrder = 4;
windowSize = 10;          % Number of points to confirm direction change

% Initialize results
RS = -1;
EC = EC_UKE;
EM = EM_UKE;

% Ingest arguments
MOTOR_SIZE = arg1;
RESULTS_PATH = arg2;
RESULTS_NAME = arg3;

% Initialize other result variables
numLongWavelengths = 0;
numShortWavelengths = 0;
numPeaks = 0;
time = 0;
signal = 0;

% Other parameters
savePlotImage = true;



try
    %% Connect to DAQ
    
    % Get list of connected NI devices
    devices = daqlist("ni");
    
    if isempty(devices)
        RS=-1;
        EC=EC_NOC;
        EM=EM_NOC;
        throw;
    end
    
    deviceID = devices.DeviceID{1};  % Use first available device
    dq = daq("ni");
    disp("Device found");

    try
        addinput(dq, deviceID, "ai0", "Voltage");
    catch
        RS = -1;
        EC = EC_FAI;
        EM = EM_FAI;
        throw;
    end
    disp("AI0 added");
    
    dq.Rate = sampleRate;
    disp("Sample rate set");
    
    %% Collect data

    try
        data = read(dq, seconds(sampleTime));
    catch
        RS = -1;
        EC = EC_DAF;
        EM = EM_DAF;
        throw;
    end
    disp("Data collected");
    
    if isempty(data) || isempty(data.Variables)
        RS = -1;
        EC = EC_DME;
        EM = EM_DME;
        throw;
    end
    disp("Data not empty");
      
    
    %% Process Data
    % Motor specs lookup - RINGS MUST SPIN AT 1 RPS
    
    MOTOR_SPECS = struct( ...
       'ddm_57', struct('expectedWavelength', 1/48, 'expectedMagnets', 48), ...
       'ddm_95', struct('expectedWavelength', 1/60, 'expectedMagnets', 60), ...
       'ddm_116', struct('expectedWavelength', 1/80, 'expectedMagnets', 80), ...
       'ddm_170', struct('expectedWavelength', 1/90, 'expectedMagnets', 90), ...
       'ddm_170_tall', struct('expectedWavelength', 1/90, 'expectedMagnets', 90) ...
       );
    
    if ~isfield(MOTOR_SPECS, MOTOR_SIZE)
       RS = -1;
       EC = EC_UMS;
       EM = EM_UMS;
       throw;
    end
    disp("Motor size valid");
    
    expectedWavelength = MOTOR_SPECS.(MOTOR_SIZE).expectedWavelength;
    expectedMagnets = MOTOR_SPECS.(MOTOR_SIZE).expectedMagnets;
    
    time = seconds(data.Time);
    signal = data.Variables;
    
    [b, a] = butter(filterOrder, cutoffFreq / (sampleRate / 2), 'low');
    filteredSignal = filtfilt(b, a, signal);
    
    dy = diff(filteredSignal);
    peakIndices = [];
    valleyIndices = [];
    
    for i = windowSize+1 : length(dy)-windowSize
        if all(dy(i-windowSize:i-1) > 0) && all(dy(i:i+windowSize-1) < 0)
            peakIndices(end+1) = i;
        end
        if all(dy(i-windowSize:i-1) < 0) && all(dy(i:i+windowSize-1) > 0)
            valleyIndices(end+1) = i;
        end
    end
    
    allIndices = sort([peakIndices, valleyIndices]);
    peakTimes = time(allIndices);
    wavelengths = diff(peakTimes);
    numPeaks = length(allIndices);
    
    if numPeaks >= 1.25 * expectedMagnets || numPeaks <= 0.75 * expectedMagnets
        RS=0;
        EC=EC_INP;
        EM=EM_INP; 
        throw;
    end
    
    numLongWavelengths = sum(wavelengths > 1.8 * expectedWavelength);
    numShortWavelengths = sum(wavelengths < 0.65 * expectedWavelength);

    disp("Processing complete");
    
    if numLongWavelengths == 1 && numShortWavelengths == 0
        RS=1;
        EC=EC_NON;
        EM=EM_NON;
        throw;
    else
        RS=0;
        EC=EC_POL;
        EM=EM_POL;
        throw;
    end

catch 

end


%% Publish Results
% Create results JSON file and save to specified directory

results = struct();
results.version = VERSION;
results.result = RS;
results.error_code = EC;
results.error_message = EM;
results.peaks_detected = numPeaks;
results.num_short_wavelengths = numShortWavelengths;
results.num_long_wavelengths = numLongWavelengths;
results.hall_data = [time, signal];

disp("Results:");
disp(results);

if ~exist(RESULTS_PATH, "dir")
    mkdir(RESULTS_PATH)
end
fileID = fopen(fullfile(RESULTS_PATH, RESULTS_NAME), 'w');
fprintf(fileID, jsonencode(results,PrettyPrint=true));
fclose(fileID);

if savePlotImage
    fig = figure('Visible', 'off');
    plot(time, signal);
    xlabel('Time (sec)');
    ylabel('Signal');
    title('Hall Effect Data');
    theme(fig,"light");
    exportgraphics(gca, fullfile(RESULTS_PATH, 'plot.png'), 'Resolution', 300);
    close(fig);
end