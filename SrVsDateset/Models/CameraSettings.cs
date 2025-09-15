namespace SrVsDataset.Models
{
    public enum WhiteBalanceMode
    {
        Auto,
        Manual
    }

    public enum ExposureMode
    {
        Auto,
        Manual
    }

    public enum VideoCompressionType
    {
        Uncompressed,
        H264,
        H265,
        MJPEG
    }

    public enum ImageRotation
    {
        Rotate0 = 0,
        Rotate90 = 90,
        Rotate180 = 180,
        Rotate270 = 270
    }

    public enum ImageFlip
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    public class CameraSettings
    {
        public int ExposureTime { get; set; }  // microseconds
        public ExposureMode ExposureMode { get; set; }
        public WhiteBalanceMode WhiteBalanceMode { get; set; }
        public int WhiteBalanceTemperature { get; set; }  // Kelvin

        public CameraSettings()
        {
            ExposureTime = 5000;  // Default 5ms
            ExposureMode = ExposureMode.Manual;
            WhiteBalanceMode = WhiteBalanceMode.Auto;
            WhiteBalanceTemperature = 5600;  // Daylight
        }
    }

    public static class CameraPresets
    {
        public static readonly Dictionary<string, int> WhiteBalancePresets = new()
        {
            { "Daylight", 5600 },
            { "Cloudy", 6500 },
            { "Tungsten", 3200 },
            { "Fluorescent", 4000 }
        };

        public static readonly Dictionary<string, (int min, int max)> ExposurePresets = new()
        {
            { "Daylight", (1000, 5000) },
            { "Cloudy", (3000, 10000) },
            { "Indoor", (5000, 20000) },
            { "Night", (10000, 50000) }
        };
    }
}