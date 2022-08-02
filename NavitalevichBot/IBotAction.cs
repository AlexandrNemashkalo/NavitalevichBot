using NavitalevichBot.Models;

namespace NavitalevichBot.Actions;


public interface IBotAction
{
    string Name { get; }

    Task<bool> HandleAction(BotActionParams data, CancellationToken cancellationToken = default);
}
