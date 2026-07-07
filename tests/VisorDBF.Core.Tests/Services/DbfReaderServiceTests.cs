using System.Text;
using FluentAssertions;
using Xunit;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Services;
using VisorDBF.Core.Tests.Helpers;

namespace VisorDBF.Core.Tests.Services;

public class DbfReaderServiceTests : IDisposable
{
    private readonly string _tempDir;

    public DbfReaderServiceTests()
    {
        // Registrar code pages para encodings extendidos (CP1252, etc.)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        _tempDir = Path.Combine(Path.GetTempPath(), $"VisorDBF_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task ReadAsync_ValidDbf_ReturnsCorrectFieldCount()
    {
        // Arrange
        var path = DbfTestHelper.CreateMinimalDbf(_tempDir);
        var service = new DbfReaderService();
        var encoding = Encoding.GetEncoding("windows-1252");

        // Act
        var result = await service.ReadAsync(path, encoding);

        // Assert
        result.Should().NotBeNull();
        result.Fields.Should().NotBeEmpty();
        result.Fields.Count.Should().Be(1);
        result.Fields[0].Name.Should().Be("NOMBRE");
    }

    [Fact]
    public async Task ReadAsync_ValidDbf_ReturnsCorrectRecordCount()
    {
        // Arrange
        var path = DbfTestHelper.CreateMinimalDbf(_tempDir);
        var service = new DbfReaderService();
        var encoding = Encoding.GetEncoding("windows-1252");

        // Act
        var result = await service.ReadAsync(path, encoding);

        // Assert
        result.Records.Should().NotBeEmpty();
        result.Records.Count.Should().Be(2);
    }

    [Fact]
    public async Task ReadAsync_ValidDbf_MapsIsDeletedCorrectly()
    {
        // Arrange
        var path = DbfTestHelper.CreateMinimalDbf(_tempDir);
        var service = new DbfReaderService();
        var encoding = Encoding.GetEncoding("windows-1252");

        // Act
        var result = await service.ReadAsync(path, encoding);

        // Assert — registro 0 activo, registro 1 eliminado
        result.Records[0].IsDeleted.Should().BeFalse();
        result.Records[1].IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ReadAsync_ValidDbf_ReturnsCorrectLanguageDriverId()
    {
        // Arrange
        var path = DbfTestHelper.CreateMinimalDbf(_tempDir);
        var service = new DbfReaderService();
        var encoding = Encoding.GetEncoding("windows-1252");

        // Act
        var result = await service.ReadAsync(path, encoding);

        // Assert — el fixture tiene Language Driver ID 0x57
        result.LanguageDriverId.Should().Be(0x57);
    }

    [Fact]
    public async Task ReadAsync_NonExistentFile_ThrowsDbfReadException()
    {
        // Arrange
        var service = new DbfReaderService();

        // Act
        Func<Task> act = () => service.ReadAsync("nonexistent.dbf", Encoding.UTF8);

        // Assert
        await act.Should().ThrowAsync<DbfReadException>();
    }

    [Fact]
    public async Task ReadAsync_EmptyFilePath_ThrowsDbfReadException()
    {
        // Arrange
        var service = new DbfReaderService();

        // Act
        Func<Task> act = () => service.ReadAsync(string.Empty, Encoding.UTF8);

        // Assert
        await act.Should().ThrowAsync<DbfReadException>();
    }

    [Fact]
    public async Task ReadAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var path = DbfTestHelper.CreateMinimalDbf(_tempDir);
        var service = new DbfReaderService();
        var encoding = Encoding.GetEncoding("windows-1252");
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // cancelar antes de iniciar

        // Act
        Func<Task> act = () => service.ReadAsync(path, encoding, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
