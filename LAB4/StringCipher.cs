using System.Security.Cryptography;
using System.Text;

namespace Lab4;

/// <summary>AES-256-CBC + PKCS7. Формат Base64(salt || IV || ciphertext).</summary>
public static class StringCipher
{
    public const int KeySize = 256;
    public const int SaltSize = 32;
    public const int IvSize = 16;
    public const int DerivationIterations = 210_000;

    // Строгая UTF-8: повреждённые данные не заменяются незаметно символом �.
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    public static string Encrypt(string plainText, string passPhrase)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        ArgumentException.ThrowIfNullOrEmpty(passPhrase);
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] iv = RandomNumberGenerator.GetBytes(IvSize);
        byte[] key = DeriveKey(passPhrase, salt);
        try
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            byte[] encrypted = aes.EncryptCbc(Utf8.GetBytes(plainText), iv, PaddingMode.PKCS7);
            byte[] result = new byte[SaltSize + IvSize + encrypted.Length];
            salt.CopyTo(result, 0);
            iv.CopyTo(result, SaltSize);
            encrypted.CopyTo(result, SaltSize + IvSize);
            return Convert.ToBase64String(result);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public static string Decrypt(string cipherText, string passPhrase)
    {
        ArgumentNullException.ThrowIfNull(cipherText);
        ArgumentException.ThrowIfNullOrEmpty(passPhrase);
        byte[] packed = Convert.FromBase64String(cipherText);
        int encryptedSize = packed.Length - SaltSize - IvSize;
        if (encryptedSize < IvSize || encryptedSize % IvSize != 0)
            throw new FormatException("Неверная длина файла: ожидаются salt, IV и целые блоки AES.");

        byte[] key = DeriveKey(passPhrase, packed.AsSpan(0, SaltSize));
        try
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            byte[] decrypted = aes.DecryptCbc(
                packed.AsSpan(SaltSize + IvSize),
                packed.AsSpan(SaltSize, IvSize), PaddingMode.PKCS7);
            return Utf8.GetString(decrypted);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DeriveKey(string password, ReadOnlySpan<byte> salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, DerivationIterations,
            HashAlgorithmName.SHA256, KeySize / 8);
}
