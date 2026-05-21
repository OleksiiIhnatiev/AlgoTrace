using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlgoTrace.CLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Використання: AlgoTrace.CLI <file_path_1> <file_path_2> [api_url]");
                Console.WriteLine("Приклад: AlgoTrace.CLI file1.py file2.py http://localhost:8080");
                return;
            }

            string file1 = args[0];
            string file2 = args[1];
            // Якщо запускаємо з Docker для Windows/Mac, хост за замовчуванням host.docker.internal
            string apiUrl = args.Length > 2 ? args[2].TrimEnd('/') : "http://localhost:8080";

            if (!File.Exists(file1))
            {
                Console.WriteLine($"Помилка: Файл не знайдено - {file1}");
                return;
            }
            if (!File.Exists(file2))
            {
                Console.WriteLine($"Помилка: Файл не знайдено - {file2}");
                return;
            }

            string content1 = await File.ReadAllTextAsync(file1);
            string content2 = await File.ReadAllTextAsync(file2);
            string ext = Path.GetExtension(file1).TrimStart('.').ToLower();

            // Автоматичне визначення мови на основі розширення
            string language = ext switch {
                "py" => "python",
                "cs" => "csharp",
                "js" or "jsx" => "javascript",
                "ts" or "tsx" => "typescript",
                "cpp" or "c" or "h" or "hpp" => "cpp",
                "java" => "java",
                "go" => "go",
                "rs" => "rust",
                "php" => "php",
                _ => "python" // За замовчуванням
            };

            var requestBody = new
            {
                language = language,
                submission_a = new { files = new[] { new { filename = Path.GetFileName(file1), content = content1 } } },
                submission_b = new { files = new[] { new { filename = Path.GetFileName(file2), content = content2 } } },
                analysis_config = new
                {
                    parameters = new { ignore_comments = true, ignore_whitespace = true },
                    execute_categories = new
                    {
                        text_based = new[] { "levenshtein", "rabin_karp", "ngram_search" },
                        token_based = new[] { "winnowing", "jaccard_token" },
                        tree_based = new[] { "ast_compare", "ast_hashing" },
                        graph_based = new[] { "cfg", "pdg" },
                        metrics_based = new[] { "halstead", "mccabe" }
                    }
                }
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);

            using var client = new HttpClient();
            Console.WriteLine($"\n🚀 Ініціалізація аналізу...");
            Console.WriteLine($"Файл 1: {file1}");
            Console.WriteLine($"Файл 2: {file2}");
            Console.WriteLine($"Відправка запиту до API: {apiUrl}/api/analysis/unified");

            try
            {
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{apiUrl}/api/analysis/unified", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Помилка сервера: {response.StatusCode}");
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Деталі: {errorDetail}");
                    return;
                }

                string responseStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseStr);
                
                var root = doc.RootElement;
                double score = root.GetProperty("global_similarity_score").GetDouble() * 100; // Множимо на 100 для відсотків
                
                Console.WriteLine();
                Console.ForegroundColor = GetColorForScore(score);
                Console.WriteLine($"=================================================");
                Console.WriteLine($"✅ Аналіз успішно завершено!");
                Console.WriteLine($"📊 Загальний відсоток схожості: {score:0.##}%");
                Console.WriteLine($"=================================================");
                Console.ResetColor();

                if (root.TryGetProperty("categories_results", out var categories))
                {
                    Console.WriteLine("\n📋 ДЕТАЛЬНИЙ ЗВІТ ЗА КАТЕГОРІЯМИ:");
                    foreach (var cat in categories.EnumerateArray())
                    {
                        string catName = cat.GetProperty("category_name").GetString();
                        double catScore = cat.GetProperty("category_similarity_score").GetDouble() * 100;

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"\n▶ {FormatCategoryName(catName)} (Схожість: {catScore:0.##}%)");
                        Console.ResetColor();

                        if (cat.TryGetProperty("algorithms", out var algos))
                        {
                            foreach (var algo in algos.EnumerateArray())
                            {
                                string algoName = algo.GetProperty("method").GetString();
                                double algoScore = algo.GetProperty("similarity_score").GetDouble() * 100;

                                Console.Write($"  - {FormatMethodName(algoName).PadRight(30)} : ");
                                Console.ForegroundColor = GetColorForScore(algoScore);
                                Console.WriteLine($"{algoScore:0.##}%");
                                Console.ResetColor();
                            }
                        }
                    }
                }
                Console.WriteLine();
            }
            catch (HttpRequestException)
            {
                Console.WriteLine($"❌ Помилка з'єднання з API.");
                Console.WriteLine("Переконайтеся, що основний сервер (docker-compose) запущений і доступний.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Непередбачена помилка: {ex.Message}");
            }
        }

        static ConsoleColor GetColorForScore(double score)
        {
            if (score < 30) return ConsoleColor.Green;
            if (score < 70) return ConsoleColor.Yellow;
            return ConsoleColor.Red;
        }

        static string FormatCategoryName(string name) => name switch {
            "text_based" => "Текстовий аналіз",
            "token_based" => "Токенний аналіз",
            "tree_based" => "Аналіз AST-дерев",
            "graph_based" => "Аналіз графів (CFG/PDG)",
            "metrics_based" => "Метрики коду",
            _ => name
        };

        static string FormatMethodName(string name) => name switch {
            "levenshtein" => "Відстань Левенштейна",
            "line_matching" => "Порядкове порівняння",
            "rabin_karp" => "Алгоритм Рабіна-Карпа",
            "ngram_search" => "Пошук за N-грамами",
            "jaccard_token" => "Токени Джаккарда",
            "winnowing" => "Вінновінг (Winnowing)",
            "ast_hashing" => "Хешування AST",
            "ast_compare" => "Пряме порівняння AST",
            "subtree_isomorphism" => "Ізоморфізм піддерев",
            "cfg" => "Граф потоку керування (CFG)",
            "pdg" => "Граф залежностей даних (PDG)",
            "subgraph_isomorphism" => "Ізоморфізм підграфів",
            "halstead" => "Метрики Холстеда",
            "mccabe" => "Складність Маккейба",
            _ => name.Replace("_", " ")
        };
    }
}
