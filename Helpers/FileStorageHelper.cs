using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SailingResultsPortal.Helpers
{
    public static class FileStorageHelper
    {
        private static readonly string DataFolder = Path.Combine(Directory.GetCurrentDirectory(), "Data");

        static FileStorageHelper()
        {
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }
        }

        public static async Task SaveAsync<T>(string fileName, List<T> data)
        {
            string filePath = Path.Combine(DataFolder, fileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            using FileStream createStream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(createStream, data, options);
            await createStream.DisposeAsync();
        }

        public static async Task<List<T>> LoadAsync<T>(string fileName)
        {
            string filePath = Path.Combine(DataFolder, fileName);
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }
            using FileStream openStream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<List<T>>(openStream) ?? new List<T>();
        }
    }
}
