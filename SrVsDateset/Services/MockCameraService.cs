using MVSDK;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SrVsDataset.Services
{
    /// <summary>
    /// Mock camera service for testing and cross-platform compatibility
    /// </summary>
    public class MockCameraService : ICameraService
    {
        private readonly CameraSettings _currentSettings;
        private readonly Random _random = new();
        private bool _isConnected = false;

        public event EventHandler<(byte[] data, int width, int height, bool isColor)> FrameReceived;
        public event EventHandler<double> TemperatureChanged;

        public bool IsConnected => _isConnected;
        public CameraSettings CurrentSettings => _currentSettings;

        public MockCameraService()
        {
            _currentSettings = new CameraSettings();
        }

        public async Task<bool> InitializeAsync()
        {
            await Task.Delay(500); // Simulate initialization delay
            _isConnected = true;
            return true;
        }

        public async Task<bool> StartPreviewAsync()
        {
            if (!IsConnected) return false;
            
            await Task.Delay(100);
            
            // Simulate frame generation
            _ = Task.Run(async () =>
            {
                while (IsConnected)
                {
                    var mockFrame = new byte[1920 * 1080 * 3]; // Mock RGB frame
                    FrameReceived?.Invoke(this, (mockFrame, 1920, 1080, true));
                    
                    // Simulate temperature changes
                    var temperature = 42.0 + _random.NextDouble() * 8.0; // 42-50°C
                    TemperatureChanged?.Invoke(this, temperature);
                    
                    await Task.Delay(33); // ~30 FPS
                }
            });
            
            return true;
        }

        public async Task TriggerSoftwareCaptureAsync()
        {
            await Task.Delay(50);
            return;
        }

        public async Task<bool> StopPreviewAsync()
        {
            await Task.Delay(100);
            return true;
        }

        public async Task<bool> SetExposureTimeAsync(int microseconds)
        {
            await Task.Delay(50);
            _currentSettings.ExposureTime = microseconds;
            _currentSettings.ExposureMode = ExposureMode.Manual;
            return true;
        }

        public async Task<bool> SetExposureModeAsync(ExposureMode mode)
        {
            await Task.Delay(50);
            _currentSettings.ExposureMode = mode;
            return true;
        }

        public async Task<bool> SetWhiteBalanceAsync(WhiteBalanceMode mode, int temperature)
        {
            await Task.Delay(50);
            _currentSettings.WhiteBalanceMode = mode;
            _currentSettings.WhiteBalanceTemperature = temperature;
            return true;
        }

        public async Task<bool> DisconnectAsync()
        {
            await Task.Delay(100);
            _isConnected = false;
            return true;
        }

        public async Task<CameraSettings> GetCurrentSettingsAsync()
        {
            await Task.Delay(10);
            return _currentSettings;
        }

        public async Task<double> GetTemperatureAsync()
        {
            await Task.Delay(10);
            return 45.0 + _random.NextDouble() * 5.0; // Mock temperature
        }

        public byte[] GetCurrentFrame()
        {
            return new byte[1920 * 1080 * 3]; // Mock frame data
        }

        public void Dispose()
        {
            _isConnected = false;
        }
    }
}