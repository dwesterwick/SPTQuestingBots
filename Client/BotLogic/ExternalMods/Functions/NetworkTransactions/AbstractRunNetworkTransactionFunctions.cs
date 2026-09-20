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
    public abstract class AbstractRunNetworkTransactionFunctions : AbstractBaseExternalFunctionForBot
    {
        public AbstractRunNetworkTransactionFunctions(BotOwner botOwner) : base (botOwner)
        {

        }

        public virtual bool TryMoveItem(OperationResult<MoveResult> moveResult)
        {
            if (moveResult.Value.Item.Parent.Equals(moveResult.Value.To))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Item " + moveResult.Value.Item.LocalizedName() + " is already in the inventory of " + BotOwner.GetText());
                return true;
            }

            InventoryController inventoryController = BotOwner.GetInventoryController();
            Callback callback = new Callback(MoveItemCallback);

            if (QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Moving item " + moveResult.Value.Item.LocalizedName() + " to inventory of " + BotOwner.GetText() + "...");
            }

            // Execute the transation to transfer the key to the bot
            Task<IResult> networkTask = inventoryController.TryRunNetworkTransaction(moveResult, callback);

            return !networkTask.IsFaulted;
        }

        protected void MoveItemCallback(IResult result)
        {
            if (result.Succeed && QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Moved item to inventory of " + BotOwner.GetText());
            }

            if (result.Failed)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not move item to inventory of " + BotOwner.GetText() + " - Error " + result.ErrorCode + ": " + result.Error);
            }
        }
    }
}
