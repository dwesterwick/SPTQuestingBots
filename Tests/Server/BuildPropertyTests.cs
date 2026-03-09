using System.Reflection;

namespace QuestingBots.Server;

public class BuildPropertyTests
{
    private ModMetadata modMetadata;

    [SetUp]
    public void Setup()
    {
        modMetadata = new ModMetadata();
    }

    [Test]
    public void CheckModTitle()
    {
        string? buildPropertiesAssemblyTitle = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        Assert.IsNotNull(buildPropertiesAssemblyTitle, "AssemblyTitle has not been set in solution build properties");

        string serverModTitle = modMetadata.Name;
        Assert.True(buildPropertiesAssemblyTitle == serverModTitle, "Mod title does not match value in build properties");
    }

    [Test]
    public void CheckAssemblyVersion()
    {
        string? buildPropertiesVersionString = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        Assert.IsNotNull(buildPropertiesVersionString, "Version has not been set in solution build properties");

        SemanticVersioning.Version buildPropertiesVersion = new(buildPropertiesVersionString);
        SemanticVersioning.Version serverVersion = modMetadata.Version;

        Assert.Zero(buildPropertiesVersion.CompareTo(serverVersion), "Server mod version does not match build properties version");
    }
}