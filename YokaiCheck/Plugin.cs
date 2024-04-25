using Dalamud.Game.Command;
using Dalamud.Plugin;
using YokaiCheck.Windows;

namespace YokaiCheck;

public sealed class Plugin : IDalamudPlugin
{
    private readonly CommandInfo CommandInfo;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        Service.Initialize(pluginInterface);

        Service.PluginInterface.LanguageChanged += PluginInterface_LanguageChanged;
        Service.PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;

        CommandInfo = new CommandInfo(OnCommand) { HelpMessage = t("CommandHandlerHelpMessage") };

        Service.CommandManager.AddHandler("/yokai", CommandInfo);
    }

    private void PluginInterface_LanguageChanged(string langCode)
    {
        CommandInfo.HelpMessage = t("CommandHandlerHelpMessage");
    }

    private void OpenMainUi()
    {
        Service.WindowManager.ToggleWindow<MainWindow>();
    }

    private void OnCommand(string command, string arguments)
    {
        Service.WindowManager.ToggleWindow<MainWindow>();
    }

    void IDisposable.Dispose()
    {
        Service.CommandManager.RemoveHandler("/yokai");

        Service.PluginInterface.LanguageChanged -= PluginInterface_LanguageChanged;
        Service.PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;

        Service.Dispose();
    }
}
