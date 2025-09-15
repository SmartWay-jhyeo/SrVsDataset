using System;
using System.Threading.Tasks;
using SrVsDataset.Models;

namespace SrVsDataset.Interfaces
{
    public interface IGpsService : IDisposable
    {
        event EventHandler<GpsPoint> LocationUpdated;
        
        bool IsConnected { get; }
        GpsPoint CurrentLocation { get; }
        
        Task<bool> ConnectAsync(string portName, int baudRate);
        Task DisconnectAsync();
        Task<GpsPoint> GetCurrentLocationAsync();
    }
}