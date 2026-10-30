using HarmonyPrompts.Core.Services;

namespace HarmonyPrompts.Windows.Services;

public interface IUserInteraction
{
    string? PickImportPath();
    void OpenTemplatesFolder();
    void ShowSettings();
    PasteTarget? PasteTarget { get; }
    void NotifyPasteResult(string message, bool warning);
}
