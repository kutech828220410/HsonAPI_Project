using Basic;
using HsonAPILib; // 這裡引用你的 enum_products, productsClass
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
namespace HsonWebAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class product_files : ControllerBase
    {
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

        /// <summary>
        /// 初始化 dbvm.products 資料表
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 會檢查並建立 `products` 資料表  
        /// **以下為 JSON 範例**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "ValueAry": []
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>回傳 Table 結構資訊</returns>
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                // 取得系統所有 Server 設定
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();

                sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");

                if (sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無 Server 資料!";
                    return returnData.JsonSerializationt();
                }

                // 檢查並建立 Table
                List<Table> tables = CheckCreatTable(sys_serverSettingClasses[0]);

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = tables;
                returnData.Result = $"初始化 products 資料表完成";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception : {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 上傳產品檔案（支援單檔與批次）
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 可為指定產品上傳一或多個檔案（如圖片、說明文件、製造方式等），並將檔案實體儲存至伺服器，
        /// 同時把中繼資料寫入 `product_files` 資料表。支援以 `file`（單檔）或 `files`（多檔、`files[]`）傳入，兩者可同時使用。  
        /// 
        /// **處理邏輯：**
        /// 1. 驗證 `product_code` 與上傳檔案是否存在；單/多檔皆可。  
        /// 2. 檔案副檔名白名單檢查（預設支援：.jpg/.jpeg/.png/.webp/.gif/.bmp/.tif/.tiff/.pdf/.doc/.docx/.xls/.xlsx/.txt）。  
        /// 3. 依 `product_code` 建立儲存目錄：`wwwroot/uploads/products/{product_code}/`（Docker 內為 `/app/wwwroot/...`）。  
        /// 4. 逐檔計算 SHA256，產生安全檔名並寫入磁碟；同時組出可公開存取之 URL。  
        /// 5. 依「產品代碼 + 檔案雜湊（SHA256）」檢查是否重複：  
        ///    - 若 **已存在**：刪除剛寫入的重複實體檔，**不新增 DB**，回傳 `status=duplicate` 與既有的檔案 `url`。  
        ///    - 若 **不存在**：寫入 `product_files` 一筆中繼資料（含檔案網址、雜湊、大小、版本等），回傳 `status=uploaded`。  
        /// 6. 若 `update_cover = true` 且檔案類型為圖片（`圖片` 或 `image`），會把該檔之 URL 寫回 `products.圖片連結`（多檔時僅處理第一個符合條件者）。  
        /// 7. 回傳每個檔案一筆處理結果的陣列。  
        /// 
        /// **表單欄位（multipart/form-data）：**
        /// - `file`：單一檔案（可選）。  
        /// - `files` 或 `files[]`：多個檔案（可選）。  
        /// - `product_code`（必填）：產品代碼。  
        /// - `file_category`（可選，預設 `圖片`）：建議值 `圖片` / `文件` / `製造方式` / `其他`（或對應英文）。  
        /// - `display_name`（可選）：顯示名稱。未填則以檔名（去除副檔名、清洗特殊字元）為準。  
        /// - `note`（可選）：備註。  
        /// - `version`（可選，預設 `1`）：自訂版本字串。  
        /// - `update_cover`（可選，bool，預設 `false`）：若為圖片時，是否寫回 `products.圖片連結`。  
        /// - `server_name`（可選，預設 `Main`）、`server_type`（可選，預設 `網頁`）：對應你的 Server 設定。  
        /// 
        /// **限制與說明：**
        /// - 單一請求上傳總大小受 `[RequestSizeLimit(50MB)]` 限制；可依需要調整。  
        /// - 需於 Startup/Program 中啟用靜態檔案：`app.UseStaticFiles();`。  
        /// - Docker 部署時請將 `/app/wwwroot/uploads` 對應為持久化 Volume。  
        /// - 回傳每筆檔案的 `status` 可能為：`uploaded`、`duplicate`、`error`（含 `reason`）。  
        /// 
        /// **cURL（單檔）範例：**
        /// <code>
        /// curl -X POST "https://your-host/api/products/upload_product_file" \
        ///   -H "Content-Type: multipart/form-data" \
        ///   -F "product_code=P001" \
        ///   -F "file_category=圖片" \
        ///   -F "update_cover=true" \
        ///   -F "file=@./cover.jpg"
        /// </code>
        /// 
        /// **cURL（多檔）範例：**
        /// <code>
        /// curl -X POST "https://your-host/api/products/upload_product_file" \
        ///   -H "Content-Type: multipart/form-data" \
        ///   -F "product_code=P001" \
        ///   -F "file_category=文件" \
        ///   -F "note=上傳手冊與圖檔" \
        ///   -F "files[]=@./manual.pdf" \
        ///   -F "files[]=@./front.png"
        /// </code>
        /// 
        /// **JSON 回應範例（部分上傳、部分重複）：**
        /// <code>
        /// {
        ///   "Code": 200,
        ///   "Result": "處理完成",
        ///   "Data": [
        ///     {
        ///       "status": "uploaded",
        ///       "guid": "a1b2c3d4-...-9z",
        ///       "product_code": "P001",
        ///       "file_category": "圖片",
        ///       "file_name": "20250814103015999_ab12cd34_front.png",
        ///       "display_name": "front",
        ///       "content_type": "image/png",
        ///       "size_bytes": 234567,
        ///       "sha256": "1f3a...e9",
        ///       "relative_path": "/uploads/products/P001/20250814103015999_ab12cd34_front.png",
        ///       "url": "https://your-host/uploads/products/P001/20250814103015999_ab12cd34_front.png"
        ///     },
        ///     {
        ///       "status": "duplicate",
        ///       "guid": "e5f6g7h8-...-yx",
        ///       "product_code": "P001",
        ///       "file_category": "文件",
        ///       "original_name": "manual.pdf",
        ///       "content_type": "application/pdf",
        ///       "size_bytes": 1024000,
        ///       "sha256": "aa77...bb",
        ///       "url": "https://your-host/uploads/products/P001/20240701121212_xx99_manual.pdf"
        ///     }
        ///   ],
        ///   "TimeTaken": "00:00:00.242"
        /// }
        /// </code>
        /// </remarks>
        /// <param name="file">單一檔案（可選）</param>
        /// <param name="files">多檔上傳（可選，`files[]`）</param>
        /// <param name="product_code">產品代碼（必填）</param>
        /// <param name="file_category">檔案類別（可選；預設「圖片」）</param>
        /// <param name="display_name">顯示名稱（可選）</param>
        /// <param name="note">備註（可選）</param>
        /// <param name="version">版本（可選；預設「1」）</param>
        /// <param name="update_cover">若為圖片是否寫回 `products.圖片連結`（可選）</param>
        /// <param name="server_name">Server 名稱（可選；預設「Main」）</param>
        /// <param name="server_type">Server 類型（可選；預設「網頁」）</param>
        /// <returns>回傳每個檔案的處理結果陣列（含 `status`、`guid`、`url` 等），以及執行時間與狀態碼</returns>
        [HttpPost("upload_product_file")]
        [DisableRequestSizeLimit]
        [RequestSizeLimit(50L * 1024 * 1024)] // 50MB
        public async Task<string> upload_product_file(
      IFormFile? file,                              // 單檔
      [FromForm] List<IFormFile>? files,            // 多檔 (files 或 files[])
      [FromForm] string product_code,
      [FromForm] string file_category = "圖片",      // 建議：圖片/文件/製造方式/其他 或 image/document/...
      [FromForm] string? display_name = null,
      [FromForm] string? note = null,
      [FromForm] string? version = null,
      [FromForm] bool update_cover = false,         // 若為圖片且要設為代表圖（寫回 products.圖片連結）
      [FromForm] string? server_name = "Main",
      [FromForm] string? server_type = "網頁"
  )
        {
            var t = new MyTimerBasic();
            var rd = new returnData { Method = "upload_product_file" };

            try
            {
                if (product_code.StringIsEmpty())
                {
                    rd.Code = -200; rd.Result = "product_code 不可為空";
                    Console.WriteLine("[upload] error: product_code empty");
                    return rd.JsonSerializationt();
                }

                // 收集所有檔案：file (單檔) + files (多檔) + Request.Form.Files（保底）
                var allFiles = new List<IFormFile>();
                if (files != null && files.Count > 0) allFiles.AddRange(files);
                if (file != null && file.Length > 0) allFiles.Add(file);
                if (Request?.Form?.Files?.Count > 0)
                {
                    foreach (var f in Request.Form.Files)
                    {
                        if (!allFiles.Contains(f)) allFiles.Add(f);
                    }
                }

                if (allFiles.Count == 0)
                {
                    rd.Code = -200; rd.Result = "未收到檔案或檔案大小為 0";
                    Console.WriteLine("[upload] error: no files");
                    return rd.JsonSerializationt();
                }

                // 副檔名白名單
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".jpg",".jpeg",".png",".webp",".gif",".bmp",".tif",".tiff",".pdf",".doc",".docx",".xls",".xlsx",".txt"
        };

                // 連線設定
                var servers = serverSetting.GetAllServerSetting();
                var sv = servers.myFind(server_name ?? "Main", server_type ?? "網頁", "VM端");
                if (sv == null)
                {
                    rd.Code = -200; rd.Result = $"找不到 Server 設定（{server_name}/{server_type}/VM端）";
                    Console.WriteLine($"[upload] error: server setting not found ({server_name}/{server_type}/VM端)");
                    return rd.JsonSerializationt();
                }

                string tFiles = new enum_product_files().GetEnumDescription();
                string tProd = new enum_products().GetEnumDescription();

                var sqlFiles = new SQLControl(sv.Server, sv.DBName, tFiles, sv.User, sv.Password, sv.Port.StringToUInt32(), SSLMode);
                var sqlProd = new SQLControl(sv.Server, sv.DBName, tProd, sv.User, sv.Password, sv.Port.StringToUInt32(), SSLMode);
                string esc(string s) => (s ?? "").Replace("'", "''");

                // 檢查產品是否存在（避免掛不存在的 product_code）
                var chkProd = sqlProd.GetRowsByDefult(null, (int)enum_products.產品代碼, product_code);
                if (chkProd.Count == 0)
                {
                    rd.Code = -200; rd.Result = $"找不到產品代碼 {product_code}";
                    Console.WriteLine($"[upload] error: product not found {product_code}");
                    return rd.JsonSerializationt();
                }

                var results = new List<object>();
                bool coverSet = false;

                // 相對路徑與實體路徑準備
                string baseDir = AppContext.BaseDirectory; // /app (Docker) 或 bin 路徑
                string safeCode = SanitizeCode(product_code);

                foreach (var f in allFiles)
                {
                    try
                    {
                        if (f == null || f.Length == 0)
                        {
                            results.Add(new { status = "error", reason = "empty_file" });
                            Console.WriteLine("[upload] skip: empty file");
                            continue;
                        }

                        var ext = Path.GetExtension(f.FileName);
                        if (ext.StringIsEmpty() || !allowed.Contains(ext))
                        {
                            results.Add(new { status = "error", reason = $"unsupported_ext:{ext}" });
                            Console.WriteLine($"[upload] skip: unsupported ext {ext}");
                            continue;
                        }

                        // === 建立相對路徑 (相對於執行目錄) ===
                        // 例：wwwroot/uploads/products/{product_code}
                        string relativeDir = Path.Combine("wwwroot", "uploads", "products", safeCode);
                        string fullDir = Path.Combine(baseDir, relativeDir);
                        Directory.CreateDirectory(fullDir);

                        // 產生安全檔名
                        string baseName = Path.GetFileNameWithoutExtension(f.FileName);
                        string safeName = SanitizeFileName(baseName);
                        string stamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                        string rand = Guid.NewGuid().ToString("N")[..8];
                        string fileName = $"{stamp}_{rand}_{safeName}{ext}";
                        string fullPath = Path.Combine(fullDir, fileName);

                        Console.WriteLine($"[upload] baseDir   : {baseDir}");
                        Console.WriteLine($"[upload] relativeDir: {relativeDir}");
                        Console.WriteLine($"[upload] fullDir    : {fullDir}");
                        Console.WriteLine($"[upload] fileName   : {fileName}");
                        Console.WriteLine($"[upload] fullPath   : {fullPath}");

                        // === 寫檔 + 計算 SHA256（不使用 CryptoStream；避免 finalize 問題）===
                        string sha256Hex;
                        await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                        using (var sha = SHA256.Create())
                        await using (var input = f.OpenReadStream())
                        {
                            byte[] buffer = new byte[81920];
                            int read;
                            long totalWritten = 0;

                            while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fs.WriteAsync(buffer.AsMemory(0, read));
                                sha.TransformBlock(buffer, 0, read, null, 0);
                                totalWritten += read;
                            }
                            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                            sha256Hex = BitConverter.ToString(sha.Hash!).Replace("-", "").ToLowerInvariant();

                            Console.WriteLine($"[upload] written bytes: {totalWritten}");
                            Console.WriteLine($"[upload] sha256        : {sha256Hex}");
                        }

                        // === URL 與相對路徑（存 DB 與回前端）===
                        // DB/前端相對路徑慣例：/uploads/products/{product_code}/{fileName}
                        string rel = $"/uploads/products/{safeCode}/{fileName}";
                        string url = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{rel}";

                        Console.WriteLine($"[upload] rel: {rel}");
                        Console.WriteLine($"[upload] url: {url}");

                        // === 重複檢查（同 產品代碼 + SHA256）===
                        var dtDup = sqlFiles.WtrteCommandAndExecuteReader($@"
                    SELECT GUID, 檔案網址 FROM {sv.DBName}.{tFiles}
                    WHERE 產品代碼 = '{esc(product_code)}' AND 檔案雜湊 = '{esc(sha256Hex)}'
                    LIMIT 1;");

                        Console.WriteLine($"[upload] duplicate rows: {dtDup.Rows.Count}");

                        string guid;
                        bool isDuplicate = dtDup.Rows.Count > 0;

                        if (isDuplicate)
                        {
                            // 刪除剛存的重複實體檔（只保留先前那份）
                            try { System.IO.File.Delete(fullPath); Console.WriteLine("[upload] deleted duplicated physical file"); } catch (Exception delEx) { Console.WriteLine($"[upload] delete dup file error: {delEx.Message}"); }
                            guid = dtDup.Rows[0]["GUID"].ObjectToString();
                            url = dtDup.Rows[0]["檔案網址"].ObjectToString();

                            results.Add(new
                            {
                                status = "duplicate",
                                guid,
                                product_code,
                                file_category,
                                original_name = f.FileName,
                                content_type = f.ContentType,
                                size_bytes = f.Length,
                                sha256 = sha256Hex,
                                url
                            });
                        }
                        else
                        {
                            guid = Guid.NewGuid().ToString();
                            var row = new product_filesClass
                            {
                                GUID = guid,
                                產品代碼 = product_code,
                                檔案名稱 = fileName,
                                顯示名稱 = string.IsNullOrWhiteSpace(display_name) ? safeName : display_name!,
                                檔案類型 = string.IsNullOrWhiteSpace(file_category) ? "其他" : file_category!,
                                內容類型 = f.ContentType,
                                檔案大小 = f.Length.ToString(),
                                檔案雜湊 = sha256Hex,
                                儲存方式 = "local",
                                相對路徑 = rel,
                                檔案網址 = url,
                                版本 = string.IsNullOrWhiteSpace(version) ? "1" : version!,
                                啟用狀態 = "1",
                                備註 = note ?? "",
                                建立時間 = DateTime.Now.ToDateTimeString(),
                                更新時間 = DateTime.Now.ToDateTimeString()
                            };
                            sqlFiles.AddRow(null, row.ClassToSQL<product_filesClass, enum_product_files>());

                            results.Add(new
                            {
                                status = "uploaded",
                                guid,
                                product_code,
                                file_category = row.檔案類型,
                                file_name = fileName,
                                display_name = row.顯示名稱,
                                content_type = f.ContentType,
                                size_bytes = f.Length,
                                sha256 = sha256Hex,
                                relative_path = rel,
                                url
                            });
                        }

                        // 設定代表圖（只做一次；僅當此檔案類型屬圖片時）
                        if (!coverSet &&
                            update_cover &&
                            !string.IsNullOrWhiteSpace(file_category) &&
                            (file_category.Equals("圖片") || file_category.Equals("image", StringComparison.OrdinalIgnoreCase)))
                        {
                            var rows = sqlProd.GetRowsByDefult(null, (int)enum_products.產品代碼, product_code);
                            if (rows.Count > 0)
                            {
                                var prod = rows[0].SQLToClass<productsClass, enum_products>();
                                prod.圖片連結 = url;
                                prod.更新時間 = DateTime.Now.ToDateTimeString();
                                sqlProd.UpdateByDefulteExtra(null, prod.ClassToSQL<productsClass, enum_products>());
                                coverSet = true; // 僅首次符合條件的圖片會設為封面
                                Console.WriteLine($"[upload] cover set: {url}");
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"[upload] file failed: {innerEx.Message}");
                        results.Add(new { status = "error", reason = innerEx.Message });
                    }
                }

                rd.Code = 200;
                rd.Result = "處理完成";
                rd.TimeTaken = $"{t}";
                rd.Data = results;
                return rd.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                rd.Code = -200; rd.Result = $"Exception: {ex.Message}";
                Console.WriteLine($"[upload] exception: {ex.Message}");
                return rd.JsonSerializationt(true);
            }
        }

      


        /// <summary>
        /// 取得產品圖片檔案清單
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 可依指定的 `product_code`（產品代碼）查詢 `product_files` 資料表，  
        /// 並回傳該產品對應的所有圖片檔案資訊（包含上傳時間與檔案連結）。  
        /// 
        /// **查詢邏輯：**
        /// 1. 驗證 `ValueAry` 欄位不可為空，且必須包含 `product_code` 條件。  
        /// 2. 依據 `product_code` 查詢 `product_files` 資料表，取得所有符合條件的紀錄。  
        /// 3. 依 `upload_time` 欄位由新到舊排序。  
        /// 4. 將結果以 JSON 格式回傳。  
        /// 
        /// **注意事項：**
        /// - 若該產品尚未上傳任何圖片，則回傳空陣列。  
        /// - `product_code` 必須完全一致（區分大小寫規則依資料庫設定）。  
        /// 
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "ValueAry": [ "product_code=P001" ]
        /// }
        /// </code>
        /// 
        /// **JSON 回應範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "查詢完成，共 2 筆資料",
        ///     "Data": [
        ///         {
        ///             "product_code": "P001",
        ///             "file_url": "https://example.com/images/P001_20250101.jpg",
        ///             "upload_time": "2025-01-01 10:20:30"
        ///         },
        ///         {
        ///             "product_code": "P001",
        ///             "file_url": "https://example.com/images/P001_20241230.jpg",
        ///             "upload_time": "2024-12-30 14:05:10"
        ///         }
        ///     ],
        ///     "TimeTaken": "00:00:00.045"
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("get_product_files")]
        public string get_product_files([FromBody] returnData returnData)
        {
            var t = new MyTimerBasic();
            returnData.Method = "get_product_files";
            try
            {
                string productCode = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("product_code="))?.Split('=')[1] ?? "";
                if (productCode.StringIsEmpty())
                {
                    returnData.Code = -200; returnData.Result = "缺少 product_code"; return returnData.JsonSerializationt();
                }

                string category = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("category="))?.Split('=')[1] ?? "";
                string active = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("active="))?.Split('=')[1] ?? "";
                string keyword = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("keyword="))?.Split('=')[1] ?? "";
                int page = (returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("page="))?.Split('=')[1]).StringToInt32();
                int pageSize = (returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("pageSize="))?.Split('=')[1]).StringToInt32();
                if (page <= 0) page = 1; if (pageSize <= 0) pageSize = 50;
                int offset = (page - 1) * pageSize;

                var servers = serverSetting.GetAllServerSetting();
                var sv = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sv == null) { returnData.Code = -200; returnData.Result = "找不到 Server 設定"; return returnData.JsonSerializationt(); }

                string tFiles = new enum_product_files().GetEnumDescription();
                var sql = new SQLControl(sv.Server, sv.DBName, tFiles, sv.User, sv.Password, sv.Port.StringToUInt32(), SSLMode);
                string esc(string s) => (s ?? "").Replace("'", "''");

                string where = $"WHERE 產品代碼 = '{esc(productCode)}'";
                if (!category.StringIsEmpty()) where += $" AND 檔案類型 = '{esc(category)}'";
                if (active == "1" || active == "0") where += $" AND 啟用狀態 = '{esc(active)}'";
                if (!keyword.StringIsEmpty())
                {
                    string k = esc(keyword);
                    where += $" AND (顯示名稱 LIKE '%{k}%' OR 檔案名稱 LIKE '%{k}%' OR 備註 LIKE '%{k}%' OR 內容類型 LIKE '%{k}%')";
                }

                var dt = sql.WtrteCommandAndExecuteReader($@"
            SELECT * FROM {sv.DBName}.{tFiles}
            {where}
            ORDER BY 建立時間 DESC
            LIMIT {offset},{pageSize};");

                var list = dt.DataTableToRowList().SQLToClass<product_filesClass, enum_product_files>();

                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{t}";
                returnData.Data = list;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                return new returnData { Code = -200, Method = "get_product_files", Result = ex.Message }.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 更新產品圖片檔案的中繼資料（Meta）
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 可更新 `product_files` 資料表中指定檔案的中繼資料（例如：檔案說明、標籤等）。  
        /// 可依 `product_code` 與 `file_url` 找到目標紀錄後進行更新。  
        /// 
        /// **更新邏輯：**
        /// 1. 驗證 `Data` 欄位不可為空，且必須為陣列。  
        /// 2. 每筆資料需包含：  
        ///    - `product_code`（產品代碼，必填）  
        ///    - `file_url`（檔案連結，必填）  
        ///    - 可選的中繼資料欄位（如 `description`、`tags` 等）。  
        /// 3. 依據 `product_code` + `file_url` 查詢 `product_files` 資料表，若存在則更新對應欄位。  
        /// 4. 若找不到對應紀錄則回傳錯誤訊息。  
        /// 
        /// **注意事項：**
        /// - 欄位型別限制：`TEXT`、`DATETIME`、`VARCHAR`。  
        /// - 僅更新傳入的欄位，其餘欄位保持不變。  
        /// - `upload_time` 不會被更新。  
        /// 
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "Data": [
        ///         {
        ///             "product_code": "P001",
        ///             "file_url": "https://example.com/images/P001_20250101.jpg",
        ///             "description": "產品正面照片",
        ///             "tags": "正面,包裝,高清"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// 
        /// **JSON 回應範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "更新完成，共 1 筆資料",
        ///     "TimeTaken": "00:00:00.032"
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("update_product_file_meta")]
        public string update_product_file_meta([FromBody] returnData returnData)
        {
            var t = new MyTimerBasic();
            returnData.Method = "update_product_file_meta";
            try
            {
                var input = returnData.Data.ObjToClass<product_filesClass>();
                if (input == null || input.GUID.StringIsEmpty())
                {
                    returnData.Code = -200; returnData.Result = "缺少 GUID 或 Data 格式錯誤"; return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var sv = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sv == null) { returnData.Code = -200; returnData.Result = "找不到 Server 設定"; return returnData.JsonSerializationt(); }

                string tFiles = new enum_product_files().GetEnumDescription();
                var sql = new SQLControl(sv.Server, sv.DBName, tFiles, sv.User, sv.Password, sv.Port.StringToUInt32(), SSLMode);

                var rows = sql.GetRowsByDefult(null, (int)enum_product_files.GUID, input.GUID);
                if (rows.Count == 0) { returnData.Code = -200; returnData.Result = "找不到檔案資料"; return returnData.JsonSerializationt(); }

                var old = rows[0].SQLToClass<product_filesClass, enum_product_files>();

                if (!input.顯示名稱.StringIsEmpty()) old.顯示名稱 = input.顯示名稱;
                if (!input.備註.StringIsEmpty()) old.備註 = input.備註;
                if (!input.版本.StringIsEmpty()) old.版本 = input.版本;
                if (!input.啟用狀態.StringIsEmpty()) old.啟用狀態 = input.啟用狀態;
                if (!input.檔案類型.StringIsEmpty()) old.檔案類型 = input.檔案類型;

                old.更新時間 = DateTime.Now.ToDateTimeString();
                sql.UpdateByDefulteExtra(null, old.ClassToSQL<product_filesClass, enum_product_files>());

                returnData.Code = 200;
                returnData.Result = "更新成功";
                returnData.TimeTaken = $"{t}";
                returnData.Data = old;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                return new returnData { Code = -200, Method = "update_product_file_meta", Result = ex.Message }.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 刪除產品圖片檔案
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 可刪除 `product_files` 資料表中指定的產品圖片紀錄。  
        /// 依據 `product_code` 與 `file_url` 來精確刪除指定的檔案紀錄。  
        /// 
        /// **刪除邏輯：**
        /// 1. 驗證 `Data` 欄位不可為空，且必須為陣列。  
        /// 2. 每筆資料需包含：  
        ///    - `product_code`（產品代碼，必填）  
        ///    - `file_url`（檔案連結，必填）。  
        /// 3. 系統會依 `product_code` + `file_url` 查詢 `product_files` 資料表。  
        ///    - 若找到 → 刪除該筆紀錄。  
        ///    - 若找不到 → 回傳錯誤訊息。  
        /// 
        /// **注意事項：**
        /// - 欄位型別限制：`TEXT`、`DATETIME`、`VARCHAR`。  
        /// - 刪除動作僅影響資料表，不會刪除實體檔案。  
        /// 
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "Data": [
        ///         {
        ///             "product_code": "P001",
        ///             "file_url": "https://example.com/images/P001_20250101.jpg"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// 
        /// **JSON 回應範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "刪除完成，共 1 筆資料",
        ///     "TimeTaken": "00:00:00.018"
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("delete_product_file")]
        public string delete_product_file([FromBody] returnData returnData)
        {
            var t = new MyTimerBasic();
            returnData.Method = "delete_product_file";
            try
            {
                string guid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("guid="))?.Split('=')[1] ?? "";
                if (guid.StringIsEmpty())
                {
                    returnData.Code = -200; returnData.Result = "缺少 guid"; return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var sv = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sv == null) { returnData.Code = -200; returnData.Result = "找不到 Server 設定"; return returnData.JsonSerializationt(); }

                string tFiles = new enum_product_files().GetEnumDescription();
                var sql = new SQLControl(sv.Server, sv.DBName, tFiles, sv.User, sv.Password, sv.Port.StringToUInt32(), SSLMode);

                var rows = sql.GetRowsByDefult(null, (int)enum_product_files.GUID, guid);
                if (rows.Count == 0) { returnData.Code = -200; returnData.Result = "找不到檔案資料"; return returnData.JsonSerializationt(); }

                var file = rows[0].SQLToClass<product_filesClass, enum_product_files>();

                // 刪實體
                if (!file.相對路徑.StringIsEmpty())
                {
                    string full = Path.Combine(AppContext.BaseDirectory, "wwwroot", file.相對路徑.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(full))
                    {
                        try { System.IO.File.Delete(full); } catch { /* log */ }
                    }
                }

                // 刪 DB
                sql.DeleteExtra(tFiles, rows);

                returnData.Code = 200;
                returnData.Result = "刪除完成";
                returnData.TimeTaken = $"{t}";
                returnData.Data = new { guid = file.GUID, file_url = file.檔案網址 };
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                return new returnData { Code = -200, Method = "delete_product_file", Result = ex.Message }.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 設定產品封面圖片
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 可將指定產品的某張圖片設為封面。  
        /// 系統會依據 `product_code` 與 `file_url` 找到對應的圖片紀錄，並將該圖片標記為封面，其他圖片的封面標記將自動取消。  
        /// 
        /// **設定邏輯：**
        /// 1. 驗證 `Data` 欄位不可為空，且必須為陣列。  
        /// 2. 每筆資料需包含：  
        ///    - `product_code`（產品代碼，必填）  
        ///    - `file_url`（檔案連結，必填）。  
        /// 3. 系統會：  
        ///    - 將該 `product_code` 所有圖片的 `is_cover` 設為 false。  
        ///    - 將符合 `file_url` 的圖片 `is_cover` 設為 true。  
        /// 4. 若未找到圖片紀錄，回傳錯誤訊息。  
        /// 
        /// **注意事項：**
        /// - `is_cover` 欄位為布林值（true/false），表示是否為封面。  
        /// - 設定封面僅影響資料表，不會對實體檔案進行搬移或修改。  
        /// 
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "Data": [
        ///         {
        ///             "product_code": "P001",
        ///             "file_url": "https://example.com/images/P001_cover.jpg"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// 
        /// **JSON 回應範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "封面設定成功",
        ///     "TimeTaken": "00:00:00.012"
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("set_product_cover")]
        public string set_product_cover([FromBody] returnData returnData)
        {
            var t = new MyTimerBasic();
            returnData.Method = "set_product_cover";
            try
            {
                string guid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("guid="))?.Split('=')[1] ?? "";
                if (guid.StringIsEmpty())
                {
                    returnData.Code = -200; returnData.Result = "缺少 guid"; return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var sv = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sv == null) { returnData.Code = -200; returnData.Result = "找不到 Server 設定"; return returnData.JsonSerializationt(); }

                string tFiles = new enum_product_files().GetEnumDescription();
                string tProd = new enum_products().GetEnumDescription();

                var sqlFiles = new SQLControl(sv.Server, sv.DBName, tFiles, sv.User, sv.Password, sv.Port.StringToUInt32(), SSLMode);
                var sqlProd = new SQLControl(sv.Server, sv.DBName, tProd, sv.User, sv.Password, sv.Port.StringToUInt32(), SSLMode);

                var rows = sqlFiles.GetRowsByDefult(null, (int)enum_product_files.GUID, guid);
                if (rows.Count == 0) { returnData.Code = -200; returnData.Result = "找不到檔案資料"; return returnData.JsonSerializationt(); }

                var f = rows[0].SQLToClass<product_filesClass, enum_product_files>();
                bool isImage = f.檔案類型.Equals("圖片") || f.檔案類型.Equals("image", StringComparison.OrdinalIgnoreCase);
                if (!isImage)
                {
                    returnData.Code = -200; returnData.Result = "此檔案非圖片類型，無法設為代表圖"; return returnData.JsonSerializationt();
                }

                var prodRows = sqlProd.GetRowsByDefult(null, (int)enum_products.產品代碼, f.產品代碼);
                if (prodRows.Count == 0) { returnData.Code = -200; returnData.Result = $"找不到產品代碼 {f.產品代碼}"; return returnData.JsonSerializationt(); }

                var prod = prodRows[0].SQLToClass<productsClass, enum_products>();
                prod.圖片連結 = f.檔案網址;
                prod.更新時間 = DateTime.Now.ToDateTimeString();
                sqlProd.UpdateByDefulteExtra(null, prod.ClassToSQL<productsClass, enum_products>());

                returnData.Code = 200;
                returnData.Result = "已設定代表圖";
                returnData.TimeTaken = $"{t}";
                returnData.Data = new { product_code = f.產品代碼, cover_url = f.檔案網址 };
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                return new returnData { Code = -200, Method = "set_product_cover", Result = ex.Message }.JsonSerializationt(true);
            }
        }

        private static string SanitizeCode(string code) =>
            string.IsNullOrWhiteSpace(code) ? "unknown" : Regex.Replace(code.Trim(), @"[^A-Za-z0-9_\-]", "_");
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "file";
            var invalid = string.Join("", Path.GetInvalidFileNameChars());
            var pattern = $"[{Regex.Escape(invalid)}]";
            var cleaned = Regex.Replace(name, pattern, "_").Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned;
        }

        private List<Table> CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            string Server = sys_serverSettingClass.Server;
            string DB = sys_serverSettingClass.DBName;
            string UserName = sys_serverSettingClass.User;
            string Password = sys_serverSettingClass.Password;
            uint Port = (uint)sys_serverSettingClass.Port.StringToInt32();
            List<Table> tables = new List<Table>();
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_product_files()));
            return tables;
        }
    }
}
