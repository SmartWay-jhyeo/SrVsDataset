namespace SrVsDataset.Models
{
    public enum RoadType
    {
        Highway,
        Urban
    }

    public enum Weather
    {
        Clear,
        Cloudy
    }

    public enum TimeOfDay
    {
        AM,      // 07-10시
        Midday,  // 10-14시
        PM,      // 14-18시
        Night    // 18-23시
    }

    public enum RecordingSide
    {
        Left,
        Right
    }

    public class EnvironmentSettings
    {
        public RoadType RoadType { get; set; }
        public Weather Weather { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
        public RecordingSide RecordingSide { get; set; }

        public string GetFolderPath(string rootPath)
        {
            return System.IO.Path.Combine(
                rootPath,
                RoadType.ToString().ToLower(),
                Weather.ToString().ToLower(),
                TimeOfDay.ToString().ToLower(),
                RecordingSide.ToString().ToLower()
            );
        }
    }
}