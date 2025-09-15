
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;
using MVSDK;
using System.Windows.Media;
using System.Windows;
using CameraHandle = System.Int32;

namespace SrVsDataset.Services
{
    /// <summary>
    /// MVSDK를 사용한 실제 카메라 서비스 구현
    /// </summary>
    public class MVCameraService : ICameraService, IDisposable
    {
        private CameraHandle _hCamera = 0;  // 카메라 핸들
        private IntPtr _imageBuffer;  // 이미지 버퍼
        private tSdkCameraCapbility _cameraCapability;  // 카메라 특성
        private CameraSettings _currentSettings;  // 현재 카메라 설정
        private bool _isConnected = false;  // 카메라 연결 상태
        private bool _isCapturing = false;  // 캡처 진행 상태
        private Task _captureTask;  // 캡처 작업
        private int _currentWidth = 0;  // 현재 이미지 너비
        private int _currentHeight = 0;  // 현재 이미지 높이
        private readonly ILoggingService _logger;

        public event EventHandler<(byte[] data, int width, int height, bool isColor)> FrameReceived;
        public event EventHandler<double> TemperatureChanged;

        public bool IsConnected => _isConnected;
        public CameraSettings CurrentSettings => _currentSettings;
        public int CurrentWidth => _currentWidth;
        public int CurrentHeight => _currentHeight;

        public MVCameraService(ILoggingService logger = null)
        {
            _currentSettings = new CameraSettings();
            _logger = logger ?? new LoggingService();
        }

        public async Task<bool> InitializeAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // SDK 초기화
                    CameraSdkStatus status = MvApi.CameraSdkInit(0);
                    if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                        return false;

                    // 카메라 목록 가져오기
                    tSdkCameraDevInfo[] tCameraDevInfoList = null;
                    int iCameraCounts = 12;
                    status = MvApi.CameraEnumerateDevice(out tCameraDevInfoList);
                    if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS || tCameraDevInfoList == null || tCameraDevInfoList.Length == 0)
                        return false;

                    // 첫 번째 카메라 초기화
                    status = MvApi.CameraInit(ref tCameraDevInfoList[0], -1, -1, ref _hCamera);
                    if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                        return false;

                    // 카메라 특성 가져오기
                    MvApi.CameraGetCapability(_hCamera, out _cameraCapability);

                    // 현재 해상도 설정 (최대 해상도 사용)
                    _currentWidth = _cameraCapability.sResolutionRange.iWidthMax;
                    _currentHeight = _cameraCapability.sResolutionRange.iHeightMax;

                    // 이미지 버퍼 할당 (RGB24 형식으로 3바이트 per 픽셀)
                    int bufferSize = _currentWidth * _currentHeight * 3 + 1024;
                    _imageBuffer = Marshal.AllocHGlobal(bufferSize);

                    // 컬러 카메라인지 확인하여 출력 형식 설정 (RGB24로 통일)
                    if (_cameraCapability.sIspCapacity.bMonoSensor == 0)
                    {
                        // 컬러 카메라: RGB24 포맷으로 설정
                        MvApi.CameraSetIspOutFormat(_hCamera, (uint)emImageFormat.CAMERA_MEDIA_TYPE_RGB8);
                    }
                    else
                    {
                        // 모노 카메라: MONO8 포맷으로 설정
                        MvApi.CameraSetIspOutFormat(_hCamera, (uint)emImageFormat.CAMERA_MEDIA_TYPE_MONO8);
                    }

