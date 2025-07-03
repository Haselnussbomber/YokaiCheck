using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Microsoft.Extensions.DependencyInjection;
using YokaiCheck.Services;

namespace YokaiCheck;

public sealed class Plugin : IDalamudPlugin
{
    private readonly ServiceProvider _serviceProvider;

    public Plugin(IDalamudPluginInterface pluginInterface, IFramework framework)
    {
        _serviceProvider = new ServiceCollection()
            .AddDalamud(pluginInterface)
            .AddHaselCommon()
            .AddYokaiCheck()
            .BuildServiceProvider();

        framework.RunOnFrameworkThread(() =>
        {
            _serviceProvider.GetRequiredService<CommandManager>();
        });
    }

    void IDisposable.Dispose()
    {
        _serviceProvider.Dispose();
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
