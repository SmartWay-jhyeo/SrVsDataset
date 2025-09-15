using System;

namespace SrVsDataset.Models
{
    public class RecordingSession
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string VideoFilePath { get; set; }
        public string MetadataFilePath { get; set; }
        public string GpxFilePath { get; set; }
        public RecordingMetadata Metadata { get; set; }
        public bool IsRecording { get; set; }
        public long FileSize { get; set; }
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.Now - StartTime;

        public RecordingSession()
        {
            StartTime = DateTime.Now;
            Metadata = new RecordingMetadata();
        }

        public string GenerateFileName(RecordingSide side, string extension = "mp4")
        {
            var startStr = StartTime.ToString("yyyyMMdd_HHmmss");
            var sideStr = side.ToString().ToLower();
            return $"{startStr}_{sideStr}.{extension}";
        }

        public string GenerateBaseFileName(RecordingSide side)
        {
            var startStr = StartTime.ToString("yyyyMMdd_HHmmss");
            var sideStr = side.ToString().ToLower();
            return $"{startStr}_{{0}}_{sideStr}";
        }

        public void UpdateFilePathsOnCompletion(string basePath, RecordingSide side)
        {
            if (!EndTime.HasValue) return;

            var endStr = EndTime.Value.ToString("HHmmss");
            var fileName = GenerateFileName(side, "mp4");
            
            VideoFilePath = System.IO.Path.Combine(basePath, fileName);
            MetadataFilePath = System.IO.Path.Combine(basePath, fileName.Replace(".mp4", ".json"));
            GpxFilePath = System.IO.Path.Combine(basePath, fileName.Replace(".mp4", ".gpx"));
        }
    }
}