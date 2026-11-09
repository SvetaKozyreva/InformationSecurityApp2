
using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace InformationSecurityApp.Services;

public class ElGamalService
{
    // Простое число длиной 64 десятичных знака.
    private static readonly BigInteger P =
        BigInteger.Parse(
            "9252807569334395113358424540384295700949811480493272995759784379",
            CultureInfo.InvariantCulture);

    private static readonly BigInteger G = new BigInteger(2);

    public class KeyPair
    {
        public BigInteger PrivateKey { get; init; }
        public BigInteger PublicKey { get; init; }
    }

    public KeyPair GenerateKeys()
    {
        BigInteger privateKey = RandomInRange(2, P - 2);
        BigInteger publicKey = BigInteger.ModPow(G, privateKey, P);

        return new KeyPair
        {
            PrivateKey = privateKey,
            PublicKey = publicKey
        };
    }

    public string Encrypt(string text, BigInteger publicKey)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Введіть текст для шифрування.");

        if (publicKey <= 0 || publicKey >= P)
            throw new ArgumentException("Некоректний відкритий ключ.");

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        var encryptedParts = new string[bytes.Length];

        for (int i = 0; i < bytes.Length; i++)
        {
            BigInteger message = bytes[i];
            BigInteger k = RandomInRange(2, P - 2);

            BigInteger a = BigInteger.ModPow(G, k, P);
            BigInteger shared = BigInteger.ModPow(publicKey, k, P);
            BigInteger b = (message * shared) % P;

            encryptedParts[i] =
                $"{a.ToString(CultureInfo.InvariantCulture)}," +
                $"{b.ToString(CultureInfo.InvariantCulture)}";
        }

        return string.Join(";", encryptedParts);
    }

    public string Decrypt(string encryptedText, BigInteger privateKey)
    {
        if (string.IsNullOrWhiteSpace(encryptedText))
            throw new ArgumentException("Немає даних для розшифрування.");

        if (privateKey < 2 || privateKey > P - 2)
            throw new ArgumentException("Некоректний закритий ключ.");

        string[] pairs = encryptedText.Split(';');
        byte[] bytes = new byte[pairs.Length];

        for (int i = 0; i < pairs.Length; i++)
        {
            string[] values = pairs[i].Split(',');

            if (values.Length != 2 ||
                !BigInteger.TryParse(values[0], out BigInteger a) ||
                !BigInteger.TryParse(values[1], out BigInteger b))
            {
                throw new FormatException(
                    "Формат зашифрованих даних некоректний.");
            }

            if (a <= 0 || a >= P || b < 0 || b >= P)
            {
                throw new FormatException(
                    "Зашифровані дані містять некоректні значення.");
            }

            BigInteger shared = BigInteger.ModPow(a, privateKey, P);
            BigInteger inverse = BigInteger.ModPow(shared, P - 2, P);
            BigInteger message = (b * inverse) % P;

            if (message < 0 || message > 255)
            {
                throw new FormatException(
                    "Не вдалося відновити байт повідомлення.");
            }

            bytes[i] = (byte)message;
        }

        return new UTF8Encoding(false, true).GetString(bytes);
    }

    private static BigInteger RandomInRange(
        BigInteger minInclusive,
        BigInteger maxInclusive)
    {
        if (minInclusive > maxInclusive)
            throw new ArgumentException("Некоректний діапазон.");

        BigInteger range = maxInclusive - minInclusive + 1;
        byte[] maxBytes = range.ToByteArray(isUnsigned: true, isBigEndian: true);

        while (true)
        {
            byte[] bytes = new byte[maxBytes.Length];
            RandomNumberGenerator.Fill(bytes);

            BigInteger candidate =
                new BigInteger(bytes, isUnsigned: true, isBigEndian: true);

            if (candidate < range)
                return minInclusive + candidate;
        }
    }
}
