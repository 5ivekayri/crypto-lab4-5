using System.Text;

namespace Lab5;

internal static class Demo
{
    public static void Run(string directory)
    {
        if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
            throw new IOException("Для демонстрации укажите новую или пустую папку.");
        Directory.CreateDirectory(directory);
        string PathFor(string name) => Path.Combine(directory, name);
        var utf8 = new UTF8Encoding(false);
        File.WriteAllText(PathFor("input.txt"), "Лабораторная работа № 5\nХеширование файлов 🔑\n", utf8);
        File.Copy(PathFor("input.txt"), PathFor("copy.txt"));
        File.WriteAllText(PathFor("changed.txt"), "Лабораторная работа № 5\nХеширование файлов 🔑!\n", utf8);

        var report = new StringBuilder("# Результаты демонстрации LAB5\n\n");
        foreach (string algorithm in HashService.Algorithms)
        {
            byte[] original = HashService.FileHash(PathFor("input.txt"), algorithm);
            bool same = HashService.Equal(original, HashService.FileHash(PathFor("copy.txt"), algorithm));
            bool changed = HashService.Equal(original, HashService.FileHash(PathFor("changed.txt"), algorithm));
            if (!same || changed) throw new InvalidOperationException("Проверка сравнения файлов не пройдена.");
            report.AppendLine($"- {algorithm}: `{HashService.Hex(original)}`; копия совпадает: {same}; изменённый файл совпадает: {changed}.");
        }
        report.AppendLine();
        foreach (string text in new[] { "MySourceData", "NotMySourceData" })
            report.AppendLine($"MD5 строки `{text}`: `{HashService.Hex(HashService.Text(text, "MD5"))}`.");
        // Воспроизведение ошибки исходного цикла: последний байт не выводился.
        byte[] hash = HashService.Text("MySourceData", "MD5");
        var truncated = new StringBuilder();
        for (int i = 0; i < hash.Length - 1; i++) truncated.Append(hash[i].ToString("X2"));
        report.AppendLine();
        report.AppendLine($"Ошибка цикла из методички: `{truncated}` ({truncated.Length} HEX-символов), исправленный результат: `{HashService.Hex(hash)}` (32 HEX-символа).");
        report.AppendLine($"ASCII превращает разные строки «А» и «Б» в одинаковые байты: {Encoding.ASCII.GetBytes("А").SequenceEqual(Encoding.ASCII.GetBytes("Б"))}. Это потеря информации до хеширования, а не коллизия хеш-функции.");
        report.AppendLine($"UTF-8 + SHA256 различает «А» и «Б»: {!HashService.Equal(HashService.Text("А", "SHA256"), HashService.Text("Б", "SHA256"))}.");
        File.WriteAllText(PathFor("report.md"), report.ToString(), utf8);

        // Файл для замеров создаётся отдельно и удаляется после использования.
        string temporary = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lab5-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        try
        {
            string benchmarkFile = System.IO.Path.Combine(temporary, "benchmark-16MiB.bin");
            byte[] block = Enumerable.Range(0, 65536).Select(i => (byte)(i % 256)).ToArray();
            using (var stream = new FileStream(benchmarkFile, FileMode.CreateNew))
                for (int i = 0; i < 256; i++) stream.Write(block);
            string benchmarkReport = Benchmark.Report(benchmarkFile, 7, Benchmark.Run(benchmarkFile, 7));
            benchmarkReport += "\nДанные воспроизводимы: последовательность байтов 0..255, повторённая 65536 раз (16777216 байт). Временный файл после замеров удаляется.\n";
            File.WriteAllText(PathFor("benchmark.md"), benchmarkReport, utf8);
            Console.WriteLine(report);
            Console.WriteLine(benchmarkReport);
        }
        finally { Directory.Delete(temporary, recursive: true); }
        Console.WriteLine($"Результаты: {System.IO.Path.GetFullPath(directory)}");
    }
}
