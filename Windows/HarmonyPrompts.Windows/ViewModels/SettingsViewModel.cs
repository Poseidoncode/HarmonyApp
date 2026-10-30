namespace HarmonyPrompts.Windows.ViewModels;

public sealed class SettingsViewModel(MainViewModel main)
{
    public string TemplatesPath => main.TemplatesPath;
    public RelayCommand OpenFolderCommand => main.OpenFolderCommand;
    public bool EnableAutoPaste { get => main.EnableAutoPaste; set => main.EnableAutoPaste = value; }
    public string Privacy => "100% offline. No accounts, telemetry, uploads or background network requests. Templates remain in your Windows user profile. Copies opt out of Windows clipboard history and cloud clipboard using OS formats, but other clipboard readers and OS policies are outside the app's control.";
    public string PasteHelp => "Auto Paste is disabled by default pending real-application safety acceptance. The checkbox is an explicit experimental opt-in for this session only; restarting disables it again. It is not appropriate for sensitive content. First focus the intended input in another app, then open Harmony. Copy & Paste copies text, attempts to restore that exact window, and sends Ctrl+V only after identity/focus checks. SendInput does not prove text reached an editor. Windows cannot atomically bind global keyboard input to a window, so these checks are best-effort, not a destination guarantee. For sensitive text use Copy and Ctrl+V manually; do not switch windows during Auto Paste. Never run Harmony as administrator to work around a target's permissions.";
    public string LifecycleHelp => "Closing the main window hides it in the notification area. Use the tray menu → Exit to quit. No global key listener, startup registration or synchronization is installed. Keyboard: Ctrl+Shift+C copies the generated prompt; Ctrl+Alt+V requests Auto Paste. Ctrl+C in text boxes keeps its normal selection behavior.";
}
