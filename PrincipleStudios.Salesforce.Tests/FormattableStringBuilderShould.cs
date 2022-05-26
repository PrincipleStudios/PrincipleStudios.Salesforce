using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PrincipleStudios.Salesforce;

public class FormattableStringBuilderShould
{
    [Fact]
    public void HaveAnEmptyState()
    {
        // Arrange
        var target = FormattableStringBuilder.Empty;

        // Act
        var actual = target.Build();

        // Assert
        Assert.Equal(string.Empty, actual.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void PreserveASingleFormattable()
    {
        // Arrange
        var what = "my name";
        var target = FormattableStringBuilder.From($"My name is {what}");

        // Act
        var actual = target.Build();

        // Assert
        Assert.Equal("My name is {0}", actual.Format);
        Assert.Equal("My name is my name", actual.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void MergeTwoFormattableStrings()
    {
        // Arrange
        var what = "my name";
        var how = "busy";
        var target = FormattableStringBuilder.From($"My name is {what}")
            .Add($" and I'm feeling {how}");

        // Act
        var actual = target.Build();

        // Assert
        Assert.Equal("My name is {0} and I'm feeling {1}", actual.Format);
        Assert.Equal("My name is my name and I'm feeling busy", actual.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void MergeThreeFormattableStrings()
    {
        // Arrange
        var what = "my name";
        var how = "busy";
        var randomNumber = 4;
        var target = FormattableStringBuilder.From($"My name is {what}")
            .Add($" and I'm feeling {how}")
            .Add($". A random number is {randomNumber}");

        // Act
        var actual = target.Build();

        // Assert
        Assert.Equal("My name is {0} and I'm feeling {1}. A random number is {2}", actual.Format);
        Assert.Equal("My name is my name and I'm feeling busy. A random number is 4", actual.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void MergeFormattableStringsWithFormats()
    {
        // Arrange
        var what = "my name";
        var how = "busy";
        var randomNumber = 4;
        var target = FormattableStringBuilder.From($"My name is {what}")
            .Add($" and I'm feeling {how}")
            .Add($". A random number is {randomNumber:0.0}");

        // Act
        var actual = target.Build();

        // Assert
        Assert.Equal("My name is {0} and I'm feeling {1}. A random number is {2:0.0}", actual.Format);
        Assert.Equal("My name is my name and I'm feeling busy. A random number is 4.0", actual.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
