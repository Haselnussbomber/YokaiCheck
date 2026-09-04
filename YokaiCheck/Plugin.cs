using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace YokaiCheck;

[AutoConstruct]
public partial class Plugin : IAsyncDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private IHost _host;

    [AutoPostConstruct]
    private void Initialize()
    {
        _host = new HostBuilder()
            .UseContentRoot(_pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureHostOptions(options =>
            {
                options.ServicesStartConcurrently = true;
                options.ServicesStopConcurrently = true;
            })
            .ConfigureServices(services =>
            {
                services.AddDalamud(_pluginInterface);
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
            _host.Dispose();
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
