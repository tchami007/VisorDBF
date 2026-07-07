using FluentAssertions;
using Xunit;
using VisorDBF.Core.Services;

namespace VisorDBF.Core.Tests.Services;

public class EncodingDetectionServiceTests
{
    private readonly EncodingDetectionService _sut = new();

    [Theory]
    [InlineData(0x03, "windows-1252")]
    [InlineData(0x57, "windows-1252")]
    [InlineData(0x02, "IBM850")]
    [InlineData(0xC8, "windows-1250")]
    [InlineData(0xC9, "windows-1251")]
    public void DetectEncoding_KnownId_ReturnsCorrectEncoding(byte id, string expectedName)
    {
        // Asegurar que el proveedor de code pages este registrado para este test
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var result = _sut.DetectEncoding(id);
        result.Should().NotBeNull();
        result!.WebName.Should().BeOneOf(expectedName, expectedName.ToLower());
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0xFF)]
    [InlineData(0xFE)]
    public void DetectEncoding_UnknownId_ReturnsNull(byte id)
    {
        _sut.DetectEncoding(id).Should().BeNull();
    }

    [Fact]
    public void ReadLanguageDriverId_NonExistentFile_ThrowsDbfReadException()
    {
        Action act = () => _sut.ReadLanguageDriverId("no_existe.dbf");
        act.Should().Throw<VisorDBF.Core.Exceptions.DbfReadException>();
    }

    [Fact]
    public void DetectEncoding_0x57_ReturnsWindows1252()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var result = _sut.DetectEncoding(0x57);
        result.Should().NotBeNull();
        result!.CodePage.Should().Be(1252);
    }

    [Fact]
    public void DetectEncoding_0x00_ReturnsNull()
    {
        _sut.DetectEncoding(0x00).Should().BeNull();
    }
}
