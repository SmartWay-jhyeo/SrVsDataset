using System;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;

namespace SrVsDataset.Services
{
    public class SensorService : ISensorService
    {
        private SerialPort _serialPort;
        private Thread _readThread;
        private bool _isReading;
        private readonly ILoggingService _logger;
        private readonly IGpsService _gpsService;
        private CancellationTokenSource _cancellationTokenSource;
        private StringBuilder _dataBuffer;
        
        public event EventHandler<SensorData> SensorDataUpdated;
        
        public bool IsConnected => _serialPort?.IsOpen ?? false;
        public SensorData CurrentSensorData { get; private set; }
        
        public SensorService(ILoggingService logger = null, IGpsService gpsService = null)
        {
            _logger = logger ?? new LoggingService();
            _gpsService = gpsService;
            CurrentSensorData = new SensorData();
            _dataBuffer = new StringBuilder();
        }
        
        public async Task<bool> ConnectAsync(string portName, int baudRate)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_serialPort?.IsOpen == true)
                    {
                        DisconnectAsync().Wait();
                    }
                    
                    _serialPort = new SerialPort
                    {
                        PortName = portName,
                        BaudRate = baudRate,
                        DataBits = 8,
                        Parity = Parity.None,
                        StopBits = StopBits.One,
                        Handshake = Handshake.None,
                        ReadTimeout = 500,
                        WriteTimeout = 500,
                        Encoding = Encoding.ASCII
                    };
                    
                    _serialPort.Open();
                    
