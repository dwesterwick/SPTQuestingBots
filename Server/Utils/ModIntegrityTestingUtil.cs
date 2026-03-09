using QuestingBots.Helpers;
using QuestingBots.Utils.ModIntegrityTests.Internal;
using SPTarkov.DI.Annotations;

namespace QuestingBots.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class ModIntegrityTestingUtil
    {
        private readonly List<IModIntegrityTest> tests = new List<IModIntegrityTest>();
        private LoggingUtil _loggingUtil;

        public bool AllTestsPassed => !tests.Any() || tests.All(test => test.Result == true);
        public bool AnyTestFailed => tests.Any(test => test.Result == false);

        public ModIntegrityTestingUtil(LoggingUtil loggingUtil)
        {
            _loggingUtil = loggingUtil;
        }

        public void AddTest<T>(params object?[] args) where T: IModIntegrityTest
        {
            T newTest = CreateTestInstance<T>(args);
            AddTest(newTest);
        }

        private T CreateTestInstance<T>(params object?[] args) where T : IModIntegrityTest
        {
            if (!TypeHelpers.CanArgumentsCanBeUsedToCreateType<T>(args))
            {
                throw new InvalidOperationException($"Cannot use passed {args.Length} arguments to create a new instance of IModIntegrityTest type {typeof(T)}.");
            }

            T? newTest = (T?)Activator.CreateInstance(typeof(T), args);
            if (newTest == null)
            {
                throw new InvalidOperationException($"Could not create a new instance of IModIntegrityTest type {typeof(T)}");
            }

            return newTest;
        }

        public void AddTest(IModIntegrityTest test)
        {
            if (tests.Contains(test))
            {
                _loggingUtil.Warning($"An instance of IModIntegrityTest type {test.GetType()} has already been added");
                return;
            }

            tests.Add(test);
        }

        public void RunAllTests()
        {
            foreach (IModIntegrityTest test in tests)
            {
                if (test.Result != null)
                {
                    continue;
                }

                test.Run();
            }
        }

        public bool RunAllTestsAndVerifyAllPassed()
        {
            RunAllTests();
            return AllTestsPassed;
        }

        public void LogAllFailureMessages()
        {
            foreach (string failureMessage in GetAllFailureMessages())
            {
                _loggingUtil.Error(failureMessage);
            }
        }

        public IEnumerable<string> GetAllFailureMessages()
        {
            foreach (IModIntegrityTest test in tests)
            {
                if ((test.Result == true) || (test.FailureMessage == null))
                {
                    continue;
                }

                yield return test.FailureMessage;
            }
        }
    }
}
