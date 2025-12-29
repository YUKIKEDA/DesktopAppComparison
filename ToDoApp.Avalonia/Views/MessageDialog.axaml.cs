using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace ToDoApp.Avalonia.Views;

public partial class MessageDialog : Window, INotifyPropertyChanged
{
    private string _message = string.Empty;
    private string _primaryButtonText = "OK";
    private string _secondaryButtonText = "キャンセル";
    private bool _hasSecondaryButton = false;

    public string Message
    {
        get => _message;
        set
        {
            if (_message != value)
            {
                _message = value;
                OnPropertyChanged();
            }
        }
    }

    public string PrimaryButtonText
    {
        get => _primaryButtonText;
        set
        {
            if (_primaryButtonText != value)
            {
                _primaryButtonText = value;
                OnPropertyChanged();
            }
        }
    }

    public string SecondaryButtonText
    {
        get => _secondaryButtonText;
        set
        {
            if (_secondaryButtonText != value)
            {
                _secondaryButtonText = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasSecondaryButton
    {
        get => _hasSecondaryButton;
        set
        {
            if (_hasSecondaryButton != value)
            {
                _hasSecondaryButton = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Result { get; private set; } = false;

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public MessageDialog()
    {
        InitializeComponent();
        DataContext = this;
        PrimaryButton.Click += (s, e) => { Result = true; Close(true); };
        SecondaryButton.Click += (s, e) => { Result = false; Close(false); };
    }

    public static async Task<bool> ShowAsync(Window parent, string title, string message, string primaryButtonText = "OK", string secondaryButtonText = "キャンセル", bool showSecondary = false)
    {
        var dialog = new MessageDialog
        {
            Title = title,
            Message = message,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            HasSecondaryButton = showSecondary
        };
        var result = await dialog.ShowDialog<bool>(parent);
        return result;
    }
}

