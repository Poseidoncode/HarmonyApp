using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using HarmonyPrompts.Core.Services;
using HarmonyPrompts.Core.Storage;
using HarmonyPrompts.Windows.Services;
using HarmonyPrompts.Windows.ViewModels;
using HarmonyPrompts.Windows.Views;
using Microsoft.Win32;

namespace HarmonyPrompts.Windows;

public partial class App : System.Windows.Application, IUserInteraction
{
    private Mutex? _instanceMutex;
    private WindowFocusService? _focus;
    private TrayService? _tray;
    private MainWindow? _main;
    private MainViewModel? _viewModel;
    private SettingsWindow? _settings;
    private bool _exiting;
    private TemplateRepository? _repository;
    public PasteTarget? PasteTarget => _focus?.SessionTarget;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _instanceMutex = new Mutex(true, @"Local\HarmonyPrompts.Windows.Desktop", out var firstInstance);
        if (!firstInstance)
        {
            System.Windows.MessageBox.Show("Harmony Prompts is already running. Open it from its notification-area icon.", "Harmony Prompts");
            Shutdown();
            return;
        }
        try
        {
            _focus = new WindowFocusService();
            _main = new MainWindow();
            MainWindow = _main;
            _repository = new TemplateRepository(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HarmonyPrompts", "templates.json"),
                Path.Combine(AppContext.BaseDirectory, "Resources", "templates.json"));
            var clipboard = new ClipboardService(Dispatcher);
            var paste = new PasteService(new WindowsPastePlatform(clipboard, _focus, HideHarmony));
            _viewModel = new MainViewModel(_repository, paste, this);
            _main.DataContext = _viewModel;
            _focus.TargetChanged += (_, _) => _viewModel.RefreshTarget();
            _tray = new TrayService(OpenMain, ShowSettings, ExitApplication);
            _main.HiddenToTray += (_, _) => _tray.Notify("Still running. Click the notification-area icon to reopen; use Exit to quit.", false);
            OpenMain();
            await _viewModel.LoadAsync();
        }
        catch (Exception)
        {
            System.Windows.MessageBox.Show("Harmony Prompts could not start. Check that Resources/templates.json is present and the notification area is available. No user templates were deleted.", "Harmony Prompts");
            ExitApplication();
        }
    }

    private void OpenMain()
    {
        if (_main is null || _exiting) return;
        _focus?.CaptureBeforeOpen();
        _main.Show();
        if (_main.WindowState == WindowState.Minimized) _main.WindowState = WindowState.Normal;
        _main.Activate();
        _viewModel?.RefreshTarget();
    }
    public void ShowSettings()
    {
        if (_viewModel is null || _main is null) return;
        _focus?.CaptureBeforeOpen();
        if (_settings is null)
        {
            _settings = new SettingsWindow { DataContext = new SettingsViewModel(_viewModel), Owner = _main };
            _settings.Closed += (_, _) => _settings = null;
        }
        _settings.Show();
        _settings.Activate();
    }
    private void HideHarmony() { _settings?.Hide(); _main?.Hide(); }
    public string? PickImportPath()
    {
        var dialog = new OpenFileDialog { Title = "Import templates — replaces the current collection", Filter = "JSON templates (*.json)|*.json|All files (*.*)|*.*", CheckFileExists = true, Multiselect = false };
        return dialog.ShowDialog(_main) == true ? dialog.FileName : null;
    }
    public void OpenTemplatesFolder()
    {
        var folder = Path.GetDirectoryName(_viewModel!.TemplatesPath)!;
        Directory.CreateDirectory(folder);
        // Only the app-controlled folder is opened; no prompt/template text is executed.
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }
    public void NotifyPasteResult(string message, bool warning) => _tray?.Notify(message, warning);

    private async void ExitApplication()
    {
        if (_exiting) return;
        _exiting = true;
        if (_viewModel is not null && _viewModel.IsBusy)
        {
            // Don't tear down a file operation/dialog mid-write. User can retry Exit.
            _exiting = false;
            _tray?.Notify("Finish the current operation before exiting.", true);
            return;
        }
        if (_repository is not null)
        {
            try { await _repository.WhenIdleAsync(); }
            catch (Exception) { /* Operation error has already been surfaced in UI. */ }
        }
        if (_main is not null) _main.IsExiting = true;
        _settings?.Close();
        _main?.Close();
        Shutdown();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _focus?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
