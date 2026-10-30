using System.Windows;

namespace HarmonyPrompts.Windows.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow() => InitializeComponent();
    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
