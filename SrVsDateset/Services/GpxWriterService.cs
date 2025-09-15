using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SrVsDataset.Models;
using SrVsDataset.Interfaces;

namespace SrVsDataset.Services
{
    public class GpxWriterService
    {
        private readonly ILoggingService _logger;
        private XmlWriter _xmlWriter;
        private FileStream _fileStream;
        private string _currentFilePath;
        private bool _isWriting;
        private readonly object _writeLock = new object();
        private int _pointCount = 0;

        public GpxWriterService(ILoggingService logger = null)
        {
            _logger = logger ?? new LoggingService();
        }

        public async Task<bool> StartWritingAsync(string filePath, string trackName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_writeLock)
                    {
                        if (_isWriting)
                        {
                            _logger.LogWarning("GPX writing is already in progress");
                            return false;
                        }

                        _currentFilePath = filePath;
                        _pointCount = 0;

                        // Create file stream
                        _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                        
                        // Create XML writer with settings
                        var settings = new XmlWriterSettings
                        {
                            Encoding = Encoding.UTF8,
                            Indent = true,
                            IndentChars = "  ",
                            Async = true
                        };
                        
                        _xmlWriter = XmlWriter.Create(_fileStream, settings);
                        
                        // Write GPX header
                        _xmlWriter.WriteStartDocument();
                        _xmlWriter.WriteStartElement("gpx", "http://www.topografix.com/GPX/1/1");
                        _xmlWriter.WriteAttributeString("version", "1.1");
                        _xmlWriter.WriteAttributeString("creator", "SrVsDataset");
                        
                        // Start track
                        _xmlWriter.WriteStartElement("trk");
                        _xmlWriter.WriteElementString("name", trackName);
                        
                        // Start track segment
                        _xmlWriter.WriteStartElement("trkseg");
                        _xmlWriter.Flush();
                        
                        _isWriting = true;
                        _logger.LogInfo($"Started writing GPX file: {filePath}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to start GPX writing: {ex.Message}");
                    CleanupResources();
                    return false;
                }
            });
        }

        public async Task<bool> AddTrackPointAsync(GpsPoint point)
        {
            if (!_isWriting || _xmlWriter == null)
            {
                _logger.LogWarning("Cannot add track point - GPX writer not initialized");
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    lock (_writeLock)
                    {
                        if (!point.Latitude.HasValue || !point.Longitude.HasValue)
                        {
                            return false;
                        }

                        // Write track point
                        _xmlWriter.WriteStartElement("trkpt");
                        _xmlWriter.WriteAttributeString("lat", point.Latitude.Value.ToString("F6"));
                        _xmlWriter.WriteAttributeString("lon", point.Longitude.Value.ToString("F6"));
                        
                        // Write time
                        if (!string.IsNullOrEmpty(point.Timestamp))
                        {
                            _xmlWriter.WriteElementString("time", point.Timestamp);
                        }
                        else
                        {
                            _xmlWriter.WriteElementString("time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                        }
                        
                        // Write satellite count if available
                        if (point.Satellites.HasValue)
                        {
                            _xmlWriter.WriteElementString("sat", point.Satellites.Value.ToString());
                        }
                        
                        _xmlWriter.WriteEndElement(); // trkpt
                        _xmlWriter.Flush();
                        
                        _pointCount++;
                        
                        // Log every 10 points
                        if (_pointCount % 10 == 0)
                        {
                            _logger.LogDebug($"Written {_pointCount} GPS points to GPX");
                        }
                        
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to add track point: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> StopWritingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_writeLock)
                    {
                        if (!_isWriting)
                        {
                            return true;
                        }

                        // Close track segment
                        _xmlWriter.WriteEndElement(); // trkseg
                        
                        // Close track
                        _xmlWriter.WriteEndElement(); // trk
                        
                        // Close GPX
                        _xmlWriter.WriteEndElement(); // gpx
                        _xmlWriter.WriteEndDocument();
                        
                        _xmlWriter.Flush();
                        _xmlWriter.Close();
                        _xmlWriter.Dispose();
                        _xmlWriter = null;
                        
                        _fileStream.Close();
                        _fileStream.Dispose();
                        _fileStream = null;
                        
                        _isWriting = false;
                        
                        _logger.LogInfo($"Finished writing GPX file: {_currentFilePath} ({_pointCount} points)");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to stop GPX writing: {ex.Message}");
                    CleanupResources();
                    return false;
                }
            });
        }

        private void CleanupResources()
        {
            try
            {
                _xmlWriter?.Close();
                _xmlWriter?.Dispose();
                _xmlWriter = null;
                
                _fileStream?.Close();
                _fileStream?.Dispose();
                _fileStream = null;
                
                _isWriting = false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during cleanup: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isWriting)
            {
                StopWritingAsync().Wait();
            }
            CleanupResources();
        }
    }
}