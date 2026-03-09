using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace QuestingBots;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + QuestingBots_Server.LOAD_ORDER_OFFSET)]
public class QuestingBots_Server
{
    public const int LOAD_ORDER_OFFSET = 1;

    public QuestingBots_Server()
    {

    }

    public Task OnLoad()
    {
        return Task.CompletedTask;
    }
}
