
using System.Windows;
using System.Windows.Controls;
using InformationSecurityApp.Models;
using InformationSecurityApp.Services;

namespace InformationSecurityApp.Views;

public partial class FileOperationsWindow : Window
{
    private readonly User _currentUser;
    private readonly ProtectedFileService _fileService = new();

    public FileOperationsWindow(User user)
    {
        InitializeComponent();

        _currentUser = user;
        Title = $"Робота з файлами — {_currentUser.Login}";
    }

    private string GetSelectedDirectory()
    {
        return (DirectoryBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
               ?? "A";
    }

    private string GetFileName()
    {
        return FileNameBox.Text.Trim();
    }

    private void ReadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_fileService.ReadFile(
                _currentUser,
                GetSelectedDirectory(),
                GetFileName(),
                out string content,
                out string message))
        {
            ContentBox.Text = content;
        }

        StatusText.Text = message;
    }

    private void WriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_fileService.WriteFile(
                _currentUser,
                GetSelectedDirectory(),
                GetFileName(),
                ContentBox.Text,
                out string message))
        {
            StatusText.Text = message;
            return;
        }

        StatusText.Text = message;
    }

    private void AppendButton_Click(object sender, RoutedEventArgs e)
    {
        if (_fileService.AppendToFile(
                _currentUser,
                GetSelectedDirectory(),
                GetFileName(),
                ContentBox.Text,
                out string message))
        {
            StatusText.Text = message;
            return;
        }

        StatusText.Text = message;
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        _fileService.ExecuteAction(
            _currentUser,
            GetSelectedDirectory(),
            out string message);

        StatusText.Text = message;
    }
}
