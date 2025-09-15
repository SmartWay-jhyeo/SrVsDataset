using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SrVsDataset.Interfaces;
using SrVsDataset.Models;

namespace SrVsDataset.Services
{
    public class FileManagementService : IFileManagementService
    {
        public string RootPath { get; set; }

        public FileManagementService(string rootPath)
        {
            RootPath = rootPath;
        }

        public async Task<bool> EnsureDirectoryStructureAsync(EnvironmentSettings settings)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string fullPath = settings.GetFolderPath(RootPath);
                    Directory.CreateDirectory(fullPath);
                    
                    // 실제로 디렉토리가 생성되었는지 확인
                    return Directory.Exists(fullPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Directory creation failed: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> SaveMetadataAsync(string filePath, RecordingMetadata metadata)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 디렉토리가 존재하는지 확인하고 없으면 생성
                    string directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                    
                    // 파일이 실제로 생성되었는지 확인
                    return File.Exists(filePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Metadata save failed: {ex.Message}");
                    return false;
                }
            });
        }

        public string GenerateFilePath(EnvironmentSettings settings, RecordingSide side, string extension)
        {
            string folderPath = settings.GetFolderPath(RootPath);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{timestamp}_{side.ToString().ToLower()}.{extension}";
            return Path.Combine(folderPath, fileName);
        }
    }
}