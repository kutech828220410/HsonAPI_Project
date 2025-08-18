using Basic;
using HsonAPILib; // 這裡引用你的 enum_products, productsClass
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace HsonWebAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class products : ControllerBase
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
        /// 新增或更新多筆產品資料
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 可一次新增或更新多筆 `products` 資料表的產品資料。  
        /// 若產品代碼已存在，則更新該筆資料內容，否則新增。  
        /// **以下為 JSON 範例**  
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "Data": [
        ///         {
        ///             "product_code": "P001",
        ///             "product_name": "感冒藥",
        ///             "product_type": "single",
        ///             "spec": "500mg",
        ///             "brand": "ABC",
        ///             "category": "藥品",
        ///             "unit": "盒",
        ///             "price": "100",
        ///             "status": "啟用",
        ///             "note": "一般感冒用藥",
        ///             "BARCODE": ["1234567890123"],
        ///             "doc_name": "產品說明書",
        ///             "doc_url": "https://example.com/doc.pdf",
        ///             "doc_version": "v1.0",
        ///             "img_url": "https://example.com/image.jpg"
        ///         },
        ///         {
        ///             "product_code": "P002",
        ///             "product_name": "止痛藥",
        ///             "product_type": "single",
        ///             "spec": "500mg",
        ///             "brand": "XYZ",
        ///             "category": "藥品",
        ///             "unit": "盒",
        ///             "price": "120",
        ///             "status": "啟用",
        ///             "note": "一般止痛用藥",
        ///             "BARCODE": ["9876543210987"]
        ///         }
        ///     ]
        /// }
        /// </code>
        /// </remarks>
        [Route("add_or_update_product")]
        [HttpPost]
        public string add_or_update_product([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "add_or_update_product";
            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 嘗試將 Data 轉成多筆資料
                List<productsClass> productList = returnData.Data.ObjToClass<List<productsClass>>();

     

                if (productList == null || productList.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // 取得資料庫連線設定
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                var _sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");

                if (_sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無 Server 資料";
                    return returnData.JsonSerializationt();
                }

                string Server = _sys_serverSettingClasses[0].Server;
                string DB = _sys_serverSettingClasses[0].DBName;
                string UserName = _sys_serverSettingClasses[0].User;
                string Password = _sys_serverSettingClasses[0].Password;
                uint Port = (uint)_sys_serverSettingClasses[0].Port.StringToInt32();
                string TableName = new enum_products().GetEnumDescription();
                SQLControl sQLControl_products = new SQLControl(Server, DB, TableName, UserName, Password, Port, SSLMode);

                List<string> processResults = new List<string>();
                List<productsClass> processedProducts = new List<productsClass>();

                foreach (var newProduct in productList)
                {
                    if (newProduct == null || newProduct.產品代碼.StringIsEmpty())
                    {
                        processResults.Add("缺少必要欄位 product_code，該筆資料已跳過");
                        continue;
                    }

                    // 檢查是否存在
                    List<object[]> existRows = sQLControl_products.GetRowsByDefult(null, (int)enum_products.產品代碼, newProduct.產品代碼);
                    if (existRows.Count > 0)
                    {
                        // 已存在 → 更新
                        object[] updateRow = existRows[0];
                        newProduct.GUID = updateRow[(int)enum_products.GUID].ObjectToString();
                        newProduct.建立時間 = updateRow[(int)enum_products.建立時間].ToDateTimeString();
                        newProduct.更新時間 = DateTime.Now.ToDateTimeString();

                        object[] updateValue = newProduct.ClassToSQL<productsClass, enum_products>();
                        sQLControl_products.UpdateByDefulteExtra(null, updateValue);

                        processResults.Add($"產品代碼 {newProduct.產品代碼} 已存在，已更新資料");
                    }
                    else
                    {
                        // 不存在 → 新增
                        newProduct.GUID = Guid.NewGuid().ToString();
                        newProduct.建立時間 = DateTime.Now.ToDateTimeString();
                        newProduct.更新時間 = DateTime.Now.ToDateTimeString();

                        object[] insertValue = newProduct.ClassToSQL<productsClass, enum_products>();
                        sQLControl_products.AddRow(null, insertValue);

                        processResults.Add($"新增產品成功 (產品代碼: {newProduct.產品代碼})");
                    }

                    processedProducts.Add(newProduct);
                }

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = processedProducts;
                returnData.Result = string.Join("；", processResults);
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 批次刪除產品資料
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 可一次刪除多筆 `products` 資料表的產品資料。  
        /// 系統會依據 `product_code`（產品代碼）來刪除對應資料。  
        /// 
        /// **刪除邏輯：**
        /// 1. 驗證 `Data` 欄位不可為空，且必須為陣列。  
        /// 2. 每筆資料必須包含 `product_code`，且不可為空白。  
        /// 3. 依據 `product_code` 查詢資料庫：  
        ///    - 若存在 → 刪除該筆產品資料。  
        ///    - 若不存在 → 記錄「找不到產品代碼」的訊息。  
        /// 4. 批次刪除完成後回傳成功與失敗的清單。  
        /// 
        /// **注意事項：**
        /// - 刪除產品不會自動刪除與之關聯的子項目（若需同步刪除，請先呼叫 `batch_sync_product_components` 移除關聯）。  
        /// - `product_code` 必須完全一致（區分大小寫規則依資料庫設定）。  
        /// 
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "Data": [
        ///         { "product_code": "P001" },
        ///         { "product_code": "P002" }
        ///     ]
        /// }
        /// </code>
        /// 
        /// **JSON 回應範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "刪除完成，成功 1 筆，失敗 1 筆",
        ///     "Data": {
        ///         "deleted": ["P001"],
        ///         "not_found": ["P002"]
        ///     },
        ///     "TimeTaken": "00:00:00.123"
        /// }
        /// </code>
        /// </remarks>
        [Route("delete_products")]
        [HttpPost]
        public string delete_products([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "delete_products";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 嘗試將 Data 轉成多筆資料
                List<productsClass> productList = returnData.Data.ObjToClass<List<productsClass>>();
                if (productList == null || productList.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // 取得資料庫連線設定
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                var _sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");

                if (_sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無 Server 資料";
                    return returnData.JsonSerializationt();
                }

                string Server = _sys_serverSettingClasses[0].Server;
                string DB = _sys_serverSettingClasses[0].DBName;
                string UserName = _sys_serverSettingClasses[0].User;
                string Password = _sys_serverSettingClasses[0].Password;
                uint Port = (uint)_sys_serverSettingClasses[0].Port.StringToInt32();
                string TableName = new enum_products().GetEnumDescription();
                SQLControl sQLControl_products = new SQLControl(Server, DB, TableName, UserName, Password, Port, SSLMode);

                List<string> deletedCodes = new List<string>();
                List<string> notFoundCodes = new List<string>();

                foreach (var product in productList)
                {
                    if (product == null || product.產品代碼.StringIsEmpty())
                    {
                        notFoundCodes.Add("(空白代碼)");
                        continue;
                    }

                    List<object[]> existRows = sQLControl_products.GetRowsByDefult(null, (int)enum_products.產品代碼, product.產品代碼);
                    if (existRows.Count > 0)
                    {
                        sQLControl_products.DeleteExtra(TableName, existRows);
                        deletedCodes.Add(product.產品代碼);
                    }
                    else
                    {
                        notFoundCodes.Add(product.產品代碼);
                    }
                }

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = new { deleted = deletedCodes, not_found = notFoundCodes };
                returnData.Result = $"刪除完成，成功 {deletedCodes.Count} 筆，失敗 {notFoundCodes.Count} 筆";
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 批次同步產品子項關聯資料（新增、更新、刪除，確保節點與傳入資料完全一致）
        /// </summary>
        /// <remarks>
        /// 此 API 用於將指定父產品的子項目結構與傳入資料同步，系統會依據 <c>parent_code</c> 與 <c>child_code</c> 進行比對與處理：  
        /// - 若節點已存在於資料庫 → 更新數量與備註。  
        /// - 若節點不存在於資料庫 → 新增新的子項關聯。  
        /// - 若節點存在於資料庫但未出現在本批資料 → 刪除該關聯。  
        /// 
        /// <b>邏輯流程：</b>
        /// 1. 驗證 Data 欄位不可為空，且同一批次所有 <c>parent_code</c> 必須一致且不可為空。  
        /// 2. 檢查父產品是否存在於產品主檔。  
        /// 3. 批次去除重複的 (<c>parent_code</c> + <c>child_code</c>) pair，保留最後一筆資料。  
        /// 4. 過濾不存在於產品主檔的子產品代碼。  
        /// 5. 查詢資料庫中該父產品的現有子項關聯，分類為：  
        ///    - 新增清單（傳入有、資料庫沒有）  
        ///    - 更新清單（傳入有、資料庫也有）  
        ///    - 刪除清單（傳入沒有、資料庫有）  
        /// 6. 依分類結果批次執行新增、更新、刪除動作。  
        /// 
        /// <b>注意事項：</b>
        /// - <c>parent_code</c> 與 <c>child_code</c> 均不可為空，且必須存在於產品主檔。  
        /// - 同一批次的 <c>parent_code</c> 必須一致。  
        /// - 若 <c>parent_code</c> 與 <c>child_code</c> 相同，該筆資料會被忽略（避免自連）。  
        /// - 執行刪除時，會移除所有不在本批資料中的舊節點，確保結構完全同步。  
        /// 
        /// <b>JSON 請求範例：</b>
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "Data": [
        ///         { "parent_code": "P001", "child_code": "C001", "數量": "10", "備註": "第一批子項" },
        ///         { "parent_code": "P001", "child_code": "C002", "數量": "5", "備註": "第二批子項" }
        ///     ]
        /// }
        /// </code>
        /// 
        /// <b>JSON 回應範例：</b>
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "處理完成，新增 1 筆，更新 2 筆，刪除 1 筆",
        ///     "TimeTaken": "0.145 秒"
        /// }
        /// </code>
        /// </remarks>
        [Route("batch_sync_product_components")]
        [HttpPost]
        public string batch_sync_product_components([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "batch_sync_product_components";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 反序列化
                List<product_componentsClass> componentsList = returnData.Data.ObjToListClass<product_componentsClass>();
                if (componentsList == null || componentsList.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 無有效資料";
                    return returnData.JsonSerializationt();
                }

                // 基本檢核：parent_code 一致
                string parentCode = componentsList[0].parent_code;
                if (parentCode.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "parent_code 不可為空";
                    return returnData.JsonSerializationt();
                }
                if (componentsList.Any(x => x.parent_code != parentCode))
                {
                    returnData.Code = -200;
                    returnData.Result = "同一批次 Data 的 parent_code 必須一致";
                    return returnData.JsonSerializationt();
                }

                // 取得資料庫設定
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                var sys_ServerSetting = sys_serverSettingClasses.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sys_ServerSetting == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string productsTable = new enum_products().GetEnumDescription();
                string componentsTable = new enum_product_components().GetEnumDescription();

                SQLControl sQLControl = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, componentsTable, sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);

                // SQL escape
                string Esc(string s) => (s ?? "").Replace("'", "''");

                // 檢查父產品是否存在
                string checkParentSql = $"SELECT 1 FROM {sys_ServerSetting.DBName}.{productsTable} WHERE 產品代碼 = '{Esc(parentCode)}' LIMIT 1";
                if (sQLControl.WtrteCommandAndExecuteReader(checkParentSql).Rows.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到父產品代碼 {parentCode}";
                    return returnData.JsonSerializationt();
                }

                // 去除批次輸入中的重複 pair（保留最後一筆）
                var dedupMap = new Dictionary<string, product_componentsClass>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in componentsList)
                {
                    if (c.child_code.StringIsEmpty()) continue;
                    if (c.parent_code == c.child_code) continue; // 跳過自連
                    string key = $"{c.parent_code}||{c.child_code}";
                    dedupMap[key] = c;
                }
                var dedupList = dedupMap.Values.ToList();

                if (dedupList.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 無有效的子項目資料";
                    return returnData.JsonSerializationt();
                }

                // 檢查子產品是否存在
                var childCodes = dedupList.Select(x => x.child_code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var existsChild = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                {
                    var inList = string.Join(",", childCodes.Select(c => $"'{Esc(c)}'"));
                    string sql = $@"SELECT 產品代碼 FROM {sys_ServerSetting.DBName}.{productsTable} WHERE 產品代碼 IN ({inList})";
                    var dt = sQLControl.WtrteCommandAndExecuteReader(sql);
                    foreach (System.Data.DataRow r in dt.Rows) existsChild.Add(r["產品代碼"].ObjectToString());
                }
                dedupList = dedupList.Where(x => existsChild.Contains(x.child_code)).ToList();

                if (dedupList.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "所有子產品代碼皆不存在";
                    return returnData.JsonSerializationt();
                }

                // 取得現有關聯
                var existingMap = new Dictionary<string, List<object[]>>(StringComparer.OrdinalIgnoreCase);
                {
                    string sql = $@"SELECT * FROM {sys_ServerSetting.DBName}.{componentsTable} WHERE parent_code = '{Esc(parentCode)}'";
                    var dt = sQLControl.WtrteCommandAndExecuteReader(sql);
                    foreach (System.Data.DataRow r in dt.Rows)
                    {
                        string child = r["child_code"].ObjectToString();
                        if (!existingMap.TryGetValue(child, out var list))
                        {
                            list = new List<object[]>();
                            existingMap[child] = list;
                        }
                        list.Add(r.ItemArray);
                    }
                }

                // 分類
                var list_add = new List<object[]>();
                var list_update = new List<object[]>();
                var list_all_keep = new List<object[]>(); // 最終應保留的資料庫資料

                foreach (var child in dedupList)
                {
                    if (existingMap.TryGetValue(child.child_code, out var existRows) && existRows.Count > 0)
                    {
                        foreach (var row in existRows)
                        {
                            row[(int)enum_product_components.數量] = child.數量;
                            row[(int)enum_product_components.備註] = child.備註;
                            row[(int)enum_product_components.更新時間] = DateTime.Now.ToDateTimeString();
                            list_update.Add(row);
                            list_all_keep.Add(row);
                        }
                    }
                    else
                    {
                        child.GUID = Guid.NewGuid().ToString();
                        child.建立時間 = DateTime.Now.ToDateTimeString();
                        child.更新時間 = DateTime.Now.ToDateTimeString();
                        var newRow = child.ClassToSQL<product_componentsClass, enum_product_components>();
                        list_add.Add(newRow);
                        list_all_keep.Add(newRow);
                    }
                }

                // 找出要刪除的資料
                var list_delete = new List<object[]>();
                foreach (var kv in existingMap)
                {
                    bool stillExists = dedupList.Any(x => x.child_code.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                    if (!stillExists)
                    {
                        list_delete.AddRange(kv.Value);
                    }
                }

                // 寫入資料庫
                if (list_add.Count > 0) sQLControl.AddRows(null, list_add);
                if (list_update.Count > 0) sQLControl.UpdateByDefulteExtra(null, list_update);
                if (list_delete.Count > 0) sQLControl.DeleteExtra(componentsTable, list_delete);

                returnData.Code = 200;
                returnData.Result = $"處理完成，新增 {list_add.Count} 筆，更新 {list_update.Count} 筆，刪除 {list_delete.Count} 筆";
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 取得所有產品列表（含子項數量與父項數量）
        /// </summary>
        /// <remarks>
        /// 從 `products` 資料表查詢產品資料，並同時計算每個產品的子項與父項數量。  
        /// 支援關鍵字搜尋與分頁功能。  
        ///
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "ValueAry": [ "keyword=測試", "page=1", "pageSize=50" ]
        /// }
        /// </code>
        ///
        /// **欄位說明：**
        /// - ServerName：伺服器名稱（對應設定檔）
        /// - ServerType：伺服器類型（如「網頁」、「API」）
        /// - ValueAry：
        ///   - keyword：關鍵字（搜尋產品代碼、名稱、廠牌）
        ///   - page：分頁頁碼（預設 1）
        ///   - pageSize：每頁筆數（預設 50）
        ///
        /// **回傳範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "OK",
        ///     "Data": [
        ///         {
        ///             "GUID": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
        ///             "product_code": "P001",
        ///             "product_name": "測試產品",
        ///             "brand": "廠牌A",
        ///             "child_components": [ {}, {}, {} ], // 子項數量=3
        ///             "parent_products": [ {} ]           // 父項數量=1
        ///         }
        ///     ],
        ///     "TimeTaken": "0.123 秒"
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構（包含伺服器資訊與查詢條件）</param>
        /// <returns>產品列表，並包含子項與父項數量</returns>
        [Route("get_all")]
        [HttpPost]
        public string get_all([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "get_all";

            try
            {
                // 取得資料庫設定
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                var sys_ServerSetting = sys_serverSettingClasses.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sys_ServerSetting == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string productsTable = new enum_products().GetEnumDescription();
                string componentsTable = new enum_product_components().GetEnumDescription();

                SQLControl sQLControl = new SQLControl(
                    sys_ServerSetting.Server,
                    sys_ServerSetting.DBName,
                    productsTable,
                    sys_ServerSetting.User,
                    sys_ServerSetting.Password,
                    sys_ServerSetting.Port.StringToUInt32(),
                    SSLMode
                );

                // 預設條件
                string sql = $"SELECT * FROM {sys_ServerSetting.DBName}.{productsTable} WHERE 1=1";

                // 關鍵字搜尋
                string keyword = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("keyword="))?.Split('=')[1];
                if (!string.IsNullOrEmpty(keyword))
                {
                    string safeKeyword = keyword.Replace("'", "''");
                    sql += $" AND (產品代碼 LIKE '%{safeKeyword}%' OR 產品名稱 LIKE '%{safeKeyword}%' OR 廠牌 LIKE '%{safeKeyword}%')";
                }

                // 分頁
                int page = 1;
                int pageSize = 50;
                string pageStr = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("page="))?.Split('=')[1];
                string pageSizeStr = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("pageSize="))?.Split('=')[1];
                int.TryParse(pageStr, out page);
                int.TryParse(pageSizeStr, out pageSize);

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 50;

                int offset = (page - 1) * pageSize;
                sql += $" LIMIT {offset}, {pageSize}";

                // 查詢主要產品資料
                var dataTable = sQLControl.WtrteCommandAndExecuteReader(sql);
                var productList = dataTable.DataTableToRowList().SQLToClass<productsClass, enum_products>();

                // 取得所有產品代碼
                var allCodes = productList.Select(p => p.產品代碼).Where(c => !string.IsNullOrEmpty(c)).ToList();

                if (allCodes.Count > 0)
                {
                    // 子項數量
                    string childCountSql = $@"
                SELECT parent_code, COUNT(*) AS cnt
                FROM {sys_ServerSetting.DBName}.{componentsTable}
                WHERE parent_code IN ({string.Join(",", allCodes.Select(c => $"'{c}'"))})
                GROUP BY parent_code;
            ";
                    var childCounts = sQLControl.WtrteCommandAndExecuteReader(childCountSql)
                        .AsEnumerable()
                        .ToDictionary(r => r["parent_code"].ToString(), r => r["cnt"].ToString().StringToInt32());

                    // 父項數量
                    string parentCountSql = $@"
                SELECT child_code, COUNT(*) AS cnt
                FROM {sys_ServerSetting.DBName}.{componentsTable}
                WHERE child_code IN ({string.Join(",", allCodes.Select(c => $"'{c}'"))})
                GROUP BY child_code;
            ";
                    var parentCounts = sQLControl.WtrteCommandAndExecuteReader(parentCountSql)
                        .AsEnumerable()
                        .ToDictionary(r => r["child_code"].ToString(), r => r["cnt"].ToString().StringToInt32());

                    // 套用數量
                    foreach (var p in productList)
                    {
                        int childCount = childCounts.ContainsKey(p.產品代碼) ? childCounts[p.產品代碼] : 0;
                        int parentCount = parentCounts.ContainsKey(p.產品代碼) ? parentCounts[p.產品代碼] : 0;

                        p.child_components = Enumerable.Range(0, childCount).Select(_ => new product_componentsClass()).ToList();
                        p.parent_products = Enumerable.Range(0, parentCount).Select(_ => new product_componentsClass()).ToList();
                    }
                }

                returnData.Data = productList;
                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{myTimerBasic}";

                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 依產品代碼取得產品詳細資料（含遞迴的子項與父項）
        /// </summary>
        /// <remarks>
        /// 從 `products` 資料表查詢指定產品代碼的資料，並同時遞迴取得：  
        /// - 子項樹（往下展開，最多 5 層）  
        /// - 父項樹（由最上層往下展開，最多 5 層）  
        ///
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "VM端",
        ///     "ValueAry": [ "product_code=C003" ]
        /// }
        /// </code>
        ///
        /// **欄位說明：**
        /// - ServerName：伺服器名稱（對應設定檔）
        /// - ServerType：伺服器類型（如「VM端」、「API」）
        /// - ValueAry：
        ///   - product_code：要查詢的產品代碼
        ///
        /// **回傳範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "OK",
        ///     "Data": {
        ///         "產品代碼": "C003",
        ///         "產品名稱": "子產品三號",
        ///         "廠牌": "品牌X",
        ///         "售價": "150",
        ///         "狀態": "active",
        ///         "child_count": 2,
        ///         "parent_count": 0,
        ///         "child_components": [
        ///             {
        ///                 "parent_code": "C003",
        ///                 "child_code": "C006",
        ///                 "產品名稱": "子產品六號",
        ///                 "廠牌": "品牌Y",
        ///                 "售價": "200",
        ///                 "狀態": "active",
        ///                 "數量": "3",
        ///                 "child_components": []
        ///             },
        ///             {
        ///                 "parent_code": "C003",
        ///                 "child_code": "C005",
        ///                 "產品名稱": "子產品五號",
        ///                 "廠牌": "品牌Z",
        ///                 "售價": "180",
        ///                 "狀態": "active",
        ///                 "數量": "4",
        ///                 "child_components": []
        ///             }
        ///         ],
        ///         "parent_products": []
        ///     },
        ///     "TimeTaken": "0.045 秒"
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構（包含伺服器資訊與查詢條件）</param>
        /// <returns>指定產品的詳細資訊，並包含遞迴展開的子項與父項資料</returns>
        [Route("get_product_hierarchy_by_codes")]
        [HttpPost]
        public string get_product_hierarchy_by_codes([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "get_product_hierarchy_by_codes";

            try
            {
                // 解析輸入的多個或單個產品代碼
                var codesInput = new List<string>();

                // 支援 product_codes=code1,code2
                var codesStr = returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith("product_codes="))?
                    .Split('=')[1];
                if (!string.IsNullOrEmpty(codesStr))
                {
                    codesInput.AddRange(
                        codesStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(c => c.Trim())
                                .Where(c => !string.IsNullOrWhiteSpace(c))
                    );
                }

                // 支援多個單獨的 product_code=xxx
                var singleCodes = returnData.ValueAry?
                    .Where(x => x.StartsWith("product_code="))
                    .Select(x => x.Split('=')[1]?.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();
                if (singleCodes != null && singleCodes.Count > 0)
                {
                    codesInput.AddRange(singleCodes);
                }

                // 移除重複
                codesInput = codesInput.Distinct().ToList();

                if (codesInput.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 product_code 或 product_codes 參數";
                    return returnData.JsonSerializationt();
                }

                // 取得資料庫設定
                var sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                var sys_ServerSetting = sys_serverSettingClasses.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sys_ServerSetting == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string productsTable = new enum_products().GetEnumDescription();
                string componentsTable = new enum_product_components().GetEnumDescription();

                var sqlProducts = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, productsTable,
                    sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);
                var sqlComponents = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, componentsTable,
                    sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);

                var results = new List<productsClass>();

                foreach (var code in codesInput)
                {
                    var product = GetProductInfo(code, sqlProducts, productsTable);
                    if (product == null) continue;

                    var visitedChild = new HashSet<string>();
                    var visitedParent = new HashSet<string>();

                    // 子項樹
                    product.child_components = GetChildTree(code, sqlComponents, componentsTable, sqlProducts, productsTable, 1, 5, visitedChild);

                    // 父項樹（由最上層往下）
                    product.parent_products = GetParentTreeNaturalOrder(code, sqlComponents, componentsTable, sqlProducts, productsTable, 1, 5, visitedParent);

                    product.child_count = product.child_components.Count;
                    product.parent_count = product.parent_products.Count;

                    results.Add(product);
                }

                returnData.Data = results;
                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{myTimerBasic}";

                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 依多個產品代碼取得完整產品階層（一次批次載入全部資料後於後端組樹）
        /// </summary>
        /// <remarks>
        /// 從 <c>products</c> 與 <c>product_components</c> 資料表一次載入所有資料，  
        /// 並在後端記憶體中組裝出每個指定產品的完整階層樹結構（含子項與父項）。  
        /// 子項樹為向下展開，父項樹為由最上層往下的自然順序。  
        /// 此方法僅進行兩次 SQL 查詢（一次撈產品，一次撈組件），其餘由後端遞迴組合完成，  
        /// 適合 BOM 表、全表展開或需要一次查詢多個代碼的情境。
        ///
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "VM端",
        ///     "ValueAry": [ "product_codes=C001,C003,C005" ]
        /// }
        /// </code>
        /// 或：
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "VM端",
        ///     "ValueAry": [
        ///         "product_code=C001",
        ///         "product_code=C003",
        ///         "product_code=C005"
        ///     ]
        /// }
        /// </code>
        ///
        /// **欄位說明：**
        /// - ServerName：伺服器名稱（對應設定檔）
        /// - ServerType：伺服器類型（如「VM端」、「API」、「網頁」）
        /// - ValueAry：
        ///   - product_codes：逗號分隔的多個產品代碼
        ///   - product_code：單一產品代碼，可重複多筆
        ///
        /// **回傳範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "OK",
        ///     "Data": [
        ///         {
        ///             "產品代碼": "C003",
        ///             "產品名稱": "子產品三號",
        ///             "廠牌": "品牌X",
        ///             "售價": "150",
        ///             "狀態": "active",
        ///             "child_count": 2,
        ///             "parent_count": 1,
        ///             "child_components": [
        ///                 {
        ///                     "parent_code": "C003",
        ///                     "child_code": "C006",
        ///                     "產品名稱": "子產品六號",
        ///                     "廠牌": "品牌Y",
        ///                     "售價": "200",
        ///                     "狀態": "active",
        ///                     "數量": "3",
        ///                     "child_components": []
        ///                 },
        ///                 {
        ///                     "parent_code": "C003",
        ///                     "child_code": "C005",
        ///                     "產品名稱": "子產品五號",
        ///                     "廠牌": "品牌Z",
        ///                     "售價": "180",
        ///                     "狀態": "active",
        ///                     "數量": "4",
        ///                     "child_components": []
        ///                 }
        ///             ],
        ///             "parent_products": [
        ///                 {
        ///                     "parent_code": "C001",
        ///                     "child_code": "C003",
        ///                     "產品名稱": "父產品一號",
        ///                     "廠牌": "品牌A",
        ///                     "售價": "500",
        ///                     "狀態": "active",
        ///                     "數量": "1",
        ///                     "child_components": []
        ///                 }
        ///             ]
        ///         }
        ///     ],
        ///     "TimeTaken": "0.045 秒"
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">
        /// 共用傳遞資料結構（包含伺服器資訊與查詢條件）。  
        /// 支援 <c>product_codes</c>（逗號分隔多碼）與多個 <c>product_code</c>（單碼多筆）。
        /// </param>
        /// <returns>
        /// 多筆 <see cref="productsClass"/>，每筆包含：  
        /// - 基本資訊（產品代碼、名稱、廠牌、售價、狀態）  
        /// - 子項樹（child_components）與子項數量（child_count）  
        /// - 父項樹（parent_products）與父項數量（parent_count）  
        /// </returns>
        [Route("get_full_product_hierarchy")]
        [HttpPost]
        public string get_full_product_hierarchy([FromBody] returnData returnData)
        {
            const int MaxLevel = 5;

            var myTimerBasic = new MyTimerBasic();
            returnData.Method = "get_full_product_hierarchy";

            try
            {
                // 1) 解析輸入代碼（支援 product_codes 與多個 product_code）
                var codesInput = new List<string>();

                var codesStr = returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith("product_codes=", StringComparison.OrdinalIgnoreCase))?
                    .Split('=')[1];

                if (!string.IsNullOrWhiteSpace(codesStr))
                {
                    codesInput.AddRange(
                        codesStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(c => c.Trim())
                                .Where(c => !string.IsNullOrWhiteSpace(c))
                    );
                }

                var singleCodes = returnData.ValueAry?
                    .Where(x => x.StartsWith("product_code=", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Split('=')[1]?.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c));

                if (singleCodes != null) codesInput.AddRange(singleCodes);

                codesInput = codesInput.Distinct().ToList();
                if (codesInput.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 product_code 或 product_codes 參數";
                    return returnData.JsonSerializationt();
                }

                // 2) 取得資料庫設定
                var sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                var sys_ServerSetting = sys_serverSettingClasses.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sys_ServerSetting == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string productsTable = new enum_products().GetEnumDescription();
                string componentsTable = new enum_product_components().GetEnumDescription();

                var sqlProducts = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, productsTable,
                    sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);

                var sqlComponents = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, componentsTable,
                    sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);

                // 3) 批次載入：產品全表 / 組件全表（只兩次 SQL）
                string sqlProd = $@"SELECT 產品代碼, 產品名稱, 廠牌, 售價, 狀態 
                            FROM {sqlProducts.Database}.{productsTable}";
                var dtProd = sqlProducts.WtrteCommandAndExecuteReader(sqlProd);

                string sqlComp = $@"SELECT parent_code, child_code, 數量 
                            FROM {sqlComponents.Database}.{componentsTable}
                            WHERE parent_code <> child_code";
                var dtComp = sqlComponents.WtrteCommandAndExecuteReader(sqlComp);

                // 4) 建索引（記憶體結構）
                // 4.1 產品快取
                var productMap = new Dictionary<string, productsClass>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow row in dtProd.Rows)
                {
                    var code = row["產品代碼"]?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(code)) continue;

                    if (!productMap.ContainsKey(code))
                    {
                        productMap[code] = new productsClass
                        {
                            產品代碼 = code,
                            產品名稱 = row["產品名稱"]?.ToString() ?? "",
                            廠牌 = row["廠牌"]?.ToString() ?? "",
                            售價 = row["售價"]?.ToString() ?? "",
                            狀態 = row["狀態"]?.ToString() ?? "",
                            child_components = new List<product_componentsClass>(),
                            parent_products = new List<product_componentsClass>()
                        };
                    }
                }

                // 4.2 關聯快取：parent -> [children]、child -> [parents]
                var childrenByParent = new Dictionary<string, List<(string child, string qty)>>(StringComparer.OrdinalIgnoreCase);
                var parentsByChild = new Dictionary<string, List<(string parent, string qty)>>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow row in dtComp.Rows)
                {
                    var p = row["parent_code"]?.ToString() ?? "";
                    var c = row["child_code"]?.ToString() ?? "";
                    var q = row["數量"]?.ToString() ?? "";

                    if (string.IsNullOrWhiteSpace(p) || string.IsNullOrWhiteSpace(c)) continue;

                    if (!childrenByParent.TryGetValue(p, out var cl))
                    {
                        cl = new List<(string child, string qty)>();
                        childrenByParent[p] = cl;
                    }
                    cl.Add((c, q));

                    if (!parentsByChild.TryGetValue(c, out var pl))
                    {
                        pl = new List<(string parent, string qty)>();
                        parentsByChild[c] = pl;
                    }
                    pl.Add((p, q));
                }

                // 5) 組樹工具（記憶體遞迴，不再打 DB）
                List<product_componentsClass> BuildChildTree(string parentCode, int level, HashSet<string> visited)
                {
                    var list = new List<product_componentsClass>();
                    if (level > MaxLevel) return list;
                    if (!visited.Add($"C:{parentCode}")) return list; // C: 標記子向下

                    if (!childrenByParent.TryGetValue(parentCode, out var childs)) return list;

                    foreach (var (childCode, qty) in childs)
                    {
                        // 子節點資訊取自 child 的 productMap
                        if (!productMap.TryGetValue(childCode, out var childProd))
                        {
                            // 子項沒有對應產品主檔，仍可輸出代碼與數量（其餘留空）
                            var orphan = new product_componentsClass
                            {
                                parent_code = parentCode,
                                child_code = childCode,
                                數量 = qty,
                                產品名稱 = "",
                                廠牌 = "",
                                售價 = "",
                                狀態 = "",
                                child_components = BuildChildTree(childCode, level + 1, visited)
                            };
                            list.Add(orphan);
                            continue;
                        }

                        var node = new product_componentsClass
                        {
                            parent_code = parentCode,
                            child_code = childCode,
                            數量 = qty,
                            產品名稱 = childProd.產品名稱,
                            廠牌 = childProd.廠牌,
                            售價 = childProd.售價,
                            狀態 = childProd.狀態,
                            child_components = BuildChildTree(childCode, level + 1, visited)
                        };
                        list.Add(node);
                    }
                    return list;
                }

                List<product_componentsClass> BuildParentTreeNatural(string childCode, int level, HashSet<string> visited)
                {
                    var list = new List<product_componentsClass>();
                    if (level > MaxLevel) return list;
                    if (!visited.Add($"P:{childCode}")) return list; // P: 標記父向上

                    if (!parentsByChild.TryGetValue(childCode, out var parents)) return list;

                    foreach (var (parentCode, qty) in parents)
                    {
                        // 父節點資訊取自 parent 的 productMap
                        productMap.TryGetValue(parentCode, out var parentProd);

                        var parentNode = new product_componentsClass
                        {
                            parent_code = parentCode,
                            child_code = childCode,
                            數量 = qty,
                            產品名稱 = parentProd?.產品名稱 ?? "",
                            廠牌 = parentProd?.廠牌 ?? "",
                            售價 = parentProd?.售價 ?? "",
                            狀態 = parentProd?.狀態 ?? "",
                            child_components = new List<product_componentsClass>()
                        };

                        // 遞迴向上找更上層父
                        var uppers = BuildParentTreeNatural(parentCode, level + 1, visited);

                        if (uppers.Count > 0)
                        {
                            // 把目前父節點掛到最上層鏈的最底端
                            var chain = uppers;
                            while (chain.Any() && chain.First().child_components.Any())
                                chain = chain.First().child_components;

                            chain.First().child_components.Add(parentNode);
                            list.AddRange(uppers);
                        }
                        else
                        {
                            // 沒有更上層，這層就是最上層
                            list.Add(parentNode);
                        }
                    }
                    return list;
                }

                // 6) 逐一輸入代碼組裝結果
                var results = new List<productsClass>();
                foreach (var code in codesInput)
                {
                    if (!productMap.TryGetValue(code, out var prod)) continue; // 無對應產品 → 略過

                    // 重新建立 visited（每棵樹各自獨立）
                    var visitedChild = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var visitedParent = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    var childTree = BuildChildTree(code, 1, visitedChild);
                    var parentTree = BuildParentTreeNatural(code, 1, visitedParent);

                    // 產出一筆 productsClass（不影響快取內物件）
                    var item = new productsClass
                    {
                        產品代碼 = prod.產品代碼,
                        產品名稱 = prod.產品名稱,
                        廠牌 = prod.廠牌,
                        售價 = prod.售價,
                        狀態 = prod.狀態,
                        child_components = childTree,
                        parent_products = parentTree,
                        child_count = childTree.Count,
                        parent_count = parentTree.Count
                    };

                    results.Add(item);
                }

                returnData.Data = results;
                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 取得所有產品結構與基本資訊（高效全表版本）
        /// </summary>
        /// <remarks>
        /// 從 <c>products</c> 與 <c>product_components</c> 資料表一次載入全部資料，  
        /// 回傳每個產品的基本屬性（產品代碼、產品名稱、廠牌、售價、狀態、產品類型、產品分類）、  
        /// 直接子項清單（<c>child_components</c>）與直接父項清單（<c>parent_products</c>），  
        /// 關聯清單中包含 <c>parent_code</c>、<c>child_code</c> 與 <c>數量</c>，  
        /// 並提供 <c>child_count</c> 與 <c>parent_count</c> 數量欄位，方便前端快速顯示。  
        /// 此方法僅進行兩次 SQL 查詢，適合網頁載入時快速初始化全產品結構。
        ///
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "VM端",
        ///     "ValueAry": []
        /// }
        /// </code>
        ///
        /// **欄位說明：**
        /// - ServerName：伺服器名稱（對應設定檔）
        /// - ServerType：伺服器類型（如「VM端」、「API」、「網頁」）
        /// - ValueAry：無需參數（保留擴充用途）
        ///
        /// **回傳範例：**
        /// <code>
        /// {
        ///     "Code": 200,
        ///     "Result": "OK",
        ///     "Data": [
        ///         {
        ///             "產品代碼": "P001",
        ///             "產品名稱": "主產品一號",
        ///             "廠牌": "品牌A",
        ///             "售價": "100",
        ///             "狀態": "active",
        ///             "產品類型": "bundle",
        ///             "產品分類": "醫療器材",
        ///             "child_count": 2,
        ///             "parent_count": 0,
        ///             "child_components": [
        ///                 { "parent_code": "P001", "child_code": "C001", "數量": "3" },
        ///                 { "parent_code": "P001", "child_code": "C002", "數量": "5" }
        ///             ],
        ///             "parent_products": []
        ///         }
        ///     ],
        ///     "TimeTaken": "0.015 秒"
        /// }
        /// </code>
        /// </remarks>
        [Route("get_product_structure")]
        [HttpPost]
        public string get_product_structure([FromBody] returnData returnData)
        {
            var myTimerBasic = new MyTimerBasic();
            returnData.Method = "get_product_structure";

            try
            {
                // 取得資料庫設定
                var sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                var sys_ServerSetting = sys_serverSettingClasses.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (sys_ServerSetting == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string productsTable = new enum_products().GetEnumDescription();
                string componentsTable = new enum_product_components().GetEnumDescription();

                var sqlProducts = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, productsTable,
                    sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);

                var sqlComponents = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, componentsTable,
                    sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);

                // 一次撈全部產品（含基本屬性 + 產品類型 + 產品分類）
                var dtProd = sqlProducts.WtrteCommandAndExecuteReader($@"
            SELECT 產品代碼, 產品名稱, 廠牌, 售價, 狀態, 產品類型, 產品分類, 規格, 條碼清單, 備註
            FROM {sqlProducts.Database}.{productsTable}");

                // 一次撈全部關聯（含數量）
                var dtComp = sqlComponents.WtrteCommandAndExecuteReader($@"
            SELECT parent_code, child_code, 數量
            FROM {sqlComponents.Database}.{componentsTable}
            WHERE parent_code <> child_code");

                // 關聯映射
                var childrenByParent = dtComp.AsEnumerable()
                    .GroupBy(r => r["parent_code"].ToString())
                    .ToDictionary(g => g.Key, g => g.Select(r => new product_componentsClass
                    {
                        parent_code = r["parent_code"].ToString(),
                        child_code = r["child_code"].ToString(),
                        數量 = r["數量"].ToString(),
                        child_components = new List<product_componentsClass>()
                    }).ToList());

                var parentsByChild = dtComp.AsEnumerable()
                    .GroupBy(r => r["child_code"].ToString())
                    .ToDictionary(g => g.Key, g => g.Select(r => new product_componentsClass
                    {
                        parent_code = r["parent_code"].ToString(),
                        child_code = r["child_code"].ToString(),
                        數量 = r["數量"].ToString(),
                        child_components = new List<product_componentsClass>()
                    }).ToList());

                // 組 List<productsClass>
                var results = new List<productsClass>();
                foreach (DataRow row in dtProd.Rows)
                {
                    var code = row["產品代碼"].ToString();
                    var product = new productsClass
                    {
                        產品代碼 = code,
                        產品名稱 = row["產品名稱"].ToString(),
                        廠牌 = row["廠牌"].ToString(),
                        售價 = row["售價"].ToString(),
                        狀態 = row["狀態"].ToString(),
                        產品類型 = row["產品類型"].ToString(),
                        產品分類 = row["產品分類"].ToString(),
                        規格 = row["規格"].ToString(),
                        條碼清單 = row["條碼清單"].ToString(),
                        備註 = row["備註"].ToString(),
                        child_components = childrenByParent.ContainsKey(code) ? childrenByParent[code] : new List<product_componentsClass>(),
                        parent_products = parentsByChild.ContainsKey(code) ? parentsByChild[code] : new List<product_componentsClass>(),
                        child_count = childrenByParent.ContainsKey(code) ? childrenByParent[code].Count : 0,
                        parent_count = parentsByChild.ContainsKey(code) ? parentsByChild[code].Count : 0
                    };
                    results.Add(product);
                }

                returnData.Data = results;
                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

    


        /// <summary>
        /// 依產品代碼取得產品基本資訊（不包含父項與子項遞迴資料）
        /// </summary>
        /// <remarks>
        /// 從指定資料表 <paramref name="prodTable"/> 中，查詢對應的產品代碼，
        /// 回傳產品的基本欄位資料（產品代碼、產品名稱、廠牌、售價、狀態）。
        /// 
        /// 此方法僅回傳單筆產品基本資訊，不會遞迴取得父項或子項結構，
        /// 父子樹結構請搭配 <c>GetChildTree</c> 及 <c>GetParentTree</c> / <c>GetParentTreeNaturalOrder</c> 使用。
        /// </remarks>
        /// <param name="code">要查詢的產品代碼</param>
        /// <param name="sqlProd">資料庫存取物件，需已連線至產品資料庫</param>
        /// <param name="prodTable">產品資料表名稱</param>
        /// <returns>
        /// 若找到對應產品，回傳 <see cref="productsClass"/> 物件（包含基本欄位）；
        /// 若查無資料，則回傳 <c>null</c>。
        /// </returns>
        /// <example>
        /// **呼叫範例：**
        /// <code>
        /// var sqlProducts = new SQLControl(server, dbName, tableName, user, password, port, sslMode);
        /// var productInfo = GetProductInfo("C003", sqlProducts, "products");
        /// if (productInfo != null)
        /// {
        ///     Console.WriteLine($"產品名稱：{productInfo.產品名稱}");
        /// }
        /// </code>
        /// </example>
        private productsClass GetProductInfo(string code, SQLControl sqlProd, string prodTable)
        {
            string sql = $@"SELECT 產品代碼, 產品名稱, 廠牌, 售價, 狀態 , 規格 , 條碼清單, 備註 
                    FROM {sqlProd.Database}.{prodTable}
                    WHERE 產品代碼 = '{code.Replace("'", "''")}'";
            var dt = sqlProd.WtrteCommandAndExecuteReader(sql);
            if (dt.Rows.Count == 0) return null;

            return new productsClass
            {
                產品代碼 = dt.Rows[0]["產品代碼"].ToString(),
                產品名稱 = dt.Rows[0]["產品名稱"].ToString(),
                廠牌 = dt.Rows[0]["廠牌"].ToString(),
                售價 = dt.Rows[0]["售價"].ToString(),
                狀態 = dt.Rows[0]["狀態"].ToString(),
                規格 = dt.Rows[0]["規格"].ToString(),
                條碼清單 = dt.Rows[0]["條碼清單"].ToString(),
                備註 = dt.Rows[0]["備註"].ToString(),
                child_components = new List<product_componentsClass>(),
                parent_products = new List<product_componentsClass>()
            };
        }
        /// <summary>
        /// 遞迴取得指定父項產品的所有子項結構樹
        /// </summary>
        /// <remarks>
        /// 從組件關聯表 <paramref name="compTable"/> 與產品資料表 <paramref name="prodTable"/>  
        /// 查詢指定父項產品 (<paramref name="parentCode"/>) 的直接子項，  
        /// 並遞迴展開所有層級的子項，直到達到 <paramref name="maxLevel"/> 層或已拜訪過的節點。  
        /// 
        /// 此方法會避免循環參考（使用 <paramref name="visited"/> 記錄已拜訪節點）。
        /// </remarks>
        /// <param name="parentCode">父項產品代碼</param>
        /// <param name="sqlComp">SQLControl 物件，已連線至產品組件關聯資料表所在的資料庫</param>
        /// <param name="compTable">產品組件關聯資料表名稱</param>
        /// <param name="sqlProd">SQLControl 物件，已連線至產品資料表所在的資料庫</param>
        /// <param name="prodTable">產品資料表名稱</param>
        /// <param name="level">目前遞迴層級（從 1 開始）</param>
        /// <param name="maxLevel">最大遞迴層級限制</param>
        /// <param name="visited">已拜訪過的產品代碼集合，用於避免循環參考</param>
        /// <returns>
        /// 以 <see cref="product_componentsClass"/> 清單形式回傳該父項的所有直接子項，  
        /// 每個子項物件的 <c>child_components</c> 屬性包含它的下層子項資料。  
        /// 若無子項或已達遞迴限制，回傳空清單。
        /// </returns>
        /// <example>
        /// **範例：取得代碼 C003 的完整子項結構**
        /// <code>
        /// var visited = new HashSet&lt;string&gt;();
        /// var childTree = GetChildTree(
        ///     "C003", sqlComponents, "product_components", 
        ///     sqlProducts, "products", 1, 5, visited
        /// );
        /// foreach (var child in childTree)
        /// {
        ///     Console.WriteLine($"{child.child_code} - {child.產品名稱}");
        /// }
        /// </code>
        /// </example>
        private List<product_componentsClass> GetChildTree(string parentCode, SQLControl sqlComp, string compTable, SQLControl sqlProd, string prodTable, int level, int maxLevel, HashSet<string> visited)
        {
            if (level > maxLevel || visited.Contains(parentCode)) return new List<product_componentsClass>();
            visited.Add(parentCode);

            string sql = $@"
        SELECT c.*, p.產品名稱, p.廠牌, p.售價, p.狀態, p.規格, p.備註, p.條碼清單
        FROM {sqlComp.Database}.{compTable} AS c
        LEFT JOIN {sqlProd.Database}.{prodTable} AS p ON c.child_code = p.產品代碼
        WHERE c.parent_code = '{parentCode.Replace("'", "''")}'
          AND c.child_code <> c.parent_code";
            var dt = sqlComp.WtrteCommandAndExecuteReader(sql);

            var list = new List<product_componentsClass>();
            foreach (DataRow row in dt.Rows)
            {
                var node = new product_componentsClass
                {
                    parent_code = row["parent_code"].ToString(),
                    child_code = row["child_code"].ToString(),
                    產品名稱 = row["產品名稱"].ToString(),
                    廠牌 = row["廠牌"].ToString(),
                    售價 = row["售價"].ToString(),
                    狀態 = row["狀態"].ToString(),
                    數量 = row["數量"].ToString(),
                    規格 = row["規格"].ToString(),
                    備註 = row["備註"].ToString(),
                    child_components = GetChildTree(row["child_code"].ToString(), sqlComp, compTable, sqlProd, prodTable, level + 1, maxLevel, visited)
                };
                list.Add(node);
            }
            return list;
        }
        /// <summary>
        /// 遞迴取得指定子項產品的父項結構樹
        /// </summary>
        /// <remarks>
        /// 從組件關聯表 <paramref name="compTable"/> 與產品資料表 <paramref name="prodTable"/>  
        /// 查詢指定子項產品 (<paramref name="childCode"/>) 的直接父項，  
        /// 並遞迴向上查詢，直到達到 <paramref name="maxLevel"/> 層或已拜訪過的節點。  
        /// 
        /// 回傳的父項結構為由當前子項向上展開的樹狀資料，  
        /// 與 <c>GetParentTreeNaturalOrder</c> 不同的是，順序不保證由最上層開始。
        /// </remarks>
        /// <param name="childCode">子項產品代碼</param>
        /// <param name="sqlComp">SQLControl 物件，已連線至產品組件關聯資料表所在的資料庫</param>
        /// <param name="compTable">產品組件關聯資料表名稱</param>
        /// <param name="sqlProd">SQLControl 物件，已連線至產品資料表所在的資料庫</param>
        /// <param name="prodTable">產品資料表名稱</param>
        /// <param name="level">目前遞迴層級（從 1 開始）</param>
        /// <param name="maxLevel">最大遞迴層級限制</param>
        /// <param name="visited">已拜訪過的產品代碼集合，用於避免循環參考</param>
        /// <returns>
        /// 以 <see cref="product_componentsClass"/> 清單形式回傳該子項的所有直接父項，  
        /// 每個父項物件的 <c>child_components</c> 屬性包含它的下層（即當前子項）。  
        /// 若無父項或已達遞迴限制，回傳空清單。
        /// </returns>
        /// <example>
        /// **範例：取得代碼 C006 的完整父項結構**
        /// <code>
        /// var visited = new HashSet&lt;string&gt;();
        /// var parentTree = GetParentTree(
        ///     "C006", sqlComponents, "product_components",
        ///     sqlProducts, "products", 1, 5, visited
        /// );
        /// foreach (var parent in parentTree)
        /// {
        ///     Console.WriteLine($"{parent.parent_code} - {parent.產品名稱}");
        /// }
        /// </code>
        /// </example>
        private List<product_componentsClass> GetParentTree(string childCode, SQLControl sqlComp, string compTable, SQLControl sqlProd, string prodTable, int level, int maxLevel, HashSet<string> visited)
        {
            if (level > maxLevel || visited.Contains(childCode)) return new List<product_componentsClass>();
            visited.Add(childCode);

            // 找直接父項
            string sql = $@"
        SELECT c.parent_code, c.child_code, c.數量, p.產品名稱, p.廠牌, p.售價, p.狀態, p.規格, p.條碼清單, p.備註
        FROM {sqlComp.Database}.{compTable} AS c
        LEFT JOIN {sqlProd.Database}.{prodTable} AS p ON c.parent_code = p.產品代碼
        WHERE c.child_code = '{childCode.Replace("'", "''")}'
          AND c.parent_code <> c.child_code";

            var dt = sqlComp.WtrteCommandAndExecuteReader(sql);
            var list = new List<product_componentsClass>();

            foreach (DataRow row in dt.Rows)
            {
                var parentNode = new product_componentsClass
                {
                    parent_code = row["parent_code"].ToString(),
                    child_code = row["child_code"].ToString(),
                    產品名稱 = row["產品名稱"].ToString(),
                    廠牌 = row["廠牌"].ToString(),
                    售價 = row["售價"].ToString(),
                    狀態 = row["狀態"].ToString(),
                    規格 = row["規格"].ToString(),
                    數量 = row["數量"].ToString(),
                    備註 = row["備註"].ToString(),
                };

                // 遞迴找更上層父項
                var upperParents = GetParentTree(parentNode.parent_code, sqlComp, compTable, sqlProd, prodTable, level + 1, maxLevel, visited);

                // 如果有更上層父項，把目前節點放進它們的 child_components
                if (upperParents.Count > 0)
                {
                    foreach (var upper in upperParents)
                    {
                        upper.child_components.Add(parentNode);
                    }
                    list.AddRange(upperParents);
                }
                else
                {
                    // 沒有更上層，就直接加這一層
                    list.Add(parentNode);
                }
            }
            return list;
        }
        /// <summary>
        /// 取得父項樹（自然順序：最上層 → 最下層）
        /// </summary>
        /// /// <summary>
        /// 遞迴取得指定子項產品的父項結構樹（自然順序：最上層 → 最下層）
        /// </summary>
        /// <remarks>
        /// 從組件關聯表 <paramref name="compTable"/> 與產品資料表 <paramref name="prodTable"/>  
        /// 查詢指定子項產品 (<paramref name="childCode"/>) 的直接父項，並遞迴向上查詢所有更上層父項，  
        /// 最後依照層級由最上層父項開始，往下依序展開子項，形成自然的上下層順序。  
        /// 
        /// 與 <c>GetParentTree</c> 不同，該方法會先找到最高層父項，  
        /// 再將中間層級依序接回，確保回傳結果是「最上層 → 最下層」的順序。  
        /// 方法會避免循環參考（使用 <paramref name="visited"/> 記錄已拜訪節點）。
        /// </remarks>
        /// <param name="childCode">子項產品代碼</param>
        /// <param name="sqlComp">SQLControl 物件，已連線至產品組件關聯資料表所在的資料庫</param>
        /// <param name="compTable">產品組件關聯資料表名稱</param>
        /// <param name="sqlProd">SQLControl 物件，已連線至產品資料表所在的資料庫</param>
        /// <param name="prodTable">產品資料表名稱</param>
        /// <param name="level">目前遞迴層級（從 1 開始）</param>
        /// <param name="maxLevel">最大遞迴層級限制</param>
        /// <param name="visited">已拜訪過的產品代碼集合，用於避免循環參考</param>
        /// <returns>
        /// 以 <see cref="product_componentsClass"/> 清單形式回傳該子項的完整父項結構，  
        /// 其層級順序由最上層開始，每個父項的 <c>child_components</c> 屬性包含下一層父項或當前子項。  
        /// 若無父項或已達遞迴限制，回傳空清單。
        /// </returns>
        /// <example>
        /// **範例：取得代碼 C006 的完整父項結構（由最上層到當前子項）**
        /// <code>
        /// var visited = new HashSet&lt;string&gt;();
        /// var parentTreeNatural = GetParentTreeNaturalOrder(
        ///     "C006", sqlComponents, "product_components",
        ///     sqlProducts, "products", 1, 5, visited
        /// );
        /// 
        /// // 以巢狀方式輸出
        /// void PrintTree(List&lt;product_componentsClass&gt; tree, int indent = 0)
        /// {
        ///     foreach (var node in tree)
        ///     {
        ///         Console.WriteLine($"{new string(' ', indent * 2)}{node.parent_code} - {node.產品名稱}");
        ///         PrintTree(node.child_components, indent + 1);
        ///     }
        /// }
        /// 
        /// PrintTree(parentTreeNatural);
        /// </code>
        /// </example>
        private List<product_componentsClass> GetParentTreeNaturalOrder(string childCode, SQLControl sqlComp, string compTable, SQLControl sqlProd, string prodTable, int level, int maxLevel, HashSet<string> visited)
        {
            if (level > maxLevel || visited.Contains(childCode))
                return new List<product_componentsClass>();

            visited.Add(childCode);

            // 找直接父項
            string sql = $@"
        SELECT c.parent_code, c.child_code, c.數量, 
               p.產品名稱, p.廠牌, p.售價, p.狀態, p.規格, p.條碼清單, p.備註
        FROM {sqlComp.Database}.{compTable} AS c
        LEFT JOIN {sqlProd.Database}.{prodTable} AS p 
               ON c.parent_code = p.產品代碼
        WHERE c.child_code = '{childCode.Replace("'", "''")}'
          AND c.parent_code <> c.child_code";

            var dt = sqlComp.WtrteCommandAndExecuteReader(sql);
            var list = new List<product_componentsClass>();

            foreach (DataRow row in dt.Rows)
            {
                var parentNode = new product_componentsClass
                {
                    parent_code = row["parent_code"].ToString(),
                    child_code = row["child_code"].ToString(),
                    產品名稱 = row["產品名稱"].ToString(),
                    廠牌 = row["廠牌"].ToString(),
                    售價 = row["售價"].ToString(),
                    狀態 = row["狀態"].ToString(),
                    數量 = row["數量"].ToString(),
                    備註 = row["備註"].ToString(),
                    child_components = new List<product_componentsClass>()
                };

                // 先遞迴找到更上層
                var upperParents = GetParentTreeNaturalOrder(
                    parentNode.parent_code, sqlComp, compTable, sqlProd, prodTable, level + 1, maxLevel, visited);

                if (upperParents.Count > 0)
                {
                    // 把目前的父項接到最上層父項的 child_components
                    var deepest = upperParents;
                    while (deepest.Any() && deepest.First().child_components.Any())
                        deepest = deepest.First().child_components;

                    deepest.First().child_components.Add(parentNode);

                    list.AddRange(upperParents);
                }
                else
                {
                    // 沒有更上層父項，這就是最上層
                    list.Add(parentNode);
                }
            }

            return list;
        }
        /// <summary>
        /// 遞迴取得指定子項產品的父項結構樹（自然順序：最上層 → 最下層）
        /// </summary>
        /// <remarks>
        /// 從組件關聯表 <paramref name="compTable"/> 與產品資料表 <paramref name="prodTable"/>  
        /// 查詢指定子項產品 (<paramref name="childCode"/>) 的直接父項，並遞迴向上查詢所有更上層父項，  
        /// 最後依照層級由最上層父項開始，往下依序展開子項，形成自然的上下層順序。  
        /// 
        /// 與 <c>GetParentTree</c> 不同，該方法會先找到最高層父項，  
        /// 再將中間層級依序接回，確保回傳結果是「最上層 → 最下層」的順序。  
        /// 方法會避免循環參考（使用 <paramref name="visited"/> 記錄已拜訪節點）。
        /// </remarks>
        /// <param name="childCode">子項產品代碼</param>
        /// <param name="sqlComp">SQLControl 物件，已連線至產品組件關聯資料表所在的資料庫</param>
        /// <param name="compTable">產品組件關聯資料表名稱</param>
        /// <param name="sqlProd">SQLControl 物件，已連線至產品資料表所在的資料庫</param>
        /// <param name="prodTable">產品資料表名稱</param>
        /// <param name="level">目前遞迴層級（從 1 開始）</param>
        /// <param name="maxLevel">最大遞迴層級限制</param>
        /// <param name="visited">已拜訪過的產品代碼集合，用於避免循環參考</param>
        /// <returns>
        /// 以 <see cref="product_componentsClass"/> 清單形式回傳該子項的完整父項結構，  
        /// 其層級順序由最上層開始，每個父項的 <c>child_components</c> 屬性包含下一層父項或當前子項。  
        /// 若無父項或已達遞迴限制，回傳空清單。
        /// </returns>
        /// <example>
        /// **範例：取得代碼 C006 的完整父項結構（由最上層到當前子項）**
        /// <code>
        /// var visited = new HashSet&lt;string&gt;();
        /// var parentTreeNatural = GetParentTreeNaturalOrder(
        ///     "C006", sqlComponents, "product_components",
        ///     sqlProducts, "products", 1, 5, visited
        /// );
        /// 
        /// // 以巢狀方式輸出
        /// void PrintTree(List&lt;product_componentsClass&gt; tree, int indent = 0)
        /// {
        ///     foreach (var node in tree)
        ///     {
        ///         Console.WriteLine($"{new string(' ', indent * 2)}{node.parent_code} - {node.產品名稱}");
        ///         PrintTree(node.child_components, indent + 1);
        ///     }
        /// }
        /// 
        /// PrintTree(parentTreeNatural);
        /// </code>
        /// </example>
        private int CountComponents(List<product_componentsClass> list)
        {
            if (list == null || list.Count == 0) return 0;
            int count = list.Count;
            foreach (var item in list)
            {
                count += CountComponents(item.child_components);
            }
            return count;
        }







        private List<Table> CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            string Server = sys_serverSettingClass.Server;
            string DB = sys_serverSettingClass.DBName;
            string UserName = sys_serverSettingClass.User;
            string Password = sys_serverSettingClass.Password;
            uint Port = (uint)sys_serverSettingClass.Port.StringToInt32();
            List<Table> tables = new List<Table>();
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_products()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_product_components()));
            return tables;
        }
    }
}
