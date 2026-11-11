using System.Security.Cryptography;
using System.Text;

namespace Lab4;

internal static class Demo
{
    public static void Run(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new IOException("Для демонстрации укажите новую или пустую папку.");
        Directory.CreateDirectory(directory);
        string PathFor(string name) => Path.Combine(directory, name);
        const string original = "Лабораторная работа № 4\nAES: шифрование файлов на C#.\nСмена ключа 🔑\n";
        // Эти пароли публичны и предназначены только для демонстрации.
        const string passwordA = "Lab4-password-A";
        const string passwordB = "Lab4-password-B";
        File.WriteAllText(PathFor("input.txt"), original, new UTF8Encoding(false));
        FileCipher.Encrypt(PathFor("input.txt"), PathFor("encrypted-A.txt"), passwordA);
        FileCipher.Decrypt(PathFor("encrypted-A.txt"), PathFor("decrypted-A.txt"), passwordA);
        FileCipher.Encrypt(PathFor("input.txt"), PathFor("encrypted-B.txt"), passwordB);
        FileCipher.Decrypt(PathFor("encrypted-B.txt"), PathFor("decrypted-B.txt"), passwordB);
        FileCipher.Encrypt(PathFor("input.txt"), PathFor("encrypted-A-repeat.txt"), passwordA);

        var report = new List<string>
        {
            "Лабораторная № 4 — результаты демонстрации",
            "AES-256-CBC, блок 128 бит, PKCS7",
            $"PBKDF2-SHA256: {StringCipher.DerivationIterations} итераций",
            $"Учебный пароль A: {passwordA}",
            $"Учебный пароль B: {passwordB}",
            $"Расшифрование A → A: {File.ReadAllText(PathFor("decrypted-A.txt")) == original}",
            $"Расшифрование B → B: {File.ReadAllText(PathFor("decrypted-B.txt")) == original}",
            $"Шифротексты A и B различаются: {File.ReadAllText(PathFor("encrypted-A.txt")) != File.ReadAllText(PathFor("encrypted-B.txt"))}",
            $"Повторное шифрование с A даёт другой результат: {File.ReadAllText(PathFor("encrypted-A.txt")) != File.ReadAllText(PathFor("encrypted-A-repeat.txt"))}"
        };
        try
        {
            string wrong = StringCipher.Decrypt(File.ReadAllText(PathFor("encrypted-A.txt")), passwordB);
            if (wrong == original) throw new InvalidOperationException("Чужой пароль восстановил текст.");
            report.Add("Расшифрование A → B: исходный текст не восстановлен.");
        }
        catch (Exception ex) when (ex is CryptographicException or DecoderFallbackException)
        {
            report.Add("Расшифрование A → B: ошибка, исходный текст не восстановлен.");
        }
        File.WriteAllLines(PathFor("report.txt"), report, new UTF8Encoding(false));
        foreach (string line in report) Console.WriteLine(line);
        Console.WriteLine($"Файлы: {Path.GetFullPath(directory)}");
    }
}
