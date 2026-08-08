using System.Windows;
using Key = System.Windows.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace TrayWebApps;

public partial class SetPinWindow : Window
{
    private string _entered = "";
    private const int MaxDigits = 8;

    public bool RemovePin { get; private set; }
    public string EnteredPin => _entered;

    public SetPinWindow(string appName)
    {
        InitializeComponent();
        SubtitleText.Text = $"Enter a new PIN required to open {appName}.";
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

    private void ClearDigits_Click(object sender, RoutedEventArgs e)
    {
        _entered = "";
        RefreshDisplay();
    }

    private void RefreshDisplay() => PinDisplay.Text = string.Join(" ", _entered.Select(_ => "●"));

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key >= Key.D0 && e.Key <= Key.D9) { AppendDigit(((int)e.Key - (int)Key.D0).ToString()); return; }
        if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) { AppendDigit(((int)e.Key - (int)Key.NumPad0).ToString()); return; }
        switch (e.Key)
        {
            case Key.Back: Backspace_Click(sender, e); break;
            case Key.Delete: ClearDigits_Click(sender, e); break;
            case Key.Enter: Save_Click(sender, e); break;
            case Key.Escape: DialogResult = false; break;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_entered.Length == 0) return;
        DialogResult = true;
    }

    private void RemovePin_Click(object sender, RoutedEventArgs e)
    {
        RemovePin = true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
