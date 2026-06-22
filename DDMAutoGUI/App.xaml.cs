using System.Diagnostics;
using System.Windows;
using DDMAutoGUI.Services;
using DDMAutoGUI.CustomWindows;
using Microsoft.Extensions.DependencyInjection;
using DDMAutoGUI.ViewModels;

namespace DDMAutoGUI
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Debug.Print("App starting up");

            // Configure services
            var serviceCollection = new ServiceCollection();

            // === Core Configuration ===
            var appConfig = new ApplicationConfiguration(
                isSimulationMode: false,
                calibrationPassword: "ddm",
                advancedSettingsPassword: "ddm");

            serviceCollection.AddSingleton<IApplicationConfiguration>(appConfig);

            // === Core Services (with clear dependency order) ===
            serviceCollection.AddSingleton<IControllerManager, ControllerManager>();
            serviceCollection.AddSingleton<ISettingsManager, SettingsManager>();
            serviceCollection.AddSingleton<ICameraManager, CameraManager>();

            // === Data & I/O Services ===
            serviceCollection.AddSingleton<IResultsManager, ResultsManager>();
            serviceCollection.AddSingleton<ILocalDataManager, LocalDataManager>();
            serviceCollection.AddSingleton<IFlowCalibrationManager, FlowCalibrationManager>();

            // === ViewModels & UI Components ===
            serviceCollection.AddTransient<MainWindowViewModel>();
            serviceCollection.AddTransient<DispenseProcessService>();

            serviceCollection.AddTransient<ServicePanel>();
            serviceCollection.AddTransient<SettingsPanel>();
            serviceCollection.AddTransient<CalibPositionPanel>();
            serviceCollection.AddTransient<CalibFlowPanel>();
            serviceCollection.AddTransient<CalibHeightPanel>();
            serviceCollection.AddTransient<LocalDataPanel>();

            serviceCollection.AddTransient<MainWindow>();

            // Build the service provider
            Services = serviceCollection.BuildServiceProvider();

            // === Manual Orchestration: Wire up circular dependency ===
            // SettingsManager needs ControllerManager, but ControllerManager depends on CameraManager,
            // which depends on SettingsManager. We resolve this by injecting the controller manager
            // after the DI container is built.
            var settingsManager = Services.GetRequiredService<ISettingsManager>();
            var controllerManager = Services.GetRequiredService<IControllerManager>();
            ((SettingsManager)settingsManager).SetControllerManager(controllerManager);

            Debug.Print("Dependency injection configured");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Services?.GetService<IControllerManager>()?.Disconnect().Wait();
            base.OnExit(e);
        }
    }
}
