using FluentAssertions;
using VisorDBF.Core.Models;
using Xunit;

namespace VisorDBF.Core.Tests.Models;

public class ExtraColumnConfigTests
{
    [Fact]
    public void ExtraColumnConfig_Defaults_AreEmpty()
    {
        var config = new ExtraColumnConfig();

        config.ColumnName.Should().BeEmpty();
        config.RawValue.Should().BeEmpty();
        config.Type.Should().Be(ExtraColumnType.DateTime);
    }

    [Fact]
    public void ExtraColumnConfig_CanSetProperties()
    {
        var config = new ExtraColumnConfig
        {
            ColumnName = "periodo",
            Type = ExtraColumnType.Integer,
            RawValue = "202601"
        };

        config.ColumnName.Should().Be("periodo");
        config.Type.Should().Be(ExtraColumnType.Integer);
        config.RawValue.Should().Be("202601");
    }

    [Fact]
    public void ExtraColumnConfig_WithSyntax_CreatesCopy()
    {
        var config = new ExtraColumnConfig
        {
            ColumnName = "periodo",
            Type = ExtraColumnType.Integer,
            RawValue = "202601"
        };

        var copy = config with { RawValue = "202602" };

        config.RawValue.Should().Be("202601");
        copy.RawValue.Should().Be("202602");
        copy.ColumnName.Should().Be("periodo");
        copy.Type.Should().Be(ExtraColumnType.Integer);
    }

    [Fact]
    public void ExtraColumnType_HasBothMembers()
    {
        ((int)ExtraColumnType.DateTime).Should().Be(0);
        ((int)ExtraColumnType.Integer).Should().Be(1);
    }
}
