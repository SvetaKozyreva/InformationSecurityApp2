
using System;
using System.IO;
using System.Numerics;
using System.Windows;
using InformationSecurityApp.Models;
using InformationSecurityApp.Services;

namespace InformationSecurityApp.Views;

public partial class CryptoWindow : Window
{
    private readonly User _currentUser;
    private readonly ElGamalService _elGamal;
    private readonly AuditService _audit;
    private readonly ElGamalSignatureService _signatureService;
    private readonly string _filesFolder;

    public CryptoWindow(User user)
    {
        InitializeComponent();

        _currentUser = user;
        _elGamal = new ElGamalService();
        _signatureService = new ElGamalSignatureService();
        _audit = new AuditService();

        string projectFolder = Path.GetFullPath(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", ".."));

        _filesFolder = Path.Combine(projectFolder, "Files");
        Directory.CreateDirectory(_filesFolder);
        LoadSavedKeys();
    }

    private string GetFilePath(string fileName)
    {
        return Path.Combine(_filesFolder, fileName);
    }
    private void LoadSavedKeys()
    {
        string keysPath = GetFilePath("elgamal_keys.txt");

        if (!File.Exists(keysPath))
            return;

        string[] keys = File.ReadAllLines(keysPath);

        if (keys.Length < 2 ||
            !BigInteger.TryParse(keys[0], out _) ||
            !BigInteger.TryParse(keys[1], out _))
        {
            MessageBox.Show(
                "Файл із ключами пошкоджений або має неправильний формат.",
                "ElGamal",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        PublicKeyBox.Text = keys[0];
        PrivateKeyBox.Text = keys[1];
    }

    private void GenerateKeysButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var keys = _elGamal.GenerateKeys();

            PublicKeyBox.Text = keys.PublicKey.ToString();
            PrivateKeyBox.Text = keys.PrivateKey.ToString();

            // Зберігаємо пару ключів у файл
            File.WriteAllLines(
                GetFilePath("elgamal_keys.txt"),
                new[]
                {
                keys.PublicKey.ToString(),
                keys.PrivateKey.ToString()
                });

            _audit.Write(
                _currentUser.Login,
                "ElGamal: згенеровано та збережено пару ключів");

            MessageBox.Show(
                "Ключі успішно згенеровано та збережено.",
                "ElGamal",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Помилка генерації ключів: {ex.Message}",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void LoadCloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = GetFilePath("close.txt");

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    "Файл close.txt не знайдено.",
                    "ElGamal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            DecryptCipherTextBox.Text = File.ReadAllText(path);

            _audit.Write(
                _currentUser.Login,
                "ElGamal: завантажено зашифровані дані з close.txt");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Помилка завантаження файлу: {ex.Message}",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    private void LoadInputButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string path = GetFilePath("input.txt");

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    $"Файл input.txt не знайдено.\nСтворіть його у папці:\n{_filesFolder}",
                    "Файл відсутній",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            PlainTextBox.Text = File.ReadAllText(path);

            _audit.Write(_currentUser.Login,
                "ElGamal: завантажено текст із input.txt");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Помилка читання файлу: {ex.Message}",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void EncryptButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!BigInteger.TryParse(
                    PublicKeyBox.Text, out BigInteger publicKey))
            {
                MessageBox.Show(
                    "Спочатку згенеруйте ключі.",
                    "ElGamal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string plainText = PlainTextBox.Text;

            if (string.IsNullOrWhiteSpace(plainText))
            {
                MessageBox.Show(
                    "Введіть текст або завантажте input.txt.",
                    "ElGamal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string encryptedText = _elGamal.Encrypt(plainText, publicKey);

            DecryptCipherTextBox.Text = encryptedText;
            File.WriteAllText(GetFilePath("close.txt"), encryptedText);
            DecryptedTextBox.Clear();

            _audit.Write(_currentUser.Login,
                "ElGamal: текст зашифровано та збережено у close.txt");

            MessageBox.Show(
                "Текст зашифровано та збережено у close.txt.",
                "ElGamal",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Помилка шифрування: {ex.Message}",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void DecryptButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!BigInteger.TryParse(
                    PrivateKeyBox.Text, out BigInteger privateKey))
            {
                MessageBox.Show(
                    "Спочатку згенеруйте ключі.",
                    "ElGamal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string encryptedText = DecryptCipherTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                string path = GetFilePath("close.txt");

                if (!File.Exists(path))
                {
                    MessageBox.Show(
                        "Немає зашифрованих даних. Спочатку зашифруйте текст.",
                        "ElGamal",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                encryptedText = File.ReadAllText(path).Trim();
            }

            string decryptedText =
                _elGamal.Decrypt(encryptedText, privateKey);

            DecryptedTextBox.Text = decryptedText;
            File.WriteAllText(GetFilePath("out.txt"), decryptedText);

            _audit.Write(_currentUser.Login,
                "ElGamal: текст розшифровано та збережено у out.txt");

            MessageBox.Show(
                "Текст розшифровано та збережено у out.txt.",
                "ElGamal",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Помилка розшифрування: {ex.Message}",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SignButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!BigInteger.TryParse(
                    PrivateKeyBox.Text, out BigInteger privateKey))
            {
                MessageBox.Show(
                    "Спочатку згенеруйте ключі.",
                    "ElGamal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string message = PlainTextBox.Text;

            if (string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show(
                    "Введіть або завантажте текст для підпису.",
                    "ElGamal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var signature = _signatureService.Sign(message, privateKey);

            string signatureText = $"{signature.R};{signature.S}";
            SignatureBox.Text = signatureText;

            File.WriteAllText(
                GetFilePath("signature.txt"), signatureText);

            SignatureStatusText.Text =
                "Підпис створено та збережено у signature.txt.";

            _audit.Write(_currentUser.Login,
                "ElGamal: створено цифровий підпис");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Помилка створення підпису: {ex.Message}",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void VerifySignatureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!BigInteger.TryParse(
                    PublicKeyBox.Text, out BigInteger publicKey))
            {
                MessageBox.Show(
                    "Спочатку згенеруйте ключі.",
                    "ElGamal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string message = PlainTextBox.Text;
            string signatureText = SignatureBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(signatureText))
            {
                string path = GetFilePath("signature.txt");

                if (File.Exists(path))
                {
                    signatureText = File.ReadAllText(path).Trim();
                    SignatureBox.Text = signatureText;
                }
            }

            if (string.IsNullOrWhiteSpace(message) ||
                string.IsNullOrWhiteSpace(signatureText))
            {
                MessageBox.Show(
                    "Потрібні вихідний текст і цифровий підпис.",
                    "ElGamal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string[] parts = signatureText.Split(';');

            if (parts.Length != 2 ||
                !BigInteger.TryParse(parts[0], out BigInteger r) ||
                !BigInteger.TryParse(parts[1], out BigInteger s))
            {
                MessageBox.Show(
                    "Формат підпису некоректний. Очікується формат r;s.",
                    "Помилка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool isValid = _signatureService.Verify(
                message, publicKey, r, s);

            SignatureStatusText.Text = isValid
                ? "Підпис дійсний. Цілісність повідомлення підтверджено."
                : "Підпис недійсний. Повідомлення могло бути змінено.";

            _audit.Write(_currentUser.Login,
                isValid
                    ? "ElGamal: цифровий підпис підтверджено"
                    : "ElGamal: перевірку цифрового підпису не пройдено");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Помилка перевірки підпису: {ex.Message}",
                "Помилка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
