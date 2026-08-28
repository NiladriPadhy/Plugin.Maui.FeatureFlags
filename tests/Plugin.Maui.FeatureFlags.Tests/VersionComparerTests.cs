namespace Plugin.Maui.FeatureFlags.Tests;

public sealed class VersionComparerTests
{
    [Theory]
    [InlineData("1.2.3", "1.2.3", 0)]
    [InlineData("1.2.3", "1.2.10", -1)]
    [InlineData("2.0", "1.9.9", 1)]
    [InlineData("15.0", "15", 0)]
    [InlineData("16.1.2", "16.1", 1)]
    [InlineData("2.0.0-beta", "2.0.0", 0)]
    public void Compare_numeric_segments(string left, string right, int expectedSign)
    {
        var cmp = VersionComparer.Compare(left, right);
        Assert.Equal(expectedSign, Math.Sign(cmp));
    }

    [Fact]
    public void IsAtLeast_treats_missing_minimum_as_pass()
    {
        Assert.True(VersionComparer.IsAtLeast("1.0.0", null));
        Assert.True(VersionComparer.IsAtLeast("2.0.0", "1.9.0"));
        Assert.False(VersionComparer.IsAtLeast("1.0.0", "2.0.0"));
    }
}
