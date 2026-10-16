using Comfort.Common;
using Fika.Core.Main.GameMode;
using QuestingBots.BehaviorExtensions;
using QuestingBots.Utils.DataBroadcasters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.Components
{
    internal class BotInfoDataBroadcasterComponent : MonoBehaviourDelayedUpdate
    {
        private static IEnumerable<Type>? _allDataBroadcasterTypes = null;
        public static IEnumerable<Type> AllDataBroadcasterTypes
        {
            get
            {
                if (_allDataBroadcasterTypes == null)
                {
                    _allDataBroadcasterTypes = GetAllDataBroadcasterTypes();
                }

                return _allDataBroadcasterTypes;
            }
        }

        private static List<IAbstractBotDataBroadcaster>? _dataBroadcasters = null;
        public static List<IAbstractBotDataBroadcaster> DataBroadcasters
        {
            get
            {
                if (_dataBroadcasters == null)
                {
                    _dataBroadcasters = CreateAllDataBroadcasters();
                }

                return _dataBroadcasters;
            }
        }

        public BotInfoDataBroadcasterComponent()
        {
            UpdateInterval = 5;
        }

        protected void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            HostGameController? hostGameController = Singleton<IBotGame>.Instance as HostGameController;
            if (hostGameController == null)
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("No HostGameController instance available");
                return;
            }

            foreach (IAbstractBotDataBroadcaster botDataBroadcaster in DataBroadcasters)
            {
                botDataBroadcaster.Broadcast(hostGameController.Bots.Values);
            }
        }

        private static List<IAbstractBotDataBroadcaster> CreateAllDataBroadcasters()
        {
            List<IAbstractBotDataBroadcaster> allDataBroadcasters = AllDataBroadcasterTypes
                .Select(t => (IAbstractBotDataBroadcaster)Activator.CreateInstance(t))
                .ToList();

#if DEBUG
            QuestingBotsFikaSyncPlugin.PluginLogger.LogDebug("Created bot data broadcasters: " + string.Join(", ", AllDataBroadcasterTypes.Select(t => t.Name)));
#endif

            return allDataBroadcasters;
        }

        private static IEnumerable<Type> GetAllDataBroadcasterTypes()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IAbstractBotDataBroadcaster).IsAssignableFrom(t));
        }
    }
}
