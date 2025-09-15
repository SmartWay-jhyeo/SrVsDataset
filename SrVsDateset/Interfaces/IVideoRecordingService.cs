using System;
using System.Threading.Tasks;
using SrVsDataset.Models;

namespace SrVsDataset.Interfaces
{
    public interface IVideoRecordingService : IDisposable
    {
        event EventHandler<RecordingSession> RecordingStarted;
        event EventHandler<RecordingSession> RecordingStopped;
        event EventHandler<long> FileSizeUpdated;
        
        bool IsRecording { get; }
        RecordingSession CurrentSession { get; }
        
        Task<bool> StartRecordingAsync(string filePath, int frameRate = 30);
        Task<bool> StartRecordingAsync(string filePath, int width, int height, int frameRate = 30);
        Task<RecordingSession> StopRecordingAsync();
        void AddFrame(byte[] frameData);
    }
}