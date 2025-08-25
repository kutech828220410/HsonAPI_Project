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
    public class suppliers : ControllerBase
    {
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

        /// <summary>
        /// 初始化 dbvm.suppliers 資料表
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 會檢查並建立 `suppliers` 資料表  
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
        /// 批次新增或更新供應商資料（存在則更新，不存在則新增）
        /// </summary>
        /// <remarks>
        /// 此 API 用於同步供應商主檔，會依據 <c>GUID</c> 與 <c>suppliername</c> 判斷資料狀態：  
        /// - 若資料已存在於資料庫 → 更新欄位並修改 <c>updateddate</c>。  
        /// - 若資料不存在於資料庫 → 新增新的供應商並建立 <c>GUID</c>、<c>createddate</c>。  
        ///
        /// <b>邏輯流程：</b>
        /// 1. 驗證 Data 欄位不可為空。  
        /// 2. 驗證每筆資料的 <c>suppliername</c> 不可為空。  
        /// 3. 批次去除重複的 <c>GUID</c>（若無 GUID，則以 <c>suppliername</c> 為基準），保留最後一筆資料。  
        /// 4. 查詢資料庫中既有的供應商紀錄，分類為：  
        ///    - 新增清單（傳入有、資料庫沒有）  
        ///    - 更新清單（傳入有、資料庫也有）  
        /// 5. 依分類結果批次執行新增或更新。  
        ///
        /// <b>注意事項：</b>
        /// - <c>suppliername</c> 為必填欄位。  
        /// - 系統會自動維護 <c>GUID</c>、<c>createddate</c> 與 <c>updateddate</c>。  
        /// - 缺少必要欄位的資料會被跳過並記錄訊息。  
        ///
        /// <b>JSON 請求範例：</b>
        ///  ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": 
        ///     [
        ///       { "GUID": "550E8400-E29B-41D4-A716-446655440000", "suppliername": "供應商A", "contactperson": "王小明", "phone": "0912345678" },
        ///       { "suppliername": "供應商B", "contactperson": "李小華", "phone": "0987654321" }
        ///     ]
        /// }
        /// ```
        ///
        /// <b>JSON 回應範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "新增 1 筆，更新 1 筆",
        ///   "Data": 
        ///     [
        ///       { "GUID": "550E8400-E29B-41D4-A716-446655440000", "suppliername": "供應商A", "contactperson": "王小明", "phone": "0912345678", "createddate": "2025-08-25 14:00:00", "updateddate": "2025-08-25 14:05:00" },
        ///       { "GUID": "660F9500-F39C-52E5-B827-557766551111", "suppliername": "供應商B", "contactperson": "李小華", "phone": "0987654321", "createddate": "2025-08-25 14:05:00", "updateddate": "2025-08-25 14:05:00" }
        ///     ],
        ///   "TimeTaken": "0.132 秒"
        /// }
        /// ```
        /// </remarks>
        [Route("add_or_update_suppliers")]
        [HttpPost]
        public string add_or_update_suppliers([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "add_or_update_suppliers";
            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 嘗試將 Data 轉成多筆資料
                List<supplierClass> supplierClasses = returnData.Data.ObjToClass<List<supplierClass>>();



                if (supplierClasses == null || supplierClasses.Count == 0)
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
                var conf = _sys_serverSettingClasses[0];
                SQLControl sQLControl_suppliers = conf.GetSQLControl(new enum_suppliers().GetEnumDescription());

                List<string> processResults = new List<string>();
                List<supplierClass> supplierClasses_add = new List<supplierClass>();
                List<supplierClass> supplierClasses_update = new List<supplierClass>();

                foreach (var supplier in supplierClasses)
                {
                    if (supplier == null)
                    {
                        processResults.Add("(supplier == null)");
                        continue;
                    }

                    // 檢查是否存在
                    List<object[]> existRows = sQLControl_suppliers.GetRowsByDefult(null, (int)enum_suppliers.GUID, supplier.GUID);
                    if (existRows.Count > 0)
                    {
                        supplierClass supplierClass = existRows[0].SQLToClass<supplierClass, enum_suppliers>();
                        supplier.GUID = supplierClass.GUID;
                        supplier.createddate = supplierClass.createddate;
                        supplier.updateddate = DateTime.Now.ToDateTimeString();
                        supplierClasses_update.Add(supplier);
                    }
                    else
                    {
                        // 不存在 → 新增
                        supplier.GUID = Guid.NewGuid().ToString();
                        supplier.createddate = DateTime.Now.ToDateTimeString();
                        supplier.updateddate = DateTime.Now.ToDateTimeString();
                        supplierClasses_add.Add(supplier);
                    }

                }
                if (supplierClasses_update.Count > 0) sQLControl_suppliers.UpdateByDefulteExtra(null, supplierClasses_update.ClassToSQL<supplierClass, enum_suppliers>());
                if (supplierClasses_add.Count > 0) sQLControl_suppliers.AddRows(null, supplierClasses_add.ClassToSQL<supplierClass, enum_suppliers>());

                processResults.Add($"新增<{supplierClasses_add.Count}>筆資料,修改<{supplierClasses_update.Count}>筆資料");
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = supplierClasses;
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
        /// 取得供應商清單（支援篩選、關鍵字、分頁、排序）
        /// </summary>
        /// <remarks>
        /// 此 API 用於查詢供應商主檔，支援依據供應商類型、關鍵字搜尋、分頁與排序，回傳符合條件的供應商列表。
        ///
        /// <b>邏輯流程：</b>
        /// 1. 驗證伺服器連線設定是否存在，否則回傳錯誤。  
        /// 2. 解析 <c>ValueAry</c> 參數，包括：  
        ///    - <c>suppliertype</c>：供應商類型（可空）。  
        ///    - <c>searchTerm</c>：搜尋關鍵字（模糊比對名稱/聯絡人/電話，可空）。  
        ///    - <c>page</c>：分頁頁碼，預設 1。  
        ///    - <c>pageSize</c>：每頁筆數，預設 50。  
        ///    - <c>sortBy</c>：排序欄位，預設 <c>updateddate</c>。  
        ///    - <c>sortOrder</c>：排序方向（asc/desc），預設 desc。  
        /// 3. 呼叫靜態函式 <c>GetSuppliers()</c> 查詢資料。  
        /// 4. 將查詢結果回傳至前端，包含資料清單、耗時等資訊。  
        ///
        /// <b>注意事項：</b>
        /// - 若未指定分頁參數，系統會自動套用預設值 page=1、pageSize=50。  
        /// - 排序欄位僅允許資料表中的合法欄位。  
        /// - 查詢結果為供應商清單，回傳格式為 JSON 陣列。  
        ///
        /// <b>JSON 請求範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "suppliertype=藥品",
        ///     "searchTerm=供應商A",
        ///     "page=1",
        ///     "pageSize=20",
        ///     "sortBy=updateddate",
        ///     "sortOrder=desc"
        ///   ]
        /// }
        /// ```
        ///
        /// <b>JSON 回應範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "獲取供應商列表成功",
        ///   "TimeTaken": "0.085 秒",
        ///   "Data": [
        ///     { "GUID": "550E8400-E29B-41D4-A716-446655440000", "SupplierName": "供應商A", "Contact": "王小明", "Phone": "0912345678", "SupplierType": "藥品", "CreatedDate": "2025-08-25 13:00:00", "UpdatedDate": "2025-08-25 13:05:00" },
        ///     { "GUID": "660F9500-F39C-52E5-B827-557766551111", "SupplierName": "供應商B", "Contact": "李小華", "Phone": "0987654321", "SupplierType": "設備", "CreatedDate": "2025-08-20 11:00:00", "UpdatedDate": "2025-08-22 09:30:00" }
        ///   ]
        /// }
        /// ```
        ///
        /// <b>JSON 錯誤回應範例：</b>
        ///```json
        /// {
        ///   "Code": -200,
        ///   "Result": "找無 Server 資料"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_suppliers")]
        public string get_suppliers([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_suppliers";

            try
            {
                var conf = GetConfOrFail(returnData, out string confErr);
                if (confErr != null)
                {
                    returnData.Code = -200;
                    returnData.Result = confErr;
                    return returnData.JsonSerializationt();
                }

                // 解析 ValueAry
                string suppliertype = GetVal(returnData.ValueAry, "suppliertype") ?? "";
                string searchTerm = GetVal(returnData.ValueAry, "searchTerm") ?? "";            
                int page = (GetVal(returnData.ValueAry, "page") ?? "1").StringToInt32();
                int pageSize = (GetVal(returnData.ValueAry, "pageSize") ?? "50").StringToInt32();
                string sortBy = GetVal(returnData.ValueAry, "sortBy") ?? "updateddate";
                string sortOrder = GetVal(returnData.ValueAry, "sortOrder") ?? "desc";

                // 呼叫靜態函式
                var list = GetSuppliers(
                    suppliertype, searchTerm, page, pageSize, sortBy, sortOrder, conf
                );

                returnData.Code = 200;
                returnData.Result = "獲取供應商列表成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = list;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }

        public static List<supplierClass> GetSuppliers(
         string suppliertype = "",
         string searchTerm = "",
         int page = 1,
         int pageSize = 50,
         string sortBy = "updateddate",
         string sortOrder = "desc",
         sys_serverSettingClass conf = null   // 可以選擇傳 DB 設定，或在內部抓取
     )
        {
            if (conf == null) throw new ArgumentNullException(nameof(conf));

            var sql = conf.GetSQLControl(new enum_suppliers().GetEnumDescription());

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            string where = " WHERE 1=1 ";
            if (!suppliertype.StringIsEmpty()) where += $" AND suppliertype = '{Esc(suppliertype)}' ";
            if (!searchTerm.StringIsEmpty())
            {
                string k = Esc(searchTerm);
                where += $" AND (suppliername LIKE '%{k}%' OR countryregion LIKE '%{k}%' OR contactperson LIKE '%{k}%' OR platform LIKE '%{k}%') ";
            }

            string orderBy = $"updateddate {sortOrder},suppliername ASC";
            int offset = (page - 1) * pageSize;

            // 3) 查詢 Projects
            string mainSql = $@"
                        SELECT *
                        FROM {conf.DBName}.{new enum_suppliers().GetEnumDescription()}
                        {where}
                        ORDER BY {orderBy}
                        LIMIT {offset}, {pageSize}";
            var dt = sql.WtrteCommandAndExecuteReader(mainSql);

            var list = dt.DataTableToRowList().SQLToClass<supplierClass, enum_suppliers>() ?? new List<supplierClass>();
            return list;
        }

        private static string GetVal(List<string> valueAry, string key, string defaultVal = null)
    => valueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1] ?? defaultVal;
        private static sys_serverSettingClass GetConfOrFail(returnData rd, out string error)
        {
            error = null;
            var servers = serverSetting.GetAllServerSetting();
            var conf = servers.myFind(rd.ServerName, rd.ServerType, "VM端");
            if (conf == null) error = "找不到 Server 設定";
            return conf;
        }
        private static string Now() => DateTime.Now.ToDateTimeString();
        private static string Esc(string s) => (s ?? "").Replace("'", "''");
        private List<Table> CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            string Server = sys_serverSettingClass.Server;
            string DB = sys_serverSettingClass.DBName;
            string UserName = sys_serverSettingClass.User;
            string Password = sys_serverSettingClass.Password;
            uint Port = (uint)sys_serverSettingClass.Port.StringToInt32();
            List<Table> tables = new List<Table>();
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_suppliers()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_supplierproducts()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_supplierproductpricehistory()));
            return tables;
        }
    }
}
