using System;
using System.Windows;
using DDMAutoGUI.Services;
using DDMAutoGUI.CustomWindows;
using Microsoft.Extensions.DependencyInjection;
using DDMAutoGUI.ViewModels;
using System.Diagnostics;

namespace DDMAutoGUI
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                Debug.Print("App starting up");

                var serviceCollection = new ServiceCollection();

                // Core Configuration
                var appConfig = new ApplicationConfiguration(
                    isSimulationMode: false,
                    calibrationPassword: "ddm",
                    advancedSettingsPassword: "ddm");

                serviceCollection.AddSingleton<IApplicationConfiguration>(appConfig);
                serviceCollection.AddSingleton<IConnectionEventService, ConnectionEventService>();

                // Core Services (simple, no circular dependencies)
                serviceCollection.AddSingleton<ISettingsManager, SettingsManager>();
                serviceCollection.AddSingleton<ICameraManager, CameraManager>();
                
                serviceCollection.AddSingleton<ControllerManager>();
                serviceCollection.AddSingleton<IControllerManager>(sp => sp.GetRequiredService<ControllerManager>());
                serviceCollection.AddSingleton<ILightController>(sp => sp.GetRequiredService<ControllerManager>());

                // Data & I/O Services
                serviceCollection.AddSingleton<IResultsManager, ResultsManager>();
                serviceCollection.AddSingleton<ILocalDataManager, LocalDataManager>();
                serviceCollection.AddSingleton<IFlowCalibrationManager, FlowCalibrationManager>();

                // ViewModels & UI
                serviceCollection.AddTransient<MainWindowViewModel>();
                serviceCollection.AddTransient<DispenseProcessService>();
                serviceCollection.AddTransient<ServicePanel>();
                serviceCollection.AddTransient<SettingsPanel>();
                serviceCollection.AddTransient<CalibPositionPanel>();
                serviceCollection.AddTransient<CalibFlowPanel>();
                serviceCollection.AddTransient<CalibHeightPanel>();
                serviceCollection.AddTransient<LocalDataPanel>();
                serviceCollection.AddTransient<MainWindow>();

                Debug.Print("Building service provider...");
                Services = serviceCollection.BuildServiceProvider();
                Debug.Print("Service provider built successfully");

                Debug.Print("Testing individual service resolution...");
                try
                {
                    Debug.Print("Testing IControllerManager...");
                    var cm = Services.GetRequiredService<IControllerManager>();
                    Debug.Print("✓ IControllerManager resolved");

                    Debug.Print("Testing ISettingsManager...");
                    var sm = Services.GetRequiredService<ISettingsManager>();
                    Debug.Print("✓ ISettingsManager resolved");

                    Debug.Print("Testing ICameraManager...");
                    var cam = Services.GetRequiredService<ICameraManager>();
                    Debug.Print("✓ ICameraManager resolved");

                    Debug.Print("Testing IResultsManager...");
                    var rm = Services.GetRequiredService<IResultsManager>();
                    Debug.Print("✓ IResultsManager resolved");

                    Debug.Print("Testing ILocalDataManager...");
                    var ldm = Services.GetRequiredService<ILocalDataManager>();
                    Debug.Print("✓ ILocalDataManager resolved");

                    Debug.Print("Testing IFlowCalibrationManager...");
                    var fcm = Services.GetRequiredService<IFlowCalibrationManager>();
                    Debug.Print("✓ IFlowCalibrationManager resolved");

                    Debug.Print("Testing MainWindowViewModel...");
                    var vm = Services.GetRequiredService<MainWindowViewModel>();
                    Debug.Print("✓ MainWindowViewModel resolved");

                    Debug.Print("All individual services resolved successfully!");
                }
                catch (Exception testEx)
                {
                    Debug.Print($"ERROR during service resolution test: {testEx.Message}");
                    if (testEx.InnerException != null)
                    {
                        Debug.Print($"Inner: {testEx.InnerException.Message}");
                    }
                }

                Debug.Print("Creating main window...");
                var mainWindow = Services.GetRequiredService<MainWindow>();
                Debug.Print("Main window created successfully");

                Debug.Print("Showing main window...");
                mainWindow.Show();
                Debug.Print("Main window shown successfully");
            }
            catch (Exception ex)
            {
                Debug.Print($"FATAL ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.Print($"Inner: {ex.InnerException.Message}");
                }

                MessageBox.Show(
                    $"Failed to start:\n\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Services?.GetService<IControllerManager>()?.Disconnect().Wait();
            base.OnExit(e);
        }
    }
}
