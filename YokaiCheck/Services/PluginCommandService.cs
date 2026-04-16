using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselCommon.Services.Commands;
using Microsoft.Extensions.Hosting;
using YokaiCheck.Windows;

namespace YokaiCheck.Services;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class PluginCommandService : IHostedService
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly WindowManager _windowManager;
    private readonly CommandService _commandService;
    private readonly IClientState _clientState;

    private bool _mainUiHandlerRegistered;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _commandService.AddCommand("yokai", cmd =>
        {
            cmd.WithHelpTextKey("CommandHandlerHelpMessage");
            cmd.WithHandler(OnMainCommand);
        });

        _clientState.Login += OnLogin;
        _clientState.Logout += OnLogout;

        if (_clientState.IsLoggedIn)
            EnableMainUiHandler();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        DisableMainUiHandler();

        _clientState.Login -= OnLogin;
        _clientState.Logout -= OnLogout;

        return Task.CompletedTask;
    }

    private void OnLogin()
    {
        EnableMainUiHandler();
    }

    private void OnLogout(int type, int code)
    {
        DisableMainUiHandler();
    }

    private void OnMainCommand(CommandContext ctx)
    {
        ToggleMainWindow();
    }

    private void EnableMainUiHandler()
    {
        if (!_mainUiHandlerRegistered)
        {
            _pluginInterface.UiBuilder.OpenMainUi += ToggleMainWindow;
            _mainUiHandlerRegistered = true;
        }
    }

    private void DisableMainUiHandler()
    {
        if (_mainUiHandlerRegistered)
        {
            _pluginInterface.UiBuilder.OpenMainUi -= ToggleMainWindow;
            _mainUiHandlerRegistered = false;
        }
    }

    private void ToggleMainWindow()
    {
        _windowManager.CreateOrToggle<MainWindow>();
    }
}
