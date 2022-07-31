using NavitalevichBot.Models;

namespace NavitalevichBot.Actions;

public interface IBotAction
{
    Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default);
}
