using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HarmonyPrompts.Windows.ViewModels;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(property);
        return true;
    }
    protected void Raise([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new(property));
}
