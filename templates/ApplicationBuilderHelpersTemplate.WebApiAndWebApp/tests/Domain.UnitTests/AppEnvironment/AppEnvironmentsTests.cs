using System;
using Domain.AppEnvironment.Constants;

namespace Domain.UnitTests.AppEnvironment;

public class AppEnvironmentsTests
{
    [Theory]
    [InlineData("prerelease", "Development", "pre")]
    [InlineData("master", "Production", "prod")]
    public void GetByTag_ReturnsExpectedEnvironment(string tag, string name, string shortName)
    {
        var environment = AppEnvironments.GetByTag(tag);

        Assert.Equal(name, environment.Environment);
        Assert.Equal(shortName, environment.EnvironmentShort);
    }

    [Fact]
    public void GetByTag_WithInvalidTag_Throws()
    {
        Assert.Throws<ArgumentException>(() => AppEnvironments.GetByTag("unknown"));
    }

    [Fact]
    public void IsValidHelpers_RespectCaseInsensitivity()
    {
        Assert.True(AppEnvironments.IsValidAppTag("MASTER"));
        Assert.True(AppEnvironments.IsValidEnvironment("production"));
        Assert.False(AppEnvironments.IsValidAppTag("devops"));
    }
}
