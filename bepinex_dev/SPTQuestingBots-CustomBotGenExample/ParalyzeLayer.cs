using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SPTQuestingBots.Components.Spawning;

namespace SPTQuestingBots_CustomBotGenExample
{
    public class ParalyzeLayer : CustomLayer
    {
        private bool isEnabled = false;

        public ParalyzeLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {
            Singleton<GameWorld>.Instance.TryGetComponent(out TestBotGenerator testBotGenerator);

            // Only use this layer for test bots
            isEnabled = WasBotSpawnedByGenerator(_botOwner, testBotGenerator);
        }

        public static bool WasBotSpawnedByGenerator(BotOwner botOwner, BotGenerator botGenerator)
        {
            return botGenerator?.GetBotGroups()?.Any(group => group.SpawnedBots.Contains(botOwner)) == true;
        }

        public override string GetName() => "ParalyzeLayer";

        public override Action GetNextAction() => new Action(typeof(ParalyzeAction), "Paralyze");
        public override bool IsCurrentActionEnding() => false;

        public override bool IsActive() => isEnabled;
    }
}
