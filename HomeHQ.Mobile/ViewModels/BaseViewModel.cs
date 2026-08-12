using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HomeHQ.Mobile.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get;
        set => SetProperty(ref field, value);
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Executes an async action with standardised exception handling.
    /// Calls <paramref name="onError"/> when an exception is caught;
    /// if no handler is supplied the exception is swallowed silently.
    /// </summary>
    protected static async Task SafeExecuteAsync(Func<Task> action, Action<Exception>? onError = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Executes an async function that returns a value with standardised
    /// exception handling.  Returns <paramref name="defaultValue"/> on failure.
    /// </summary>
    protected static async Task<T> SafeExecuteAsync<T>(Func<Task<T>> action, T defaultValue, Action<Exception>? onError = null)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
            return defaultValue;
        }
    }
}
