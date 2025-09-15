using System;
using System.Diagnostics;

namespace SrVsDataset.Models
{
    /// <summary>
    /// 고해상도 타임스탬프 관리 클래스
    /// 모든 센서 데이터의 통일된 시각 기준을 제공
    /// </summary>
    public static class TimestampManager
    {
        private static readonly Stopwatch _highResTimer = Stopwatch.StartNew();
        private static readonly DateTime _baseTime = DateTime.Now;
        
        /// <summary>
        /// 마이크로초 정밀도의 현재 시각을 반환
        /// </summary>
        public static DateTime GetPreciseTimestamp()
        {
            return _baseTime.AddTicks(_highResTimer.Elapsed.Ticks);
        }
        
        /// <summary>
        /// 마이크로초 포함 타임스탬프 문자열 반환
        /// 형식: yyyy-MM-ddTHH:mm:ss.ffffff
        /// </summary>
        public static string GetTimestampString()
        {
            return GetPreciseTimestamp().ToString("yyyy-MM-ddTHH:mm:ss.ffffff");
        }
        
        /// <summary>
        /// Unix 타임스탬프 (밀리초) 반환
        /// 아두이노 통신에 사용
        /// </summary>
        public static long GetUnixTimestampMs()
        {
            return ((DateTimeOffset)GetPreciseTimestamp()).ToUnixTimeMilliseconds();
        }
        
        /// <summary>
        /// Unix 타임스탬프 (마이크로초) 반환
        /// 고정밀도 동기화에 사용
        /// </summary>
        public static long GetUnixTimestampUs()
        {
            return ((DateTimeOffset)GetPreciseTimestamp()).ToUnixTimeMilliseconds() * 1000 +
                   (GetPreciseTimestamp().Ticks % TimeSpan.TicksPerMillisecond) / (TimeSpan.TicksPerMillisecond / 1000);
        }
        
        /// <summary>
        /// 타이머 리셋 (새로운 녹화 세션 시작 시 사용)
        /// </summary>
        public static void Reset()
        {
            _highResTimer.Restart();
        }
        
        /// <summary>
        /// 현재 타이머 경과 시간 (밀리초)
        /// </summary>
        public static double ElapsedMilliseconds => _highResTimer.Elapsed.TotalMilliseconds;
    }
}