
using System;
using System.Globalization;
using System.Windows;
using InformationSecurityApp.Models;
using InformationSecurityApp.Services;

namespace InformationSecurityApp.Views;

public partial class AuthenticationWindow : Window
{
    public event Action? AuthenticationFailed;
    private readonly User _currentUser;
    private readonly AuditService _audit;
    private readonly Random _random = new();

    private int _currentX;
    private bool _challengeCompleted;

    public AuthenticationWindow(User user)
    {
        InitializeComponent();

        _currentUser = user;
        _audit = new AuditService();

        UserText.Text = $"Поточний користувач: {user.Login}";

        GenerateChallenge();
    }

    private void GenerateChallenge()
    {
        _currentX = _random.Next(1, 65);
        ChallengeText.Text = _currentX.ToString();
        AnswerBox.Clear();
        ResultText.Text = string.Empty;
        _challengeCompleted = false;
        AnswerBox.Focus();
    }

    private void CheckButton_Click(object sender, RoutedEventArgs e)
    {
        if (_challengeCompleted)
        {
            ResultText.Text =
                "Це завдання вже перевірено. Створіть нове.";
            return;
        }

        string answerText = AnswerBox.Text.Trim();

        // Приймаємо крапку або кому як десятковий роздільник.
        answerText = answerText.Replace(',', '.');

        if (!double.TryParse(
                answerText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double answer)
            || double.IsNaN(answer)
            || double.IsInfinity(answer))
        {
            ResultText.Text = "Введіть коректне числове значення.";
            return;
        }

        // F(X) = 4 * X * ln(7).
        double expected = 4 * _currentX * Math.Log(7);

        // Допускаємо невелику похибку через округлення до сотих.
        bool success = Math.Abs(answer - expected) <= 0.011;

        _challengeCompleted = true;

        if (success)
        {
            ResultText.Text =
                $"Перевірку пройдено! F(X) ≈ {expected:F2}";

            ResultText.Foreground =
                System.Windows.Media.Brushes.ForestGreen;

            _audit.Write(
                _currentUser.Login,
                $"Стохастична автентифікація успішна. X={_currentX}");
        }
        else
        {
            ResultText.Text =
                "Неправильна відповідь. Доступ до системи заблоковано.";

            ResultText.Foreground =
                System.Windows.Media.Brushes.Firebrick;

            _audit.Write(
                _currentUser.Login,
                $"Стохастична автентифікація неуспішна. X={_currentX}");

            MessageBox.Show(
                "Автентифікацію не пройдено. Необхідно увійти в систему повторно.",
                "Доступ заблоковано",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            AuthenticationFailed?.Invoke();
            Close();
        }
    }

    private void NewChallengeButton_Click(
        object sender, RoutedEventArgs e)
    {
        GenerateChallenge();
    }
}
