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

                // Core Services (simple, no circular dependencies)
                serviceCollection.AddSingleton<ISettingsManager, SettingsManager>();
                serviceCollection.AddSingleton<ICameraManager, CameraManager>();
                
                // ControllerManager implements both IControllerManager and ILightController
                // This allows CameraManager to depend only on ILightController, breaking circular dependency
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
                    var cm = Services.GetRequiredService<IControllerManager>();
                    var sm = Services.GetRequiredService<ISettingsManager>();
                    var cam = Services.GetRequiredService<ICameraManager>();
                    var rm = Services.GetRequiredService<IResultsManager>();
                    var ldm = Services.GetRequiredService<ILocalDataManager>();
                    var fcm = Services.GetRequiredService<IFlowCalibrationManager>();
                    var vm = Services.GetRequiredService<MainWindowViewModel>();
                }
                catch (Exception testEx)
                {
                    Debug.Print($"ERROR during service resolution test: {testEx.Message}");
                    if (testEx.InnerException != null)
                    {
                        Debug.Print($"Inner: {testEx.InnerException.Message}");
                    }
                }

                var mainWindow = Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
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
            // Clean up: Disconnect from controller on application exit
            var controllerManager = Services?.GetService<IControllerManager>();
            if (controllerManager != null)
            {
                try
                {
                    controllerManager.Disconnect().Wait();
                }
                catch (Exception ex)
                {
                    Debug.Print($"Error during shutdown disconnect: {ex.Message}");
                }
            }
            
            base.OnExit(e);
        }
    }
}
