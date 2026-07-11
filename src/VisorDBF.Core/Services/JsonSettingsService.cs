using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VisorDBF.Core.Models;
namespace VisorDBF.Core.Services;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new DoubleJsonConverter(), new EncodingJsonConverter() }
    };

    private readonly string _directory;
    private readonly string _filePath;

    public JsonSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VisorDBF"))
    {
    }

    public JsonSettingsService(string directory)
    {
        _directory = directory;
        _filePath = Path.Combine(_directory, "settings.json");
    }

    public string GetFilePath() => _filePath;
    public string GetDirectory() => _directory;

    public ApplicationSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return ApplicationSettings.Default;

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<ApplicationSettings>(json, JsonOptions);
            return settings ?? ApplicationSettings.Default;
        }
        catch (JsonException ex)
        {
            CreateBackup($"corrupt-json-{ex.Message}");
            return ApplicationSettings.Default;
        }
        catch (IOException ex)
        {
            CreateBackup($"io-error-{ex.Message}");
            return ApplicationSettings.Default;
        }
    }

    public void Save(ApplicationSettings settings)
    {
        Directory.CreateDirectory(_directory);

        var json = JsonSerializer.Serialize(settings, JsonOptions);

        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }

    public void CreateBackup(string reason)
    {
        if (!File.Exists(_filePath)) return;

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var sanitizedReason = SanitizeFileName(reason);
        var backupPath = $"{_filePath}.corrupt.{sanitizedReason}.{timestamp}";

        try
        {
            File.Copy(_filePath, backupPath, overwrite: false);
        }
        catch (IOException)
        {
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Create(name.Length, name, (span, n) =>
        {
            for (int i = 0; i < n.Length; i++)
                span[i] = Array.IndexOf(invalid, n[i]) >= 0 ? '_' : n[i];
        });
    }

    private sealed class DoubleJsonConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return double.NaN;
            return reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value))
                writer.WriteNullValue();
            else
                writer.WriteNumberValue(value);
        }
    }

    private sealed class EncodingJsonConverter : JsonConverter<Encoding>
    {
        public override Encoding? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var name = reader.GetString();
            return string.IsNullOrEmpty(name) ? null : Encoding.GetEncoding(name);
        }

        public override void Write(Utf8JsonWriter writer, Encoding value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.WebName);
        }
    }
}
