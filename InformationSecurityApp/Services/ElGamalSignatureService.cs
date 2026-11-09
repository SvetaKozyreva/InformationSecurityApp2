
using System;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace InformationSecurityApp.Services;

public class ElGamalSignatureService
{
    private static readonly BigInteger P =
        BigInteger.Parse(
            "9252807569334395113358424540384295700949811480493272995759784379",
            CultureInfo.InvariantCulture);

    private static readonly BigInteger G = new BigInteger(2);
    private static readonly BigInteger Phi = P - 1;

    public (BigInteger R, BigInteger S) Sign(
        string message,
        BigInteger privateKey)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentException(
                "Повідомлення не може бути порожнім.");

        if (privateKey < 2 || privateKey > P - 2)
            throw new ArgumentException("Некоректний закритий ключ.");

        BigInteger h = CalculateHash(message);

        while (true)
        {
            BigInteger k = RandomInRange(2, P - 2);

            if (GreatestCommonDivisor(k, Phi) != 1)
                continue;

            BigInteger r = BigInteger.ModPow(G, k, P);
            BigInteger kInverse = ModularInverse(k, Phi);

            BigInteger s = ((h - privateKey * r) * kInverse) % Phi;

            if (s < 0)
                s += Phi;

            if (s == 0)
                continue;

            return (r, s);
        }
    }

    public bool Verify(
        string message,
        BigInteger publicKey,
        BigInteger r,
        BigInteger s)
    {
        if (string.IsNullOrEmpty(message))
            return false;

        if (publicKey <= 0 || publicKey >= P ||
            r <= 0 || r >= P ||
            s <= 0 || s >= Phi)
        {
            return false;
        }

        BigInteger h = CalculateHash(message);

        BigInteger left = BigInteger.ModPow(G, h, P);

        BigInteger right =
            (BigInteger.ModPow(publicKey, r, P) *
             BigInteger.ModPow(r, s, P)) % P;

        return left == right;
    }

    private static BigInteger CalculateHash(string message)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(message));

        BigInteger value = new BigInteger(
            hash, isUnsigned: true, isBigEndian: true);

        return value % Phi;
    }

    private static BigInteger GreatestCommonDivisor(
        BigInteger a,
        BigInteger b)
    {
        while (b != 0)
        {
            BigInteger temp = b;
            b = a % b;
            a = temp;
        }

        return BigInteger.Abs(a);
    }

    private static BigInteger ModularInverse(
        BigInteger value,
        BigInteger modulus)
    {
        BigInteger t = 0;
        BigInteger newT = 1;
        BigInteger r = modulus;
        BigInteger newR = value;

        while (newR != 0)
        {
            BigInteger quotient = r / newR;

            BigInteger tempT = t - quotient * newT;
            t = newT;
            newT = tempT;

            BigInteger tempR = r - quotient * newR;
            r = newR;
            newR = tempR;
        }

        if (r != 1)
            throw new ArgumentException(
                "Не вдалося обчислити обернений елемент.");

        return (t % modulus + modulus) % modulus;
    }

    private static BigInteger RandomInRange(
        BigInteger minInclusive,
        BigInteger maxInclusive)
    {
        BigInteger range = maxInclusive - minInclusive + 1;

        byte[] maxBytes = range.ToByteArray(
            isUnsigned: true, isBigEndian: true);

        while (true)
        {
            byte[] bytes = new byte[maxBytes.Length];
            RandomNumberGenerator.Fill(bytes);

            BigInteger candidate = new BigInteger(
                bytes, isUnsigned: true, isBigEndian: true);

            if (candidate < range)
                return minInclusive + candidate;
        }
    }
}
