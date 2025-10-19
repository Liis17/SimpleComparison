using System.Security.Cryptography;
using System.Text;

namespace SimpleComparison
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║        Поиск и удаление дубликатов файлов (SHA256)           ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            Console.Write("Введите путь к папке для анализа: ");
            string? folderPath = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Указанная папка не существует!");
                Console.ResetColor();
                Console.WriteLine("\nНажмите Enter для выхода...");
                Console.ReadLine();
                return;
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⏳ Сканирование файлов...");
            Console.ResetColor();

            string[] allFiles;
            try
            {
                allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Ошибка при сканировании: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("\nНажмите Enter для выхода...");
                Console.ReadLine();
                return;
            }

            if (allFiles.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ В указанной папке нет файлов.");
                Console.ResetColor();
                Console.WriteLine("\nНажмите Enter для выхода...");
                Console.ReadLine();
                return;
            }

            var fileHashes = new Dictionary<string, string>();
            int processedCount = 0;

            foreach (var file in allFiles)
            {
                try
                {
                    string hash = CalculateSHA256(file);
                    fileHashes[file] = hash;
                    processedCount++;

                    if (processedCount % 10 == 0)
                    {
                        Console.Write($"\r⏳ Обработано файлов: {processedCount}/{allFiles.Length}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"⚠ Не удалось обработать файл: {file}");
                    Console.WriteLine($"  Причина: {ex.Message}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine($"\r⏳ Обработано файлов: {processedCount}/{allFiles.Length}");
            Console.WriteLine();

            var hashGroups = fileHashes.GroupBy(kvp => kvp.Value)
                                       .Where(g => g.Count() > 1)
                                       .OrderByDescending(g => g.Count())
                                       .ToList();
            if (hashGroups.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                  НАЙДЕННЫЕ ДУБЛИКАТЫ                         ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();

                int groupNumber = 1;
                foreach (var group in hashGroups)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"┌─── Группа #{groupNumber} ───────────────────────────────────────────────");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"│ SHA256: {group.Key}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"│ Найдено копий: {group.Count()}");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("├─────────────────────────────────────────────────────────────────");
                    Console.ResetColor();

                    int fileNum = 1;
                    foreach (var file in group)
                    {
                        var fileInfo = new FileInfo(file.Key);
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write($"│ [{fileNum}] ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(file.Key);
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"│     Размер: {FormatFileSize(fileInfo.Length)}");
                        Console.ResetColor();
                        fileNum++;
                    }

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("└─────────────────────────────────────────────────────────────────");
                    Console.ResetColor();
                    Console.WriteLine();
                    groupNumber++;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⏳ Перемещение дубликатов...");
                Console.ResetColor();

                string duplicatesFolder = Path.Combine(folderPath, "_Duplicates");
                Directory.CreateDirectory(duplicatesFolder);

                int movedCount = 0;
                int uniqueHashes = 0;

                foreach (var group in hashGroups)
                {
                    uniqueHashes++;
                    var files = group.ToList();

                    for (int i = 1; i < files.Count; i++)
                    {
                        try
                        {
                            var sourceFile = files[i].Key;
                            var fileName = Path.GetFileName(sourceFile);
                            var destFile = Path.Combine(duplicatesFolder, fileName);

                            int counter = 1;
                            while (File.Exists(destFile))
                            {
                                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                                var ext = Path.GetExtension(fileName);
                                destFile = Path.Combine(duplicatesFolder, $"{nameWithoutExt}_{counter}{ext}");
                                counter++;
                            }

                            File.Move(sourceFile, destFile);
                            movedCount++;

                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"✓ Перемещен: {Path.GetFileName(sourceFile)}");
                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"✗ Ошибка при перемещении: {files[i].Key}");
                            Console.WriteLine($"  Причина: {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }

                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Дубликаты не найдены!");
                Console.ResetColor();
                Console.WriteLine();
            }

            int totalFiles = allFiles.Length;
            int duplicatesCount = hashGroups.Sum(g => g.Count() - 1);
            int uniqueFiles = totalFiles - duplicatesCount;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      СТАТИСТИКА                              ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("📊 Всего файлов:        ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(totalFiles);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("📂 Уникальных файлов:   ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(uniqueFiles);

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("🔄 Дубликатов:          ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(duplicatesCount);

            if (hashGroups.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("📁 Перемещено файлов:   ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(duplicatesCount);

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("📍 Папка дубликатов:    ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(Path.Combine(folderPath, "_Duplicates"));
            }

            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("\nНажмите Enter для завершения...");
            Console.ResetColor();
            Console.ReadLine();
        }

        static string CalculateSHA256(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}
