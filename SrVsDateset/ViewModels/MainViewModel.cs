using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;
using SrVsDataset.Services;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows;
using SrVsDataset.Utils;

namespace SrVsDataset.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ICameraService _cameraService;
        private readonly IFileManagementService _fileService;
        private readonly IVideoRecordingService _videoService;
        private readonly IGpsService _gpsService;
        private readonly ISensorService _sensorService;
        private SensorDataWriterService _sensorWriter;
        private SyncManager _syncManager;
        private RecordingSession _currentSession;
        private DateTime _lastFrameTime = DateTime.Now;
        private DateTime _lastUIUpdateTime = DateTime.Now;
        private int _frameCount = 0;
        private GpxWriterService _gpxWriter;
        private bool _isRecordingGps = false;

        [ObservableProperty]
        private BitmapSource _cameraPreview;

        [ObservableProperty]
        private bool _isRecording;

        [ObservableProperty]
        private bool _isCameraInitializing;

        [ObservableProperty]
        private bool _isCameraConnected;

        [ObservableProperty]
        private string _recordingStatus = "준비";

        [ObservableProperty]
        private TimeSpan _recordingDuration;

        [ObservableProperty]
        private double _cameraTemperature;

        // Environment Settings
        [ObservableProperty]
        private RoadType _selectedRoadType = RoadType.Highway;

        [ObservableProperty]
        private Weather _selectedWeather = Weather.Clear;

        [ObservableProperty]
        private TimeOfDay _selectedTimeOfDay = TimeOfDay.Midday;

        [ObservableProperty]
        private RecordingSide _selectedRecordingSide = RecordingSide.Left;

        // Camera Settings
        [ObservableProperty]
        private int _exposureTime = 5000;

        [ObservableProperty]
        private ExposureMode _selectedExposureMode = ExposureMode.Manual;

        [ObservableProperty]
        private bool _isExposureManual = true;

        [ObservableProperty]
        private WhiteBalanceMode _selectedWhiteBalanceMode = WhiteBalanceMode.Auto;

        [ObservableProperty]
        private int _whiteBalanceTemperature = 5600;

        [ObservableProperty]
        private bool _isWhiteBalanceManual;

        [ObservableProperty]
        private string _savePath;

        [ObservableProperty]
        private string _currentResolution = "1920x1080";

        [ObservableProperty]
        private double _currentFps = 0.0;

        [ObservableProperty]
        private VideoCompressionType _selectedVideoCompression = VideoCompressionType.H264;

        // Recording Mode Settings
        [ObservableProperty]
        private RecordingMode _selectedRecordingMode = RecordingMode.Continuous;

        [ObservableProperty]
        private bool _isSynchronizedMode;

        [ObservableProperty]
        private bool _isSyncActive;

        [ObservableProperty]
        private string _syncStatus = "동기화 비활성";

        [ObservableProperty]
        private ImageRotation _selectedImageRotation = ImageRotation.Rotate0;

        [ObservableProperty]
        private ImageFlip _selectedImageFlip = ImageFlip.None;

        private BitmapSource _originalCameraPreview;

        // GPS Properties
        [ObservableProperty]
        private bool _isGpsConnected;

        [ObservableProperty]
        private string _selectedGpsPort;

        [ObservableProperty]
        private string _gpsStatus = "GPS 미연결";

        [ObservableProperty]
        private GpsPoint _currentGpsLocation;

        [ObservableProperty]
        private ObservableCollection<string> _availableGpsPorts = new();

        // Sensor Properties
        [ObservableProperty]
        private bool _isSensorConnected;

        [ObservableProperty]
        private string _selectedSensorPort;

        [ObservableProperty]
        private int _selectedBaudrate = 9600;

        [ObservableProperty]
        private ObservableCollection<string> _availableSensorPorts = new();

        [ObservableProperty]
        private ObservableCollection<int> _availableBaudrates = new() { 9600, 14400, 19200, 38400, 57600, 115200 };

        [ObservableProperty]
        private double _lightLevel;

        [ObservableProperty]
        private double _temperature;

        [ObservableProperty]
        private double _humidity;

        [ObservableProperty]
        private double _accelX;

        [ObservableProperty]
        private double _accelY;

        [ObservableProperty]
        private double _accelZ;

        [ObservableProperty]
        private double _gyroX;

        [ObservableProperty]
        private double _gyroY;

        [ObservableProperty]
        private double _gyroZ;

        [ObservableProperty]
        private double _magX;

        [ObservableProperty]
        private double _magY;

        [ObservableProperty]
        private double _magZ;

        [ObservableProperty]
        private double _roll;

        [ObservableProperty]
        private double _pitch;

        [ObservableProperty]
        private double _yaw;

        // Collections for ComboBoxes
        public ObservableCollection<RoadType> RoadTypes { get; } = new()
        {
            RoadType.Highway,
            RoadType.Urban
        };

        public ObservableCollection<Weather> WeatherTypes { get; } = new()
        {
            Weather.Clear,
            Weather.Cloudy
        };

        public ObservableCollection<TimeOfDay> TimeOfDayTypes { get; } = new()
        {
            TimeOfDay.AM,
            TimeOfDay.Midday,
            TimeOfDay.PM,
            TimeOfDay.Night
        };

        public ObservableCollection<RecordingSide> RecordingSides { get; } = new()
        {
            RecordingSide.Left,
            RecordingSide.Right
        };

        public ObservableCollection<WhiteBalanceMode> WhiteBalanceModes { get; } = new()
        {
            WhiteBalanceMode.Auto,
            WhiteBalanceMode.Manual
        };

        public ObservableCollection<ExposureMode> ExposureModes { get; } = new()
        {
            ExposureMode.Auto,
            ExposureMode.Manual
        };

        public ObservableCollection<VideoCompressionType> VideoCompressionTypes { get; } = new()
        {
            VideoCompressionType.Uncompressed,
            VideoCompressionType.H264,
            VideoCompressionType.H265,
            VideoCompressionType.MJPEG
        };

        public ObservableCollection<RecordingMode> RecordingModes { get; } = new()
        {
            RecordingMode.Continuous,
            RecordingMode.Synchronized
        };

        public ObservableCollection<ImageRotation> ImageRotations { get; } = new()
        {
            ImageRotation.Rotate0,
            ImageRotation.Rotate90,
            ImageRotation.Rotate180,
            ImageRotation.Rotate270
        };

        public ObservableCollection<ImageFlip> ImageFlips { get; } = new()
        {
            ImageFlip.None,
            ImageFlip.Horizontal,
            ImageFlip.Vertical,
            ImageFlip.Both
        };

        // Commands
        public IAsyncRelayCommand InitializeCameraCommand { get; }
        public IAsyncRelayCommand DisconnectCameraCommand { get; }
        public IAsyncRelayCommand StartRecordingCommand { get; }
        public IAsyncRelayCommand StopRecordingCommand { get; }
        public IAsyncRelayCommand ApplyCameraSettingsCommand { get; }
        public IAsyncRelayCommand OnePushWhiteBalanceCommand { get; }
        public IRelayCommand<string> SetWhiteBalancePresetCommand { get; }
        public IRelayCommand ChangeSavePathCommand { get; }
        public IRelayCommand TakeSnapshotCommand { get; }
        public IAsyncRelayCommand ConnectGpsCommand { get; }
        public IAsyncRelayCommand DisconnectGpsCommand { get; }
        public IRelayCommand RefreshGpsPortsCommand { get; }
        public IAsyncRelayCommand ConnectSensorCommand { get; }
        public IAsyncRelayCommand DisconnectSensorCommand { get; }
        public IRelayCommand RefreshSensorPortsCommand { get; }

        public MainViewModel(ICameraService cameraService, IFileManagementService fileService, IVideoRecordingService videoService, IGpsService gpsService, ISensorService sensorService = null)
        {
            _cameraService = cameraService;
            _fileService = fileService;
            _videoService = videoService;
            _gpsService = gpsService;
            _sensorService = sensorService ?? new SensorService(new LoggingService(), gpsService);

            // Initialize commands
            InitializeCameraCommand = new AsyncRelayCommand(InitializeCameraAsync, () => !IsCameraInitializing);
            DisconnectCameraCommand = new AsyncRelayCommand(DisconnectCameraAsync, () => IsCameraConnected);
            StartRecordingCommand   = new AsyncRelayCommand(StartRecordingAsync, () => !IsRecording && IsCameraConnected);
            StopRecordingCommand    = new AsyncRelayCommand(StopRecordingAsync, () => IsRecording);
            ApplyCameraSettingsCommand = new AsyncRelayCommand(ApplyCameraSettingsAsync);
            OnePushWhiteBalanceCommand = new AsyncRelayCommand(OnePushWhiteBalanceAsync, () => IsCameraConnected && IsWhiteBalanceManual);
            SetWhiteBalancePresetCommand = new RelayCommand<string>(SetWhiteBalancePreset);
            ChangeSavePathCommand = new RelayCommand(ChangeSavePath);
            TakeSnapshotCommand = new RelayCommand(TakeSnapshot, () => IsCameraConnected);
            ConnectGpsCommand = new AsyncRelayCommand(ConnectGpsAsync, () => !IsGpsConnected && !string.IsNullOrEmpty(SelectedGpsPort));
            DisconnectGpsCommand = new AsyncRelayCommand(DisconnectGpsAsync, () => IsGpsConnected);
            RefreshGpsPortsCommand = new RelayCommand(RefreshGpsPorts);
            ConnectSensorCommand = new AsyncRelayCommand(ConnectSensorAsync, () => !IsSensorConnected && !string.IsNullOrEmpty(SelectedSensorPort));
            DisconnectSensorCommand = new AsyncRelayCommand(DisconnectSensorAsync, () => IsSensorConnected);
            RefreshSensorPortsCommand = new RelayCommand(RefreshSensorPorts);

            // Subscribe to camera events
            _cameraService.FrameReceived += OnFrameReceived;
            _cameraService.TemperatureChanged += OnTemperatureChanged;
            
            // Subscribe to GPS events
            _gpsService.LocationUpdated += OnGpsLocationUpdated;
            
            // Subscribe to sensor events
            _sensorService.SensorDataUpdated += OnSensorDataUpdated;
            
            // Initialize SyncManager
            _syncManager = new SyncManager(_cameraService, _sensorService, new LoggingService());
            _syncManager.SyncTriggered += OnSyncTriggered;
            
            // Initialize save path from FileManagementService
            SavePath = _fileService.RootPath;
            
            // Initialize ports
            RefreshGpsPorts();
            RefreshSensorPorts();
        }

        private async Task InitializeCameraAsync()
        {
            try
            {
                IsCameraInitializing = true;
                RecordingStatus = "카메라 초기화 중...";
                
                var success = await _cameraService.InitializeAsync();
                
                if (success)
                {
                    await _cameraService.StartPreviewAsync();
                    IsCameraConnected = true;
                    RecordingStatus = "준비 완료";
                    
                    // 카메라 해상도 정보 업데이트
                    if (_cameraService is MVCameraService mvCamera)
                    {
                        CurrentResolution = $"{mvCamera.CurrentWidth}x{mvCamera.CurrentHeight}";
                    }
                }
                else
                {
                    IsCameraConnected = false;
                    RecordingStatus = "카메라 초기화 실패";
                }
            }
            catch (Exception ex)
            {
                IsCameraConnected = false;
                RecordingStatus = $"오류: {ex.Message}";
            }
            finally
            {
                IsCameraInitializing = false;
                InitializeCameraCommand.NotifyCanExecuteChanged();
                DisconnectCameraCommand.NotifyCanExecuteChanged();
                StartRecordingCommand.NotifyCanExecuteChanged();
                TakeSnapshotCommand.NotifyCanExecuteChanged();
                OnePushWhiteBalanceCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task DisconnectCameraAsync()
        {
            try
            {
                RecordingStatus = "카메라 연결 해제 중...";
                
                var success = await _cameraService.DisconnectAsync();
                
                if (success)
                {
                    IsCameraConnected = false;
                    RecordingStatus = "카메라 연결 해제 완료";
                    CurrentResolution = "disconnected";
                    CurrentFps = 0.0;
                }
                else
                {
                    RecordingStatus = "카메라 연결 해제 실패";
                }
            }
            catch (Exception ex)
            {
                RecordingStatus = $"연결 해제 오류: {ex.Message}";
            }
            finally
            {
                InitializeCameraCommand.NotifyCanExecuteChanged();
                DisconnectCameraCommand.NotifyCanExecuteChanged();
                StartRecordingCommand.NotifyCanExecuteChanged();
                TakeSnapshotCommand.NotifyCanExecuteChanged();
                OnePushWhiteBalanceCommand.NotifyCanExecuteChanged();
            }
        }

        private async Task StartRecordingAsync()
        {
            try
            {
                // Create environment settings
                var envSettings = new EnvironmentSettings
                {
                    RoadType = SelectedRoadType,
                    Weather = SelectedWeather,
                    TimeOfDay = SelectedTimeOfDay,
                    RecordingSide = SelectedRecordingSide
                };

                // Ensure directory structure
                RecordingStatus = $"디렉토리 생성: {SavePath}";
                var dirResult = await _fileService.EnsureDirectoryStructureAsync(envSettings);
                if (!dirResult)
                {
                    RecordingStatus = "디렉토리 생성 실패";
                    return;
                }

                // Create new recording session
                _currentSession = new RecordingSession();
                _currentSession.Metadata.Environment = new EnvironmentInfo
                {
                    RoadType = SelectedRoadType.ToString().ToLower(),
                    Weather = SelectedWeather.ToString().ToLower(),
                    TimeOfDay = SelectedTimeOfDay.ToString().ToLower(),
                    RecordingSide = SelectedRecordingSide.ToString().ToLower()
                };

                // Set camera settings in metadata
                _currentSession.Metadata.CameraSettings = new CameraSettingsInfo
                {
                    ExposureTime = ExposureTime,
                    WhiteBalanceMode = SelectedWhiteBalanceMode.ToString().ToLower(),
                    WhiteBalanceTemperature = WhiteBalanceTemperature
                };

                // Set recording mode in metadata
                _currentSession.Metadata.RecordingMode = SelectedRecordingMode.GetMetadataString();

                // Set timestamp (use high-precision timestamp for synchronized mode)
                DateTime startTime = SelectedRecordingMode == RecordingMode.Synchronized ? 
                    TimestampManager.GetPreciseTimestamp() : DateTime.Now;
                    
                _currentSession.Metadata.Timestamp = new TimestampInfo
                {
                    Date = startTime.ToString("yyyy-MM-dd"),
                    StartTime = startTime.ToString("HH:mm:ss")
                };

                // Capture GPS start location
                if (_gpsService.IsConnected && _gpsService.CurrentLocation != null)
                {
                    var currentLocation = _gpsService.CurrentLocation;
                    DateTime gpsTimestamp = SelectedRecordingMode == RecordingMode.Synchronized ? 
                        TimestampManager.GetPreciseTimestamp() : DateTime.Now;
                    _currentSession.Metadata.Hardware.Gps.Start = new GpsPoint
                    {
                        Latitude = currentLocation.Latitude,
                        Longitude = currentLocation.Longitude,
                        Timestamp = gpsTimestamp.ToString("yyyy-MM-ddTHH:mm:ss")
                    };
                }

                // 초기 비디오 파일 경로 생성 (임시 파일명)
                string tempVideoPath = _fileService.GenerateFilePath(envSettings, SelectedRecordingSide, "mp4");
                RecordingStatus = $"파일 경로: {tempVideoPath}";
                
                // 카메라 서비스에서 현재 해상도 가져오기
                int width = 1920; // 기본값
                int height = 1080; // 기본값
                int actualFps = 30; // 기본값
                
                if (_cameraService is MVCameraService mvCamera)
                {
                    width = mvCamera.CurrentWidth > 0 ? mvCamera.CurrentWidth : 1920;
                    height = mvCamera.CurrentHeight > 0 ? mvCamera.CurrentHeight : 1080;
                    
                    // 실제 카메라 FPS 사용 (CurrentFps는 실시간 측정된 값)
                    if (CurrentFps > 0 && CurrentFps <= 120) // 합리적인 FPS 범위
                    {
                        actualFps = (int)Math.Round(CurrentFps);
                    }
                }
                
                // 세션에 임시 파일 경로 저장
                _currentSession.VideoFilePath = tempVideoPath;
                
                // GPX 파일 경로 생성 (같은 이름, 다른 확장자)
                string tempGpxPath = tempVideoPath.Replace(".mp4", ".gpx");
                _currentSession.GpxFilePath = tempGpxPath;
                
                // GPS 트랙 기록 시작 (GPS가 연결된 경우)
                if (_gpsService.IsConnected)
                {
                    _gpxWriter = new GpxWriterService();
                    var gpxStarted = await _gpxWriter.StartWritingAsync(tempGpxPath, _currentSession.GenerateFileName(SelectedRecordingSide, ""));
                    if (gpxStarted)
                    {
                        _isRecordingGps = true;
                        RecordingStatus = "GPS 트랙 기록 시작됨";
                    }
                }
                
                // 센서 데이터 기록 시작 (센서가 연결된 경우)
                if (_sensorService.IsConnected)
                {
                    _sensorWriter = new SensorDataWriterService(new LoggingService());
                    var sensorFileName = $"{_currentSession.GenerateFileName(SelectedRecordingSide, "")}_sensors";
                    
                    // 동기화 모드인 경우 SensorDataWriter에 모드 설정
                    _sensorWriter.SetRecordingMode(SelectedRecordingMode);
                    
                    await _sensorWriter.StartNewFileAsync(Path.GetDirectoryName(tempVideoPath), sensorFileName);
                }
                
                // 비디오 녹화 시작 (실제 카메라 해상도와 FPS 사용)
                bool videoStarted = await _videoService.StartRecordingAsync(tempVideoPath, width, height, actualFps);
                if (!videoStarted)
                {
                    RecordingStatus = "비디오 녹화 시작 실패";
                    // GPS 기록도 중지
                    if (_isRecordingGps)
                    {
                        await _gpxWriter.StopWritingAsync();
                        _isRecordingGps = false;
                    }
                    return;
                }

                // 동기화 모드인 경우 SyncManager 시작
                if (SelectedRecordingMode == RecordingMode.Synchronized)
                {
                    _syncManager.Start();
                    SyncStatus = "동기화 활성";
                    IsSyncActive = true;
                }

                IsRecording = true;
                RecordingStatus = "녹화 중...";
                StartRecordingCommand.NotifyCanExecuteChanged();
                StopRecordingCommand.NotifyCanExecuteChanged();
                
                // Start duration timer
                _ = Task.Run(async () =>
                {
                    while (IsRecording)
                    {
                        RecordingDuration = DateTime.Now - _currentSession.StartTime;
                        await Task.Delay(1000);
                    }
                });
            }
            catch (Exception ex)
            {
                RecordingStatus = $"녹화 시작 오류: {ex.Message}";
                IsRecording = false;
            }
        }

        private async Task StopRecordingAsync()
        {
            try
            {
                if (_currentSession == null) return;

                // Use high-precision timestamp for synchronized mode
                DateTime endTime = SelectedRecordingMode == RecordingMode.Synchronized ? 
                    TimestampManager.GetPreciseTimestamp() : DateTime.Now;
                    
                _currentSession.EndTime = endTime;
                _currentSession.Metadata.Timestamp.EndTime = endTime.ToString("HH:mm:ss");
                
                // Set hardware info
                _currentSession.Metadata.Hardware.CameraTemperature = CameraTemperature;

                // Capture GPS end location
                if (_gpsService.IsConnected && _gpsService.CurrentLocation != null)
                {
                    var currentLocation = _gpsService.CurrentLocation;
                    DateTime gpsEndTimestamp = SelectedRecordingMode == RecordingMode.Synchronized ? 
                        TimestampManager.GetPreciseTimestamp() : DateTime.Now;
                    _currentSession.Metadata.Hardware.Gps.End = new GpsPoint
                    {
                        Latitude = currentLocation.Latitude,
                        Longitude = currentLocation.Longitude,
                        Timestamp = gpsEndTimestamp.ToString("yyyy-MM-ddTHH:mm:ss")
                    };
                }

                // GPS 트랙 기록 중지
                if (_isRecordingGps && _gpxWriter != null)
                {
                    await _gpxWriter.StopWritingAsync();
                    _isRecordingGps = false;
                    _gpxWriter = null;
                }
                
                // 센서 데이터 기록 중지
                if (_sensorWriter != null)
                {
                    var sensorSummary = await _sensorWriter.StopAsync();
                    if (sensorSummary != null)
                    {
                        _currentSession.Metadata.Hardware.Sensors.Summary = sensorSummary;
                        _currentSession.Metadata.Hardware.Sensors.TotalSamples = _sensorWriter.DataPointCount;
                        
                        // 동기화 모드인 경우 센서 데이터 파일명 설정
                        if (SelectedRecordingMode == RecordingMode.Synchronized)
                        {
                            var sensorFileName = $"{_currentSession.GenerateFileName(SelectedRecordingSide, "")}_sensors.csv";
                            _currentSession.Metadata.Hardware.Sensors.DataFile = sensorFileName;
                        }
                    }
                    _sensorWriter = null;
                }

                // 비디오 녹화 중지 및 실제 파일 경로 가져오기
                var completedSession = await _videoService.StopRecordingAsync();
                
                // 환경 설정으로 베이스 경로 생성
                var envSettings = new EnvironmentSettings
                {
                    RoadType = SelectedRoadType,
                    Weather = SelectedWeather,
                    TimeOfDay = SelectedTimeOfDay,
                    RecordingSide = SelectedRecordingSide
                };
                string basePath = envSettings.GetFolderPath(_fileService.RootPath);
                
                // 완료된 세션의 파일 경로 업데이트
                _currentSession.UpdateFilePathsOnCompletion(basePath, SelectedRecordingSide);

                // 파일 이름 변경
                try
                {
                    string tempVideoPath = _fileService.GenerateFilePath(envSettings, SelectedRecordingSide, "mp4");
                    if (System.IO.File.Exists(tempVideoPath) && tempVideoPath != _currentSession.VideoFilePath)
                    {
                        System.IO.File.Move(tempVideoPath, _currentSession.VideoFilePath);
                    }
                    
                    // GPX 파일도 이동
                    string tempGpxPath = tempVideoPath.Replace(".mp4", ".gpx");
                    if (System.IO.File.Exists(tempGpxPath) && tempGpxPath != _currentSession.GpxFilePath)
                    {
                        System.IO.File.Move(tempGpxPath, _currentSession.GpxFilePath);
                    }
                }
                catch
                {
                    // 파일 이동 실패 시 기존 경로 유지
                }

                // GPX 파일 정보를 메타데이터에 추가
                if (!string.IsNullOrEmpty(_currentSession.GpxFilePath))
                {
                    var gpxFileName = System.IO.Path.GetFileName(_currentSession.GpxFilePath);
                    _currentSession.Metadata.Hardware.Gps.TrackFile = gpxFileName;
                    _currentSession.Metadata.Hardware.Gps.SampleRate = 1; // 1 Hz
                    
                    // GPX 파일이 존재하면 대략적인 포인트 수 계산 (세션 지속 시간 기준)
                    if (System.IO.File.Exists(_currentSession.GpxFilePath) && _currentSession.EndTime.HasValue)
                    {
                        var duration = _currentSession.EndTime.Value - _currentSession.StartTime;
                        _currentSession.Metadata.Hardware.Gps.TotalPoints = (int)duration.TotalSeconds;
                    }
                }

                // 동기화 모드인 경우 SyncManager 중지 및 동기화 메타데이터 생성
                if (SelectedRecordingMode == RecordingMode.Synchronized)
                {
                    _syncManager.Stop();
                    SyncStatus = "동기화 비활성";
                    IsSyncActive = false;
                    
                    var duration = _currentSession.EndTime.Value - _currentSession.StartTime;
                    var totalFrames = (long)(duration.TotalSeconds * 30); // 30 Hz 기준
                    
                    // SyncManager에서 실제 통계 가져오기
                    var qualityMetrics = _syncManager.GetQualityMetrics();
                    
                    _currentSession.Metadata.Synchronization = new SynchronizationInfo
                    {
                        Method = "software_trigger_30hz",
                        MasterFrequencyHz = 30,
                        TotalSyncPoints = totalFrames,
                        SyncLogFile = $"{_currentSession.GenerateFileName(SelectedRecordingSide, "")}_sync.csv",
                        QualityMetrics = qualityMetrics,
                        
                        // TODO: SyncManager에서 실제 통계 계산 구현 후 교체
                        Statistics = new SyncStatistics
                        {
                            CameraLatency = new LatencyStats { Min = 1.2, Max = 4.5, Average = qualityMetrics.AverageCameraLatencyMs, StandardDeviation = 0.8 },
                            SensorLatency = new LatencyStats { Min = 0.8, Max = 3.2, Average = qualityMetrics.AverageSensorLatencyMs, StandardDeviation = 0.6 },
                            SyncErrorDistribution = new SyncErrorDistribution
                            {
                                Below1Ms = (int)(totalFrames * 0.75),
                                Between1And3Ms = (int)(totalFrames * 0.20),
                                Above3Ms = (int)(totalFrames * 0.05)
                            }
                        }
                    };
                }

                // Save metadata
                RecordingStatus = $"메타데이터 저장: {_currentSession.MetadataFilePath}";
                var metadataResult = await _fileService.SaveMetadataAsync(_currentSession.MetadataFilePath, _currentSession.Metadata);
                
                if (!metadataResult)
                {
                    RecordingStatus = "메타데이터 저장 실패";
                }
                else
                {
                    RecordingStatus = $"녹화 완료 - 파일: {System.IO.Path.GetFileName(_currentSession.VideoFilePath)}";
                }

                IsRecording = false;
                RecordingDuration = TimeSpan.Zero;
                StartRecordingCommand.NotifyCanExecuteChanged();
                StopRecordingCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                RecordingStatus = $"녹화 중지 오류: {ex.Message}";
            }
        }

        private async Task ApplyCameraSettingsAsync()
        {
            try
            {
                await _cameraService.SetExposureModeAsync(SelectedExposureMode);
                if (SelectedExposureMode == ExposureMode.Manual)
                {
                    await _cameraService.SetExposureTimeAsync(ExposureTime);
                }
                await _cameraService.SetWhiteBalanceAsync(SelectedWhiteBalanceMode, WhiteBalanceTemperature);
                RecordingStatus = "카메라 설정 적용 완료";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"설정 적용 오류: {ex.Message}";
            }
        }

        private async Task OnePushWhiteBalanceAsync()
        {
            try
            {
                RecordingStatus = "원푸시 화이트밸런스 실행 중...";
                
                // 임시로 Auto 모드로 설정하여 화이트밸런스 자동 조정 트리거
                await _cameraService.SetWhiteBalanceAsync(WhiteBalanceMode.Auto, WhiteBalanceTemperature);
                await Task.Delay(1000); // 1초 대기하여 자동 조정이 완료되도록 함
                
                // 다시 Manual 모드로 돌아가서 조정된 값 유지
                await _cameraService.SetWhiteBalanceAsync(WhiteBalanceMode.Manual, WhiteBalanceTemperature);
                
                RecordingStatus = "원푸시 화이트밸런스 완료";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"원푸시 화이트밸런스 오류: {ex.Message}";
            }
        }

        private void SetWhiteBalancePreset(string preset)
        {
            if (CameraPresets.WhiteBalancePresets.TryGetValue(preset, out int temperature))
            {
                WhiteBalanceTemperature = temperature;
                _ = ApplyCameraSettingsAsync();
            }
        }

        private void OnFrameReceived(object sender, (byte[] data, int width, int height, bool isColor) frameInfo)
        {
            // FPS 계산
            _frameCount++;
            var currentTime = DateTime.Now;
            var elapsedTime = (currentTime - _lastFrameTime).TotalSeconds;
            
            if (elapsedTime >= 1.0) // 1초마다 FPS 업데이트
            {
                CurrentFps = _frameCount / elapsedTime;
                _frameCount = 0;
                _lastFrameTime = currentTime;
            }

            // 비디오 녹화 처리 (UI와 완전 분리, 최고 우선순위)
            if (IsRecording && _videoService != null)
            {
                // 백그라운드 스레드에서 즉시 처리하여 UI 영향 최소화
                Task.Run(() =>
                {
                    try
                    {
                        // 프레임 데이터를 복사하여 원본 데이터 보호
                        byte[] frameDataCopy = new byte[frameInfo.data.Length];
                        Array.Copy(frameInfo.data, frameDataCopy, frameInfo.data.Length);
                        _videoService.AddFrame(frameDataCopy);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding frame to recording: {ex.Message}");
                    }
                });
            }

            // UI 프리뷰 처리 (프레임 스킵으로 성능 최적화)
            if ((currentTime - _lastUIUpdateTime).TotalMilliseconds < 16)
            {
                return; // 60 FPS로 제한하여 더 부드러운 영상 제공
            }
            _lastUIUpdateTime = currentTime;

            // UI 프리뷰 이미지 처리 (낮은 우선순위)
            Task.Run(() =>
            {
                try
                {
                    PixelFormat pixelFormat;
                    int bytesPerPixel;
                    
                    if (frameInfo.isColor)
                    {
                        // 컬러 이미지: RGB24 포맷
                        pixelFormat = PixelFormats.Rgb24;
                        bytesPerPixel = 3;
                    }
                    else
                    {
                        // 모노 이미지: Gray8 포맷
                        pixelFormat = PixelFormats.Gray8;
                        bytesPerPixel = 1;
                    }
                    
                    int stride = frameInfo.width * bytesPerPixel;
                    int expectedSize = frameInfo.width * frameInfo.height * bytesPerPixel;
                    
                    if (frameInfo.data.Length >= expectedSize)
                    {
                        var bitmap = BitmapSource.Create(
                            frameInfo.width, frameInfo.height, 96, 96, 
                            pixelFormat, null, 
                            frameInfo.data, stride);
                        
                        bitmap.Freeze();
                        
                        // 이미지 변환 적용 (백그라운드에서 처리)
                        var transformedBitmap = ImageTransformations.ApplyTransformationsOptimized(
                            bitmap, SelectedImageRotation, SelectedImageFlip);
                        
                        if (transformedBitmap != bitmap)
                        {
                            transformedBitmap.Freeze();
                        }
                        
                        // UI 스레드에서 UI 업데이트 (더 높은 우선순위로 부드러운 영상 제공)
                        App.Current?.Dispatcher.BeginInvoke(() =>
                        {
                            _originalCameraPreview = bitmap;
                            CameraPreview = transformedBitmap;
                        }, System.Windows.Threading.DispatcherPriority.Render);
                    }
                }
                catch (Exception ex)
                {
                    // 프레임 처리 오류는 빈번할 수 있으므로 Debug 레벨로 로그
                    System.Diagnostics.Debug.WriteLine($"Error processing UI frame: {ex.Message}");
                }
            });
        }

        private void OnTemperatureChanged(object sender, double temperature)
        {
            CameraTemperature = temperature;
        }

        partial void OnSelectedWhiteBalanceModeChanged(WhiteBalanceMode value)
        {
            IsWhiteBalanceManual = value == WhiteBalanceMode.Manual;
            OnePushWhiteBalanceCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedExposureModeChanged(ExposureMode value)
        {
            IsExposureManual = value == ExposureMode.Manual;
        }

        partial void OnSelectedImageRotationChanged(ImageRotation value)
        {
            UpdateImagePreview();
        }

        partial void OnSelectedImageFlipChanged(ImageFlip value)
        {
            UpdateImagePreview();
        }

        partial void OnSelectedRecordingModeChanged(RecordingMode value)
        {
            IsSynchronizedMode = value == RecordingMode.Synchronized;
            
            if (IsSynchronizedMode)
            {
                SyncStatus = IsRecording ? "동기화 활성" : "동기화 준비";
                IsSyncActive = IsRecording;
            }
            else
            {
                SyncStatus = "동기화 비활성";
                IsSyncActive = false;
            }

            // 녹화 중이 아닐 때만 모드 변경 가능하도록 UI 업데이트
            // TODO: 필요시 RecordingMode 변경 불가능한 상황에 대한 처리 추가
        }

        private void UpdateImagePreview()
        {
            if (_originalCameraPreview != null)
            {
                var transformedBitmap = ImageTransformations.ApplyTransformationsOptimized(
                    _originalCameraPreview, SelectedImageRotation, SelectedImageFlip);
                
                if (transformedBitmap != _originalCameraPreview)
                {
                    transformedBitmap.Freeze();
                }
                
                CameraPreview = transformedBitmap;
            }
        }

        private void ChangeSavePath()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "데이터셋 저장 경로 선택";
                dialog.SelectedPath = SavePath;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SavePath = dialog.SelectedPath;
                    _fileService.RootPath = SavePath;
                }
            }
        }

        private void TakeSnapshot()
        {
            try
            {
                // 카메라 연결 상태 확인
                if (!IsCameraConnected)
                {
                    RecordingStatus = "카메라가 연결되지 않음";
                    return;
                }

                // 프리뷰 이미지 확인 (원본과 변환된 이미지 모두 확인)
                BitmapSource imageToSave = CameraPreview ?? _originalCameraPreview;
                
                if (imageToSave == null)
                {
                    RecordingStatus = "저장할 이미지가 없음";
                    return;
                }

                // 저장 경로 확인 및 생성
                if (string.IsNullOrEmpty(SavePath) || !System.IO.Directory.Exists(SavePath))
                {
                    RecordingStatus = "저장 경로가 유효하지 않음";
                    return;
                }

                // 현재 프리뷰 이미지를 파일로 저장
                string fileName = $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = System.IO.Path.Combine(SavePath, fileName);
                
                // 이미지가 Frozen 상태가 아니면 Freeze
                if (!imageToSave.IsFrozen)
                {
                    imageToSave.Freeze();
                }
                
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(imageToSave));
                
                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    encoder.Save(stream);
                }
                
                // 파일 크기 확인
                var fileInfo = new System.IO.FileInfo(filePath);
                RecordingStatus = $"스냅샷 저장: {fileName} ({fileInfo.Length / 1024:F1}KB)";
            }
            catch (UnauthorizedAccessException)
            {
                RecordingStatus = "스냅샷 저장 실패: 파일 접근 권한 없음";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"스냅샷 저장 실패: {ex.Message}";
            }
        }

        // GPS Methods
        private void RefreshGpsPorts()
        {
            try
            {
                var ports = System.IO.Ports.SerialPort.GetPortNames();
                AvailableGpsPorts.Clear();
                foreach (var port in ports.OrderBy(p => p))
                {
                    AvailableGpsPorts.Add(port);
                }
                
                // Select first port if available
                if (AvailableGpsPorts.Count > 0 && string.IsNullOrEmpty(SelectedGpsPort))
                {
                    SelectedGpsPort = AvailableGpsPorts[0];
                }
            }
            catch (Exception ex)
            {
                GpsStatus = $"포트 조회 실패: {ex.Message}";
            }
        }

        private async Task ConnectGpsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedGpsPort))
                {
                    GpsStatus = "GPS 포트를 선택하세요";
                    return;
                }

                GpsStatus = $"{SelectedGpsPort} 연결 중...";
                
                var connected = await _gpsService.ConnectAsync(SelectedGpsPort, 9600);
                
                if (connected)
                {
                    IsGpsConnected = true;
                    GpsStatus = "GPS 연결됨 (신호 대기 중)";
                    ConnectGpsCommand.NotifyCanExecuteChanged();
                    DisconnectGpsCommand.NotifyCanExecuteChanged();
                }
                else
                {
                    GpsStatus = $"{SelectedGpsPort} 연결 실패";
                }
            }
            catch (Exception ex)
            {
                GpsStatus = $"GPS 연결 오류: {ex.Message}";
            }
        }

        private async Task DisconnectGpsAsync()
        {
            try
            {
                await _gpsService.DisconnectAsync();
                IsGpsConnected = false;
                CurrentGpsLocation = null;
                GpsStatus = "GPS 미연결";
                ConnectGpsCommand.NotifyCanExecuteChanged();
                DisconnectGpsCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                GpsStatus = $"GPS 해제 오류: {ex.Message}";
            }
        }

        private void OnGpsLocationUpdated(object sender, GpsPoint location)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                CurrentGpsLocation = location;
                if (location != null && location.Latitude.HasValue && location.Longitude.HasValue)
                {
                    GpsStatus = $"GPS 수신 중: {location.Latitude:F6}, {location.Longitude:F6}";
                    
                    // GPX 파일에 GPS 포인트 추가 (녹화 중인 경우)
                    if (_isRecordingGps && _gpxWriter != null)
                    {
                        await _gpxWriter.AddTrackPointAsync(location);
                    }
                }
            });
        }

        partial void OnSelectedGpsPortChanged(string value)
        {
            ConnectGpsCommand.NotifyCanExecuteChanged();
        }

        // Sensor Methods
        private void RefreshSensorPorts()
        {
            try
            {
                var ports = System.IO.Ports.SerialPort.GetPortNames();
                AvailableSensorPorts.Clear();
                foreach (var port in ports.OrderBy(p => p))
                {
                    AvailableSensorPorts.Add(port);
                }
                
                // Select first available port that's not used by GPS
                if (AvailableSensorPorts.Count > 0 && string.IsNullOrEmpty(SelectedSensorPort))
                {
                    var availablePort = AvailableSensorPorts.FirstOrDefault(p => p != SelectedGpsPort);
                    if (availablePort != null)
                    {
                        SelectedSensorPort = availablePort;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        private async Task ConnectSensorAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedSensorPort))
                {
                    return;
                }

                var connected = await _sensorService.ConnectAsync(SelectedSensorPort, SelectedBaudrate);
                
                if (connected)
                {
                    IsSensorConnected = true;
                    ConnectSensorCommand.NotifyCanExecuteChanged();
                    DisconnectSensorCommand.NotifyCanExecuteChanged();
                }
            }
            catch (Exception ex)
            {
                IsSensorConnected = false;
            }
        }

        private async Task DisconnectSensorAsync()
        {
            try
            {
                await _sensorService.DisconnectAsync();
                IsSensorConnected = false;
                
                // Reset sensor values
                LightLevel = 0;
                Temperature = 0;
                Humidity = 0;
                AccelX = AccelY = AccelZ = 0;
                GyroX = GyroY = GyroZ = 0;
                MagX = MagY = MagZ = 0;
                Roll = Pitch = Yaw = 0;
                
                ConnectSensorCommand.NotifyCanExecuteChanged();
                DisconnectSensorCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                // Log error
            }
        }

        private void OnSensorDataUpdated(object sender, SensorData data)
        {
            if (data == null) return;
            
            // Update UI properties
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LightLevel = data.LightLevel;
                Temperature = data.Temperature;
                Humidity = data.Humidity;
                AccelX = data.Imu.Acceleration.X;
                AccelY = data.Imu.Acceleration.Y;
                AccelZ = data.Imu.Acceleration.Z;
                GyroX = data.Imu.Gyroscope.X;
                GyroY = data.Imu.Gyroscope.Y;
                GyroZ = data.Imu.Gyroscope.Z;
                MagX = data.Imu.Magnetometer.X;
                MagY = data.Imu.Magnetometer.Y;
                MagZ = data.Imu.Magnetometer.Z;
                Roll = data.Imu.Euler.Roll;
                Pitch = data.Imu.Euler.Pitch;
                Yaw = data.Imu.Euler.Yaw;
            });
            
            // Write to sensor data file if recording
            if (_sensorWriter != null && IsRecording)
            {
                Task.Run(async () =>
                {
                    await _sensorWriter.AddDataPointAsync(data);
                });
            }
        }

        partial void OnSelectedSensorPortChanged(string value)
        {
            ConnectSensorCommand.NotifyCanExecuteChanged();
        }

        private void OnSyncTriggered(object sender, SyncTriggerEventArgs e)
        {
            // 동기화 트리거 이벤트 처리 - 로깅 및 상태 업데이트
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (IsSyncActive)
                {
                    SyncStatus = $"동기화 활성 (Seq: {e.SequenceNumber})";
                }
            });
        }

        public void Dispose()
        {
            _syncManager?.Dispose();
        }
    }
}