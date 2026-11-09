
using InformationSecurityApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InformationSecurityApp.Services;

public class FileStorageService
{
    private readonly string _dataFolder;
    private readonly string _usersFile;

    private static readonly string[] Directories =
        { "A", "B", "C", "D", "E" };

    public FileStorageService()
    {
        string projectFolder = Path.GetFullPath(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", ".."));

        _dataFolder = Path.Combine(projectFolder, "Files");
        Directory.CreateDirectory(_dataFolder);

        _usersFile = Path.Combine(_dataFolder, "nameuser.txt");

        if (!File.Exists(_usersFile))
        {
            File.WriteAllText(
                _usersFile,
                "admin|admin123|Admin|2099-12-31|" +
                "A:RWAE;B:RWAE;C:RWAE;D:RWAE;E:RWAE" +
                Environment.NewLine,
                Encoding.UTF8);
        }
    }

    public List<User> LoadUsers()
    {
        var users = new List<User>();

        foreach (string line in File.ReadAllLines(_usersFile, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split('|');

            // Підтримуємо старий формат із чотирьох полів.
            if (parts.Length < 4)
                continue;

            if (!DateTime.TryParse(parts[3], out DateTime validUntil))
                continue;

            var user = new User
            {
                Login = parts[0],
                Password = parts[1],
                Role = parts[2],
                PasswordValidUntil = validUntil,
                Permissions = CreateDefaultPermissions(parts[2])
            };

            // Якщо права записані у файлі, завантажуємо їх.
            if (parts.Length >= 5 && !string.IsNullOrWhiteSpace(parts[4]))
            {
                LoadPermissions(user, parts[4]);
            }

            users.Add(user);
        }

        return users;
    }

    public bool AddUser(User user, out string message)
    {
        var users = LoadUsers();

        if (users.Count >= 8)
        {
            message = "Досягнуто максимальної кількості користувачів (8).";
            return false;
        }

        if (users.Any(u =>
                u.Login.Equals(
                    user.Login,
                    StringComparison.OrdinalIgnoreCase)))
        {
            message = "Користувач із таким логіном уже існує.";
            return false;
        }

        user.Permissions ??= CreateDefaultPermissions(user.Role);

        foreach (string directory in Directories)
        {
            if (!user.Permissions.ContainsKey(directory))
                user.Permissions[directory] = "R";
        }

        users.Add(user);
        SaveUsers(users);

        message = "Користувача додано.";
        return true;
    }

    public bool DeleteUser(string login, out string message)
    {
        var users = LoadUsers();

        var user = users.FirstOrDefault(u =>
            u.Login.Equals(
                login,
                StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            message = "Користувача не знайдено.";
            return false;
        }

        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
            users.Count(u =>
                u.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) <= 1)
        {
            message = "Не можна видалити останнього адміністратора.";
            return false;
        }

        users.Remove(user);
        SaveUsers(users);

        message = "Користувача видалено.";
        return true;
    }

    private static Dictionary<string, string> CreateDefaultPermissions(
        string role)
    {
        var permissions = new Dictionary<string, string>();

        string defaultRights =
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                ? "RWAE"
                : "R";

        foreach (string directory in Directories)
        {
            permissions[directory] = defaultRights;
        }

        return permissions;
    }

    private static void LoadPermissions(User user, string value)
    {
        foreach (string item in value.Split(';'))
        {
            string[] pair = item.Split(':');

            if (pair.Length != 2)
                continue;

            string directory = pair[0].Trim().ToUpperInvariant();
            string rights = pair[1].Trim().ToUpperInvariant();

            if (!Directories.Contains(directory))
                continue;

            if (rights.Length == 0 ||
                rights.Any(c => !"RWAE".Contains(c)) ||
                rights.Distinct().Count() != rights.Length)
            {
                continue;
            }

            user.Permissions[directory] = rights;
        }
    }

    private void SaveUsers(List<User> users)
    {
        var lines = users.Select(user =>
        {
            string permissions = string.Join(
                ";",
                Directories.Select(directory =>
                {
                    string rights =
                        user.Permissions.TryGetValue(directory, out string? value)
                            ? value
                            : "R";

                    return $"{directory}:{rights}";
                }));

            return
                $"{user.Login}|{user.Password}|{user.Role}|" +
                $"{user.PasswordValidUntil:yyyy-MM-dd}|{permissions}";
        });

        File.WriteAllLines(_usersFile, lines, Encoding.UTF8);
    }

    public bool SetUserDirectoryPermissions(
        string login,
        string directory,
        string rights,
        out string message)
    {
        string[] allowedDirectories = { "A", "B", "C", "D", "E" };

        directory = directory.Trim().ToUpperInvariant();
        rights = rights.Trim().ToUpperInvariant();

        // Перевірка назви каталогу
        if (!allowedDirectories.Contains(directory))
        {
            message = "Невідомий каталог.";
            return false;
        }

        // Перевірка прав доступу
        if (string.IsNullOrWhiteSpace(rights))
        {
            message = "Виберіть хоча б одне право доступу.";
            return false;
        }

        foreach (char right in rights)
        {
            if (!"RWAE".Contains(right))
            {
                message = "Вибрано некоректні права доступу.";
                return false;
            }

            if (rights.IndexOf(right) != rights.LastIndexOf(right))
            {
                message = "Права доступу не повинні повторюватися.";
                return false;
            }
        }

        // Знаходимо користувача
        var users = LoadUsers();

        var user = users.FirstOrDefault(u =>
            u.Login.Equals(
                login,
                StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            message = "Користувача не знайдено.";
            return false;
        }

        // Зберігаємо права у встановленому порядку R, W, A, E
        string normalizedRights = "";

        foreach (char right in "RWAE")
        {
            if (rights.Contains(right))
            {
                normalizedRights += right;
            }
        }

        if (user.Permissions == null)
        {
            user.Permissions = new Dictionary<string, string>();
        }

        user.Permissions[directory] = normalizedRights;

        // Записуємо оновлені дані у файл
        SaveUsers(users);

        message = $"Права для каталогу {directory} успішно збережено.";
        return true;
    }

}
