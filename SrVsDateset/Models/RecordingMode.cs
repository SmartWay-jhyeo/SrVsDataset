namespace SrVsDataset.Models
{
    /// <summary>
    /// 녹화 모드 정의
    /// </summary>
    public enum RecordingMode
    {
        /// <summary>
        /// 연속 모드 (기존 방식)
        /// 각 센서가 독립적으로 데이터를 수집
        /// 타임스탬프 기반 느슨한 동기화
        /// </summary>
        Continuous = 0,
        
        /// <summary>
        /// 동기화 모드 (새로운 방식)
        /// 30Hz 소프트웨어 트리거 기반 정밀 동기화
        /// 모든 센서가 동일한 시점에 트리거됨
        /// </summary>
        Synchronized = 1
    }
    
    /// <summary>
    /// RecordingMode 확장 메서드
    /// </summary>
    public static class RecordingModeExtensions
    {
        /// <summary>
        /// 모드의 표시 이름 반환
        /// </summary>
        public static string GetDisplayName(this RecordingMode mode)
        {
            return mode switch
            {
                RecordingMode.Continuous => "연속 모드 (기존)",
                RecordingMode.Synchronized => "동기화 모드 (30Hz)",
                _ => mode.ToString()
            };
        }
        
        /// <summary>
        /// 메타데이터에 저장될 문자열 반환
        /// </summary>
        public static string GetMetadataString(this RecordingMode mode)
        {
            return mode switch
            {
                RecordingMode.Continuous => "continuous",
                RecordingMode.Synchronized => "synchronized",
                _ => "unknown"
            };
        }
        
        /// <summary>
        /// 메타데이터 문자열로부터 RecordingMode 파싱
        /// </summary>
        public static RecordingMode FromMetadataString(string modeString)
        {
            return modeString?.ToLower() switch
            {
                "continuous" => RecordingMode.Continuous,
                "synchronized" => RecordingMode.Synchronized,
                _ => RecordingMode.Continuous // 기본값
            };
        }
    }
}