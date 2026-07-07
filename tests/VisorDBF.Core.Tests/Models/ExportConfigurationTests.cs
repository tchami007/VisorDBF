using System.Text;
using FluentAssertions;
using VisorDBF.Core.Models;
using Xunit;

namespace VisorDBF.Core.Tests.Models;

public class ExportConfigurationTests
{
    [Fact]
    public void Default_HasSemicolonSeparator()
    {
        ExportConfiguration.Default.ColumnSeparator.Should().Be(";");
    }

    [Fact]
    public void Default_HasUtf8Encoding()
    {
        ExportConfiguration.Default.OutputEncoding.Should().Be(Encoding.UTF8);
    }

    [Fact]
    public void Default_IncludeHeaderIsTrue()
    {
        ExportConfiguration.Default.IncludeHeader.Should().BeTrue();
    }

    [Fact]
    public void Default_RowLimitModeIsAll()
    {
        ExportConfiguration.Default.RowLimitMode.Should().Be(RowLimitMode.All);
    }

    [Fact]
    public void Default_RowEndDelimiterIsEmpty()
    {
        ExportConfiguration.Default.RowEndDelimiter.Should().Be(string.Empty);
    }

    [Fact]
    public void EqualityIsStructural()
    {
        var c1 = new ExportConfiguration { ColumnSeparator = "|" };
        var c2 = new ExportConfiguration { ColumnSeparator = "|" };
        c1.Should().Be(c2);
    }

    [Fact]
    public void WithExpressionCreatesCopy()
    {
        var original = ExportConfiguration.Default;
        var modified = original with { ColumnSeparator = "," };
        modified.ColumnSeparator.Should().Be(",");
        original.ColumnSeparator.Should().Be(";");
    }
}
