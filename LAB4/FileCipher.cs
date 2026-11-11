using System.Text;

namespace Lab4;

public static class FileCipher
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    public static void Encrypt(string input, string output, string password) =>
        WriteNew(output, StringCipher.Encrypt(File.ReadAllText(input, Utf8), password));

    public static void Decrypt(string input, string output, string password) =>
        WriteNew(output, StringCipher.Decrypt(File.ReadAllText(input, Utf8), password));

    // CreateNew защищает исходник и существующие файлы от перезаписи.
    private static void WriteNew(string path, string text)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var writer = new StreamWriter(stream, Utf8);
        writer.Write(text);
    }
}
