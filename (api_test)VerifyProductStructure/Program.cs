using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HsonAPILib;
using Basic;

namespace VerifyProductStructure
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== 驗證貨品 / 子項目結構 ===");
            Console.WriteLine("按 Enter 開始，或輸入 q 離開");

            while (true)
            {
                string input = Console.ReadLine();
                if (input?.Trim().ToLower() == "q") break;

                await VerifyStructureAsync();

                Console.WriteLine("=== 驗證完成 ===");
                Console.WriteLine("按 Enter 重新驗證，或輸入 q 離開");
            }
        }

        static async Task VerifyStructureAsync()
        {
            string apiUrl = "https://localhost:44322/api/products/get_product_structure";
            var jsonBody = JsonSerializer.Serialize(new
            {
                ServerName = "Main",
                ServerType = "網頁",
                ValueAry = new string[] { }
            });

            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
                using (var client = new HttpClient(handler))
                {
                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(apiUrl, content);
                    string result = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var return_data = result.JsonDeserializet<returnData>();
                        var products = return_data.Data.ObjToClass<List<productsClass>>();

                        if (products != null)
                        {
                            var productMap = new Dictionary<string, productsClass>();
                            foreach (var p in products)
                                productMap[p.產品代碼] = p;

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("=== 所有產品結構 ===");
                            Console.ResetColor();

                            foreach (var p in products)
                            {
                                var visited = new HashSet<string>();
                                PrintProductTreeFull(p, productMap, 0, visited);
                                Console.WriteLine();
                            }
                        }
                        else
                        {
                            PrintPrettyJson(result);
                        }
                    }
                    catch
                    {
                        PrintPrettyJson(result);
                    }
                }
            }
        }

        static void PrintProductTreeFull(productsClass p, Dictionary<string, productsClass> productMap, int level, HashSet<string> visited)
        {
            if (visited.Contains(p.產品代碼)) return;
            visited.Add(p.產品代碼);

            string prefix = new string(' ', level * 4) + (level > 0 ? "└─ " : "");

            if (p.產品類型 == "貨品")
                Console.ForegroundColor = ConsoleColor.Green;
            else if (p.產品類型 == "子項目")
                Console.ForegroundColor = ConsoleColor.Cyan;
            else
                Console.ResetColor();

            Console.WriteLine($"{prefix}[{p.產品類型}] {p.產品代碼} - {p.產品名稱}");
            Console.ResetColor();

            foreach (var child in p.child_components)
            {
                if (productMap.TryGetValue(child.child_code, out var childProduct))
                {
                    PrintProductTreeFull(childProduct, productMap, level + 1, visited);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{new string(' ', (level + 1) * 4)}└─ [子項目] {child.child_code} (未找到詳細資料)");
                    Console.ResetColor();
                }
            }
        }

        static void PrintPrettyJson(string json)
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                string pretty = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Console.WriteLine(pretty);
            }
            catch
            {
                Console.WriteLine(json);
            }
        }
    }
}
