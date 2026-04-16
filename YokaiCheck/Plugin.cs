using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace YokaiCheck;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;

    public Plugin(IDalamudPluginInterface pluginInterface, IFramework framework)
    {
        _host = new HostBuilder()
            .UseContentRoot(pluginInterface.AssemblyLocation.Directory!.FullName)
            .ConfigureServices(services =>
            {
                services.AddDalamud(pluginInterface);
                services.AddHaselCommon();
                services.AddYokaiCheck();
            })
            .Build();

        framework.RunOnFrameworkThread(_host.Start);
    }

    void IDisposable.Dispose()
    {
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
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
