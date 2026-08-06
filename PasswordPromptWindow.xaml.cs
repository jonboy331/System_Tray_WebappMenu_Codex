using System.Windows;
using TrayWebApps.Services;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace TrayWebApps;

public partial class PasswordPromptWindow : Window
{
    private readonly string _expectedHash;

    public PasswordPromptWindow(string expectedHash)
    {
        InitializeComponent();
        _expectedHash = expectedHash;
        Loaded += (_, _) => PasswordInput.Focus();
    }

    private void Unlock_Click(object sender, RoutedEventArgs e) => TryUnlock();

    private void PasswordInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter) TryUnlock();
    }

    private void TryUnlock()
    {
        if (PasswordService.Verify(PasswordInput.Password, _expectedHash))
        {
            DialogResult = true;
        }
        else
        {
            ErrorText.Visibility = Visibility.Visible;
            PasswordInput.Clear();
            PasswordInput.Focus();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
