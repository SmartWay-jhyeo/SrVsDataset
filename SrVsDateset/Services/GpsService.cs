using System;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;

namespace SrVsDataset.Services
{
    public class GpsService : IGpsService
    {
        private SerialPort _serialPort;
        private Thread _readThread;
        private bool _isReading;
        private readonly ILoggingService _logger;
        private CancellationTokenSource _cancellationTokenSource;
        
        public event EventHandler<GpsPoint> LocationUpdated;
        
        public bool IsConnected => _serialPort?.IsOpen ?? false;
        public GpsPoint CurrentLocation { get; private set; }
        
        // GPS fix status tracking
        private bool _hasValidFix = false;
        private int _satelliteCount = 0;
        
        public GpsService(ILoggingService logger = null)
        {
            _logger = logger ?? new LoggingService();
            CurrentLocation = new GpsPoint();
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
                        Encoding = Encoding.ASCII,
                        NewLine = "\r\n"
                    };
                    
                    _serialPort.Open();
                    
                    if (_serialPort.IsOpen)
                    {
                        _logger.LogInfo($"GPS connected to {portName} at {baudRate} baud");
                        
                        // Start reading thread
                        _isReading = true;
                        _cancellationTokenSource = new CancellationTokenSource();
                        _readThread = new Thread(ReadGpsData) { IsBackground = true };
                        _readThread.Start();
                        
                        return true;
                    }
                    
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to connect GPS: {ex.Message}");
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
                    _hasValidFix = false;
                    _satelliteCount = 0;
                    CurrentLocation = new GpsPoint();
                    
                    _logger.LogInfo("GPS disconnected");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error disconnecting GPS: {ex.Message}");
                }
            });
        }
        
        public async Task<GpsPoint> GetCurrentLocationAsync()
        {
            return await Task.FromResult(CurrentLocation);
        }
        
        private void ReadGpsData()
        {
            while (_isReading && _serialPort?.IsOpen == true)
            {
                try
                {
                    string line = _serialPort.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        ProcessNmeaSentence(line);
                    }
                }
                catch (TimeoutException)
                {
                    // Normal timeout, continue reading
                }
                catch (Exception ex)
                {
                    if (_isReading)
                    {
                        _logger.LogError($"GPS read error: {ex.Message}");
                    }
                }
            }
        }
        
        private void ProcessNmeaSentence(string sentence)
        {
            try
            {
                // Remove any leading/trailing whitespace
                sentence = sentence.Trim();
                
                // Verify checksum
                if (!VerifyChecksum(sentence))
                {
                    return;
                }
                
                // Process based on sentence type
                if (sentence.StartsWith("$GPRMC"))
                {
                    ProcessGprmc(sentence);
                }
                else if (sentence.StartsWith("$GPGGA"))
                {
                    ProcessGpgga(sentence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing NMEA sentence: {ex.Message}");
            }
        }
        
        private bool VerifyChecksum(string sentence)
        {
            if (string.IsNullOrEmpty(sentence) || sentence.Length < 4)
                return false;
                
            int checksumIndex = sentence.LastIndexOf('*');
            if (checksumIndex < 1 || checksumIndex >= sentence.Length - 2)
                return false;
                
            string dataToCheck = sentence.Substring(1, checksumIndex - 1);
            string providedChecksum = sentence.Substring(checksumIndex + 1, 2);
            
            // Calculate checksum
            byte calculatedChecksum = 0;
            foreach (char c in dataToCheck)
            {
                calculatedChecksum ^= (byte)c;
            }
            
            string calculatedHex = calculatedChecksum.ToString("X2");
            return string.Equals(providedChecksum, calculatedHex, StringComparison.OrdinalIgnoreCase);
        }
        
        private void ProcessGprmc(string sentence)
        {
            // $GPRMC,063802.00,A,3724.41511,N,12643.37321,E,0.021,,291124,,,A*6C
            string[] fields = sentence.Split(',');
            if (fields.Length < 7)
                return;
                
            // Check if data is valid (A = Active, V = Void)
            if (fields[2] == "V")
            {
                _hasValidFix = false;
                return;
            }
            
            _hasValidFix = true;
        }
        
        private void ProcessGpgga(string sentence)
        {
            // $GPGGA,063802.00,3724.41511,N,12643.37321,E,1,07,1.23,42.8,M,25.0,M,,*58
            string[] fields = sentence.Split(',');
            if (fields.Length < 10)
                return;
                
            // Check GPS fix quality (0 = invalid, 1 = GPS fix, 2 = DGPS fix)
            if (fields[6] == "0")
            {
                _hasValidFix = false;
                return;
            }
            
            _hasValidFix = true;
            
            // Parse satellite count
            if (int.TryParse(fields[7], out int satCount))
            {
                _satelliteCount = satCount;
            }
            
            // Parse location data
            double? latitude = ParseCoordinate(fields[2], fields[3]);
            double? longitude = ParseCoordinate(fields[4], fields[5]);
            
            if (latitude.HasValue && longitude.HasValue)
            {
                var newLocation = new GpsPoint
                {
                    Latitude = latitude.Value,
                    Longitude = longitude.Value,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Satellites = _satelliteCount
                };
                
                CurrentLocation = newLocation;
                LocationUpdated?.Invoke(this, newLocation);
                
                _logger.LogDebug($"GPS Location: {latitude:F6}, {longitude:F6} (Satellites: {_satelliteCount})");
            }
        }
        
        private double? ParseCoordinate(string coordinateStr, string direction)
        {
            if (string.IsNullOrEmpty(coordinateStr) || string.IsNullOrEmpty(direction))
                return null;
                
            // NMEA format: DDMM.MMMMM for latitude, DDDMM.MMMMM for longitude
            double coordinate;
            if (!double.TryParse(coordinateStr, NumberStyles.Float, CultureInfo.InvariantCulture, out coordinate))
                return null;
                
            // Extract degrees
            int degrees;
            double minutes;
            
            if (direction == "N" || direction == "S")
            {
                // Latitude: DD
                degrees = (int)(coordinate / 100);
                minutes = coordinate - (degrees * 100);
            }
            else
            {
                // Longitude: DDD
                degrees = (int)(coordinate / 100);
                minutes = coordinate - (degrees * 100);
            }
            
            // Convert to decimal degrees
            double decimalDegrees = degrees + (minutes / 60.0);
            
            // Apply direction
            if (direction == "S" || direction == "W")
            {
                decimalDegrees = -decimalDegrees;
            }
            
            return decimalDegrees;
        }
        
        public void Dispose()
        {
            DisconnectAsync().Wait();
            _cancellationTokenSource?.Dispose();
        }
    }
}