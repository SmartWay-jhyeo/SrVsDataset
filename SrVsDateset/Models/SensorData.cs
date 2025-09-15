using System;
using Newtonsoft.Json;

namespace SrVsDataset.Models
{
    public class SensorData
    {
        [JsonProperty("sequence", NullValueHandling = NullValueHandling.Ignore)]
        public long? Sequence { get; set; } // 동기화 모드에서만 사용

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("processing_delay_ms", NullValueHandling = NullValueHandling.Ignore)]
        public double? ProcessingDelayMs { get; set; } // 아두이노 처리 지연 시간

        [JsonProperty("light_level")]
        public double LightLevel { get; set; } // 0-100%

        [JsonProperty("humidity")]
        public double Humidity { get; set; } // 0-100%

        [JsonProperty("temperature")]
        public double Temperature { get; set; } // Celsius

        [JsonProperty("imu")]
        public ImuData Imu { get; set; }

        [JsonProperty("gps_sync")]
        public GpsPoint GpsSync { get; set; }

        /// <summary>
        /// 동기화 모드 데이터인지 확인
        /// </summary>
        [JsonIgnore]
        public bool IsSynchronized => Sequence.HasValue;

        public SensorData()
        {
            Timestamp = TimestampManager.GetTimestampString();
            Imu = new ImuData();
        }
    }

    public class ImuData
    {
        [JsonProperty("accel")]
        public Vector3D Acceleration { get; set; }

        [JsonProperty("gyro")]
        public Vector3D Gyroscope { get; set; }

        [JsonProperty("mag")]
        public Vector3D Magnetometer { get; set; }

        [JsonProperty("euler")]
        public EulerAngles Euler { get; set; }

        public ImuData()
        {
            Acceleration = new Vector3D();
            Gyroscope = new Vector3D();
            Magnetometer = new Vector3D();
            Euler = new EulerAngles();
        }
    }

    public class Vector3D
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }
    }

    public class EulerAngles
    {
        [JsonProperty("roll")]
        public double Roll { get; set; }

        [JsonProperty("pitch")]
        public double Pitch { get; set; }

        [JsonProperty("yaw")]
        public double Yaw { get; set; }
    }
}