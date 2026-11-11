using System.Security.Cryptography;
using System.Text;

namespace Lab5;

public static class HashService
{
    public static IReadOnlyList<string> Algorithms { get; } =
        Array.AsReadOnly(new[] { "MD5", "SHA1", "SHA256", "SHA384", "SHA512" });

    public static string Normalize(string algorithm)
    {
        string name = algorithm.Replace("-", "").ToUpperInvariant();
        if (!Algorithms.Contains(name))
            throw new ArgumentException("Алгоритм должен быть MD5, SHA1, SHA256, SHA384 или SHA512.");
        return name;
    }

    public static byte[] Text(string text, string algorithm)
    {
        using HashAlgorithm hasher = Create(algorithm);
        // В отличие от ASCII, UTF-8 сохраняет кириллицу и emoji.
        return hasher.ComputeHash(Encoding.UTF8.GetBytes(text));
    }

    public static byte[] FileHash(string path, string algorithm)
    {
        using HashAlgorithm hasher = Create(algorithm);
        // Хешируются точные байты, включая BOM и переводы строк, без загрузки всего файла в память.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 64 * 1024, FileOptions.SequentialScan);
        return hasher.ComputeHash(stream);
    }

    public static bool Equal(byte[] first, byte[] second) => first.AsSpan().SequenceEqual(second);

    public static string Hex(byte[] hash) => Convert.ToHexString(hash);

    private static HashAlgorithm Create(string algorithm) => Normalize(algorithm) switch
    {
        "MD5" => MD5.Create(),
        "SHA1" => SHA1.Create(),
        "SHA256" => SHA256.Create(),
        "SHA384" => SHA384.Create(),
        "SHA512" => SHA512.Create(),
        _ => throw new ArgumentException("Неизвестный алгоритм.")
    };
}
