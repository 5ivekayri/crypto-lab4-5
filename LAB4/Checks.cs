using System.Security.Cryptography;
using System.Text;

namespace Lab4;

internal static class Checks
{
    public static int Run()
    {
        int passed = 0;
        int failed = 0;
        void Test(string name, Action action)
        {
            try { action(); Console.WriteLine($"PASS: {name}"); passed++; }
            catch (Exception ex) { Console.WriteLine($"FAIL: {name}: {ex.Message}"); failed++; }
        }
        foreach (string text in new[] { "", "a", new string('x', 15), new string('x', 16),
            new string('x', 17), "Русский текст 🔑\nВторая строка\r\n\0", new string('я', 100_000) })
        {
            Test($"Полный цикл, {Encoding.UTF8.GetByteCount(text)} байт", () =>
                Require(StringCipher.Decrypt(StringCipher.Encrypt(text, "пароль"), "пароль") == text));
        }
        Test("Совместимость с независимым примером OpenSSL", () =>
        {
            const string fixture = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8AAQIDBAUGBwgJCgsMDQ4PUBEblEWPUFar0B2R16fs/166gb7vr5tBk3McsFQZ9uv/LVtSnY/IBCEBgaknMnbZ";
            Require(StringCipher.Decrypt(fixture, "fixture-password") == "Независимый пример AES 🔑\n");
        });
        Test("Случайные salt и IV при повторном шифровании", () =>
        {
            byte[] first = Convert.FromBase64String(StringCipher.Encrypt("same", "same"));
            byte[] second = Convert.FromBase64String(StringCipher.Encrypt("same", "same"));
            Require(!first.AsSpan(0, 32).SequenceEqual(second.AsSpan(0, 32)));
            Require(!first.AsSpan(32, 16).SequenceEqual(second.AsSpan(32, 16)));
        });
        Test("Смена пароля и попытка расшифрования чужим паролем", () =>
        {
            const string original = "Проверка смены ключа";
            string a = StringCipher.Encrypt(original, "password-A");
            string b = StringCipher.Encrypt(original, "password-B");
            Require(StringCipher.Decrypt(a, "password-A") == original);
            Require(StringCipher.Decrypt(b, "password-B") == original);
            try { Require(StringCipher.Decrypt(a, "password-B") != original); }
            catch (Exception ex) when (ex is CryptographicException or DecoderFallbackException) { }
        });
        Test("Некорректный Base64", () => Throws<FormatException>(() => StringCipher.Decrypt("!bad!", "p")));
        Test("Отсутствующие блоки", () => Throws<FormatException>(() => StringCipher.Decrypt(Convert.ToBase64String(new byte[48]), "p")));
        Test("Обрезанный блок", () => Throws<FormatException>(() => StringCipher.Decrypt(Convert.ToBase64String(new byte[65]), "p")));
        Test("Пустой пароль", () => Throws<ArgumentException>(() => StringCipher.Encrypt("text", "")));
        Test("Файловый цикл, сохранность исходника и запрет перезаписи", () =>
        {
            string directory = Directory.CreateTempSubdirectory("lab4-check-").FullName;
            try
            {
                string input = Path.Combine(directory, "input.txt");
                string encrypted = Path.Combine(directory, "encrypted.txt");
                string output = Path.Combine(directory, "output.txt");
                byte[] original = Encoding.UTF8.GetBytes("Файл UTF-8\r\nТест 🔑\n");
                File.WriteAllBytes(input, original);
                FileCipher.Encrypt(input, encrypted, "file-password");
                FileCipher.Decrypt(encrypted, output, "file-password");
                Require(File.ReadAllBytes(output).SequenceEqual(original));
                Throws<IOException>(() => FileCipher.Encrypt(input, input, "file-password"));
                Throws<IOException>(() => FileCipher.Decrypt(encrypted, output, "file-password"));
                Require(File.ReadAllBytes(input).SequenceEqual(original));
                Require(File.ReadAllBytes(output).SequenceEqual(original));
            }
            finally { Directory.Delete(directory, recursive: true); }
        });
        Console.WriteLine($"Итого: {passed} успешно, {failed} ошибок.");
        return failed == 0 ? 0 : 1;
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException("Проверка не пройдена.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Ожидалось исключение {typeof(T).Name}.");
    }
}
