using System.Data.Odbc;
using FluentAssertions;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;
using VisorDBF.Core.Services;
using Xunit;

namespace VisorDBF.Core.Tests.Services;

public class SybaseExportServiceTests
{
    [Fact]
    public void CreateExtraColumnInfos_EmptyList_ReturnsEmpty()
    {
        var result = SybaseExportService.CreateExtraColumnInfos([]);
        result.Should().BeEmpty();
    }

    [Fact]
    public void CreateExtraColumnInfos_SingleIntegerColumn_ReturnsOneColumnInfo()
    {
        var extra = new List<ExtraColumnConfig>
        {
            new() { ColumnName = "periodo", Type = ExtraColumnType.Integer, RawValue = "202601" }
        };

        var result = SybaseExportService.CreateExtraColumnInfos(extra);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("periodo");
        result[0].DbTypeName.Should().Be("int");
        result[0].OdbcType.Should().Be(OdbcType.Int);
        result[0].Precision.Should().Be(0);
        result[0].Scale.Should().Be(0);
    }

    [Fact]
    public void CreateExtraColumnInfos_SingleDateTimeColumn_ReturnsOneColumnInfo()
    {
        var extra = new List<ExtraColumnConfig>
        {
            new() { ColumnName = "fecha_carga", Type = ExtraColumnType.DateTime, RawValue = "2026-01-01" }
        };

        var result = SybaseExportService.CreateExtraColumnInfos(extra);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("fecha_carga");
        result[0].DbTypeName.Should().Be("datetime");
        result[0].OdbcType.Should().Be(OdbcType.DateTime);
    }

    [Fact]
    public void CreateExtraColumnInfos_MultipleColumns_ReturnsAll()
    {
        var extra = new List<ExtraColumnConfig>
        {
            new() { ColumnName = "periodo", Type = ExtraColumnType.Integer, RawValue = "202601" },
            new() { ColumnName = "fecha_carga", Type = ExtraColumnType.DateTime, RawValue = "2026-01-01" }
        };

        var result = SybaseExportService.CreateExtraColumnInfos(extra);

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("periodo");
        result[1].Name.Should().Be("fecha_carga");
    }

    [Fact]
    public void CreateExtraColumnInfos_Convert_Integer_ReturnsRawValue()
    {
        var extra = new List<ExtraColumnConfig>
        {
            new() { ColumnName = "periodo", Type = ExtraColumnType.Integer, RawValue = "202601" }
        };

        var result = SybaseExportService.CreateExtraColumnInfos(extra);
        var converted = result[0].Convert(null);

        converted.Should().Be("202601");
    }

    [Fact]
    public void CreateExtraColumnInfos_Convert_Integer_Invalid_Throws()
    {
        var extra = new List<ExtraColumnConfig>
        {
            new() { ColumnName = "periodo", Type = ExtraColumnType.Integer, RawValue = "not-a-number" }
        };

        var result = SybaseExportService.CreateExtraColumnInfos(extra);

        Action act = () => result[0].Convert(null);
        act.Should().Throw<ExportException>()
            .WithMessage("*no es un entero valido*");
    }

    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("01/01/2026")]
    [InlineData("20260101")]
    public void CreateExtraColumnInfos_Convert_DateTime_ValidFormats(string dateStr)
    {
        var extra = new List<ExtraColumnConfig>
        {
            new() { ColumnName = "fecha_carga", Type = ExtraColumnType.DateTime, RawValue = dateStr }
        };

        var result = SybaseExportService.CreateExtraColumnInfos(extra);
        var converted = result[0].Convert(null);

        converted.Should().Be("2026-01-01 00:00:00");
    }

    [Fact]
    public void CreateExtraColumnInfos_Convert_DateTime_Invalid_Throws()
    {
        var extra = new List<ExtraColumnConfig>
        {
            new() { ColumnName = "fecha_carga", Type = ExtraColumnType.DateTime, RawValue = "bad-date" }
        };

        var result = SybaseExportService.CreateExtraColumnInfos(extra);

        Action act = () => result[0].Convert(null);
        act.Should().Throw<ExportException>()
            .WithMessage("*no es una fecha valida*");
    }

    [Fact]
    public void CreateExtraColumnInfos_Convert_IgnoresInput()
    {
        var extra = new List<ExtraColumnConfig>
        {
            new() { ColumnName = "periodo", Type = ExtraColumnType.Integer, RawValue = "202601" }
        };

        var result = SybaseExportService.CreateExtraColumnInfos(extra);

        var convertedNull = result[0].Convert(null);
        var convertedDBNull = result[0].Convert(DBNull.Value);
        var convertedString = result[0].Convert("anything");

        convertedNull.Should().Be("202601");
        convertedDBNull.Should().Be("202601");
        convertedString.Should().Be("202601");
    }
}
