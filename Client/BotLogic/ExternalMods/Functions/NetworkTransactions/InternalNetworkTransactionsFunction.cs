using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.ExternalMods.Functions.NetworkTransactions
{
    public class InternalNetworkTransactionsFunction : AbstractRunNetworkTransactionsFunction
    {
        public InternalNetworkTransactionsFunction(BotOwner botOwner) : base(botOwner)
        {

        }

        public override bool TryMoveItem(OperationResult<MoveResult> moveResult)
        {
            InventoryController inventoryController = BotOwner.GetInventoryController();
            Callback callback = new Callback(MoveItemCallback);

            if (QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Moving key " + moveResult.Value.Item.LocalizedName() + " to inventory of " + BotOwner.GetText() + "...");
            }

            // Execute the transation to transfer the key to the bot
            Task<IResult> networkTask = inventoryController.TryRunNetworkTransaction(moveResult, callback);

            return !networkTask.Result.Failed;
        }

        private void MoveItemCallback(IResult result)
        {
            if (result.Succeed && QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Moved key to inventory of " + BotOwner.GetText());
            }

            if (result.Failed)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not move key to inventory of " + BotOwner.GetText());
            }
        }
    }
}