                    if (_serialPort.IsOpen)
                    {
                        _logger.LogInfo($"Sensor connected to {portName} at {baudRate} baud");
                        
                        // Start reading thread
                        _isReading = true;
                        _cancellationTokenSource = new CancellationTokenSource();
                        _readThread = new Thread(ReadSensorData) { IsBackground = true };
                        _readThread.Start();
                        
                        return true;
                    }
                    
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to connect sensor: {ex.Message}");
                    return false;
                }
            });
        }
        
        public async Task DisconnectAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    _isReading = false;
                    _cancellationTokenSource?.Cancel();
                    
                    // Wait for read thread to finish
                    if (_readThread?.IsAlive == true)
                    {
                        _readThread.Join(1000);
                    }
                    
                    if (_serialPort?.IsOpen == true)
                    {
                        _serialPort.Close();
                        _serialPort.Dispose();
                    }
                    
                    _serialPort = null;
                    CurrentSensorData = new SensorData();
                    
                    _logger.LogInfo("Sensor disconnected");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error disconnecting sensor: {ex.Message}");
                }
            });
        }
        
        private void ReadSensorData()
        {
            while (_isReading && _serialPort?.IsOpen == true)
            {
                try
                {
                    int bytesToRead = _serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        byte[] buffer = new byte[bytesToRead];
                        _serialPort.Read(buffer, 0, bytesToRead);
                        string data = Encoding.ASCII.GetString(buffer);
                        ProcessIncomingData(data);
                    }
                    
                    Thread.Sleep(10); // Small delay to prevent CPU hogging
                }
                catch (TimeoutException)
                {
                    // Normal timeout, continue reading
                }
                catch (Exception ex)
                {
                    if (_isReading)
                    {
                        _logger.LogError($"Sensor read error: {ex.Message}");
                    }
                }
            }
        }
        
        private void ProcessIncomingData(string data)
        {
            _dataBuffer.Append(data);
            string bufferContent = _dataBuffer.ToString();
            
            // Look for complete data packets ending with @
            int endIndex;
            while ((endIndex = bufferContent.IndexOf('\n')) != -1)
            {
                // Extract complete packet
                string packet = bufferContent.Substring(0, endIndex);
                
                // Remove processed data from buffer
                _dataBuffer.Clear();
                if (endIndex + 1 < bufferContent.Length)
                {
                    _dataBuffer.Append(bufferContent.Substring(endIndex + 1));
                }
                bufferContent = _dataBuffer.ToString();
                
                // Process the packet
                ProcessSensorPacket(packet);
            }
        }
        
        private void ProcessSensorPacket(string packet)
        {
            try
            {
                if (packet.StartsWith("*")) packet = packet.Substring(1);
                packet = packet.Trim(); 

                string[] values = packet.Split(',');

                for (int i = 0; i < values.Length; i++)
                    values[i] = values[i].Trim().TrimEnd('@', '\r');

                // Check for synchronized mode packet format
                // Format: SYNC,sequence,timestamp,processing_delay,light,humidity,temp,accel_x,accel_y,accel_z,gyro_x,gyro_y,gyro_z,mag_x,mag_y,mag_z,roll,pitch,yaw
                // Or legacy format (continuous mode): light,humidity,temp,accel_x,accel_y,accel_z,gyro_x,gyro_y,gyro_z,mag_x,mag_y,mag_z,roll,pitch,yaw
                
                SensorData sensorData;
                if (values.Length >= 19 && values[0].Equals("SYNC", StringComparison.OrdinalIgnoreCase))
                {
                    // Synchronized mode packet
                    sensorData = new SensorData
                    {
                        Sequence = long.TryParse(values[1], out long seq) ? seq : (long?)null,
                        Timestamp = values[2], // Use timestamp from Arduino
                        ProcessingDelayMs = ParseDouble(values[3]),
                        LightLevel = NormalizeLight(ParseDouble(values[4])),
                        Humidity = ParseDouble(values[5]),
                        Temperature = ParseDouble(values[6]),
                        Imu = new ImuData
                        {
                            Acceleration = new Vector3D
                            {
                                X = ParseDouble(values[7]),
                                Y = ParseDouble(values[8]),
                                Z = ParseDouble(values[9])
                            },
                            Gyroscope = new Vector3D
                            {
                                X = ParseDouble(values[10]),
                                Y = ParseDouble(values[11]),
                                Z = ParseDouble(values[12])
                            },
                            Magnetometer = new Vector3D
                            {
                                X = ParseDouble(values[13]),
                                Y = ParseDouble(values[14]),
                                Z = ParseDouble(values[15])
                            },
                            Euler = new EulerAngles
                            {
                                Roll = ParseDouble(values[16]),
                                Pitch = ParseDouble(values[17]),
                                Yaw = ParseDouble(values[18])
                            }
                        }
                    };
                }
                else if (values.Length >= 15)
                {
                    // Legacy continuous mode packet
                    sensorData = new SensorData
                    {
                        Timestamp = TimestampManager.GetPreciseTimestamp().ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                        LightLevel = NormalizeLight(ParseDouble(values[0])),
                        Humidity = ParseDouble(values[1]),
                        Temperature = ParseDouble(values[2]),
                        Imu = new ImuData
                        {
                            Acceleration = new Vector3D
                            {
                                X = ParseDouble(values[3]),
                                Y = ParseDouble(values[4]),
                                Z = ParseDouble(values[5])
                            },
                            Gyroscope = new Vector3D
                            {
                                X = ParseDouble(values[6]),
                                Y = ParseDouble(values[7]),
                                Z = ParseDouble(values[8])
                            },
                            Magnetometer = new Vector3D
                            {
                                X = ParseDouble(values[9]),
                                Y = ParseDouble(values[10]),
                                Z = ParseDouble(values[11])
                            },
                            Euler = new EulerAngles
                            {
                                Roll = ParseDouble(values[12]),
                                Pitch = ParseDouble(values[13]),
                                Yaw = ParseDouble(values[14])
                            }
                        }
                    };
                }
                else
                {
                    _logger.LogWarning($"Invalid sensor packet format: {packet}");
                    return;
                }
                
                // Sync with GPS data if available
                if (_gpsService != null && _gpsService.CurrentLocation != null)
                {
                    sensorData.GpsSync = new GpsPoint
                    {
                        Latitude = _gpsService.CurrentLocation.Latitude,
                        Longitude = _gpsService.CurrentLocation.Longitude,
                        Timestamp = _gpsService.CurrentLocation.Timestamp,
                        Satellites = _gpsService.CurrentLocation.Satellites
                    };
                }
                
                CurrentSensorData = sensorData;
                SensorDataUpdated?.Invoke(this, sensorData);
                
                if (sensorData.Sequence.HasValue)
                {
                    _logger.LogDebug($"Sync sensor data (seq: {sensorData.Sequence}): Light={sensorData.LightLevel:F1}%, " +
                                    $"Temp={sensorData.Temperature:F1}°C, Delay={sensorData.ProcessingDelayMs:F1}ms");
                }
                else
                {
                    _logger.LogDebug($"Sensor data: Light={sensorData.LightLevel:F1}%, " +
                                    $"Temp={sensorData.Temperature:F1}°C, " +
                                    $"Humidity={sensorData.Humidity:F1}%");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing sensor packet: {ex.Message}");
            }
        }
        
        private double ParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return 0.0;
        }

        private double NormalizeLight(double rawValue)
        {
            // Convert 0-1024 to 0-100% (inverted)
            // 어두울 때(1024) -> 0%, 밝을 때(0) -> 100%
            return Math.Max(0, Math.Min(100, ((1024.0 - rawValue) / 1024.0) * 100.0));
        }

        public async Task SendCommandAsync(string command)
        {
            if (_serialPort?.IsOpen != true)
            {
                _logger.LogWarning("Cannot send command: sensor not connected");
                return;
            }
            
            try
            {
                await Task.Run(() =>
                {
                    var commandBytes = Encoding.ASCII.GetBytes(command + "\r\n");
                    _serialPort.Write(commandBytes, 0, commandBytes.Length);
                    _logger.LogDebug($"Sent command to sensor: {command}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending command to sensor: {ex.Message}");
            }
        }

        public void Dispose()
        {
            DisconnectAsync().Wait();
            _cancellationTokenSource?.Dispose();
        }
    }
}