using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;

namespace SrVsDataset.Services
{
    public class SensorDataWriterService : IDisposable
    {
        private readonly ILoggingService _logger;
        private StreamWriter _writer;
        private StreamWriter _csvWriter; // CSV writer for synchronized mode
        private string _currentFile;
        private string _csvFile; // CSV file for synchronized mode
        private List<SensorData> _sensorDataBuffer;
        private readonly object _lockObject = new object();
        private RecordingMode _recordingMode = RecordingMode.Continuous;
        
        public string CurrentFile => _currentFile;
        public string CsvFile => _csvFile;
        public int DataPointCount { get; private set; }
        
        public SensorDataWriterService(ILoggingService logger = null)
        {
            _logger = logger ?? new LoggingService();
            _sensorDataBuffer = new List<SensorData>();
        }
        
        public void SetRecordingMode(RecordingMode mode)
        {
            _recordingMode = mode;
        }
        
        public async Task<bool> StartNewFileAsync(string directory, string baseFileName)
        {
            try
            {
                await StopAsync();
                
                // Create sensor data file names based on recording mode
                if (_recordingMode == RecordingMode.Synchronized)
                {
                    // For synchronized mode: create both JSON summary and CSV real-time log
                    _currentFile = Path.Combine(directory, $"{baseFileName}_sensors.json");
                    _csvFile = Path.Combine(directory, $"{baseFileName}_sensors.csv");
                    
                    // Create CSV file for real-time synchronized data
                    var csvFileStream = new FileStream(_csvFile, FileMode.Create, FileAccess.Write, FileShare.Read);
                    _csvWriter = new StreamWriter(csvFileStream, Encoding.UTF8);
                    
                    // Write CSV header
                    await _csvWriter.WriteLineAsync("sequence,timestamp,temperature,humidity,light_level,accel_x,accel_y,accel_z,gyro_x,gyro_y,gyro_z,mag_x,mag_y,mag_z,roll,pitch,yaw,processing_delay_ms");
                    await _csvWriter.FlushAsync();
                }
                else
                {
                    // For continuous mode: use original JSON format
                    _currentFile = Path.Combine(directory, $"{baseFileName}_sensors.json");
                }
                
                // Open JSON file for writing (always created for summary)
                var fileStream = new FileStream(_currentFile, FileMode.Create, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(fileStream, Encoding.UTF8);
                
                // Write opening bracket for JSON array
                await _writer.WriteLineAsync("[");
                await _writer.FlushAsync();
                
                DataPointCount = 0;
                _logger.LogInfo($"Started sensor data file: {_currentFile}" + 
                    (_recordingMode == RecordingMode.Synchronized ? $" and CSV: {_csvFile}" : ""));
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start sensor data file: {ex.Message}");
                return false;
            }
        }
        
        public async Task AddDataPointAsync(SensorData data)
        {
            if (_writer == null || data == null)
                return;
                
            try
            {
                // For synchronized mode, write immediately to CSV
                if (_recordingMode == RecordingMode.Synchronized && _csvWriter != null && data.Sequence.HasValue)
                {
                    var csvLine = $"{data.Sequence},{data.Timestamp:yyyy-MM-ddTHH:mm:ss.fff}," +
                        $"{data.Temperature:F2},{data.Humidity:F2},{data.LightLevel:F2}," +
                        $"{data.Imu.Acceleration.X:F4},{data.Imu.Acceleration.Y:F4},{data.Imu.Acceleration.Z:F4}," +
                        $"{data.Imu.Gyroscope.X:F4},{data.Imu.Gyroscope.Y:F4},{data.Imu.Gyroscope.Z:F4}," +
                        $"{data.Imu.Magnetometer.X:F4},{data.Imu.Magnetometer.Y:F4},{data.Imu.Magnetometer.Z:F4}," +
                        $"{data.Imu.Euler.Roll:F3},{data.Imu.Euler.Pitch:F3},{data.Imu.Euler.Yaw:F3}," +
                        $"{data.ProcessingDelayMs:F2}";
                    
                    await _csvWriter.WriteLineAsync(csvLine);
                    await _csvWriter.FlushAsync(); // Immediate flush for real-time data
                }
                
                lock (_lockObject)
                {
                    _sensorDataBuffer.Add(data);
                }
                
                // Write buffered data to JSON periodically (for summary purposes)
                if (_sensorDataBuffer.Count >= 10)
                {
                    await FlushBufferAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add sensor data point: {ex.Message}");
            }
        }
        
        private async Task FlushBufferAsync()
        {
            List<SensorData> dataToWrite;
            
            lock (_lockObject)
            {
                if (_sensorDataBuffer.Count == 0)
                    return;
                    
                dataToWrite = new List<SensorData>(_sensorDataBuffer);
                _sensorDataBuffer.Clear();
            }
            
            foreach (var data in dataToWrite)
            {
                if (DataPointCount > 0)
                {
                    await _writer.WriteAsync(",\n");
                }
                
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                await _writer.WriteAsync(json);
                DataPointCount++;
            }
            
            await _writer.FlushAsync();
        }
        
        public async Task<SensorSummary> StopAsync()
        {
            try
            {
                if (_writer != null)
                {
                    // Flush any remaining buffered data
                    await FlushBufferAsync();
                    
                    // Close JSON array
                    await _writer.WriteLineAsync("\n]");
                    await _writer.FlushAsync();
                    
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                    
                    _logger.LogInfo($"Closed sensor data file with {DataPointCount} data points");
                }
                
                // Close CSV writer if in synchronized mode
                if (_csvWriter != null)
                {
                    await _csvWriter.FlushAsync();
                    _csvWriter.Close();
                    _csvWriter.Dispose();
                    _csvWriter = null;
                    
                    _logger.LogInfo($"Closed CSV sensor data file: {_csvFile}");
                }
                
                // Calculate summary statistics
                return await CalculateSummaryAsync(_currentFile);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error stopping sensor data writer: {ex.Message}");
                return null;
            }
        }
        
        private async Task<SensorSummary> CalculateSummaryAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;
                    
                string jsonContent = await File.ReadAllTextAsync(filePath);
                var allData = JsonConvert.DeserializeObject<List<SensorData>>(jsonContent);
                
                if (allData == null || allData.Count == 0)
                    return null;
                    
                return new SensorSummary
                {
                    Temperature = new SensorRange
                    {
                        Min = allData.Min(d => d.Temperature),
                        Max = allData.Max(d => d.Temperature),
                        Average = allData.Average(d => d.Temperature)
                    },
                    Humidity = new SensorRange
                    {
                        Min = allData.Min(d => d.Humidity),
                        Max = allData.Max(d => d.Humidity),
                        Average = allData.Average(d => d.Humidity)
                    },
                    LightLevel = new SensorRange
                    {
                        Min = allData.Min(d => d.LightLevel),
                        Max = allData.Max(d => d.LightLevel),
                        Average = allData.Average(d => d.LightLevel)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to calculate sensor summary: {ex.Message}");
                return null;
            }
        }
        
        public void Dispose()
        {
            StopAsync().Wait();
        }
    }
}