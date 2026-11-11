using System.Security.Cryptography;
using System.Text;

namespace Lab5;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            switch (args)
            {
                case ["text", var algorithm, var value]:
                    PrintHash(algorithm, HashService.Text(value, algorithm));
                    return 0;
                case ["file", var algorithm, var path]:
                    PrintHash(algorithm, HashService.FileHash(path, algorithm));
                    return 0;
                case ["compare-text", var algorithm, var first, var second]:
                    return Compare(HashService.Text(first, algorithm), HashService.Text(second, algorithm));
                case ["compare-files", var algorithm, var first, var second]:
                    return Compare(HashService.FileHash(first, algorithm), HashService.FileHash(second, algorithm));
                case ["benchmark", var path]:
                    RunBenchmark(path, 7);
                    return 0;
                case ["benchmark", var path, var count]:
                    if (!int.TryParse(count, out int iterations))
                        throw new ArgumentException("Число повторов должно быть целым числом.");
                    RunBenchmark(path, iterations);
                    return 0;
                case ["demo"]:
                    Demo.Run(Path.Combine("results", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")));
                    return 0;
                case ["demo", var directory]:
                    Demo.Run(directory);
                    return 0;
                case ["self-test"]:
                    return Checks.Run();
                default:
                    Console.WriteLine("""
                        Лабораторная № 5 — вычисление и сравнение хешей
                        Команды (после dotnet run --):
                          text <алгоритм> "строка"
                          file <алгоритм> <файл>
                          compare-text <алгоритм> "строка 1" "строка 2"
                          compare-files <алгоритм> <файл 1> <файл 2>
                          benchmark <файл> [число повторов, по умолчанию 7]
                          demo [новая или пустая папка результатов]
                          self-test
                        Алгоритмы: MD5, SHA1, SHA256, SHA384, SHA512 (допускается SHA-256).
                        Коды выхода: 0 — успех/хеши равны, 1 — ошибка, 2 — неверная команда,
                        3 — сравниваемые хеши различаются.
                        """);
                    return args.Length == 0 || args is ["--help"] ? 0 : 2;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or ArgumentException or CryptographicException)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            return 1;
        }
    }

    private static void PrintHash(string algorithm, byte[] hash) =>
        Console.WriteLine($"{HashService.Normalize(algorithm)}: {HashService.Hex(hash)}");

    private static int Compare(byte[] first, byte[] second)
    {
        Console.WriteLine($"Хеш 1: {HashService.Hex(first)}");
        Console.WriteLine($"Хеш 2: {HashService.Hex(second)}");
        bool equal = HashService.Equal(first, second);
        Console.WriteLine(equal ? "Хеши совпадают." : "Хеши различаются.");
        return equal ? 0 : 3;
    }

    private static void RunBenchmark(string path, int iterations) =>
        Console.Write(Benchmark.Report(path, iterations, Benchmark.Run(path, iterations)));
}
