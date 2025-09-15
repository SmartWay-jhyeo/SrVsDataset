using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SrVsDataset.Interfaces;
using SrVsDataset.Services;
using SrVsDataset.ViewModels;

namespace SrVsDataset
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ServiceProvider _serviceProvider;
        private IConfiguration _configuration;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Configure services
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // Configure logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs/srvsdataset-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Application starting...");

            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Load configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Register configuration
            services.AddSingleton(_configuration);

            // Register services
            // Use MVCameraService on Windows, MockCameraService otherwise
#if WINDOWS
            // TODO: Uncomment when MVSDK is available
            // services.AddSingleton<ICameraService, MVCameraService>();
            services.AddSingleton<ICameraService, MVCameraService>();
#else
            services.AddSingleton<ICameraService, MockCameraService>();
#endif
            services.AddSingleton<IVideoRecordingService, VideoRecordingService>();
            services.AddSingleton<IFileManagementService>(provider =>
                new FileManagementService(_configuration["DatasetSettings:RootPath"] ?? "D:\\LANE_REPAINT_DATASET"));
            services.AddSingleton<IGpsService, GpsService>();
            services.AddSingleton<ISensorService>(provider =>
            {
                var logger = new LoggingService();
                var gpsService = provider.GetRequiredService<IGpsService>();
                return new SensorService(logger, gpsService);
            });

            // Register ViewModels
            services.AddTransient<MainViewModel>();

            // Register Views
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shutting down...");
            Log.CloseAndFlush();
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
