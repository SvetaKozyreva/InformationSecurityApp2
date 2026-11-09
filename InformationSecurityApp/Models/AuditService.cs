
using System;
using System.IO;
using System.Text;

namespace InformationSecurityApp.Services;

public class AuditService
{
    private readonly string _logFile;

    public AuditService()
    {
        // Повертаємося з bin/Debug/net8.0-windows
        // до папки проєкту та знаходимо Files.
        string projectFolder = Path.GetFullPath(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", ".."));

        string folder = Path.Combine(projectFolder, "Files");

        Directory.CreateDirectory(folder);

        _logFile = Path.Combine(folder, "us_book.txt");

        if (!File.Exists(_logFile))
        {
            File.WriteAllText(
                _logFile,
                string.Empty,
                Encoding.UTF8);
        }
    }

    public void Write(string login, string action)
    {
        string entry =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | " +
            $"Користувач: {login} | Подія: {action}";

        File.AppendAllText(
            _logFile,
            entry + Environment.NewLine,
            Encoding.UTF8);
    }
}
