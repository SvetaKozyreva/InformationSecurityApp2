
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InformationSecurityApp.Models;
using InformationSecurityApp.Services;

namespace InformationSecurityApp.Views;

public partial class UserManagementWindow : Window
{
    private readonly FileStorageService _storage;
    private readonly AuditService _audit;

    private bool _isReady;

    public UserManagementWindow()
    {
        InitializeComponent();

        _storage = new FileStorageService();
        _audit = new AuditService();

        RoleBox.SelectedIndex = 0;
        DirectoryBox.SelectedIndex = 0;
        ExpiryPicker.SelectedDate = DateTime.Today.AddDays(30);

        RefreshUsers();

        _isReady = true;
        UpdatePermissionControls();
    }

    // Оновлення списку користувачів
    private void RefreshUsers()
    {
        string? selectedLogin =
            (UsersGrid.SelectedItem as User)?.Login;

        UsersGrid.ItemsSource = null;
        UsersGrid.ItemsSource = _storage.LoadUsers();

        if (!string.IsNullOrEmpty(selectedLogin))
        {
            foreach (User user in UsersGrid.Items)
            {
                if (user.Login.Equals(
                    selectedLogin,
                    StringComparison.OrdinalIgnoreCase))
                {
                    UsersGrid.SelectedItem = user;
                    break;
                }
            }
        }

        if (_isReady)
        {
            UpdatePermissionControls();
        }
    }

    // Кнопка оновлення списку
    private void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        RefreshUsers();
    }

    // Додавання користувача
    private void AddButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string login = LoginBox.Text.Trim();
        string password = PasswordBox.Password;

        string role =
            (RoleBox.SelectedItem as ComboBoxItem)
            ?.Content?.ToString() ?? "User";

        if (string.IsNullOrWhiteSpace(login))
        {
            MessageBox.Show("Введіть логін користувача.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Введіть пароль користувача.");
            return;
        }

        if (login.Contains('|') ||
            login.Contains(';') ||
            password.Contains('|') ||
            password.Contains(';'))
        {
            MessageBox.Show(
                "Логін і пароль не повинні містити символи | або ;.");
            return;
        }

        if (ExpiryPicker.SelectedDate == null)
        {
            MessageBox.Show("Вкажіть термін дії пароля.");
            return;
        }

        DateTime expiryDate = ExpiryPicker.SelectedDate.Value;

        if (expiryDate.Date < DateTime.Today)
        {
            MessageBox.Show(
                "Термін дії пароля не може бути в минулому.");
            return;
        }

        var user = new User
        {
            Login = login,
            Password = password,
            Role = role,
            PasswordValidUntil = expiryDate
        };

        bool added = _storage.AddUser(user, out string message);

        MessageBox.Show(
            message,
            "Керування користувачами",
            MessageBoxButton.OK,
            added
                ? MessageBoxImage.Information
                : MessageBoxImage.Warning);

        _audit.Write(
            "admin",
            added
                ? $"Додано користувача {login}"
                : $"Не вдалося додати користувача {login}: {message}");

        if (!added)
        {
            return;
        }

        LoginBox.Clear();
        PasswordBox.Clear();
        RoleBox.SelectedIndex = 0;
        ExpiryPicker.SelectedDate = DateTime.Today.AddDays(30);

        RefreshUsers();

        foreach (User item in UsersGrid.Items)
        {
            if (item.Login.Equals(
                login,
                StringComparison.OrdinalIgnoreCase))
            {
                UsersGrid.SelectedItem = item;
                break;
            }
        }

        UpdatePermissionControls();
    }

    // Видалення користувача
    private void DeleteButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not User selectedUser)
        {
            MessageBox.Show(
                "Спочатку виберіть користувача в таблиці.");
            return;
        }

        MessageBoxResult answer = MessageBox.Show(
            $"Видалити користувача «{selectedUser.Login}»?",
            "Підтвердження видалення",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        bool deleted = _storage.DeleteUser(
            selectedUser.Login,
            out string message);

        MessageBox.Show(
            message,
            "Керування користувачами",
            MessageBoxButton.OK,
            deleted
                ? MessageBoxImage.Information
                : MessageBoxImage.Warning);

        _audit.Write(
            "admin",
            deleted
                ? $"Видалено користувача {selectedUser.Login}"
                : $"Не вдалося видалити користувача {selectedUser.Login}: {message}");

        RefreshUsers();
        UpdatePermissionControls();
    }

    // Зміна вибраного користувача
    private void UsersGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isReady)
        {
            UpdatePermissionControls();
        }
    }

    // Зміна вибраного каталогу
    private void DirectoryBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isReady)
        {
            UpdatePermissionControls();
        }
    }

    // Відображення поточних прав користувача
    private void UpdatePermissionControls()
    {
        if (UsersGrid.SelectedItem is not User selectedUser)
        {
            SetPermissionChecks(string.Empty);

            PermissionsInfoText.Text =
                "Виберіть користувача в таблиці нижче.";

            return;
        }

        string directory = GetSelectedDirectory();

        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        string rights = "R";

        if (selectedUser.Permissions != null &&
            selectedUser.Permissions.TryGetValue(
                directory,
                out string? savedRights))
        {
            rights = savedRights ?? string.Empty;
        }

        SetPermissionChecks(rights);

        PermissionsInfoText.Text =
            $"Користувач: {selectedUser.Login}. " +
            $"Каталог {directory}. Поточні права: {rights}";
    }

    // Отримання назви вибраного каталогу
    private string GetSelectedDirectory()
    {
        return (DirectoryBox.SelectedItem as ComboBoxItem)
            ?.Content?.ToString() ?? string.Empty;
    }

    // Встановлення прапорців відповідно до прав
    private void SetPermissionChecks(string rights)
    {
        ReadCheck.IsChecked = rights.Contains('R');
        WriteCheck.IsChecked = rights.Contains('W');
        AppendCheck.IsChecked = rights.Contains('A');
        ExecuteCheck.IsChecked = rights.Contains('E');
    }

    // Збереження прав доступу
    private void SavePermissionsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not User selectedUser)
        {
            MessageBox.Show(
                "Спочатку виберіть користувача в таблиці.");
            return;
        }

        string directory = GetSelectedDirectory();

        if (string.IsNullOrEmpty(directory))
        {
            MessageBox.Show("Виберіть каталог.");
            return;
        }

        string rights = string.Empty;

        if (ReadCheck.IsChecked == true)
        {
            rights += "R";
        }

        if (WriteCheck.IsChecked == true)
        {
            rights += "W";
        }

        if (AppendCheck.IsChecked == true)
        {
            rights += "A";
        }

        if (ExecuteCheck.IsChecked == true)
        {
            rights += "E";
        }

        if (rights.Length == 0)
        {
            MessageBox.Show(
                "Виберіть хоча б одне право доступу.");
            return;
        }

        bool saved = _storage.SetUserDirectoryPermissions(
            selectedUser.Login,
            directory,
            rights,
            out string message);

        MessageBox.Show(
            message,
            "Права доступу",
            MessageBoxButton.OK,
            saved
                ? MessageBoxImage.Information
                : MessageBoxImage.Warning);

        if (!saved)
        {
            return;
        }

        _audit.Write(
            "admin",
            $"Змінено права користувача {selectedUser.Login} " +
            $"для каталогу {directory}: {rights}");

        RefreshUsers();
        UpdatePermissionControls();
    }
}
