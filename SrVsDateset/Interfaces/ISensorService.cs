using System;
using System.Threading.Tasks;
using SrVsDataset.Models;

namespace SrVsDataset.Interfaces
{
    public interface ISensorService : IDisposable
    {
        bool IsConnected { get; }
        SensorData CurrentSensorData { get; }
        
        event EventHandler<SensorData> SensorDataUpdated;
        
        Task<bool> ConnectAsync(string portName, int baudRate);
        Task DisconnectAsync();
        Task SendCommandAsync(string command);
    }
}