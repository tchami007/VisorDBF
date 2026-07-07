using System.Globalization;
using FluentAssertions;
using VisorDBF.Core.Models;
using VisorDBF.Core.Services;
using Xunit;

namespace VisorDBF.Core.Tests.Services;

public class ColumnFormatServiceTests
{
    private readonly ColumnFormatService _sut = new();

    [Fact]
    public void ApplyFormat_NullValue_ReturnsNull()
    {
        var result = _sut.ApplyFormat("CAMPO", null, "yyyy-MM-dd");

        result.Should().BeNull();
    }

    [Fact]
    public void ApplyFormat_NullFormat_ReturnsToString()
    {
        var result = _sut.ApplyFormat("CAMPO", 42, null);

        result.Should().Be("42");
    }

    [Fact]
    public void ApplyFormat_EmptyFormat_ReturnsToString()
    {
        var result = _sut.ApplyFormat("CAMPO", 42, "");

        result.Should().Be("42");
    }

    [Fact]
    public void ApplyFormat_DateWithFormat_AppliesFormat()
    {
        var date = new DateOnly(2024, 7, 4);

        var result = _sut.ApplyFormat("FECHA", date, "yyyy-MM-dd");

        result.Should().Be("2024-07-04");
    }

    [Fact]
    public void ApplyFormat_DateTimeWithFormat_AppliesFormat()
    {
        var dt = new DateTime(2024, 7, 4, 10, 30, 0);

        var result = _sut.ApplyFormat("FECHA", dt, "yyyy-MM-dd HH:mm");

        result.Should().Be("2024-07-04 10:30");
    }

    [Fact]
    public void ApplyFormat_NumericWithFormat_AppliesFormat()
    {
        var result = _sut.ApplyFormat("MONTO", 1234.5m, "N2");

        result.Should().Be(1234.5m.ToString("N2", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void ApplyFormat_FloatWithFormat_AppliesFormat()
    {
        var result = _sut.ApplyFormat("MONTO", 1234.5, "F3");

        result.Should().Be(1234.5.ToString("F3", CultureInfo.CurrentCulture));
    }

    [Fact]
    public void ApplyFormat_StringPassthrough_ReturnsToString()
    {
        var result = _sut.ApplyFormat("NOMBRE", "Hola", "N2");

        result.Should().Be("Hola");
    }

    [Fact]
    public void ApplyFormat_InvalidFormat_ReturnsError()
    {
        var result = _sut.ApplyFormat("MONTO", 123.45m, "Q");

        result.Should().Be("ERROR");
    }

    [Fact]
    public void IsFormatValid_Null_ReturnsTrue()
    {
        var result = _sut.IsFormatValid(null, DbfFieldType.Date);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsFormatValid_Empty_ReturnsTrue()
    {
        var result = _sut.IsFormatValid("", DbfFieldType.Date);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsFormatValid_ValidDateFormat_ReturnsTrue()
    {
        var result = _sut.IsFormatValid("yyyy-MM-dd", DbfFieldType.Date);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsFormatValid_ValidNumericFormat_ReturnsTrue()
    {
        var result = _sut.IsFormatValid("N2", DbfFieldType.Numeric);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsFormatValid_InvalidDateFormat_ReturnsFalse()
    {
        var result = _sut.IsFormatValid("Q", DbfFieldType.Date);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsFormatValid_InvalidNumericFormat_ReturnsFalse()
    {
        var result = _sut.IsFormatValid("Q", DbfFieldType.Numeric);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsFormatValid_CharacterTypeWithFormat_ReturnsTrue()
    {
        var result = _sut.IsFormatValid("N2", DbfFieldType.Character);

        result.Should().BeTrue();
    }

    [Fact]
    public void GetFormatOrDefault_Found_ReturnsFormat()
    {
        var formats = new Dictionary<string, string?> { { "MONTO", "N2" } };

        var result = _sut.GetFormatOrDefault("MONTO", formats);

        result.Should().Be("N2");
    }

    [Fact]
    public void GetFormatOrDefault_Missing_ReturnsNull()
    {
        var formats = new Dictionary<string, string?> { { "MONTO", "N2" } };

        var result = _sut.GetFormatOrDefault("OTRO", formats);

        result.Should().BeNull();
    }

    [Fact]
    public void GetFormatOrDefault_MissingWithDefault_ReturnsDefault()
    {
        var formats = new Dictionary<string, string?> { { "MONTO", "N2" } };

        var result = _sut.GetFormatOrDefault("OTRO", formats, "N0");

        result.Should().Be("N0");
    }

    [Fact]
    public void ColumnFormatConfiguration_Default_HasEmptyFormats()
    {
        ColumnFormatConfiguration.Default.Formats.Should().BeEmpty();
    }

    [Fact]
    public void ColumnFormatConfiguration_Default_IsActiveFalse()
    {
        ColumnFormatConfiguration.Default.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ColumnFormatConfiguration_NewInstance_IsActiveFalse()
    {
        var config = new ColumnFormatConfiguration();

        config.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ColumnFormatConfiguration_NewInstance_HasEmptyFormats()
    {
        var config = new ColumnFormatConfiguration();

        config.Formats.Should().BeEmpty();
    }
}
