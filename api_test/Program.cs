using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HsonAPILib;
namespace ApiTestConsole
{
 

    public class ReturnData<T>
    {
        public int Code { get; set; }
        public string Result { get; set; }
        public T Data { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("請輸入產品代碼：");
            string productCode = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(productCode))
            {
                Console.WriteLine("未輸入產品代碼，結束程式。");
                return;
            }

            string apiUrl = "https://localhost:44322/api/Products/get_by_code";

            var requestJson = $@"
            {{
                ""ServerName"": ""Main"",
                ""ServerType"": ""網頁"",
                ""ValueAry"": [""product_code={productCode}""]
            }}";

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);

                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                Console.WriteLine("\n=== 正在呼叫 API... ===\n");
                var response = await client.PostAsync(apiUrl, content);
                var jsonResult = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var returnData = JsonSerializer.Deserialize<ReturnData<productsClass>>(jsonResult, options);

                if (returnData == null || returnData.Data == null)
                {
                    Console.WriteLine("API 回傳資料為空");
                    return;
                }

                var p = returnData.Data;

                Console.WriteLine("=== 產品資料 ===");
                Console.WriteLine($"代碼: {p.產品代碼}");
                Console.WriteLine($"名稱: {p.產品名稱}");
                Console.WriteLine($"廠牌: {p.廠牌}");
                Console.WriteLine($"售價: {p.售價}");
                Console.WriteLine($"狀態: {p.狀態}");
                Console.WriteLine($"子項數量: {p.child_count}");
                Console.WriteLine($"父項數量: {p.parent_count}");

                Console.WriteLine("\n=== 子項清單 ===");
                PrintComponents(p.child_components, 1);

                Console.WriteLine("\n=== 父項清單 ===");
                PrintComponents(p.parent_products, 1);

                Console.WriteLine("\n=== API 測試完成 ===");
            }

            Console.WriteLine("\n按 Enter 結束...");
            Console.ReadLine();
        }

        static void PrintComponents(List<product_componentsClass> list, int indentLevel)
        {
            if (list == null || list.Count == 0) return;

            string indent = new string(' ', indentLevel * 2);
            foreach (var item in list)
            {
                Console.WriteLine($"{indent}- 代碼: {item.child_code} 名稱: {item.產品名稱} 廠牌: {item.廠牌} 售價: {item.售價} 狀態: {item.狀態} 數量: {item.數量}");
                if (item.child_components != null && item.child_components.Count > 0)
                {
                    PrintComponents(item.child_components, indentLevel + 1);
                }
            }
        }
    }
}
