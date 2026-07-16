using System; // Zona14
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server._Zona14.Administration.Logs; // Zona14
using Content.Shared.Administration.Logs;

namespace Content.Server.Administration.Logs;

public interface IAdminLogManager : ISharedAdminLogManager
{
    void Initialize();
    Task Shutdown();
    void Update();

    void RoundStarting(int id);
    void RunLevelChanged(GameRunLevel level);

    Task<List<SharedAdminLog>> All(LogFilter? filter = null, Func<List<SharedAdminLog>>? listProvider = null);
    IAsyncEnumerable<string> AllMessages(LogFilter? filter = null);
    IAsyncEnumerable<JsonDocument> AllJson(LogFilter? filter = null);
    Task<Round> Round(int roundId);
    Task<List<SharedAdminLog>> CurrentRoundLogs(LogFilter? filter = null);
    IAsyncEnumerable<string> CurrentRoundMessages(LogFilter? filter = null);
    IAsyncEnumerable<JsonDocument> CurrentRoundJson(LogFilter? filter = null);
    Task<Round> CurrentRound();
    Task<int> CountLogs(int round);

    // Zona14: expose player ids from cached admin logs so the log viewer can select them even when the round player list is not yet updated.
    IEnumerable<Guid> GetCachedRoundPlayers(int roundId);

    // Zona14: raised after each admin log is added so anti-cheat/alerting systems can react without polling.
    event EventHandler<AdminLogAddedEventArgs> OnAdminLogAdded;
}
