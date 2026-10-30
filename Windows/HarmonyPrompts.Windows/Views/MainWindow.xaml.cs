using System;
using System.ComponentModel;
using System.Windows;

namespace HarmonyPrompts.Windows.Views;

public partial class MainWindow : Window
{
    public bool IsExiting { get; set; }
    public event EventHandler? HiddenToTray;
    public MainWindow() => InitializeComponent();
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!IsExiting) { e.Cancel = true; Hide(); HiddenToTray?.Invoke(this, EventArgs.Empty); }
        base.OnClosing(e);
    }
}
