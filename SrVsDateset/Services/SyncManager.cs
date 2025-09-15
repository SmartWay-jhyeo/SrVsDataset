using System;
using System.Threading;
using System.Threading.Tasks;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;

namespace SrVsDataset.Services
{
    public class SyncManager : IDisposable
    {
        private readonly ICameraService _cameraService;
        private readonly ISensorService _sensorService;
        private readonly ILoggingService _logger;
        
        private System.Windows.Forms.Timer _syncTimer;
        private volatile bool _isRunning;
        private long _sequenceNumber = 0;
        private readonly double _targetIntervalMs = 33.333; // 30 Hz = 33.333ms
        
        // Sync statistics
        private readonly object _statsLock = new object();
        private double _totalCameraLatency = 0;
        private double _totalSensorLatency = 0;
        private int _totalSyncPoints = 0;
        private int _successfulSyncPoints = 0;
        private double _maxSyncError = 0;
        
        public event EventHandler<SyncTriggerEventArgs> SyncTriggered;
        
        public bool IsRunning => _isRunning;
        public long CurrentSequence => _sequenceNumber;
        
        public SyncManager(ICameraService cameraService, ISensorService sensorService, ILoggingService logger = null)
        {
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
            _logger = logger ?? new LoggingService();
        }
        
        public void Start()
        {
            if (_isRunning)
                return;
                
            _isRunning = true;
            _sequenceNumber = 0;
            
            // Reset statistics
            lock (_statsLock)
            {
                _totalCameraLatency = 0;
                _totalSensorLatency = 0;
                _totalSyncPoints = 0;
                _successfulSyncPoints = 0;
                _maxSyncError = 0;
            }
            
            // Create timer with high precision
            _syncTimer = new System.Windows.Forms.Timer();
            _syncTimer.Interval = (int)_targetIntervalMs;
            _syncTimer.Tick += OnSyncTick;
            _syncTimer.Start();
            
            _logger.LogInfo($"SyncManager started at {_targetIntervalMs:F3}ms intervals (30 Hz)");
        }
        
        public void Stop()
        {
            if (!_isRunning)
                return;
                
            _isRunning = false;
            
            _syncTimer?.Dispose();
            _syncTimer = null;
            
            _logger.LogInfo($"SyncManager stopped. Total sync points: {_totalSyncPoints}, Success rate: {GetSyncSuccessRate():F2}%");
        }
        
        private async void OnSyncTick(object sender, EventArgs e)
        {
            if (!_isRunning)
                return;
                
            var startTime = TimestampManager.GetPreciseTimestamp();
            var currentSequence = Interlocked.Increment(ref _sequenceNumber);
            
            try
            {
                // Trigger camera capture (software trigger)
                var cameraTask = TriggerCameraAsync(currentSequence, startTime);
                
                // Request sensor data with sequence number
                var sensorTask = RequestSensorDataAsync(currentSequence, startTime);
                
                // Wait for both operations
                await Task.WhenAll(cameraTask, sensorTask);
                
                // Update statistics
                UpdateSyncStatistics(cameraTask.Result, sensorTask.Result);
                
                // Fire sync trigger event
                SyncTriggered?.Invoke(this, new SyncTriggerEventArgs
                {
                    SequenceNumber = currentSequence,
                    Timestamp = startTime,
                    CameraLatencyMs = cameraTask.Result,
                    SensorLatencyMs = sensorTask.Result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Sync tick error (seq: {currentSequence}): {ex.Message}");
            }
        }
        
        private async Task<double> TriggerCameraAsync(long sequence, DateTime timestamp)
        {
            try
            {
                var triggerStart = TimestampManager.GetPreciseTimestamp();
                
                // Trigger camera capture (implementation depends on camera service)
                if (_cameraService is MVCameraService mvCamera)
                {
                    await mvCamera.TriggerSoftwareCaptureAsync();
                }
                
                var triggerEnd = TimestampManager.GetPreciseTimestamp();
                var latency = (triggerEnd - triggerStart).TotalMilliseconds;
                
                _logger.LogDebug($"Camera trigger seq {sequence}: {latency:F2}ms");
                return latency;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Camera trigger error (seq: {sequence}): {ex.Message}");
                return -1; // Error indicator
            }
        }
        
        private async Task<double> RequestSensorDataAsync(long sequence, DateTime timestamp)
        {
            try
            {
                var requestStart = TimestampManager.GetPreciseTimestamp();
                
                // Send GET command to Arduino with sequence number
                if (_sensorService.IsConnected)
                {
                    // Format: GET,{sequence},{timestamp_ticks}
                    var command = $"GET,{sequence},{timestamp.Ticks}";
                    await _sensorService.SendCommandAsync(command);
                }
                
                var requestEnd = TimestampManager.GetPreciseTimestamp();
                var latency = (requestEnd - requestStart).TotalMilliseconds;
                
                _logger.LogDebug($"Sensor request seq {sequence}: {latency:F2}ms");
                return latency;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Sensor request error (seq: {sequence}): {ex.Message}");
                return -1; // Error indicator
            }
        }
        
        private void UpdateSyncStatistics(double cameraLatency, double sensorLatency)
        {
            lock (_statsLock)
            {
                _totalSyncPoints++;
                
                if (cameraLatency >= 0 && sensorLatency >= 0)
                {
                    _totalCameraLatency += cameraLatency;
                    _totalSensorLatency += sensorLatency;
                    _successfulSyncPoints++;
                    
                    // Calculate sync error (difference between camera and sensor timing)
                    var syncError = Math.Abs(cameraLatency - sensorLatency);
                    if (syncError > _maxSyncError)
                    {
                        _maxSyncError = syncError;
                    }
                }
            }
        }
        
        public SyncQualityMetrics GetQualityMetrics()
        {
            lock (_statsLock)
            {
                if (_successfulSyncPoints == 0)
                {
                    return new SyncQualityMetrics
                    {
                        SyncQualityScore = "poor"
                    };
                }
                
                var avgCameraLatency = _totalCameraLatency / _successfulSyncPoints;
                var avgSensorLatency = _totalSensorLatency / _successfulSyncPoints;
                var successRate = (double)_successfulSyncPoints / _totalSyncPoints;
                
                string qualityScore = "excellent";
                if (_maxSyncError > 5.0 || successRate < 0.95)
                    qualityScore = "poor";
                else if (_maxSyncError > 3.0 || successRate < 0.98)
                    qualityScore = "good";
                
                return new SyncQualityMetrics
                {
                    AverageCameraLatencyMs = avgCameraLatency,
                    AverageSensorLatencyMs = avgSensorLatency,
                    MaxSyncErrorMs = _maxSyncError,
                    SyncSuccessRate = successRate,
                    SyncQualityScore = qualityScore
                };
            }
        }
        
        public double GetSyncSuccessRate()
        {
            lock (_statsLock)
            {
                return _totalSyncPoints > 0 ? (double)_successfulSyncPoints / _totalSyncPoints * 100.0 : 0.0;
            }
        }
        
        public void Dispose()
        {
            Stop();
        }
    }
    
    public class SyncTriggerEventArgs : EventArgs
    {
        public long SequenceNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public double CameraLatencyMs { get; set; }
        public double SensorLatencyMs { get; set; }
    }
}