using System.Security.Cryptography;
using System.Text;

namespace Lab4;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            if (args.Length == 1 && args[0] == "self-test")
                return Checks.Run();
            if (args.Length is 1 or 2 && args[0] == "demo")
            {
                Demo.Run(args.Length == 2 ? args[1] : Path.Combine("results",
                    DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")));
                return 0;
            }
            if (args.Length == 3 && args[0] is "encrypt" or "decrypt")
            {
                string password = ReadPassword();
                if (args[0] == "encrypt") FileCipher.Encrypt(args[1], args[2], password);
                else FileCipher.Decrypt(args[1], args[2], password);
                Console.WriteLine($"Готово: {Path.GetFullPath(args[2])}");
                return 0;
            }
            Console.WriteLine("""
                Лабораторная № 4 — AES-256-CBC
                Команды (после dotnet run --):
                  encrypt <исходный.txt> <зашифрованный.txt>
                  decrypt <зашифрованный.txt> <восстановленный.txt>
                  demo [папка_результатов]
                  self-test
                Пароль вводится отдельно; существующие файлы не перезаписываются.
                """);
            return args.Length == 0 || args is ["--help"] ? 0 : 2;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or FormatException or CryptographicException)
        {
            Console.Error.WriteLine(ex is CryptographicException or DecoderFallbackException
                ? "Ошибка: неверный пароль или повреждённые данные."
                : $"Ошибка: {ex.Message}");
            return 1;
        }
    }

    private static string ReadPassword()
    {
        Console.Write("Пароль: ");
        if (Console.IsInputRedirected)
            return Console.ReadLine() ?? throw new ArgumentException("Пароль не передан.");
        var password = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0) password.Length--;
            }
            else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
        }
        Console.WriteLine();
        return password.ToString();
    }
}
