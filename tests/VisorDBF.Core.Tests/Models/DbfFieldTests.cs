using FluentAssertions;
using VisorDBF.Core.Exceptions;
using VisorDBF.Core.Models;
using Xunit;

namespace VisorDBF.Core.Tests.Models;

public class DbfFieldTests
{
    [Fact]
    public void DbfField_EqualityIsStructural()
    {
        var f1 = new DbfField("FECHA", DbfFieldType.Date, 8, 0, 0);
        var f2 = new DbfField("FECHA", DbfFieldType.Date, 8, 0, 0);
        f1.Should().Be(f2);
    }

    [Fact]
    public void DbfField_NullName_ThrowsArgumentNull()
    {
        Action act = () => _ = new DbfField(null!, DbfFieldType.Character, 10, 0, 0);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(DbfFieldType.Date, "D")]
    [InlineData(DbfFieldType.DateTime, "DT")]
    [InlineData(DbfFieldType.Numeric, "N")]
    [InlineData(DbfFieldType.Unknown, "?")]
    public void DbfFieldType_ToDisplayString_ReturnsCorrectCode(DbfFieldType type, string expected)
    {
        type.ToDisplayString().Should().Be(expected);
    }

    [Theory]
    [InlineData(DbfFieldType.Numeric, true)]
    [InlineData(DbfFieldType.Date, true)]
    [InlineData(DbfFieldType.Logical, false)]
    [InlineData(DbfFieldType.Character, false)]
    public void DbfFieldType_HasConfigurableFormat(DbfFieldType type, bool expected)
    {
        type.HasConfigurableFormat().Should().Be(expected);
    }

    [Fact]
    public void DbfFile_HasDeletedRecords_FalseWhenEmpty()
    {
        var file = new DbfFile();
        file.HasDeletedRecords.Should().BeFalse();
    }

    [Fact]
    public void UnknownEncodingException_MessageContainsHexId()
    {
        var ex = new UnknownEncodingException(0xAB, "test.dbf");
        ex.Message.Should().Contain("0xAB");
    }
}
