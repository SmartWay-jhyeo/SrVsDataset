using System.Threading.Tasks;
using SrVsDataset.Models;

namespace SrVsDataset.Interfaces
{
    public interface IFileManagementService
    {
        string RootPath { get; set; }
        
        Task<bool> EnsureDirectoryStructureAsync(EnvironmentSettings settings);
        Task<bool> SaveMetadataAsync(string filePath, RecordingMetadata metadata);
        string GenerateFilePath(EnvironmentSettings settings, RecordingSide side, string extension);
    }
}