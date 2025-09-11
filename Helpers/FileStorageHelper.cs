using Microsoft.Extensions.Logging;
using SailingResultsPortal.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SailingResultsPortal.Helpers
{
    public class TimeSpanJsonConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (TimeSpan.TryParse(stringValue, out var timeSpan))
                    return timeSpan;
            }

            throw new JsonException("Invalid TimeSpan format");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString(@"hh\:mm\:ss"));
            else
                writer.WriteNullValue();
        }
    }

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
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

                if (data == null)
                    throw new ArgumentNullException(nameof(data));

                string filePath = Path.Combine(DataFolder, fileName);

                // Ensure directory exists
                Directory.CreateDirectory(DataFolder);

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new TimeSpanJsonConverter());

                // Create a temporary file first, then move it to avoid corruption
                string tempFilePath = filePath + ".tmp";
                await using (FileStream createStream = File.Create(tempFilePath))
                {
                    await JsonSerializer.SerializeAsync(createStream, data, options);
                }

                // Atomic move
                if (File.Exists(filePath))
                {
                    File.Replace(tempFilePath, filePath, null);
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }
            }
            catch (Exception ex)
            {
                // Log the error (in a real app, you'd inject ILogger)
                Console.Error.WriteLine($"Error saving file {fileName}: {ex.Message}");
                throw new IOException($"Failed to save data to {fileName}", ex);
            }
        }

        // Save with timestamp wrapper (for caching)
        public static async Task SaveWithTimestampAsync<T>(string fileName, List<T> data)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

                if (data == null)
                    throw new ArgumentNullException(nameof(data));

                string filePath = Path.Combine(DataFolder, fileName);

                // Ensure directory exists
                Directory.CreateDirectory(DataFolder);

                var wrapper = new JsonDataWrapper<T>
                {
                    LastUpdated = DateTime.UtcNow,
                    Data = data
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new TimeSpanJsonConverter());

                // Create a temporary file first, then move it to avoid corruption
                string tempFilePath = filePath + ".tmp";
                await using (FileStream createStream = File.Create(tempFilePath))
                {
                    await JsonSerializer.SerializeAsync(createStream, wrapper, options);
                }

                // Atomic move
                if (File.Exists(filePath))
                {
                    File.Replace(tempFilePath, filePath, null);
                }
                else
                {
                    File.Move(tempFilePath, filePath);
                }

                Console.WriteLine($"Successfully saved {data.Count} items to {fileName} with timestamp {wrapper.LastUpdated}.");
            }
            catch (Exception ex)
            {
                // Log the error
                Console.Error.WriteLine($"Error saving file {fileName} with timestamp: {ex.Message}");
                throw new IOException($"Failed to save data to {fileName}", ex);
            }
        }

        public static async Task<List<T>> LoadAsync<T>(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

                string filePath = Path.Combine(DataFolder, fileName);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File {fileName} does not exist, returning empty list.");
                    return new List<T>();
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new TimeSpanJsonConverter());

                await using FileStream openStream = File.OpenRead(filePath);
                var result = await JsonSerializer.DeserializeAsync<List<T>>(openStream, options);

                Console.WriteLine($"Successfully loaded {result?.Count ?? 0} items from {fileName}.");
                return result ?? new List<T>();
            }
            catch (Exception ex)
            {
                // Log the error (in a real app, you'd inject ILogger)
                Console.WriteLine($"Error loading file {fileName}: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Return empty list instead of throwing to be more resilient
                return new List<T>();
            }
        }

        // Load with timestamp wrapper (for caching)
        public static async Task<JsonDataWrapper<T>> LoadWithTimestampAsync<T>(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

                string filePath = Path.Combine(DataFolder, fileName);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File {fileName} does not exist, returning empty wrapper.");
                    return new JsonDataWrapper<T> { LastUpdated = DateTime.UtcNow, Data = new List<T>() };
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new TimeSpanJsonConverter());

                await using FileStream openStream = File.OpenRead(filePath);
                var wrapper = await JsonSerializer.DeserializeAsync<JsonDataWrapper<T>>(openStream, options);

                Console.WriteLine($"Successfully loaded {wrapper?.Data?.Count ?? 0} items from {fileName} with timestamp {wrapper?.LastUpdated}.");
                return wrapper ?? new JsonDataWrapper<T> { LastUpdated = DateTime.UtcNow, Data = new List<T>() };
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error loading file {fileName} with timestamp: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                // Return empty wrapper instead of throwing
                return new JsonDataWrapper<T> { LastUpdated = DateTime.UtcNow, Data = new List<T>() };
            }
        }
    }
}
