using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace YokaiCheck;

[AutoConstruct]
public partial class Plugin : IAsyncDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private IHost? _host;

    public Task LoadAsync(CancellationToken cancellationToken)
    {
        _host = new HostBuilder()
            .UseContentRoot(_pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(_pluginInterface);
                services.AddHaselCommon();
                services.AddYokaiCheck();
            })
            .Build();

        return _host.StartOnFrameworkThread(_framework, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _host?.StopOnFrameworkThread(_framework) ?? ValueTask.CompletedTask;
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
