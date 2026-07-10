using System.Text;
using FluentAssertions;
using FluentAssertions.Equivalency;
using VisorDBF.Core.Models;
using VisorDBF.Core.Services;
using Xunit;
namespace VisorDBF.Core.Tests.Services;

public class JsonSettingsServiceTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CleanupTempDir(string dir)
    {
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    private static EquivalencyAssertionOptions<ApplicationSettings> CompareEncodingsByWebName(
        EquivalencyAssertionOptions<ApplicationSettings> options)
    {
        return options.Using<Encoding>(ctx => ctx.Subject.WebName.Should().Be(ctx.Expectation.WebName))
            .WhenTypeIs<Encoding>();
    }

    [Fact]
    public void Load_FileNotExists_ReturnsDefault()
    {
        var tempDir = CreateTempDir();
        try
        {
            var service = new JsonSettingsService(tempDir);
            var result = service.Load();

            result.Should().Be(ApplicationSettings.Default);
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }

    [Fact]
    public void Load_ValidJson_ReturnsDeserializedSettings()
    {
        var tempDir = CreateTempDir();
        try
        {
            var original = new ApplicationSettings
            {
                LastProfileName = "test-profile",
                Profiles = new List<ExportProfile>
                {
                    new() { Name = "test-profile", Config = ExportConfiguration.Default }
                },
                RecentFiles = new List<RecentFileEntry>
                {
                    new() { FilePath = @"C:\test.dbf", LastOpened = new DateTime(2026, 1, 1) }
                },
                WindowState = new WindowSettings { Left = 100, Top = 50, Width = 1024, Height = 768 }
            };

            var service = new JsonSettingsService(tempDir);
            service.Save(original);

            var loaded = service.Load();
            loaded.Should().BeEquivalentTo(original, CompareEncodingsByWebName);
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }

    [Fact]
    public void Load_CorruptJson_CreatesBackupAndReturnsDefault()
    {
        var tempDir = CreateTempDir();
        try
        {
            var filePath = Path.Combine(tempDir, "settings.json");
            File.WriteAllText(filePath, "this is not json {{{");

            var service = new JsonSettingsService(tempDir);
            var result = service.Load();

            result.Should().Be(ApplicationSettings.Default);

            var backupFiles = Directory.GetFiles(tempDir, "settings.json.corrupt*");
            backupFiles.Should().NotBeEmpty();
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }

    [Fact]
    public void Save_ProducesValidRoundTrip()
    {
        var tempDir = CreateTempDir();
        try
        {
            var original = new ApplicationSettings
            {
                DefaultExportConfig = ExportConfiguration.Default with
                {
                    ColumnSeparator = "|",
                    IncludeHeader = false
                },
                LastProfileName = "mi-perfil",
                Profiles = new List<ExportProfile>
                {
                    new()
                    {
                        Name = "mi-perfil",
                        Config = ExportConfiguration.Default with { ColumnSeparator = "," }
                    }
                },
                WindowState = new WindowSettings
                {
                    Left = 100, Top = 50, Width = 1024, Height = 768,
                    IsMaximized = false
                },
                RecentFiles = new List<RecentFileEntry>
                {
                    new() { FilePath = @"C:\data.dbf", LastOpened = new DateTime(2026, 6, 15, 10, 30, 0, DateTimeKind.Utc) }
                }
            };

            var service = new JsonSettingsService(tempDir);
            service.Save(original);

            var loaded = service.Load();
            loaded.Should().BeEquivalentTo(original, CompareEncodingsByWebName);
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }

    [Fact]
    public void Save_UsesCamelCasePropertyNames()
    {
        var tempDir = CreateTempDir();
        try
        {
            var settings = new ApplicationSettings
            {
                LastProfileName = "TestProfile"
            };

            var service = new JsonSettingsService(tempDir);
            service.Save(settings);

            var json = File.ReadAllText(Path.Combine(tempDir, "settings.json"));
            json.Should().Contain("lastProfileName");
            json.Should().NotContain("LastProfileName");
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }

    [Fact]
    public void AtomicWrite_NoTempFileAfterSave()
    {
        var tempDir = CreateTempDir();
        try
        {
            var service = new JsonSettingsService(tempDir);
            service.Save(ApplicationSettings.Default);

            Directory.GetFiles(tempDir, "*.tmp").Should().BeEmpty();
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }

    [Fact]
    public void GetDirectory_ReturnsExpectedPath()
    {
        var tempDir = CreateTempDir();
        try
        {
            var service = new JsonSettingsService(tempDir);
            service.GetDirectory().Should().Be(tempDir);
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }

    [Fact]
    public void GetFilePath_ReturnsExpectedPath()
    {
        var tempDir = CreateTempDir();
        try
        {
            var service = new JsonSettingsService(tempDir);
            service.GetFilePath().Should().Be(Path.Combine(tempDir, "settings.json"));
        }
        finally
        {
            CleanupTempDir(tempDir);
        }
    }
}
