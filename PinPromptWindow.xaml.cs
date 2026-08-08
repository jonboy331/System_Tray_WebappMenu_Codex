using System.Windows;
using TrayWebApps.Services;
using Key = System.Windows.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace TrayWebApps;

public partial class PinPromptWindow : Window
{
    private readonly string _expectedHash;
    private string _entered = "";
    private const int MaxDigits = 8;

    public PinPromptWindow(string expectedHash, string appName)
    {
        InitializeComponent();
        _expectedHash = expectedHash;
        SubtitleText.Text = $"Enter the PIN to open {appName}.";
    }

    private void Digit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: string digit }) AppendDigit(digit);
    }

    private void AppendDigit(string digit)
    {
        if (_entered.Length >= MaxDigits) return;
        _entered += digit;
        RefreshDisplay();
    }

    private void Backspace_Click(object sender, RoutedEventArgs e)
    {
        if (_entered.Length == 0) return;
        _entered = _entered[..^1];
        RefreshDisplay();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _entered = "";
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        PinDisplay.Text = string.Join(" ", _entered.Select(_ => "●"));
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key >= Key.D0 && e.Key <= Key.D9) { AppendDigit(((int)e.Key - (int)Key.D0).ToString()); return; }
        if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) { AppendDigit(((int)e.Key - (int)Key.NumPad0).ToString()); return; }
        switch (e.Key)
        {
            case Key.Back: Backspace_Click(sender, e); break;
            case Key.Delete: Clear_Click(sender, e); break;
            case Key.Enter: TryUnlock(); break;
            case Key.Escape: DialogResult = false; break;
        }
    }

    private void Unlock_Click(object sender, RoutedEventArgs e) => TryUnlock();

    private void TryUnlock()
    {
        if (_entered.Length > 0 && PasswordService.Verify(_entered, _expectedHash))
        {
            DialogResult = true;
        }
        else
        {
            _entered = "";
            PinDisplay.Text = "";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
