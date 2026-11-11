using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Lab5;

internal sealed record Measurement(string Algorithm, string Hash, double[] Milliseconds)
{
    public double Median
    {
        get
        {
            double[] ordered = Milliseconds.Order().ToArray();
            int middle = ordered.Length / 2;
            return ordered.Length % 2 == 0
                ? (ordered[middle - 1] + ordered[middle]) / 2 : ordered[middle];
        }
    }
}

internal static class Benchmark
{
    public static IReadOnlyList<Measurement> Run(string path, int iterations)
    {
        if (iterations is < 1 or > 100)
            throw new ArgumentException("Число повторов должно быть от 1 до 100.");
        // Один прогрев каждого алгоритма; в таблицу это время не входит.
        var results = HashService.Algorithms.Select(name => new Measurement(name,
            HashService.Hex(HashService.FileHash(path, name)), new double[iterations])).ToArray();
        for (int round = 0; round < iterations; round++)
        {
            // Сдвигаем порядок, чтобы один алгоритм не выполнялся постоянно первым.
            for (int offset = 0; offset < results.Length; offset++)
            {
                Measurement result = results[(round + offset) % results.Length];
                long started = Stopwatch.GetTimestamp();
                byte[] hash = HashService.FileHash(path, result.Algorithm);
                result.Milliseconds[round] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                if (HashService.Hex(hash) != result.Hash)
                    throw new IOException("Файл изменился во время замеров. Повторите с неизменяемым файлом.");
            }
        }
        return results;
    }

    public static string Report(string path, int iterations, IReadOnlyList<Measurement> results)
    {
        long bytes = new FileInfo(path).Length;
        var output = new StringBuilder();
        output.AppendLine("# Сравнение скорости хеширования");
        output.AppendLine();
        output.AppendLine($"Дата UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}.");
        output.AppendLine($"Среда: {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture}.");
        output.AppendLine($"Файл: `{Path.GetFileName(path)}`; размер: {bytes} байт ({bytes / 1048576.0:F2} МиБ).");
        output.AppendLine($"Прогрев: 1 проход на алгоритм; измеряемых проходов: {iterations}.");
        output.AppendLine();
        output.AppendLine("| Алгоритм | Хеш, бит | Медиана, мс | Минимум, мс | Скорость, МиБ/с |");
        output.AppendLine("| --- | ---: | ---: | ---: | ---: |");
        foreach (Measurement result in results)
        {
            double speed = bytes / 1048576.0 / (result.Median / 1000);
            output.AppendLine(FormattableString.Invariant(
                $"| {result.Algorithm} | {result.Hash.Length * 4} | {result.Median:F3} | {result.Milliseconds.Min():F3} | {speed:F2} |"));
        }
        output.AppendLine();
        Measurement fastest = results.MinBy(item => item.Median)!;
        Measurement slowest = results.MaxBy(item => item.Median)!;
        output.AppendLine($"В этом запуске наименьшая медиана у {fastest.Algorithm}, наибольшая — у {slowest.Algorithm}.");
        output.AppendLine("Измеряется открытие файла, чтение и вычисление хеша. Генерация данных, вывод и перевод в HEX в замер не входят. После прогрева данные могут находиться в файловом кеше ОС. Результат зависит от CPU, криптобиблиотеки, кеша и нагрузки; он не задаёт универсальный рейтинг алгоритмов и не оценивает их стойкость.");
        output.AppendLine();
        output.AppendLine("## Все измерения и контрольные хеши");
        foreach (Measurement result in results)
        {
            output.AppendLine();
            output.AppendLine($"- {result.Algorithm}, мс: {string.Join(", ", result.Milliseconds.Select(value => value.ToString("F3", CultureInfo.InvariantCulture)))}");
            output.AppendLine($"- HEX: `{result.Hash}`");
        }
        return output.ToString();
    }
}
