using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.ExternalMods.Functions.NetworkTransactions
{
    public class FikaRunNetworkTransactionFunctions : AbstractRunNetworkTransactionFunctions
    {
        public static void SetMoveItemFunc(Func<Player, Item, bool> func) => _tryMoveItemFunc = func;
        private static Func<Player, Item, bool>? _tryMoveItemFunc = null;

        public FikaRunNetworkTransactionFunctions(BotOwner botOwner) : base(botOwner)
        {

        }

        public override bool TryMoveItem(OperationResult<MoveResult> moveResult)
        {
            if (_tryMoveItemFunc == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Fika sync plugin did not register a MoveItem function. Spawned keys will not appear in bot inventories on client machines.");

                return base.TryMoveItem(moveResult);
            }

            return _tryMoveItemFunc(BotOwner.GetPlayer, moveResult.Value.Item);
        }
    }
}
