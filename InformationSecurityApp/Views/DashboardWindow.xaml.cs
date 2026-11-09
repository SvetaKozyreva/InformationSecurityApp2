using System;
using System.Globalization;
using System.Windows;
using InformationSecurityApp.Views;
using InformationSecurityApp.Models;
using InformationSecurityApp.Services;

namespace InformationSecurityApp.Views;

public partial class DashboardWindow : Window
{
    private readonly User _currentUser;
    private int _actionsSinceAuthentication = 0;

    public DashboardWindow(User user)
    {
        InitializeComponent();

        _currentUser = user;

        WelcomeText.Text = $"Вітаємо, {user.Login}!";
        RoleText.Text = $"Роль користувача: {user.Role}";

        // Керування користувачами доступне лише адміністратору.
        ManageUsersButton.Visibility =
            string.Equals(
                user.Role?.Trim(),
                "Admin",
                StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void ManageUsersButton_Click(
        object sender, RoutedEventArgs e)
    {
        if (_currentUser.Role != "Admin")
        {
            MessageBox.Show(
                "Недостатньо прав для керування користувачами.",
                "Доступ заборонено",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var window = new UserManagementWindow();
        window.ShowDialog();

        RegisterUserAction(); 
    }


    private void ScrollToFunction_Click(object sender, RoutedEventArgs e)
    {
        var window = new AuthenticationWindow(_currentUser);

        window.AuthenticationFailed += () =>
        {
            var loginWindow = new MainWindow();
            loginWindow.Show();

            Close();
        };

        window.Show();
    }


    private void CalculateButton_Click(
        object sender, RoutedEventArgs e)
    {
        string value = XInput.Text.Trim();

        if (!double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out double x)
            &&
            !double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out x))
        {
            ResultText.Text = "Введіть коректне числове значення X.";
            RegisterUserAction();
            return;

        }

        double result = 4 * x * Math.Log(7);

        ResultText.Text =
            $"F({x}) = {result:F4}";
        RegisterUserAction();
    }


    private void CryptoButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new CryptoWindow(_currentUser);
        window.ShowDialog();

        RegisterUserAction();
    }

    private void AuditButton_Click(object sender, RoutedEventArgs e)
    {
        string projectFolder = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", ".."));

        string logPath = System.IO.Path.Combine(
            projectFolder, "Files", "us_book.txt");

        if (!System.IO.File.Exists(logPath))
        {
            MessageBox.Show(
                "Файл журналу ще не створено.",
                "Журнал подій",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        string content = System.IO.File.ReadAllText(logPath);

        if (string.IsNullOrWhiteSpace(content))
        {
            content = "Журнал поки порожній.";
        }

        JournalWindow window = new JournalWindow(content);
        window.ShowDialog();

        RegisterUserAction();
    }


    private void RegisterUserAction()
    {
        _actionsSinceAuthentication++;

        if (_actionsSinceAuthentication < 8)
            return;

        _actionsSinceAuthentication = 0;

        var window = new AuthenticationWindow(_currentUser);

        window.AuthenticationFailed += () =>
        {
            var loginWindow = new MainWindow();
            loginWindow.Show();

            Close();
        };

        window.ShowDialog();
    }
    private void FileOperationsButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new FileOperationsWindow(_currentUser);
        window.ShowDialog();

        RegisterUserAction();
    }

}
