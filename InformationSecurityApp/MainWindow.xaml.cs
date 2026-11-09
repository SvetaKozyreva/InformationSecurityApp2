
using System.Linq;
using System.Windows;
using InformationSecurityApp.Services;
using InformationSecurityApp.Views;

namespace InformationSecurityApp;

public partial class MainWindow : Window
{
    private readonly FileStorageService _storage;
    private readonly AuditService _audit;

    public MainWindow()
    {
        InitializeComponent();

        _storage = new FileStorageService();
        _audit = new AuditService();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginBox.Text.Trim();
        string password = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(password))
        {
            StatusText.Text = "Введіть логін і пароль.";
            return;
        }

        var user = _storage.LoadUsers().FirstOrDefault(u =>
            u.Login.Equals(login, StringComparison.OrdinalIgnoreCase)
            && u.Password == password);

        if (user == null)
        {
            StatusText.Text = "Неправильний логін або пароль.";

            _audit.Write(login, "Невдала спроба входу");
            return;
        }

        if (user.PasswordValidUntil.Date < DateTime.Today)
        {
            StatusText.Text = "Термін дії пароля закінчився.";

            _audit.Write(login, "Вхід відхилено: пароль прострочений");
            return;
        }

        _audit.Write(login, "Успішний вхід у систему");

        DashboardWindow dashboard = new DashboardWindow(user);
        dashboard.Show();

        Close();
    }
}
