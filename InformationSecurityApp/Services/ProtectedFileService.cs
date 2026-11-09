
using System;
using System.IO;
using InformationSecurityApp.Models;

namespace InformationSecurityApp.Services;

public class ProtectedFileService
{
    private readonly AccessControlService _accessControl = new();
    private readonly AuditService _audit = new();
    private readonly string _filesFolder;

    public ProtectedFileService()
    {
        string projectFolder = Path.GetFullPath(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", ".."));

        _filesFolder = Path.Combine(projectFolder, "Files");
        Directory.CreateDirectory(_filesFolder);
    }

    public bool ReadFile(
        User user,
        string directory,
        string fileName,
        out string content,
        out string message)
    {
        content = string.Empty;

        if (!_accessControl.HasPermission(
                user, directory, 'R', out message))
        {
            Log(user, $"Відмовлено в читанні файлу {fileName}. {message}");
            return false;
        }

        if (!TryGetSafePath(fileName, out string path, out message))
        {
            Log(user, $"Помилка читання файлу: {message}");
            return false;
        }

        if (!File.Exists(path))
        {
            message = $"Файл {fileName} не знайдено.";
            Log(user, message);
            return false;
        }

        try
        {
            content = File.ReadAllText(path);
            message = "Файл успішно прочитано.";

            Log(user,
                $"Прочитано файл {fileName} у каталозі {directory}.");

            return true;
        }
        catch (Exception ex)
        {
            message = $"Помилка читання файлу: {ex.Message}";
            Log(user, message);
            return false;
        }
    }

    public bool WriteFile(
        User user,
        string directory,
        string fileName,
        string content,
        out string message)
    {
        if (!_accessControl.HasPermission(
                user, directory, 'W', out message))
        {
            Log(user, $"Відмовлено в записі файлу {fileName}. {message}");
            return false;
        }

        if (!TryGetSafePath(fileName, out string path, out message))
        {
            Log(user, $"Помилка запису файлу: {message}");
            return false;
        }

        try
        {
            File.WriteAllText(path, content);

            message = "Дані успішно записано у файл.";

            Log(user,
                $"Записано файл {fileName} у каталозі {directory}.");

            return true;
        }
        catch (Exception ex)
        {
            message = $"Помилка запису файлу: {ex.Message}";
            Log(user, message);
            return false;
        }
    }

    public bool AppendToFile(
        User user,
        string directory,
        string fileName,
        string content,
        out string message)
    {
        if (!_accessControl.HasPermission(
                user, directory, 'A', out message))
        {
            Log(user, $"Відмовлено в доповненні файлу {fileName}. {message}");
            return false;
        }

        if (!TryGetSafePath(fileName, out string path, out message))
        {
            Log(user, $"Помилка доповнення файлу: {message}");
            return false;
        }

        try
        {
            File.AppendAllText(
                path,
                content + Environment.NewLine);

            message = "Дані успішно доповнено.";

            Log(user,
                $"Доповнено файл {fileName} у каталозі {directory}.");

            return true;
        }
        catch (Exception ex)
        {
            message = $"Помилка доповнення файлу: {ex.Message}";
            Log(user, message);
            return false;
        }
    }

    public bool ExecuteAction(
        User user,
        string directory,
        out string message)
    {
        bool allowed = _accessControl.HasPermission(
            user, directory, 'E', out message);

        Log(user,
            $"Перевірка права виконання для каталогу {directory}: {message}");

        return allowed;
    }

    private void Log(User user, string action)
    {
        _audit.Write(user.Login, action);
    }

    private bool TryGetSafePath(
        string fileName,
        out string path,
        out string message)
    {
        path = string.Empty;

        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            fileName.Contains('/') ||
            fileName.Contains('\\') ||
            fileName == "." ||
            fileName == "..")
        {
            message = "Некоректне ім'я файлу.";
            return false;
        }

        path = Path.Combine(_filesFolder, fileName);
        message = string.Empty;

        return true;
    }
}