                    _isConnected = true;
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> StartPreviewAsync()
        {
            if (!IsConnected) return false;

            return await Task.Run(() =>
            {
                try
                {
                    // 카메라 재생 모드 시작
                    CameraSdkStatus status = MvApi.CameraPlay(_hCamera);
                    if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                        return false;

                    _isCapturing = true;

                    // 프레임 캡처 태스크 시작
                    _captureTask = Task.Run(async () =>
                    {
                        var lastTemperatureRead = DateTime.Now;
                        while (_isCapturing)
                        {
                            await CaptureFrame();
                            
                            // 온도는 1초마다만 읽기 (성능 최적화)
                            if ((DateTime.Now - lastTemperatureRead).TotalMilliseconds > 1000)
                            {
                                await ReadTemperature();
                                lastTemperatureRead = DateTime.Now;
                            }
                            
                            // 동적 FPS 조정 (프레임 버퍼 상태에 따라)
                            await Task.Delay(16); // 약 60 FPS 시도, 실제로는 카메라 성능에 의존
                        }
                    });

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        private async Task CaptureFrame()
        {
            await Task.Run(() =>
            {
                tSdkFrameHead sFrameInfo;
                IntPtr pRawBuffer = IntPtr.Zero;
                
                try
                {
                    // 카메라 연결 상태 확인
                    if (!_isConnected || _hCamera == 0)
                    {
                        return;
                    }

                    // 프레임 가져오기 (100ms 타임아웃으로 단축하여 응답성 향상)
                    CameraSdkStatus status = MvApi.CameraGetImageBuffer(_hCamera, out sFrameInfo, out pRawBuffer, 100);
                    if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    {
                        // 해상도가 변경되었는지 확인
                        if (sFrameInfo.iWidth != _currentWidth || sFrameInfo.iHeight != _currentHeight)
                        {
                            _currentWidth = sFrameInfo.iWidth;
                            _currentHeight = sFrameInfo.iHeight;
                            _logger.LogInfo($"Camera resolution changed to {_currentWidth}x{_currentHeight}");
                        }

                        // 유효한 프레임 데이터인지 확인
                        if (sFrameInfo.iWidth <= 0 || sFrameInfo.iHeight <= 0 || pRawBuffer == IntPtr.Zero)
                        {
                            _logger.LogWarning("Invalid frame data received");
                            return;
                        }

                        // RGB 이미지로 변환 (ISP 처리 포함)
                        CameraSdkStatus processStatus = MvApi.CameraImageProcess(_hCamera, pRawBuffer, _imageBuffer, ref sFrameInfo);
                        if (processStatus != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                        {
                            _logger.LogWarning($"Image processing failed: {processStatus}");
                            return;
                        }

                        // 이미지 크기 계산
                        int bytesPerPixel = _cameraCapability.sIspCapacity.bMonoSensor == 0 ? 3 : 1; // 컬러: 3바이트, 모노: 1바이트
                        int imageSize = sFrameInfo.iWidth * sFrameInfo.iHeight * bytesPerPixel;
                        
                        // 안전한 크기 확인
                        if (imageSize <= 0 || imageSize > 50 * 1024 * 1024) // 50MB 제한
                        {
                            _logger.LogWarning($"Invalid image size: {imageSize}");
                            return;
                        }
                        
                        // 바이트 배열로 변환
                        byte[] imageData = new byte[imageSize];
                        Marshal.Copy(_imageBuffer, imageData, 0, imageSize);

                        // 프레임 정보와 함께 이벤트 발생
                        bool isColor = _cameraCapability.sIspCapacity.bMonoSensor == 0;
                        
                        // 이벤트 발생을 try-catch로 감싸서 구독자 오류가 프레임 캡처를 중단하지 않도록 함
                        try
                        {
                            FrameReceived?.Invoke(this, (imageData, sFrameInfo.iWidth, sFrameInfo.iHeight, isColor));
                        }
                        catch (Exception eventEx)
                        {
                            _logger.LogError("Error in FrameReceived event handler", eventEx);
                        }
                    }
                    else if (status == CameraSdkStatus.CAMERA_STATUS_TIME_OUT)
                    {
                        // 타임아웃은 정상적인 상황이므로 로그 없이 넘어감
                    }
                    else
                    {
                        _logger.LogWarning($"Frame capture failed: {status}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception in CaptureFrame", ex);
                }
                finally
                {
                    // 버퍼 해제 (항상 실행되도록 finally 블록에서 처리)
                    if (pRawBuffer != IntPtr.Zero)
                    {
                        try
                        {
                            MvApi.CameraReleaseImageBuffer(_hCamera, pRawBuffer);
                        }
                        catch (Exception releaseEx)
                        {
                            _logger.LogError("Error releasing image buffer", releaseEx);
                        }
                    }
                }
            });
        }

        private async Task ReadTemperature()
        {
            await Task.Run(() =>
            {
                try
                {
                    float temperature = 0;
                    
                    // CameraSpecialControl을 사용하여 온도 읽기 (제조사 제공 방법)
                    // dwCtrlCode = 20: 온도 읽기 명령
                    // dwParam = 0: 추가 매개변수 없음
                    IntPtr pTemperature = Marshal.AllocHGlobal(sizeof(float));
                    
                    CameraSdkStatus status = MvApi.CameraSpecialControl(_hCamera, 20, 0, pTemperature);
                    if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    {
                        temperature = Marshal.PtrToStructure<float>(pTemperature);
                    }
                    
                    Marshal.FreeHGlobal(pTemperature);
                    TemperatureChanged?.Invoke(this, temperature);
                }
                catch { /* 온도 읽기 오류 무시 */ }
            });
        }

        public async Task<bool> StopPreviewAsync()
        {
            if (!IsConnected) return false;

            return await Task.Run(() =>
            {
                try
                {
                    _isCapturing = false;
                    _captureTask?.Wait(1000);
                    
                    MvApi.CameraPause(_hCamera);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> SetExposureTimeAsync(int microseconds)
        {
            if (!IsConnected) return false;

            return await Task.Run(() =>
            {
                try
                {
                    // Manual 모드로 설정 후 Exposure Time 적용 (Sample 코드 패턴)
                    MvApi.CameraSetAeState(_hCamera, 0); // 0 = Manual, 1 = Auto
                    MvApi.CameraSetExposureTime(_hCamera, (double)microseconds);
                    
                    _currentSettings.ExposureTime = microseconds;
                    _currentSettings.ExposureMode = ExposureMode.Manual;
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> SetExposureModeAsync(ExposureMode mode)
        {
            if (!IsConnected) return false;

            return await Task.Run(() =>
            {
                try
                {
                    uint aeState = mode == ExposureMode.Auto ? 1u : 0u;
                    MvApi.CameraSetAeState(_hCamera, aeState);
                    _currentSettings.ExposureMode = mode;
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> SetWhiteBalanceAsync(WhiteBalanceMode mode, int temperature)
        {
            if (!IsConnected) return false;

            return await Task.Run(() =>
            {
                try
                {
                    if (mode == WhiteBalanceMode.Auto)
                    {
                        // 자동 화이트밸런스 설정
                        MvApi.CameraSetWbMode(_hCamera, 1); // 1 = 자동
                    }
                    else
                    {
                        // 수동 화이트밸런스 설정
                        MvApi.CameraSetWbMode(_hCamera, 0); // 0 = 수동
                        
                        // 색온도 설정을 위한 RGB 게인 계산
                        int r, g, b;
                        CalculateRGBGainsFromTemperature(temperature, out r, out g, out b);
                        
                        MvApi.CameraSetGain(_hCamera, r, g, b);
                    }

                    _currentSettings.WhiteBalanceMode = mode;
                    _currentSettings.WhiteBalanceTemperature = temperature;
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// 색온도를 RGB 게인 값으로 변환
        /// </summary>
        private void CalculateRGBGainsFromTemperature(int temperature, out int r, out int g, out int b)
        {
            // 간단한 색온도 -> RGB 게인 변환
            // 실제로는 더 정교한 변환이 필요할 수 있음
            g = 100; // 녹색 기준값
            
            if (temperature < 5600)
            {
                // 따뜻한 색 (낮은 색온도)
                r = 120;
                b = 80;
            }
            else if (temperature > 5600)
            {
                // 차가운 색 (높은 색온도)
                r = 80;
                b = 120;
            }
            else
            {
                // 중성 색온도 (5600K)
                r = 100;
                b = 100;
            }
        }

        public async Task<CameraSettings> GetCurrentSettingsAsync()
        {
            if (!IsConnected) return _currentSettings;

            return await Task.Run(() =>
            {
                try
                {
                    // 현재 노출 시간 가져오기 (마이크로초 단위)
                    double exposureTimeMicros = 0;
                    MvApi.CameraGetExposureTime(_hCamera, ref exposureTimeMicros);
                    _currentSettings.ExposureTime = (int)exposureTimeMicros;

                    return _currentSettings;
                }
                catch
                {
                    return _currentSettings;
                }
            });
        }

        public async Task<double> GetTemperatureAsync()
        {
            if (!IsConnected) return 0;

            return await Task.Run(() =>
            {
                try
                {
                    float temperature = 0;
                    
                    // CameraSpecialControl을 사용하여 온도 읽기 (제조사 제공 방법)
                    IntPtr pTemperature = Marshal.AllocHGlobal(sizeof(float));
                    
                    CameraSdkStatus status = MvApi.CameraSpecialControl(_hCamera, 20, 0, pTemperature);
                    if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                    {
                        temperature = Marshal.PtrToStructure<float>(pTemperature);
                    }
                    
                    Marshal.FreeHGlobal(pTemperature);
                    return (double)temperature;
                }
                catch
                {
                    return 0;
                }
            });
        }

        public async Task<bool> DisconnectAsync()
        {
            if (!IsConnected) return true;

            return await Task.Run(() =>
            {
                try
                {
                    _isCapturing = false;
                    _captureTask?.Wait(1000);

                    if (_hCamera != 0)
                    {
                        MvApi.CameraPause(_hCamera);
                        MvApi.CameraUnInit(_hCamera);
                        _hCamera = 0;
                    }

                    if (_imageBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_imageBuffer);
                        _imageBuffer = IntPtr.Zero;
                    }

                    _isConnected = false;
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// 현재 프레임 데이터 반환 (향후 구현 예정)
        /// </summary>
        public byte[] GetCurrentFrame()
        {
            // 현재 프레임 반환 (필요시 구현)
            return new byte[0];
        }

        public void Dispose()
        {
            if (_isConnected)
            {
                _isCapturing = false;
                _captureTask?.Wait(1000);
                
                if (_hCamera != 0)
                {
                    MvApi.CameraUnInit(_hCamera);
                    _hCamera = 0;
                }

                if (_imageBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_imageBuffer);
                    _imageBuffer = IntPtr.Zero;
                }

                _isConnected = false;
            }
        }

        public async Task TriggerSoftwareCaptureAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_hCamera != 0 && _isConnected)
                    {
                        // Software trigger implementation for MVSDK
                        // This would trigger a single frame capture
                        MvApi.CameraSoftTrigger(_hCamera);
                        _logger.LogDebug("Software trigger executed");
                    }
                    else
                    {
                        _logger.LogWarning("Cannot trigger software capture: camera not connected");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error executing software trigger: {ex.Message}");
                }
            });
        }
    }
}
