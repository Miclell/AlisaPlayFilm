using Eto.Forms;
using Tray.App.Models;

namespace Tray.App.Components.Tray;

public class TrayMenuBuilder(AppState appState)
{
    public ContextMenu BuildMenu()
    {
        var menu = new ContextMenu();

        var statusItem = new ButtonMenuItem { Text = appState.Status, Enabled = false };
        var showLogsItem = new ButtonMenuItem { Text = "📊 Live Logs" };
        var restartItem = new ButtonMenuItem { Text = "🔄 Restart Server" };
        var separator = new SeparatorMenuItem();
        var quitItem = new ButtonMenuItem { Text = "🚪 Quit" };

        // Подписки на события будут настроены внешне через события
        menu.Items.AddRange([
            statusItem,
            new SeparatorMenuItem(),
            showLogsItem,
            restartItem,
            separator,
            quitItem
        ]);

        return menu;
    }
}