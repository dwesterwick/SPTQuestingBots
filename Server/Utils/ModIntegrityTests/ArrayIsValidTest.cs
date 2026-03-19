using QuestingBots.Helpers;

namespace QuestingBots.Utils.ModIntegrityTests
{
    internal class ArrayIsValidTest : Internal.IModIntegrityTest
    {
        public bool? Result { get; private set; }
        public string? FailureMessage { get; private set; }

        private double[][] _array;
        private bool _leftColumnMustBeInts;

        public ArrayIsValidTest(double[][] array, bool leftColumnMustBeInts)
        {
            _array = array;
            _leftColumnMustBeInts = leftColumnMustBeInts;
        }

        public void Run()
        {
            try
            {
                _array.ValidateChanceArray(_leftColumnMustBeInts);
            }
            catch(Exception ex)
            {
                FailureMessage = ex.Message;
                Result = false;
            }
        }
    }
}
