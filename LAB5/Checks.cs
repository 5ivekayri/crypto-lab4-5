using System.Text;

namespace Lab5;

internal static class Checks
{
    public static int Run()
    {
        int passed = 0, failed = 0;
        void Test(string name, Action action)
        {
            try { action(); Console.WriteLine($"PASS: {name}"); passed++; }
            catch (Exception ex) { Console.WriteLine($"FAIL: {name}: {ex.Message}"); failed++; }
        }

        string directory = Directory.CreateTempSubdirectory("lab5-check-").FullName;
        try
        {
            string binary = Path.Combine(directory, "данные с пробелом.bin");
            File.WriteAllBytes(binary, Enumerable.Range(0, 1024 * 1024).Select(i => (byte)(i % 256)).ToArray());
            foreach (var vector in KnownVectors.All)
            {
                Test($"{vector.Algorithm}: пустая строка", () =>
                    Require(HashService.Hex(HashService.Text("", vector.Algorithm)) == vector.Empty));
                Test($"{vector.Algorithm}: abc", () =>
                    Require(HashService.Hex(HashService.Text("abc", vector.Algorithm)) == vector.Abc));
                Test($"{vector.Algorithm}: бинарный файл 1 МиБ", () =>
                    Require(HashService.Hex(HashService.FileHash(binary, vector.Algorithm)) == vector.Binary));
                Test($"{vector.Algorithm}: равенство и изменение строки", () =>
                {
                    byte[] first = HashService.Text("MySourceData", vector.Algorithm);
                    Require(HashService.Equal(first, HashService.Text("MySourceData", vector.Algorithm)));
                    Require(!HashService.Equal(first, HashService.Text("NotMySourceData", vector.Algorithm)));
                });
            }
            Test("UTF-8: кириллица и emoji, строка равна файлу", () =>
            {
                const string text = "Русский текст 🔑\r\nВторая строка\n";
                string file = Path.Combine(directory, "utf8.txt");
                File.WriteAllText(file, text, new UTF8Encoding(false));
                Require(HashService.Equal(HashService.Text(text, "SHA256"), HashService.FileHash(file, "SHA256")));
                Require(!HashService.Equal(HashService.Text("А", "SHA256"), HashService.Text("Б", "SHA256")));
            });
            Test("BOM и переводы строк учитываются как байты", () =>
            {
                string file = Path.Combine(directory, "bom.txt");
                File.WriteAllText(file, "abc", new UTF8Encoding(true));
                Require(!HashService.Equal(HashService.Text("abc", "SHA256"), HashService.FileHash(file, "SHA256")));
                Require(!HashService.Equal(HashService.Text("a\n", "SHA256"), HashService.Text("a\r\n", "SHA256")));
            });
            Test("Сравнение учитывает длину и последний байт", () =>
            {
                Require(!HashService.Equal([1, 2], [1]));
                Require(!HashService.Equal([1, 2], [1, 3]));
                Require(HashService.Equal([], []));
            });
            Test("HEX сохраняет последний байт и ведущие нули", () =>
                Require(HashService.Hex([0, 1, 15, 255]) == "00010FFF"));
            Test("Имя алгоритма без учёта регистра и с дефисом", () =>
                Require(HashService.Equal(HashService.Text("abc", "sha-256"), HashService.Text("abc", "SHA256"))));
            Test("Неизвестный алгоритм", () => Throws<ArgumentException>(() => HashService.Text("abc", "CRC32")));
            Test("Отсутствующий файл", () => Throws<FileNotFoundException>(() => HashService.FileHash(Path.Combine(directory, "missing"), "MD5")));
            Test("Бенчмарк: все алгоритмы, повторы, хеши и медиана", () =>
            {
                var measurements = Benchmark.Run(binary, 2);
                Require(measurements.Count == 5);
                foreach (var item in measurements)
                {
                    Require(item.Milliseconds.Length == 2 && item.Milliseconds.All(value => value > 0));
                    Require(item.Hash == KnownVectors.All.Single(v => v.Algorithm == item.Algorithm).Binary);
                    Require(Math.Abs(item.Median - item.Milliseconds.Average()) < 1e-9);
                }
                Require(new Measurement("test", "", [9, 1, 2]).Median == 2);
            });
            Test("Недопустимое число замеров", () =>
            {
                Throws<ArgumentException>(() => Benchmark.Run(binary, 0));
                Throws<ArgumentException>(() => Benchmark.Run(binary, 101));
            });
        }
        finally { Directory.Delete(directory, recursive: true); }
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
