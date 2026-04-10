namespace QuestingBots.Utils.ModIntegrityTests.Internal
{
    public interface IModIntegrityTest
    {
        public bool? Result { get; }
        public string? FailureMessage { get; }
        public void Run();
    }
}
