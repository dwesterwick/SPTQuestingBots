namespace QuestingBots.Common;

internal class FileLocationTests
{
    private ModMetadata modMetadata;

    [SetUp]
    public void Setup()
    {
        modMetadata = new ModMetadata();
    }

    [Test]
    public void EnsureEftExecutableExists()
    {
        string cd = Directory.GetCurrentDirectory();
        string pathToSptServerExecutable = Path.Combine(cd, modMetadata.RelativePathToSptInstall, "../../../../EscapeFromTarkov.exe");

        bool executableExists = File.Exists(pathToSptServerExecutable);
        Assert.IsTrue(executableExists);
    }
}
