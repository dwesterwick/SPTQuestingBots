using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.ExternalMods.Functions.NetworkTransactions
{
    public abstract class AbstractRunNetworkTransactionsFunction : AbstractBaseExternalFunction
    {
        public AbstractRunNetworkTransactionsFunction(BotOwner botOwner) : base (botOwner)
        {

        }

        public abstract bool TryMoveItem(OperationResult<MoveResult> moveResult);
    }
}
