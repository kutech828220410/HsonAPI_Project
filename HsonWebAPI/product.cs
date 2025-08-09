using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using SQLUI;
using Basic;
using HsonAPILib; // 這裡引用你的 enum_products, productsClass
using System.Linq;
using System.Data;

namespace HsonAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class Products : ControllerBase
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
        /// 新增或更新產品資料
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 會新增一筆 `products` 資料表的產品資料，  
        /// 若產品代碼已存在，則改為更新該筆資料內容。  
        /// **以下為 JSON 範例**  
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "Data": {
        ///         "product_code": "P001",
        ///         "product_name": "感冒藥",
        ///         "product_type": "single",
        ///         "spec": "500mg",
        ///         "brand": "ABC",
        ///         "category": "藥品",
        ///         "unit": "盒",
        ///         "price": "100",
        ///         "status": "啟用",
        ///         "note": "一般感冒用藥",
        ///         "BARCODE": ["1234567890123", "9876543210987"],
        ///         "doc_name": "產品說明書",
        ///         "doc_url": "https://example.com/doc.pdf",
        ///         "doc_version": "v1.0",
        ///         "img_url": "https://example.com/image.jpg"
        ///     }
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">
        /// 共用傳遞資料結構  
        /// Data 欄位需傳入 <see cref="productsClass"/> 物件
        /// </param>
        /// <returns>
        /// 處理結果 JSON，包含新增或更新後的產品資料
        /// </returns>
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

                productsClass newProduct = returnData.Data.ObjToClass<productsClass>();
                if (newProduct == null || newProduct.產品代碼.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少必要欄位 product_code";
                    return returnData.JsonSerializationt();
                }

                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                List<sys_serverSettingClass> _sys_serverSettingClasses;

                _sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");


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

                    returnData.Result = $"產品代碼 {newProduct.產品代碼} 已存在，已更新資料";
                }
                else
                {
                    // 不存在 → 新增
                    newProduct.GUID = Guid.NewGuid().ToString();
                    newProduct.建立時間 = DateTime.Now.ToDateTimeString();
                    newProduct.更新時間 = DateTime.Now.ToDateTimeString();

                    object[] insertValue = newProduct.ClassToSQL<productsClass, enum_products>();
                    sQLControl_products.AddRow(null, insertValue);

                    returnData.Result = $"新增產品成功 (產品代碼: {newProduct.產品代碼})";
                }

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = newProduct;
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
        /// 批次新增或更新產品子項關聯資料
        /// </summary>
        /// <remarks>
        /// 此 API 用於為指定父產品代碼批次新增或更新子項產品的關聯資料。  
        /// 系統會先檢查父產品及子產品是否存在，若存在則更新數量與備註，否則新增關聯紀錄。  
        /// 
        /// **以下為 JSON 範例**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "Data": [
        ///         {
        ///             "parent_code": "P001",
        ///             "child_code": "C001",
        ///             "qty": "10",
        ///             "note": "第一批子項"
        ///         },
        ///         {
        ///             "parent_code": "P001",
        ///             "child_code": "C002",
        ///             "qty": "5",
        ///             "note": "第二批子項"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">
        /// 共用傳遞資料結構，`Data` 欄位需為 <see cref="List{product_componentsClass}"/> JSON 格式  
        /// 每筆資料的 <c>parent_code</c> 必須一致且不可為空
        /// </param>
        /// <returns>
        /// JSON 格式回應，包含處理結果與新增、更新筆數
        /// </returns>
        [Route("batch_add_or_update_product_components")]
        [HttpPost]
        public string batch_add_or_update_product_components([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "batch_add_or_update_product_components";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 反序列化為 product_componentsClass 列表
                List<product_componentsClass> componentsList = returnData.Data.ObjToListClass<product_componentsClass>();
                if (componentsList == null || componentsList.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 無有效資料";
                    return returnData.JsonSerializationt();
                }

                string parentCode = componentsList[0].parent_code;
                if (parentCode.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "parent_code 不可為空";
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

                // 檢查父產品是否存在
                string checkParentSql = $"SELECT 1 FROM {sys_ServerSetting.DBName}.{productsTable} WHERE 產品代碼 = '{parentCode.Replace("'", "''")}' LIMIT 1";
                if (sQLControl.WtrteCommandAndExecuteReader(checkParentSql).Rows.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到父產品代碼 {parentCode}";
                    return returnData.JsonSerializationt();
                }

                // 準備新增與更新的資料
                List<object[]> list_add = new List<object[]>();
                List<object[]> list_update = new List<object[]>();

                foreach (var child in componentsList)
                {
                    if (child.child_code.StringIsEmpty()) continue;

                    // 檢查子產品是否存在
                    string checkChildSql = $"SELECT 1 FROM {sys_ServerSetting.DBName}.{productsTable} WHERE 產品代碼 = '{child.child_code.Replace("'", "''")}' LIMIT 1";
                    if (sQLControl.WtrteCommandAndExecuteReader(checkChildSql).Rows.Count == 0)
                    {
                        continue; // 跳過不存在的子產品
                    }

                    // 檢查是否已有關聯
                    string selectSql = $@"
                SELECT * FROM {sys_ServerSetting.DBName}.{componentsTable}
                WHERE parent_code = '{parentCode.Replace("'", "''")}'
                AND child_code = '{child.child_code.Replace("'", "''")}'
            ";
                    var dt = sQLControl.WtrteCommandAndExecuteReader(selectSql);

                    if (dt.Rows.Count > 0)
                    {
                        // 更新
                        var row = dt.Rows[0].ItemArray;
                        row[(int)enum_product_components.數量] = child.數量;
                        row[(int)enum_product_components.備註] = child.備註;
                        row[(int)enum_product_components.更新時間] = DateTime.Now.ToDateTimeString();
                        list_update.Add(row);
                    }
                    else
                    {
                        // 新增
                        child.GUID = Guid.NewGuid().ToString();
                        child.建立時間 = DateTime.Now.ToDateTimeString();
                        child.更新時間 = DateTime.Now.ToDateTimeString();
                        list_add.Add(child.ClassToSQL<product_componentsClass, enum_product_components>());
                    }
                }

                // 寫入資料庫
                if (list_add.Count > 0) sQLControl.AddRows(null, list_add);
                if (list_update.Count > 0) sQLControl.UpdateByDefulteExtra(null, list_update);

                returnData.Code = 200;
                returnData.Result = $"處理完成，新增 {list_add.Count} 筆，更新 {list_update.Count} 筆";
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
        [Route("get_by_code")]
        [HttpPost]
        public string get_by_code([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "get_by_code";

            try
            {
                string product_code = returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith("product_code="))?
                    .Split('=')[1];
                if (string.IsNullOrWhiteSpace(product_code))
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 product_code 參數";
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

                var sqlProducts = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, productsTable, sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);
                var sqlComponents = new SQLControl(sys_ServerSetting.Server, sys_ServerSetting.DBName, componentsTable, sys_ServerSetting.User, sys_ServerSetting.Password, sys_ServerSetting.Port.StringToUInt32(), SSLMode);

                // 取得產品基本資料
                var product = GetProductInfo(product_code, sqlProducts, productsTable);
                if (product == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到產品代碼 {product_code}";
                    return returnData.JsonSerializationt();
                }

                var visited = new HashSet<string>();

                // 子項樹
                product.child_components = GetChildTree(product_code, sqlComponents, componentsTable, sqlProducts, productsTable, 1, 5, visited);

                // 父項樹
                visited.Clear();
                product.parent_products = GetParentTree(product_code, sqlComponents, componentsTable, sqlProducts, productsTable, 1, 5, visited);

                product.child_count = product.child_components.Count;
                product.parent_count = product.parent_products.Count;

                returnData.Data = product;
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


        private productsClass GetProductInfo(string code, SQLControl sqlProd, string prodTable)
        {
            string sql = $@"SELECT 產品代碼, 產品名稱, 廠牌, 售價, 狀態 
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
                child_components = new List<product_componentsClass>(),
                parent_products = new List<product_componentsClass>()
            };
        }

        private List<product_componentsClass> GetChildTree(string parentCode, SQLControl sqlComp, string compTable,
            SQLControl sqlProd, string prodTable, int level, int maxLevel, HashSet<string> visited)
        {
            if (level > maxLevel || visited.Contains(parentCode)) return new List<product_componentsClass>();
            visited.Add(parentCode);

            string sql = $@"
        SELECT c.*, p.產品名稱, p.廠牌, p.售價, p.狀態
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
                    child_components = GetChildTree(row["child_code"].ToString(), sqlComp, compTable, sqlProd, prodTable, level + 1, maxLevel, visited)
                };
                list.Add(node);
            }
            return list;
        }

        private List<product_componentsClass> GetParentTree(string childCode, SQLControl sqlComp, string compTable,
      SQLControl sqlProd, string prodTable, int level, int maxLevel, HashSet<string> visited)
        {
            if (level > maxLevel || visited.Contains(childCode)) return new List<product_componentsClass>();
            visited.Add(childCode);

            // 找直接父項
            string sql = $@"
        SELECT c.parent_code, c.child_code, c.數量, p.產品名稱, p.廠牌, p.售價, p.狀態
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
                    數量 = row["數量"].ToString(),
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

        /// <summary>遞迴計算元件總數</summary>
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
