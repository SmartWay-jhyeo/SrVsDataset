using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using OpenCvSharp;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;

namespace SrVsDataset.Services
{
    /// <summary>
    /// OpenCV를 사용한 비디오 녹화 서비스 - MP4/H.264 형식으로 저장
    /// </summary>
    public class VideoRecordingService : IVideoRecordingService, IDisposable
    {
        private VideoWriter _videoWriter;
        private bool _isRecording = false;
        private string _currentFilePath;
        private int _frameWidth = 1920;
        private int _frameHeight = 1080;
        private int _fps = 30;
        private RecordingSession _currentSession;
        private readonly ILoggingService _logger;
        private DateTime _lastFrameTime = DateTime.Now;
        private int _framesSinceLastCheck = 0;
        
        // 비동기 프레임 처리용
        private readonly ConcurrentQueue<Mat> _frameQueue;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _frameProcessingTask;
        private readonly object _lockObject = new object();

        public event EventHandler<RecordingSession> RecordingStarted;
        public event EventHandler<RecordingSession> RecordingStopped;
        public event EventHandler<long> FileSizeUpdated;

        public bool IsRecording => _isRecording;
        public RecordingSession CurrentSession => _currentSession;

        public VideoRecordingService(ILoggingService logger = null)
        {
            _logger = logger ?? new LoggingService();
            _frameQueue = new ConcurrentQueue<Mat>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<bool> StartRecordingAsync(string filePath, int frameRate = 30)
        {
            return await StartRecordingAsync(filePath, _frameWidth, _frameHeight, frameRate);
        }

        public async Task<bool> StartRecordingAsync(string filePath, int width, int height, int frameRate = 30)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        if (_isRecording)
                        {
                            _logger.LogWarning("Recording is already in progress");
                            return false;
                        }

                        // 이전 리소스가 남아있다면 정리
                        CleanupResources();

                        _currentFilePath = filePath;
                        _frameWidth = width;
                        _frameHeight = height;
                        _fps = frameRate;

                        // MP4 파일로 저장 (확장자가 없거나 다른 경우 MP4로 변경)
                        string mp4Path = Path.GetExtension(filePath).ToLower() == ".mp4" 
                            ? filePath 
                            : Path.ChangeExtension(filePath, ".mp4");
                        _currentFilePath = mp4Path;
                        
                        _logger.LogInfo($"Starting video recording: {mp4Path} ({width}x{height} @ {frameRate}fps)");
                        
                        // 디렉터리가 존재하지 않으면 생성
                        string directory = Path.GetDirectoryName(mp4Path);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        // OpenCV VideoWriter 초기화 - MJPG 코덱 사용 (안정성 향상)
                        var fourcc = VideoWriter.FourCC('M', 'J', 'P', 'G'); // MJPG 코덱
                        _videoWriter = new VideoWriter(mp4Path, fourcc, frameRate, new OpenCvSharp.Size(width, height), true);
                        
                        if (!_videoWriter.IsOpened())
                        {
                            _logger.LogError("Failed to open VideoWriter");
                            _videoWriter?.Dispose();
                            _videoWriter = null;
                            return false;
                        }
                        
                        // 새로운 CancellationTokenSource 생성 (이전 것이 취소되었을 수 있음)
                        if (_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource.Dispose();
                            _cancellationTokenSource = new CancellationTokenSource();
                        }
                        
                        _currentSession = new RecordingSession();
                        _currentSession.VideoFilePath = mp4Path;
                        _isRecording = true;
                        
                        // 프레임 처리 태스크 시작
                        _frameProcessingTask = Task.Run(ProcessFramesAsync, _cancellationTokenSource.Token);
                        
                        RecordingStarted?.Invoke(this, _currentSession);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error starting video recording", ex);
                    CleanupResources();
                    return false;
                }
            });
        }

        public void AddFrame(byte[] frameData)
        {
            if (!_isRecording || frameData == null) return;

            try
            {
                // 프레임 스킵 로직 (타겟 FPS에 맞춰 조절)
                var currentTime = DateTime.Now;
                var timeSinceLastFrame = (currentTime - _lastFrameTime).TotalMilliseconds;
                var targetFrameInterval = 1000.0 / _fps; // 목표 프레임 간격 (ms)
                
                _framesSinceLastCheck++;
                
                // 너무 빠른 프레임은 스킵 (FPS 제한)
                if (timeSinceLastFrame < targetFrameInterval * 0.8) // 80% 여유를 둠
                {
                    return; // 프레임 스킵
                }
                
                _lastFrameTime = currentTime;

                // RGB24 데이터를 OpenCV Mat으로 변환
                Mat frame = new Mat(_frameHeight, _frameWidth, MatType.CV_8UC3);
                Marshal.Copy(frameData, 0, frame.Data, frameData.Length);
                
                // BGR로 변환 (OpenCV는 BGR 사용)
                Mat bgrFrame = new Mat();
                Cv2.CvtColor(frame, bgrFrame, ColorConversionCodes.RGB2BGR);
                
                // 프레임을 큐에 추가 (비동기 처리)
                _frameQueue.Enqueue(bgrFrame.Clone());
                
                frame.Dispose();
                bgrFrame.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding frame to queue", ex);
            }
        }

        private async Task ProcessFramesAsync()
        {
            int frameCount = 0;
            var token = _cancellationTokenSource.Token;
            
            try
            {
                while (!token.IsCancellationRequested && _isRecording)
                {
                    if (_frameQueue.TryDequeue(out Mat frame))
                    {
                        try
                        {
                            _videoWriter?.Write(frame);
                            frameCount++;
                            
                            // 파일 크기 업데이트 (30프레임마다)
                            if (frameCount % 30 == 0)
                            {
                                try
                                {
                                    FileInfo fi = new FileInfo(_currentFilePath);
                                    if (fi.Exists)
                                    {
                                        FileSizeUpdated?.Invoke(this, fi.Length);
                                    }
                                }
                                catch
                                {
                                    // 파일 크기 확인 실패는 무시
                                }
                            }
                        }
                        finally
                        {
                            frame?.Dispose();
                        }
                    }
                    else
                    {
                        // 큐가 비어있으면 잠시 대기
                        await Task.Delay(1, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 정상적인 취소
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in frame processing task", ex);
            }
            finally
            {
                // 남은 프레임들 처리
                while (_frameQueue.TryDequeue(out Mat frame))
                {
                    try
                    {
                        _videoWriter?.Write(frame);
                    }
                    finally
                    {
                        frame?.Dispose();
                    }
                }
            }
        }

        public async Task<RecordingSession> StopRecordingAsync()
        {
            if (!_isRecording) return null;

            return await Task.Run(async () =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        _isRecording = false;
                    }
                    
                    // 프레임 처리 태스크 완료 대기
                    _cancellationTokenSource.Cancel();
                    if (_frameProcessingTask != null)
                    {
                        try
                        {
                            await _frameProcessingTask.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // 정상적인 취소
                        }
                    }
                    
                    // VideoWriter 종료
                    _videoWriter?.Release();
                    _videoWriter?.Dispose();
                    _videoWriter = null;
                    
                    if (_currentSession != null)
                    {
                        _currentSession.EndTime = DateTime.Now;
                        _currentSession.VideoFilePath = _currentFilePath;
                        
                        // 파일 존재 여부 및 크기 확인
                        if (!System.IO.File.Exists(_currentFilePath))
                        {
                            _logger.LogWarning($"Video file not found at {_currentFilePath}");
                        }
                        else
                        {
                            FileInfo fi = new FileInfo(_currentFilePath);
                            _logger.LogInfo($"Video recording completed: {_currentFilePath} (Size: {fi.Length / 1024 / 1024:F2} MB)");
                            FileSizeUpdated?.Invoke(this, fi.Length);
                        }
                        
                        RecordingStopped?.Invoke(this, _currentSession);
                    }
                    
                    return _currentSession;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error stopping recording", ex);
                    return null;
                }
            });
        }

        private void CleanupResources()
        {
            try
            {
                // VideoWriter 정리
                if (_videoWriter != null)
                {
                    _videoWriter.Release();
                    _videoWriter.Dispose();
                    _videoWriter = null;
                }

                // 프레임 처리 태스크 정리
                if (_frameProcessingTask != null && !_frameProcessingTask.IsCompleted)
                {
                    _cancellationTokenSource?.Cancel();
                    try
                    {
                        _frameProcessingTask.Wait(2000); // 2초 타임아웃
                    }
                    catch (AggregateException)
                    {
                        // 타임아웃 또는 취소된 경우 무시
                    }
                }

                // 큐에 남은 프레임들 정리
                while (_frameQueue.TryDequeue(out Mat frame))
                {
                    frame?.Dispose();
                }
                
                _logger.LogInfo("Video recording resources cleaned up");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error cleaning up video recording resources", ex);
            }
        }

        public void Dispose()
        {
            try
            {
                if (_isRecording)
                {
                    StopRecordingAsync().Wait(5000); // 5초 타임아웃
                }
                
                CleanupResources();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error disposing VideoRecordingService", ex);  
            }
        }
    }
}