using System.ComponentModel;
using System.Windows;
using AlAnsariPrivacyShield.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace AlAnsariPrivacyShield;

public partial class MainWindow : Window
{
    private bool _exitRequested;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            await viewModel.InitializeAsync();
    }

    public void PrepareForExit() => _exitRequested = true;

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exitRequested)
        {
            e.Cancel = true;
            Hide();

            if (System.Windows.Application.Current is App app)
                app.HideToTray();

            return;
        }

        base.OnClosing(e);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.ExitApplication();
        else
            Close();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Al-Ansari Privacy Shield v1.0.0\n" +
            "Reversible Windows privacy hardening\n\n" +
            "• 17 protections across telemetry, advertising, search, activity and location\n" +
            "• Three profiles: Balanced, Strict and Maximum\n" +
            "• Services, scheduled data collection and policy settings\n" +
            "• Automatic backup of every original value and full restore\n\n" +
            "Runs with administrator privileges — no driver, no service, no telemetry.",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
