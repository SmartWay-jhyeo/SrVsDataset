using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SrVsDataset.Models
{
    public class RecordingMetadata
    {
        [JsonProperty("timestamp")]
        public TimestampInfo Timestamp { get; set; }

        [JsonProperty("environment")]
        public EnvironmentInfo Environment { get; set; }

        [JsonProperty("camera_settings")]
        public CameraSettingsInfo CameraSettings { get; set; }

        [JsonProperty("hardware")]
        public HardwareInfo Hardware { get; set; }

        [JsonProperty("recording_mode")]
        public string RecordingMode { get; set; } = "continuous"; // 기본값: 연속 모드

        [JsonProperty("synchronization", NullValueHandling = NullValueHandling.Ignore)]
        public SynchronizationInfo Synchronization { get; set; } // 동기화 모드에서만 사용

        public RecordingMetadata()
        {
            Timestamp = new TimestampInfo();
            Environment = new EnvironmentInfo();
            CameraSettings = new CameraSettingsInfo();
            Hardware = new HardwareInfo();
        }
    }

    public class TimestampInfo
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("start_time")]
        public string StartTime { get; set; }

        [JsonProperty("end_time")]
        public string EndTime { get; set; }
    }

    public class EnvironmentInfo
    {
        [JsonProperty("road_type")]
        public string RoadType { get; set; }

        [JsonProperty("weather")]
        public string Weather { get; set; }

        [JsonProperty("time_of_day")]
        public string TimeOfDay { get; set; }

        [JsonProperty("recording_side")]
        public string RecordingSide { get; set; }
    }

    public class CameraSettingsInfo
    {
        [JsonProperty("exposure_time")]
        public int ExposureTime { get; set; }

        [JsonProperty("white_balance_mode")]
        public string WhiteBalanceMode { get; set; }

        [JsonProperty("white_balance_temperature")]
        public int WhiteBalanceTemperature { get; set; }
    }

    public class HardwareInfo
    {
        [JsonProperty("camera_temperature")]
        public double CameraTemperature { get; set; }

        [JsonProperty("gps")]
        public GpsInfo Gps { get; set; }

        [JsonProperty("sensors")]
        public SensorInfo Sensors { get; set; }

        public HardwareInfo()
        {
            Gps = new GpsInfo();
            Sensors = new SensorInfo();
        }
    }

    public class SensorInfo
    {
        [JsonProperty("sample_rate")]
        public int SampleRate { get; set; } = 1; // Hz

        [JsonProperty("total_samples")]
        public int? TotalSamples { get; set; }

        [JsonProperty("data_file")]
        public string DataFile { get; set; }

        [JsonProperty("summary")]
        public SensorSummary Summary { get; set; }

        public SensorInfo()
        {
            Summary = new SensorSummary();
        }
    }

    public class SensorSummary
    {
        [JsonProperty("temperature")]
        public SensorRange Temperature { get; set; }

        [JsonProperty("humidity")]
        public SensorRange Humidity { get; set; }

        [JsonProperty("light_level")]
        public SensorRange LightLevel { get; set; }

        public SensorSummary()
        {
            Temperature = new SensorRange();
            Humidity = new SensorRange();
            LightLevel = new SensorRange();
        }
    }

    public class SensorRange
    {
        [JsonProperty("min")]
        public double? Min { get; set; }

        [JsonProperty("max")]
        public double? Max { get; set; }

        [JsonProperty("avg")]
        public double? Average { get; set; }
    }

    public class GpsInfo
    {
        [JsonProperty("start")]
        public GpsPoint Start { get; set; }

        [JsonProperty("end")]
        public GpsPoint End { get; set; }

        [JsonProperty("track_file")]
        public string TrackFile { get; set; }

        [JsonProperty("sample_rate")]
        public int SampleRate { get; set; } = 1; // Hz

        [JsonProperty("total_points")]
        public int? TotalPoints { get; set; }

        public GpsInfo()
        {
            Start = new GpsPoint();
            End = new GpsPoint();
        }
    }

    public class GpsPoint
    {
        [JsonProperty("latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("longitude")]
        public double? Longitude { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("satellites")]
        public int? Satellites { get; set; }
    }

    /// <summary>
    /// 동기화 모드에서 사용되는 동기화 정보
    /// </summary>
    public class SynchronizationInfo
    {
        [JsonProperty("method")]
        public string Method { get; set; } = "software_trigger_30hz";

        [JsonProperty("master_frequency_hz")]
        public int MasterFrequencyHz { get; set; } = 30;

        [JsonProperty("total_sync_points")]
        public long TotalSyncPoints { get; set; }

        [JsonProperty("sync_log_file")]
        public string SyncLogFile { get; set; }

        [JsonProperty("quality_metrics")]
        public SyncQualityMetrics QualityMetrics { get; set; }

        [JsonProperty("statistics")]
        public SyncStatistics Statistics { get; set; }

        public SynchronizationInfo()
        {
            QualityMetrics = new SyncQualityMetrics();
            Statistics = new SyncStatistics();
        }
    }

    /// <summary>
    /// 동기화 품질 지표
    /// </summary>
    public class SyncQualityMetrics
    {
        [JsonProperty("average_camera_latency_ms")]
        public double AverageCameraLatencyMs { get; set; }

        [JsonProperty("average_sensor_latency_ms")]
        public double AverageSensorLatencyMs { get; set; }

        [JsonProperty("max_sync_error_ms")]
        public double MaxSyncErrorMs { get; set; }

        [JsonProperty("sync_success_rate")]
        public double SyncSuccessRate { get; set; }

        [JsonProperty("sync_quality_score")]
        public string SyncQualityScore { get; set; } // "excellent", "good", "poor"
    }

    /// <summary>
    /// 동기화 통계 정보
    /// </summary>
    public class SyncStatistics
    {
        [JsonProperty("camera_latency")]
        public LatencyStats CameraLatency { get; set; }

        [JsonProperty("sensor_latency")]
        public LatencyStats SensorLatency { get; set; }

        [JsonProperty("sync_error_distribution")]
        public SyncErrorDistribution SyncErrorDistribution { get; set; }

        public SyncStatistics()
        {
            CameraLatency = new LatencyStats();
            SensorLatency = new LatencyStats();
            SyncErrorDistribution = new SyncErrorDistribution();
        }
    }

    /// <summary>
    /// 지연 시간 통계
    /// </summary>
    public class LatencyStats
    {
        [JsonProperty("min")]
        public double Min { get; set; }

        [JsonProperty("max")]
        public double Max { get; set; }

        [JsonProperty("avg")]
        public double Average { get; set; }

        [JsonProperty("std")]
        public double StandardDeviation { get; set; }
    }

    /// <summary>
    /// 동기화 오차 분포
    /// </summary>
    public class SyncErrorDistribution
    {
        [JsonProperty("below_1ms")]
        public int Below1Ms { get; set; }

        [JsonProperty("1_to_3ms")]
        public int Between1And3Ms { get; set; }

        [JsonProperty("above_3ms")]
        public int Above3Ms { get; set; }
    }
}