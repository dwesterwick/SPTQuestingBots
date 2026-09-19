using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.ExternalMods.Functions.NetworkTransactions
{
    internal class FikaRunNetworkTransactionsFunction : AbstractRunNetworkTransactionsFunction
    {
        public FikaRunNetworkTransactionsFunction(BotOwner botOwner) : base(botOwner)
        {

        }

        public override bool TryMoveItem(OperationResult<MoveResult> moveResult)
        {
            return true;
        }
    }
}
