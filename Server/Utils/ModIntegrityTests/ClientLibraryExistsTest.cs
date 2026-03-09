using QuestingBots.Helpers;

namespace QuestingBots.Utils.ModIntegrityTests
{
    internal class ClientLibraryExistsTest : Internal.IModIntegrityTest
    {
        private const string RELATIVE_PATH_TO_CLIENT_MOD = $"{ModInfo.RELATIVE_PATH_TO_SPT_INSTALL}../../BepInEx/plugins/{ModInfo.MODNAME}/{ModInfo.MODNAME}-Client.dll";

        public bool? Result { get; private set; }
        public string? FailureMessage { get; private set; }

        private ConfigUtil _configUtil;

        public ClientLibraryExistsTest(ConfigUtil configUtil)
        {
            _configUtil = configUtil;
        }

        public void Run()
        {
            if (!DebugHelpers.IsReleaseBuild())
            {
                Result = true;
                FailureMessage = null;

                return;
            }

            CheckIfClientModFileExists();
        }

        private void CheckIfClientModFileExists()
        {
            string pathToClientMod = Path.GetFullPath(Path.Combine(_configUtil.ServerModDirectory, RELATIVE_PATH_TO_CLIENT_MOD));
            pathToClientMod = Path.GetFullPath(pathToClientMod);
            
            bool result = File.Exists(pathToClientMod);

            Result = result;
            FailureMessage = result ? null : $"Could not find client mod file, {pathToClientMod}. Without it, this mod will NOT work correctly.";
        }
    }
}
