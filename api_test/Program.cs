using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Basic;
using HsonAPILib;
using System.Text.Json.Serialization;

namespace ClientConsoleTest
{
    class Program
    {
        private static readonly string ApiBaseUrl = "https://localhost:44322/";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 客戶資料測試程式（Enter 確認後才啟動 API） ===");

            // 第一次啟動
            Console.WriteLine("\n[第一次啟動] 新增客戶");
            var client1 = CreateTestClient("台北市立醫院", "台北市信義區健康路1號");
            await ConfirmAndRun(client1);

            // 第二次啟動
            Console.WriteLine("\n[第二次啟動] 更新同一客戶資料");
            client1.地址 = "台北市信義區健康路88號";
            client1.更新時間 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            client1.聯絡人清單.Add(new client_contactClass
            {
                GUID = Guid.NewGuid().ToString(),
                姓名 = "林美麗",
                電話 = "02-87654321",
                電子郵件 = "mei@example.com",
                職稱 = "藥劑主任",
                主要聯絡人 = "1",
                備註 = "副聯絡人"
            });
            await ConfirmAndRun(client1);

            Console.WriteLine("\n測試完成，按任意鍵結束...");
            Console.ReadKey();
        }

        static async Task ConfirmAndRun(clientClass client)
        {
            string jsonPreview = client.JsonSerializationt(true);
            Console.WriteLine("即將送出的客戶資料：");
            Console.WriteLine(jsonPreview);

            Console.Write("\n按 Enter 呼叫 API，或按 ESC 取消：");
            var key = Console.ReadKey(intercept: true);
            Console.WriteLine();
            if (key.Key == ConsoleKey.Enter)
            {
                await AddOrUpdateClients(client);
            }
            else
            {
                Console.WriteLine("已取消執行 API 呼叫。");
            }
        }

        static clientClass CreateTestClient(string name, string address)
        {
            return new clientClass
            {
                GUID = Guid.NewGuid().ToString(),
                名稱 = name,
                地址 = address,
                類型 = "政府機關",
                啟用狀態 = "啟用",
                建立時間 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                更新時間 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                備註 = "測試資料",
                聯絡人清單 = new List<client_contactClass>
                {
                    new client_contactClass
                    {
                        GUID = Guid.NewGuid().ToString(),
                        姓名 = "王小明",
                        電話 = "02-12345678",
                        電子郵件 = "ming@example.com",
                        職稱 = "藥師",
                        主要聯絡人 = "1",
                        備註 = "主要聯絡人"
                    }
                }
            };
        }

        static async Task AddOrUpdateClients(clientClass client)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(ApiBaseUrl);

                // 依你後端要求，包在 returnData.Data = List<clientClass>
                List<clientClass> payload = new List<clientClass> { client };
                returnData rd = new returnData { Data = payload };
                string json = rd.JsonSerializationt(true);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync("api/clients/add_or_update_clients", content);
                    string result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"API 回應狀態碼: {response.StatusCode}");
                    Console.WriteLine($"API 回應內容: {result}");

                    // 解析回傳，把後端實際使用的 GUID 覆寫回來，確保下一次是「更新」
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var parsed = JsonSerializer.Deserialize<ApiReturn<List<clientClass>>>(result, options);
                    if (parsed?.Code == 200 && parsed.Data != null && parsed.Data.Count > 0)
                    {
                        var returnedClient = parsed.Data[0];

                        // 覆寫客戶 GUID 與時間戳
                        client.GUID = returnedClient.GUID;
                        client.建立時間 = returnedClient.建立時間;
                        client.更新時間 = returnedClient.更新時間;

                        // 覆寫聯絡人（包含 GUID / ClientGUID 等）
                        client.聯絡人清單 = returnedClient.聯絡人清單 ?? new List<client_contactClass>();

                        Console.WriteLine($"同步後端 GUID 成功：ClientGUID = {client.GUID}");
                        if (client.聯絡人清單?.Count > 0)
                        {
                            foreach (var c in client.聯絡人清單)
                                Console.WriteLine($"  Contact: {c.姓名} GUID={c.GUID}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ 無法解析或回傳非 200，可能無法同步 GUID。");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"API 呼叫失敗: {ex.Message}");
                }
            }
        }

    }
    // 新增：泛型回傳模型（對應 API 的 returnData 結構）
    public class ApiReturn<T>
    {
        [JsonPropertyName("Code")]
        public int Code { get; set; }

        [JsonPropertyName("Result")]
        public string Result { get; set; }

        [JsonPropertyName("Data")]
        public T Data { get; set; }

        [JsonPropertyName("TimeTaken")]
        public string TimeTaken { get; set; }
    }

}
