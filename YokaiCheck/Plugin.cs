using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Microsoft.Extensions.DependencyInjection;

namespace YokaiCheck;

[AutoConstruct]
public partial class Plugin : IAsyncDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private Host _host;

    [AutoPostConstruct]
    private void Initialize()
    {
        _host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_pluginInterface);
                services.AddHaselCommon();
                services.AddYokaiCheck();
            })
            .Build();
    }

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        return _host.StartAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _host.StopAsync().ConfigureAwait(false);
        }
        finally
        {
            await _host.DisposeAsync().ConfigureAwait(false);
        }
    }

    public static unsafe uint GetCurrentMinionId()
    {
        var player = Control.GetLocalPlayer();
        if (player == null)
            return 0;

        var companion = player->Character.CompanionData.CompanionObject;
        if (companion == null)
            return 0;

        return companion->Character.GameObject.BaseId;
    }
}
