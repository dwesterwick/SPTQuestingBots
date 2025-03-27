using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Components
{
    public class TarkovData : MonoBehaviour
    {
        private static string[] ignoredLocations = new string[1] { "Terminal" };

        private TarkovApplication tarkovApplication = null;
        private bool questsValidated = false;

        protected void Update()
        {
            if (questsValidated || !ConfigController.Config.Questing.Enabled)
            {
                return;
            }

            if (Singleton<GameWorld>.Instantiated)
            {
                LoggingController.LogErrorToServerConsole("Could not validate quest files");

                questsValidated = true;
            }

            LocationSettingsClass locationSettings = GetSession()?.LocationSettings;
            if (locationSettings == null)
            {
                return;
            }

            validateAllQuestFiles(locationSettings);

            questsValidated = true;
        }

        public RaidSettings GetCurrentRaidSettings()
        {
            if (getTarkovApplication() == null)
            {
                LoggingController.LogError("Invalid Tarkov application instance");
                return null;
            }

            return tarkovApplication.CurrentRaidSettings;
        }

        public ISession GetSession()
        {
            if (getTarkovApplication() == null)
            {
                LoggingController.LogError("Invalid Tarkov application instance");
                return null;
            }

            ISession session = tarkovApplication.GetClientBackEndSession();
            return session;
        }

        private TarkovApplication getTarkovApplication()
        {
            if (tarkovApplication != null)
            {
                return tarkovApplication;
            }

            tarkovApplication = FindObjectOfType<TarkovApplication>();

            return tarkovApplication;
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
                LoggingController.LogInfoToServerConsole("Validated all quest files");
            }
        }
    }
}
