using System;
using System.Threading.Tasks;
using SrVsDataset.Models;

namespace SrVsDataset.Interfaces
{
    public interface ICameraService : IDisposable
    {
        event EventHandler<(byte[] data, int width, int height, bool isColor)> FrameReceived;
        event EventHandler<double> TemperatureChanged;
        
        bool IsConnected { get; }
        CameraSettings CurrentSettings { get; }
        
        Task<bool> InitializeAsync();
        Task<bool> StartPreviewAsync();
        Task<bool> StopPreviewAsync();
        
        Task<bool> SetExposureTimeAsync(int microseconds);
        Task<bool> SetExposureModeAsync(ExposureMode mode);
        Task<bool> SetWhiteBalanceAsync(WhiteBalanceMode mode, int temperature);
        Task<bool> DisconnectAsync();
        Task<CameraSettings> GetCurrentSettingsAsync();
        Task<double> GetTemperatureAsync();
        Task TriggerSoftwareCaptureAsync();
        
        byte[] GetCurrentFrame();
    }
}