using CommunityToolkit.Mvvm.ComponentModel;

namespace FreeSeat.App.ViewModels;

/// <summary>
/// Base class for all view models.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject, IDisposable
{
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private string _busyMessage = string.Empty;

    /// <summary>
    /// Disposes the view model.
    /// </summary>
    public virtual void Dispose()
    {
        // Override in derived classes to clean up resources
    }
}