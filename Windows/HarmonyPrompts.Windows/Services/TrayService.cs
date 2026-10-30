using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HarmonyPrompts.Windows.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu;
    private readonly Icon _drawingIcon;
    public TrayService(Action open, Action settings, Action exit)
    {
        var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/HarmonyPrompts.ico"))?.Stream
            ?? throw new FileNotFoundException("Application icon resource is missing.");
        using (stream) _drawingIcon = new Icon(stream);
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Open Harmony Prompts", null, (_, _) => open());
        _menu.Items.Add("Settings…", null, (_, _) => settings());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => exit());
        _icon = new NotifyIcon { Icon = _drawingIcon, Text = "Harmony Prompts", ContextMenuStrip = _menu, Visible = true };
        _icon.DoubleClick += (_, _) => open();
        _icon.MouseClick += (_, args) => { if (args.Button == MouseButtons.Left) open(); };
    }
    public void Notify(string text, bool warning) => _icon.ShowBalloonTip(5000, "Harmony Prompts", text,
        warning ? ToolTipIcon.Warning : ToolTipIcon.Info);
    public void Dispose() { _icon.Visible = false; _icon.Dispose(); _menu.Dispose(); _drawingIcon.Dispose(); }
}
