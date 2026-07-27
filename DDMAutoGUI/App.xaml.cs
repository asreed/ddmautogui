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
                    displayTitle: "ADS Work Cell Manager",
                    calibrationPassword: "ddm",
                    servicePassword: "ddm",
                    advancedSettingsPassword: "DDM");

                serviceCollection.AddSingleton<IApplicationConfiguration>(appConfig);

                // Core Services
                serviceCollection.AddSingleton<ISettingsService, SettingsService>();
                
                serviceCollection.AddSingleton<ControllerService>();
                serviceCollection.AddSingleton<IControllerService>(sp => sp.GetRequiredService<ControllerService>());
                serviceCollection.AddSingleton<ILightController>(sp => sp.GetRequiredService<ControllerService>());

                // Data & Other Services
                serviceCollection.AddSingleton<ICameraService, CameraService>();
                serviceCollection.AddSingleton<IResultsService, ResultsService>();
                serviceCollection.AddSingleton<ILocalDataService, LocalDataService>();
                serviceCollection.AddSingleton<IDispenseExecutionService, DispenseExecutionService>();
                serviceCollection.AddSingleton<IFlowCalibrationService, FlowCalibrationService>();
                serviceCollection.AddTransient<IPartCycleService, PartCycleService>();

                // ViewModels & UI
                serviceCollection.AddTransient<MainWindowViewModel>();
                serviceCollection.AddTransient<ServicePanel>();
                serviceCollection.AddTransient<SettingsPanel>();
                serviceCollection.AddTransient<CalibPositionPanel>();
                serviceCollection.AddTransient<CalibFlowPanel>();
                serviceCollection.AddTransient<LocalDataPanel>();
                serviceCollection.AddTransient<MainWindow>();

                Debug.Print("Building service provider...");
                Services = serviceCollection.BuildServiceProvider();
                Debug.Print("Service provider built successfully");

                Debug.Print("Testing individual service resolution...");
                try
                {
                    var cs = Services.GetRequiredService<IControllerService>();
                    var ss = Services.GetRequiredService<ISettingsService>();
                    var cas = Services.GetRequiredService<ICameraService>();
                    var rs = Services.GetRequiredService<IResultsService>();
                    var lds = Services.GetRequiredService<ILocalDataService>();
                    var des = Services.GetRequiredService<IDispenseExecutionService>();
                    var fcs = Services.GetRequiredService<IFlowCalibrationService>();
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
                mainWindow.Closed += (s, args) => Shutdown();
                this.MainWindow = mainWindow;
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
            var controllerService = Services?.GetService<IControllerService>();
            if (controllerService != null)
            {
                try
                {
                    controllerService.Disconnect()
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    Debug.Print($"Error during shutdown disconnect: {ex.Message}");
                }
            }

            var cameraService = Services?.GetService<ICameraService>();
            if (cameraService != null)
            {
                try
                {
                    cameraService.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.Print($"Error disposing camera manager: {ex.Message}");
                }
            }

            base.OnExit(e);
        }
    }
}
