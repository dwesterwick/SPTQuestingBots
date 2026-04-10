using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using UnityEngine;

namespace QuestingBots.Components
{
    public class QuestValidationComponent : MonoBehaviour
    {
        private static string[] ignoredLocations = new string[1] { "Terminal" };

        private bool questsValidated = false;

        protected void Update()
        {
            if (questsValidated || !Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.Enabled)
            {
                return;
            }

            if (Singleton<GameWorld>.Instantiated)
            {
                Singleton<LoggingUtil>.Instance.LogErrorToServerConsole("Could not validate quest files");

                questsValidated = true;
            }

            if (!TarkovApplication.Exist(out TarkovApplication tarkovApplication))
            {
                throw new InvalidOperationException("Could not retrieve TarkovApplication");
            }

            LocationSettingsClass? locationSettings = tarkovApplication.GetClientBackEndSession()?.LocationSettings;
            if (locationSettings == null)
            {
                return;
            }

            validateAllQuestFiles(locationSettings);

            questsValidated = true;
        }

        private void validateAllQuestFiles(LocationSettingsClass locationSettings)
        {
            bool allValidated = true;

            foreach (LocationSettingsClass.Location location in locationSettings.locations.Values)
            {
                if (!location.Enabled || ignoredLocations.Contains(location.Id))
                {
                    continue;
                }

                allValidated &= QuestHelpers.ValidateQuestFiles(location.Id);
            }

            if (allValidated)
            {
                Singleton<LoggingUtil>.Instance.LogInfoToServerConsole("Validated all quest files");
            }
        }
    }
}
