using Basic;
using HsonAPILib; // 引用 enum_project_boms, enum_bom_items, enum_bom_documents
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace HsonWebAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class bom : ControllerBase
    {
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

        /// <summary>
        /// 初始化 project_boms / bom_items / bom_documents 資料表
        /// </summary>
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind("Main", "網頁", "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                List<Table> tables = new List<Table>();
                tables.Add(MethodClass.CheckCreatTable(conf, new enum_project_boms()));
                tables.Add(MethodClass.CheckCreatTable(conf, new enum_bom_items()));
                tables.Add(MethodClass.CheckCreatTable(conf, new enum_bom_documents()));

                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 BOM 資料表完成";
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }


        /// <summary>
        /// 取得 BOM 清單（支援篩選與分頁）
        /// </summary>
        /// <remarks>
        /// 此 API 用於查詢 BOM 主檔清單，支援以 <c>projectGuid</c>、<c>status</c>、<c>bomType</c> 等參數進行篩選，
        /// 並可透過 <c>page</c> 與 <c>pageSize</c> 控制分頁。
        /// 
        /// <b>使用方式：</b>
        /// - Method：POST  
        /// - Route：<c>/api/bom/get_bom_list</c>  
        /// - Content-Type：application/json  
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "projectGuid=660F9500-F39C-52E5-B827-557766551111",
        ///     "status=草稿",
        ///     "bomType=產品BOM",
        ///     "page=1",
        ///     "pageSize=50"
        ///   ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_bom_list",
        ///   "Result": "獲取BOM清單成功",
        ///   "TimeTaken": "35ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "ID": "BOM001",
        ///       "ProjectGUID": "660F9500-F39C-52E5-B827-557766551111",
        ///       "ProjectID": "PRJ001",
        ///       "name": "智慧交通控制器主機板",
        ///       "description": "智慧交通控制器的核心主機板組件",
        ///       "version": "V1.0",
        ///       "status": "已核准",
        ///       "bomType": "產品BOM",
        ///       "totalItems": "25",
        ///       "totalCost": "8500",
        ///       "createdBy": "李工程師",
        ///       "createdAt": "2024-02-01",
        ///       "updatedAt": "2024-02-15",
        ///       "approvedBy": "張主管",
        ///       "approvedAt": "2024-02-15",
        ///       "notes": "第一版主機板設計"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">標準請求物件，包含伺服器資訊與查詢參數</param>
        /// <returns>回傳符合條件的 BOM 清單 JSON</returns>
        [HttpPost("get_bom_list")]
        public string get_bom_list([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_bom_list";

            try
            {
                // 1) 請求驗證
                if (returnData == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "請求物件不能為空";
                    return returnData.JsonSerializationt();
                }
                if (string.IsNullOrWhiteSpace(returnData.ServerName) || string.IsNullOrWhiteSpace(returnData.ServerType))
                {
                    returnData.Code = 400;
                    returnData.Result = "ServerName 與 ServerType 為必填欄位";
                    return returnData.JsonSerializationt();
                }

                // 2) 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 404;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string bomTable = new enum_project_boms().GetEnumDescription();
                var sqlBom = new SQLControl(conf.Server, conf.DBName, bomTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                // 3) 解析查詢參數
                string GetVal(string key) => returnData.ValueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                string projectGuid = GetVal("projectGuid") ?? "";
                string status = GetVal("status") ?? "";
                string bomType = GetVal("bomType") ?? "";
                string sortBy = GetVal("sortBy") ?? "createdAt";
                string sortOrder = (GetVal("sortOrder") ?? "desc").ToLower();

                int page = (GetVal("page") ?? "1").StringToInt32();
                int pageSize = (GetVal("pageSize") ?? "50").StringToInt32();
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 50;

                string Esc(string s) => (s ?? "").Replace("'", "''");

                // 4) 組合查詢條件
                string where = " WHERE 1=1 ";
                if (!projectGuid.StringIsEmpty()) where += $" AND ProjectGUID = '{Esc(projectGuid)}' ";
                if (!status.StringIsEmpty()) where += $" AND status = '{Esc(status)}' ";
                if (!bomType.StringIsEmpty()) where += $" AND bomType = '{Esc(bomType)}' ";

                int offset = (page - 1) * pageSize;
                string sql = $@"
                    SELECT * 
                    FROM {conf.DBName}.{bomTable}
                    {where}
                    ORDER BY {sortBy} {(sortOrder == "asc" ? "ASC" : "DESC")}
                    LIMIT {offset}, {pageSize}";

                // 5) 查詢資料
                var dt = sqlBom.WtrteCommandAndExecuteReader(sql);
                var boms = dt.DataTableToRowList().SQLToClass<ProjectBomClass, enum_project_boms>() ?? new List<ProjectBomClass>();

                // 5.1) 計算每個 BOM 的項目數量
                if (boms.Count > 0)
                {
                    string bomGuids = string.Join(",", boms.Select(b => $"'{Esc(b.GUID)}'"));
                    string itemsTable = new enum_bom_items().GetEnumDescription();
                    string sqlCount = $@"
        SELECT BomGUID, COUNT(*) as cnt
        FROM {conf.DBName}.{itemsTable}
        WHERE BomGUID IN ({bomGuids})
        GROUP BY BomGUID";

                    var dtCount = sqlBom.WtrteCommandAndExecuteReader(sqlCount);
                    var countDict = dtCount.AsEnumerable().ToDictionary(
                        row => row["BomGUID"].ToString(),
                        row => row["cnt"].ToString()
                    );

                    foreach (var bom in boms)
                    {
                        if (countDict.ContainsKey(bom.GUID))
                            bom.totalItems = countDict[bom.GUID];  // 設定項目數量
                        else
                            bom.totalItems = "0";
                    }
                }

                // 6) 組合回傳
                returnData.Code = 200;
                returnData.Result = "獲取BOM清單成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = boms;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"Exception: {ex.Message}";
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 取得單一 BOM 詳細資訊
        /// </summary>
        /// <remarks>
        /// 此 API 用於依 GUID 查詢單一 BOM 的完整資訊。  
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [ "GUID=550E8400-E29B-41D4-A716-446655440000" ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_bom_details",
        ///   "Result": "OK",
        ///   "Data": {
        ///     "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///     "name": "智慧交通控制器主機板",
        ///     "description": "核心控制組件",
        ///     "status": "已核准",
        ///     "version": "V1.0",
        ///     "createdAt": "2024-02-01",
        ///     "updatedAt": "2024-02-15"
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_bom_details")]
        public string get_bom_details([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_bom_details";

            try
            {
                string guid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("GUID=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                if (guid.StringIsEmpty())
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少 GUID 參數";
                    return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 404;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_boms().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string Esc(string s) => (s ?? "").Replace("'", "''");
                string sqlStr = $"SELECT * FROM {conf.DBName}.{table} WHERE GUID='{Esc(guid)}' LIMIT 1";
                var dt = sql.WtrteCommandAndExecuteReader(sqlStr);

                if (dt.Rows.Count == 0)
                {
                    returnData.Code = 404;
                    returnData.Result = $"查無 GUID={guid} 的 BOM";
                    return returnData.JsonSerializationt();
                }

                var bom = dt.DataTableToRowList().SQLToClass<ProjectBomClass, enum_project_boms>()[0];

                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = bom;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 建立新的 BOM 主檔
        /// </summary>
        /// <remarks>
        /// 此 API 用於新增 BOM 主檔記錄，必須填入 ProjectGUID、ProjectID、name、description、version、bomType、createdBy。  
        /// 預設狀態為「草稿」。  
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "ProjectGUID": "660F9500-F39C-52E5-B827-557766551111",
        ///       "ProjectID": "PRJ001",
        ///       "name": "新產品 BOM",
        ///       "description": "新產品的物料清單",
        ///       "version": "V1.0",
        ///       "bomType": "產品BOM",
        ///       "createdBy": "工程師A",
        ///       "notes": "第一版草稿"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "create_bom",
        ///   "Result": "建立 BOM 成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "ID": "BOM001",
        ///       "ProjectGUID": "660F9500-F39C-52E5-B827-557766551111",
        ///       "ProjectID": "PRJ001",
        ///       "name": "新產品 BOM",
        ///       "status": "草稿",
        ///       "createdBy": "工程師A",
        ///       "createdAt": "2025-08-16",
        ///       "updatedAt": "2025-08-16"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("create_bom")]
        public string create_bom([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "create_bom";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ProjectBomClass> input = returnData.Data.ObjToClass<List<ProjectBomClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 404;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_boms().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var resultList = new List<ProjectBomClass>();
                foreach (var bom in input)
                {
                    string guid = Guid.NewGuid().ToString().ToUpper();
                    string now = DateTime.Now.ToDateTimeString();

                    var row = new object[Enum.GetValues(typeof(enum_project_boms)).Length];
                    row[(int)enum_project_boms.GUID] = guid;
                    row[(int)enum_project_boms.ID] = bom.ID ?? "";
                    row[(int)enum_project_boms.ProjectGUID] = bom.ProjectGUID ?? "";
                    row[(int)enum_project_boms.ProjectID] = bom.ProjectID ?? "";
                    row[(int)enum_project_boms.name] = bom.name ?? "";
                    row[(int)enum_project_boms.description] = bom.description ?? "";
                    row[(int)enum_project_boms.version] = bom.version ?? "";
                    row[(int)enum_project_boms.status] = bom.status ?? "草稿";
                    row[(int)enum_project_boms.bomType] = bom.bomType ?? "";
                    row[(int)enum_project_boms.createdBy] = bom.createdBy ?? "";
                    row[(int)enum_project_boms.notes] = bom.notes ?? "";
                    row[(int)enum_project_boms.createdAt] = now;
                    row[(int)enum_project_boms.updatedAt] = now;
                    sql.AddRow(null, row);

                    bom.GUID = guid;
                    bom.createdAt = now;
                    bom.updatedAt = now;
                    bom.status = "草稿";
                    resultList.Add(bom);
                }

                returnData.Code = 200;
                returnData.Result = "建立 BOM 成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 更新 BOM 主檔資訊
        /// </summary>
        /// <remarks>
        /// 此 API 用於更新既有的 BOM 主檔，必須提供 GUID。  
        /// 可更新的欄位：name、description、version、status、bomType、notes。  
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "name": "更新後 BOM",
        ///       "status": "審核中",
        ///       "notes": "提交審核"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "update_bom",
        ///   "Result": "更新成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "name": "更新後 BOM",
        ///       "status": "審核中",
        ///       "updatedAt": "2025-08-16"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("update_bom")]
        public string update_bom([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "update_bom";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ProjectBomClass> input = returnData.Data.ObjToClass<List<ProjectBomClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 404;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_boms().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var resultList = new List<ProjectBomClass>();
                foreach (var bom in input)
                {
                    if (bom.GUID.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "GUID 為必填";
                        return returnData.JsonSerializationt();
                    }

                    var existed = sql.GetRowsByDefult(null, (int)enum_project_boms.GUID, bom.GUID);
                    if (existed.Count == 0)
                    {
                        returnData.Code = 404;
                        returnData.Result = $"查無 GUID={bom.GUID} 的資料";
                        return returnData.JsonSerializationt();
                    }

                    var row = existed[0];
                    row[(int)enum_project_boms.name] = bom.name ?? row[(int)enum_project_boms.name].ObjectToString();
                    row[(int)enum_project_boms.description] = bom.description ?? row[(int)enum_project_boms.description].ObjectToString();
                    row[(int)enum_project_boms.version] = bom.version ?? row[(int)enum_project_boms.version].ObjectToString();
                    row[(int)enum_project_boms.status] = bom.status ?? row[(int)enum_project_boms.status].ObjectToString();
                    row[(int)enum_project_boms.bomType] = bom.bomType ?? row[(int)enum_project_boms.bomType].ObjectToString();
                    row[(int)enum_project_boms.notes] = bom.notes ?? row[(int)enum_project_boms.notes].ObjectToString();
                    row[(int)enum_project_boms.updatedAt] = DateTime.Now.ToDateTimeString();

                    sql.UpdateByDefulteExtra(null, row);

                    bom.updatedAt = row[(int)enum_project_boms.updatedAt].ObjectToString();
                    resultList.Add(bom);
                }

                returnData.Code = 200;
                returnData.Result = "更新成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 刪除 BOM 主檔（僅限草稿狀態）
        /// </summary>
        /// <remarks>
        /// 此 API 用於刪除 BOM 主檔，僅允許狀態為「草稿」的 BOM。  
        /// 刪除時必須提供 GUID。  
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     { "GUID": "550E8400-E29B-41D4-A716-446655440000" }
        ///   ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_bom",
        ///   "Result": "刪除完成：成功 1 筆，失敗 0 筆"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("delete_bom")]
        public string delete_bom([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_bom";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ProjectBomClass> input = returnData.Data.ObjToClass<List<ProjectBomClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 404;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_boms().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var deleted = 0;
                var failed = 0;

                foreach (var bom in input)
                {
                    if (bom.GUID.StringIsEmpty()) { failed++; continue; }

                    var rows = sql.GetRowsByDefult(null, (int)enum_project_boms.GUID, bom.GUID);
                    if (rows.Count == 0) { failed++; continue; }

                    var row = rows[0];
                    if (row[(int)enum_project_boms.status].ObjectToString() != "草稿")
                    {
                        failed++;
                        continue;
                    }

                    sql.DeleteExtra(null, row);
                    deleted++;
                }

                returnData.Code = 200;
                returnData.Result = $"刪除完成：成功 {deleted} 筆，失敗 {failed} 筆";
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 核准 BOM（狀態變更為已核准）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b>  
        /// 將指定 BOM 狀態由「審核中」更新為「已核准」，並寫入核准者與核准時間。
        ///
        /// <b>呼叫方式：</b>  
        /// <para>Endpoint：<c>/api/bom/approve_bom</c></para>
        /// <para>Method：<c>POST</c>（Body: JSON）</para>
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "approvedBy": "張主管"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "approve_bom",
        ///   "Result": "BOM 已成功核准",
        ///   "TimeTaken": "15.2ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "status": "已核准",
        ///       "approvedBy": "張主管",
        ///       "approvedAt": "2025-08-16T15:20:00"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("approve_bom")]
        public string approve_bom([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "approve_bom";

            try
            {
                // 驗證輸入
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ProjectBomClass> input = returnData.Data.ObjToClass<List<ProjectBomClass>>();
                if (input == null || input.Count == 0 || input.Any(x => x.GUID.StringIsEmpty()))
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少必要參數：GUID";
                    return returnData.JsonSerializationt();
                }

                // 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string bomTable = new enum_project_boms().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, bomTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string Esc(string s) => (s ?? "").Replace("'", "''");
                var results = new List<ProjectBomClass>();

                foreach (var bom in input)
                {
                    var rows = sql.GetRowsByDefult(null, (int)enum_project_boms.GUID, bom.GUID);
                    if (rows.Count == 0)
                    {
                        returnData.Code = 404;
                        returnData.Result = $"查無 GUID={bom.GUID} 的 BOM";
                        return returnData.JsonSerializationt();
                    }

                    var row = rows[0];
                    string currentStatus = row[(int)enum_project_boms.status].ObjectToString();

                    if (!string.Equals(currentStatus, "審核中", StringComparison.OrdinalIgnoreCase))
                    {
                        returnData.Code = -1;
                        returnData.Result = $"BOM 狀態必須為「審核中」才能核准，目前狀態：{currentStatus}";
                        return returnData.JsonSerializationt();
                    }

                    string now = DateTime.Now.ToDateTimeString();
                    row[(int)enum_project_boms.status] = "已核准";
                    row[(int)enum_project_boms.approvedBy] = bom.approvedBy ?? "系統";
                    row[(int)enum_project_boms.approvedAt] = now;
                    row[(int)enum_project_boms.updatedAt] = now;

                    sql.UpdateByDefulteExtra(null, row);

                    bom.status = "已核准";
                    bom.approvedBy = row[(int)enum_project_boms.approvedBy].ObjectToString();
                    bom.approvedAt = now;
                    results.Add(bom);
                }

                returnData.Code = 200;
                returnData.Result = "BOM 已成功核准";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = results;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 發佈 BOM（狀態變更為已發佈）  
        /// </summary>
        /// <remarks>
        /// 用途:  
        /// 將指定 BOM 狀態由「已核准」更新為「已發佈」。  
        ///
        /// <b>Request JSON 範例：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000"
        ///     }
        ///   ]
        /// }
        /// ```
        /// ]]>
        ///
        /// <b>Response JSON 範例：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "publish_bom",
        ///   "Result": "BOM 已成功發佈",
        ///   "TimeTaken": "12.3ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "status": "已發佈",
        ///       "updatedAt": "2025-08-16T15:25:00"
        ///     }
        ///   ]
        /// }
        /// ```
        /// ]]>
        /// </remarks>
        [HttpPost("publish_bom")]
        public string publish_bom([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "publish_bom";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ProjectBomClass> input = returnData.Data.ObjToClass<List<ProjectBomClass>>();
                if (input == null || input.Count == 0 || input.Any(x => x.GUID.StringIsEmpty()))
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少必要參數：GUID";
                    return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string bomTable = new enum_project_boms().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, bomTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string Esc(string s) => (s ?? "").Replace("'", "''");
                var results = new List<ProjectBomClass>();

                foreach (var bom in input)
                {
                    var rows = sql.GetRowsByDefult(null, (int)enum_project_boms.GUID, bom.GUID);
                    if (rows.Count == 0)
                    {
                        returnData.Code = 404;
                        returnData.Result = $"查無 GUID={bom.GUID} 的 BOM";
                        return returnData.JsonSerializationt();
                    }

                    var row = rows[0];
                    string currentStatus = row[(int)enum_project_boms.status].ObjectToString();

                    if (!string.Equals(currentStatus, "已核准", StringComparison.OrdinalIgnoreCase))
                    {
                        returnData.Code = -1;
                        returnData.Result = $"BOM 狀態必須為「已核准」才能發佈，目前狀態：{currentStatus}";
                        return returnData.JsonSerializationt();
                    }

                    string now = DateTime.Now.ToDateTimeString();
                    row[(int)enum_project_boms.status] = "已發佈";
                    row[(int)enum_project_boms.updatedAt] = now;

                    sql.UpdateByDefulteExtra(null, row);

                    bom.status = "已發佈";
                    bom.updatedAt = now;
                    results.Add(bom);
                }

                returnData.Code = 200;
                returnData.Result = "BOM 已成功發佈";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = results;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }


        /// <summary>
        /// 獲取指定 BOM 的所有項目清單
        /// </summary>
        /// <remarks>
        /// 依據 BOM GUID 查詢其所有項目資料。
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": ["bomGuid=550E8400-E29B-41D4-A716-446655440000"]
        /// }
        /// ```
        ///
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_bom_items",
        ///   "Result": "獲取BOM項目成功",
        ///   "TimeTaken": "15.2ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "ID": "ITEM001",
        ///       "BomGUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "itemCode": "MCU-001",
        ///       "itemName": "ARM Cortex-M4 微控制器",
        ///       "quantity": "1",
        ///       "unit": "個"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_bom_items")]
        public string get_bom_items([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_bom_items";

            try
            {
                // 驗證參數
                string bomGuid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("bomGuid=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                if (bomGuid.StringIsEmpty())
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少必要參數：bomGuid";
                    return returnData.JsonSerializationt();
                }

                // 取得 DB 設定
                var conf = serverSetting.GetAllServerSetting().myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                // 查詢
                var sqlItems = new SQLControl(conf.Server, conf.DBName, new enum_bom_items().GetEnumDescription(), conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                string Esc(string s) => (s ?? "").Replace("'", "''");
                string sql = $@"SELECT * FROM {conf.DBName}.{new enum_bom_items().GetEnumDescription()} WHERE BomGUID = '{Esc(bomGuid)}'";
                var dt = sqlItems.WtrteCommandAndExecuteReader(sql);

                var list = dt.DataTableToRowList().SQLToClass<BomItemClass, enum_bom_items>();

                returnData.Code = 200;
                returnData.Result = "獲取BOM項目成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = list;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 獲取單一 BOM 項目詳細資訊
        /// </summary>
        /// <remarks>
        /// 依 GUID 查詢單一項目。
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": ["itemGuid=880F9700-F59E-74G7-D049-779988773333"]
        /// }
        /// ```
        ///
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_bom_item_details",
        ///   "Result": "OK",
        ///   "Data": {
        ///     "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///     "itemCode": "MCU-001",
        ///     "itemName": "ARM Cortex-M4 微控制器"
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_bom_item_details")]
        public string get_bom_item_details([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_bom_item_details";

            try
            {
                string itemGuid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("itemGuid=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                if (itemGuid.StringIsEmpty())
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少必要參數：itemGuid";
                    return returnData.JsonSerializationt();
                }

                var conf = serverSetting.GetAllServerSetting().myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                var sqlItems = new SQLControl(conf.Server, conf.DBName, new enum_bom_items().GetEnumDescription(), conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                string Esc(string s) => (s ?? "").Replace("'", "''");
                string sql = $@"SELECT * FROM {conf.DBName}.{new enum_bom_items().GetEnumDescription()} WHERE GUID = '{Esc(itemGuid)}'";
                var dt = sqlItems.WtrteCommandAndExecuteReader(sql);

                if (dt.Rows.Count == 0)
                {
                    returnData.Code = 404;
                    returnData.Result = $"查無 GUID={itemGuid} 的項目";
                    return returnData.JsonSerializationt();
                }

                var item = dt.DataTableToRowList().SQLToClass<BomItemClass, enum_bom_items>().First();

                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = item;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 在 BOM 中創建新項目
        /// </summary>
        /// <remarks>
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [{
        ///     "BomGUID": "550E8400-E29B-41D4-A716-446655440000",
        ///     "itemCode": "MCU-001",
        ///     "itemName": "ARM Cortex-M4 微控制器",
        ///     "quantity": "1",
        ///     "unit": "個",
        ///     "itemType": "子項目"
        ///   }]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("create_bom_item")]
        public string create_bom_item([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "create_bom_item";

            try
            {
                var input = returnData.Data.ObjToClass<List<BomItemClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空或格式錯誤";
                    return returnData.JsonSerializationt();
                }

                var conf = serverSetting.GetAllServerSetting().myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                var sqlItems = new SQLControl(conf.Server, conf.DBName, new enum_bom_items().GetEnumDescription(), conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                string now = DateTime.Now.ToDateTimeString();
                var results = new List<BomItemClass>();

                foreach (var item in input)
                {
                    item.GUID = Guid.NewGuid().ToString().ToUpper();
                    item.createdAt = now;
                    item.updatedAt = now;

                    var row = new object[Enum.GetValues(typeof(enum_bom_items)).Length];
                    row[(int)enum_bom_items.GUID] = item.GUID;
                    row[(int)enum_bom_items.BomGUID] = item.BomGUID;
                    row[(int)enum_bom_items.BomID] = item.BomID;
                    row[(int)enum_bom_items.itemCode] = item.itemCode;
                    row[(int)enum_bom_items.itemName] = item.itemName;
                    row[(int)enum_bom_items.description] = item.description;
                    row[(int)enum_bom_items.specification] = item.specification;
                    row[(int)enum_bom_items.quantity] = item.quantity;
                    row[(int)enum_bom_items.unit] = item.unit;
                    row[(int)enum_bom_items.leadTime] = item.leadTime;
                    row[(int)enum_bom_items.notes] = item.notes;
                    row[(int)enum_bom_items.itemType] = item.itemType;
                    row[(int)enum_bom_items.ParentGUID] = item.ParentGUID;
                    row[(int)enum_bom_items.ParentID] = item.ParentID;
                    row[(int)enum_bom_items.createdAt] = item.createdAt;
                    row[(int)enum_bom_items.updatedAt] = item.updatedAt;

                    sqlItems.AddRow(null, row);
                    results.Add(item);
                }

                returnData.Code = 200;
                returnData.Result = "新增BOM項目成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = results;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 更新 BOM 項目資訊
        /// </summary>
        /// <remarks>
        /// 依指定的項目 GUID，更新 BOM 項目的欄位資訊。  
        /// 
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [{
        ///     "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///     "description": "新的描述",
        ///     "quantity": "3",
        ///     "leadTime": "21",
        ///     "notes": "更新備註"
        ///   }]
        /// }
        /// ```
        /// 
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "update_bom_item",
        ///   "Result": "更新BOM項目成功",
        ///   "TimeTaken": "13.5ms",
        ///   "Data": [{
        ///     "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///     "quantity": "3",
        ///     "notes": "更新備註"
        ///   }]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("update_bom_item")]
        public string update_bom_item([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "update_bom_item";

            try
            {
                var input = returnData.Data.ObjToClass<List<BomItemClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空或格式錯誤";
                    return returnData.JsonSerializationt();
                }

                var conf = serverSetting.GetAllServerSetting().myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                var sqlItems = new SQLControl(conf.Server, conf.DBName, new enum_bom_items().GetEnumDescription(),
                                              conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                string now = DateTime.Now.ToDateTimeString();
                var results = new List<BomItemClass>();

                foreach (var item in input)
                {
                    if (item.GUID.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "缺少必要欄位：GUID";
                        return returnData.JsonSerializationt();
                    }

                    var rows = sqlItems.GetRowsByDefult(null, (int)enum_bom_items.GUID, item.GUID);
                    if (rows.Count == 0)
                    {
                        returnData.Code = 404;
                        returnData.Result = $"查無 GUID={item.GUID} 的項目";
                        return returnData.JsonSerializationt();
                    }

                    var row = rows[0];
                    row[(int)enum_bom_items.itemCode] = item.itemCode ?? row[(int)enum_bom_items.itemCode].ObjectToString();
                    row[(int)enum_bom_items.itemName] = item.itemName ?? row[(int)enum_bom_items.itemName].ObjectToString();
                    row[(int)enum_bom_items.description] = item.description ?? row[(int)enum_bom_items.description].ObjectToString();
                    row[(int)enum_bom_items.specification] = item.specification ?? row[(int)enum_bom_items.specification].ObjectToString();
                    row[(int)enum_bom_items.quantity] = item.quantity ?? row[(int)enum_bom_items.quantity].ObjectToString();
                    row[(int)enum_bom_items.unit] = item.unit ?? row[(int)enum_bom_items.unit].ObjectToString();
                    row[(int)enum_bom_items.leadTime] = item.leadTime ?? row[(int)enum_bom_items.leadTime].ObjectToString();
                    row[(int)enum_bom_items.notes] = item.notes ?? row[(int)enum_bom_items.notes].ObjectToString();
                    row[(int)enum_bom_items.itemType] = item.itemType ?? row[(int)enum_bom_items.itemType].ObjectToString();
                    row[(int)enum_bom_items.ParentGUID] = item.ParentGUID ?? row[(int)enum_bom_items.ParentGUID].ObjectToString();
                    row[(int)enum_bom_items.ParentID] = item.ParentID ?? row[(int)enum_bom_items.ParentID].ObjectToString();
                    row[(int)enum_bom_items.updatedAt] = now;

                    sqlItems.UpdateByDefulteExtra(null, row);
                    item.updatedAt = now;
                    results.Add(item);
                }

                returnData.Code = 200;
                returnData.Result = "更新BOM項目成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = results;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 刪除 BOM 項目
        /// </summary>
        /// <remarks>
        /// 依 GUID 刪除指定的 BOM 項目。  
        /// 
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [{
        ///     "GUID": "880F9700-F59E-74G7-D049-779988773333"
        ///   }]
        /// }
        /// ```
        /// 
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_bom_item",
        ///   "Result": "刪除BOM項目成功",
        ///   "TimeTaken": "11.3ms",
        ///   "Data": ["880F9700-F59E-74G7-D049-779988773333"]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("delete_bom_item")]
        public string delete_bom_item([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_bom_item";

            try
            {
                var input = returnData.Data.ObjToClass<List<BomItemClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空或格式錯誤";
                    return returnData.JsonSerializationt();
                }

                var conf = serverSetting.GetAllServerSetting().myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                var sqlItems = new SQLControl(conf.Server, conf.DBName, new enum_bom_items().GetEnumDescription(),
                                              conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                var deletedGuids = new List<string>();

                foreach (var item in input)
                {
                    if (item.GUID.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "缺少必要欄位：GUID";
                        return returnData.JsonSerializationt();
                    }

                    var rows = sqlItems.GetRowsByDefult(null, (int)enum_bom_items.GUID, item.GUID);
                    if (rows.Count == 0)
                    {
                        returnData.Code = 404;
                        returnData.Result = $"查無 GUID={item.GUID} 的項目";
                        return returnData.JsonSerializationt();
                    }

                    sqlItems.DeleteExtra(null, rows);
                    deletedGuids.Add(item.GUID);
                }

                returnData.Code = 200;
                returnData.Result = "刪除BOM項目成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = deletedGuids;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt();
            }
        }
        /// <summary>
        /// 批量創建 BOM 項目
        /// </summary>
        /// <remarks>
        /// 用於在指定的 BOM 中一次性新增多個項目。  
        /// 
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": ["bomGuid=550E8400-E29B-41D4-A716-446655440000"],
        ///   "Data": [
        ///     {
        ///       "BomGUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "items": [
        ///         {
        ///           "itemCode": "MCU-001",
        ///           "itemName": "微控制器",
        ///           "description": "ARM處理器",
        ///           "quantity": "1",
        ///           "unit": "個",
        ///           "itemType": "子項目"
        ///         },
        ///         {
        ///           "itemCode": "RES-001", 
        ///           "itemName": "電阻器",
        ///           "description": "10KΩ電阻",
        ///           "quantity": "20",
        ///           "unit": "個",
        ///           "itemType": "子項目"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "batch_create_bom_items",
        ///   "Result": "批量新增 BOM 項目完成，共新增 2 筆",
        ///   "TimeTaken": "25.6ms",
        ///   "Data": [
        ///     { "GUID": "F123...", "itemCode": "MCU-001", "itemName": "微控制器", "quantity": "1" },
        ///     { "GUID": "F124...", "itemCode": "RES-001", "itemName": "電阻器", "quantity": "20" }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("batch_create_bom_items")]
        public string batch_create_bom_items([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "batch_create_bom_items";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 輸入驗證
                List<ProjectBomClass> input = returnData.Data.ObjToClass<List<ProjectBomClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // 取得資料庫設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string bomItemsTable = new enum_bom_items().GetEnumDescription();
                var sqlBomItems = new SQLControl(conf.Server, conf.DBName, bomItemsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var results = new List<BomItemClass>();
                foreach (var bom in input)
                {
                    if (bom.items == null || bom.items.Count == 0) continue;

                    foreach (var item in bom.items)
                    {
                        var ins = new object[Enum.GetValues(typeof(enum_bom_items)).Length];
                        string guid = Guid.NewGuid().ToString().ToUpper();

                        ins[(int)enum_bom_items.GUID] = guid;
                        ins[(int)enum_bom_items.ID] = item.ID ?? "";
                        ins[(int)enum_bom_items.BomGUID] = bom.GUID ?? "";
                        ins[(int)enum_bom_items.BomID] = bom.ID ?? "";
                        ins[(int)enum_bom_items.itemCode] = item.itemCode ?? "";
                        ins[(int)enum_bom_items.itemName] = item.itemName ?? "";
                        ins[(int)enum_bom_items.description] = item.description ?? "";
                        ins[(int)enum_bom_items.specification] = item.specification ?? "";
                        ins[(int)enum_bom_items.quantity] = item.quantity ?? "0";
                        ins[(int)enum_bom_items.unit] = item.unit ?? "";
                        ins[(int)enum_bom_items.leadTime] = item.leadTime ?? "";
                        ins[(int)enum_bom_items.notes] = item.notes ?? "";
                        ins[(int)enum_bom_items.itemType] = item.itemType ?? "";
                        ins[(int)enum_bom_items.ParentGUID] = item.ParentGUID ?? "";
                        ins[(int)enum_bom_items.ParentID] = item.ParentID ?? "";
                        ins[(int)enum_bom_items.createdAt] = DateTime.Now.ToDateTimeString();
                        ins[(int)enum_bom_items.updatedAt] = DateTime.Now.ToDateTimeString();

                        sqlBomItems.AddRow(null, ins);

                        item.GUID = guid;
                        item.createdAt = DateTime.Now.ToDateTimeString();
                        item.updatedAt = DateTime.Now.ToDateTimeString();
                        results.Add(item);
                    }
                }

                returnData.Code = 200;
                returnData.Result = $"批量新增 BOM 項目完成，共新增 {results.Count} 筆";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = results;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 批量更新 BOM 項目
        /// </summary>
        /// <remarks>
        /// 用於一次性更新多個 BOM 項目，依 GUID 更新。
        /// 
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "description": "新的描述內容",
        ///       "quantity": "10",
        ///       "notes": "更新批次"
        ///     },
        ///     {
        ///       "GUID": "990F9700-F59E-74G7-D049-779988774444",
        ///       "itemName": "更新後的名稱",
        ///       "specification": "新規格說明"
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "batch_update_bom_items",
        ///   "Result": "批量更新 BOM 項目完成，共更新 2 筆",
        ///   "TimeTaken": "31.2ms",
        ///   "Data": [
        ///     { "GUID": "880F9700-F59E-74G7-D049-779988773333", "quantity": "10", "notes": "更新批次" },
        ///     { "GUID": "990F9700-F59E-74G7-D049-779988774444", "itemName": "更新後的名稱", "specification": "新規格說明" }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("batch_update_bom_items")]
        public string batch_update_bom_items([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "batch_update_bom_items";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<BomItemClass> input = returnData.Data.ObjToClass<List<BomItemClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string bomItemsTable = new enum_bom_items().GetEnumDescription();
                var sqlBomItems = new SQLControl(conf.Server, conf.DBName, bomItemsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var updated = new List<BomItemClass>();
                foreach (var item in input)
                {
                    if (item.GUID.StringIsEmpty()) continue;

                    var rows = sqlBomItems.GetRowsByDefult(null, (int)enum_bom_items.GUID, item.GUID);
                    if (rows.Count == 0) continue;

                    var row = rows[0];
                    row[(int)enum_bom_items.itemName] = item.itemName ?? row[(int)enum_bom_items.itemName].ObjectToString();
                    row[(int)enum_bom_items.description] = item.description ?? row[(int)enum_bom_items.description].ObjectToString();
                    row[(int)enum_bom_items.specification] = item.specification ?? row[(int)enum_bom_items.specification].ObjectToString();
                    row[(int)enum_bom_items.quantity] = item.quantity ?? row[(int)enum_bom_items.quantity].ObjectToString();
                    row[(int)enum_bom_items.unit] = item.unit ?? row[(int)enum_bom_items.unit].ObjectToString();
                    row[(int)enum_bom_items.leadTime] = item.leadTime ?? row[(int)enum_bom_items.leadTime].ObjectToString();
                    row[(int)enum_bom_items.notes] = item.notes ?? row[(int)enum_bom_items.notes].ObjectToString();
                    row[(int)enum_bom_items.itemType] = item.itemType ?? row[(int)enum_bom_items.itemType].ObjectToString();
                    row[(int)enum_bom_items.ParentGUID] = item.ParentGUID ?? row[(int)enum_bom_items.ParentGUID].ObjectToString();
                    row[(int)enum_bom_items.ParentID] = item.ParentID ?? row[(int)enum_bom_items.ParentID].ObjectToString();
                    row[(int)enum_bom_items.updatedAt] = DateTime.Now.ToDateTimeString();

                    sqlBomItems.UpdateByDefulteExtra(null, row);

                    item.updatedAt = DateTime.Now.ToDateTimeString();
                    updated.Add(item);
                }

                returnData.Code = 200;
                returnData.Result = $"批量更新 BOM 項目完成，共更新 {updated.Count} 筆";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = updated;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }


        /// <summary>
        /// 獲取 BOM 文件清單
        /// </summary>
        /// <remarks>
        /// 依據指定的 <c>BomGUID</c> 取得其所有相關文件清單。
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [ "BomGUID=550E8400-E29B-41D4-A716-446655440000" ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_bom_documents",
        ///   "Result": "OK",
        ///   "TimeTaken": "12.5ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "DOC001-GUID-123",
        ///       "ID": "DOC001",
        ///       "BomGUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "fileName": "system_file.pdf",
        ///       "originalName": "設計圖.pdf",
        ///       "type": "圖面",
        ///       "fileSize": "102400",
        ///       "uploadDate": "2025-08-16",
        ///       "uploadedBy": "王小明",
        ///       "url": "/documents/system_file.pdf",
        ///       "notes": "第一版"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_bom_documents")]
        public string get_bom_documents([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_bom_documents";

            try
            {
                // 驗證參數
                string bomGuid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("BomGUID=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                if (bomGuid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 BomGUID 參數";
                    return returnData.JsonSerializationt();
                }

                // DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_bom_documents().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string Esc(string s) => (s ?? "").Replace("'", "''");
                string query = $"SELECT * FROM {conf.DBName}.{table} WHERE BomGUID='{Esc(bomGuid)}' ORDER BY uploadDate DESC";
                var dt = sql.WtrteCommandAndExecuteReader(query);

                var docs = dt.DataTableToRowList().SQLToClass<BomDocumentClass, enum_bom_documents>();

                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = docs;
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
        /// 上傳 BOM 文件
        /// </summary>
        /// <remarks>
        /// 使用 multipart/form-data 上傳 BOM 文件。
        ///
        /// <b>Request 格式：</b>
        /// - bomGUID: BOM 主檔 GUID（必填）
        /// - userName: 上傳者名稱（必填）
        /// - file: 檔案物件（必填）
        /// - type: 文件類型（必填：圖面|規格書|技術文件|其他）
        /// - notes: 備註（選填）
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "upload_bom_document",
        ///   "Result": "檔案上傳成功",
        ///   "TimeTaken": "50.1ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "NEW-DOC-GUID-001",
        ///       "BomGUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "BomID": "12345",
        ///       "fileName": "12345_設計圖_20250816_193000.pdf",
        ///       "originalName": "設計圖.pdf",
        ///       "type": "圖面",
        ///       "fileSize": "2048576",
        ///       "url": "/documents/12345_設計圖_20250816_193000.pdf",
        ///       "uploadDate": "2025-08-16 19:30:00",
        ///       "uploadedBy": "王小明"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("upload_bom_document")]
        [RequestSizeLimit(30_000_000)] // 限制30MB
        public string upload_bom_document([FromForm] string bomGUID, [FromForm] string userName, [FromForm] string type, [FromForm] string notes, [FromForm] IFormFile file)
        {
            var timer = new MyTimerBasic();
            var returnData = new returnData { Method = "upload_bom_document" };

            try
            {
                if (bomGUID.StringIsEmpty() || userName.StringIsEmpty() || file == null || file.Length == 0 || type.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少必要參數 (bomGUID / userName / file / type)";
                    return returnData.JsonSerializationt();
                }

                // 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind("Main", "網頁", "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                // === Step1: 用 bomID 找 BOM 主檔 GUID ===
                string bomTable = new enum_project_boms().GetEnumDescription();
                var sqlBom = new SQLControl(conf.Server, conf.DBName, bomTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                List<object[]> bomRows = sqlBom.GetRowsByDefult(null, (int)enum_project_boms.GUID, bomGUID);
                if (bomRows.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到對應的 BOM (GUID={bomGUID})";
                    return returnData.JsonSerializationt();
                }
                string bomID = bomRows[0][(int)enum_project_boms.ID].ObjectToString();

                // === Step2: 檔案處理 ===
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string safeFileName = SanitizeFileName(Path.GetFileName(file.FileName));
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string ext = Path.GetExtension(safeFileName);
                string baseName = Path.GetFileNameWithoutExtension(safeFileName);
                string newFileName = $"{bomID}_{baseName}_{timestamp}{ext}";

                string filePath = Path.Combine(folder, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                FileInfo fi = new FileInfo(filePath);

                // === Step3: 寫入文件表 ===
                string docTable = new enum_bom_documents().GetEnumDescription();
                var sqlDoc = new SQLControl(conf.Server, conf.DBName, docTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string now = DateTime.Now.ToDateTimeString();
                string guid = Guid.NewGuid().ToString().ToUpper();

                var ins = new object[Enum.GetValues(typeof(enum_bom_documents)).Length];
                ins[(int)enum_bom_documents.GUID] = guid;
                ins[(int)enum_bom_documents.BomGUID] = bomGUID; // ✅ 存 GUID，不是 ID
                ins[(int)enum_bom_documents.ID] = bomID;
                ins[(int)enum_bom_documents.fileName] = newFileName;
                ins[(int)enum_bom_documents.originalName] = file.FileName;
                ins[(int)enum_bom_documents.type] = type;
                ins[(int)enum_bom_documents.fileSize] = fi.Length.ToString();
                ins[(int)enum_bom_documents.uploadDate] = now;
                ins[(int)enum_bom_documents.uploadedBy] = userName;
                ins[(int)enum_bom_documents.url] = $"/documents/{newFileName}";
                ins[(int)enum_bom_documents.notes] = notes ?? "";
                sqlDoc.AddRow(null, ins);

                var doc = new BomDocumentClass
                {
                    GUID = guid,
                    ID = bomID,
                    BomGUID = bomGUID,
                    fileName = newFileName,
                    originalName = file.FileName,
                    type = type,
                    fileSize = fi.Length.ToString(),
                    uploadDate = now,
                    uploadedBy = userName,
                    url = $"/documents/{newFileName}",
                    notes = notes
                };

                returnData.Code = 200;
                returnData.Result = "檔案上傳成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = new List<BomDocumentClass> { doc };
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
        /// 刪除 BOM 文件
        /// </summary>
        /// <remarks>
        /// 依 <c>GUID</c> 刪除 BOM 文件。
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [ { "GUID": "DOC-GUID-001" } ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_bom_document",
        ///   "Result": "刪除成功 1 筆",
        ///   "TimeTaken": "8.9ms"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("delete_bom_document")]
        public string delete_bom_document([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_bom_document";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                var input = returnData.Data.ObjToClass<List<BomDocumentClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_bom_documents().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var deleted = new List<string>();
                foreach (var doc in input)
                {
                    if (doc.GUID.StringIsEmpty()) continue;

                    var rows = sql.GetRowsByDefult(null, (int)enum_bom_documents.GUID, doc.GUID);
                    if (rows != null && rows.Count > 0)
                    {
                        sql.DeleteExtra(null, rows);
                        deleted.Add(doc.GUID);
                    }
                }

                returnData.Code = 200;
                returnData.Result = $"刪除成功 {deleted.Count} 筆";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = deleted;
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
        /// 下載 BOM 文件
        /// </summary>
        /// <remarks>
        /// 依 <c>GUID</c> 下載 BOM 文件。
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        //    "Data": [ { "GUID": "DOC-GUID-001" } ]
        /// }
        /// ```
        ///
        /// <b>成功回應：</b>
        /// - Content-Disposition: attachment; filename="fallback.pdf"; filename*=UTF-8''設計圖.pdf
        /// - Response Body: 檔案二進位串流
        ///
        /// <b>錯誤回應 JSON 範例：</b>
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_bom_document",
        ///   "Result": "找不到檔案"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("download_bom_document")]
        public IActionResult download_bom_document([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "download_bom_document";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return new JsonResult(returnData);
                }

                var input = returnData.Data.ObjToClass<List<BomDocumentClass>>();
                if (input == null || input.Count == 0 || input[0].GUID.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 GUID 參數";
                    return new JsonResult(returnData);
                }

                string guid = input[0].GUID;

                // 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return new JsonResult(returnData);
                }

                string table = new enum_bom_documents().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var rows = sql.GetRowsByDefult(null, (int)enum_bom_documents.GUID, guid);
                if (rows == null || rows.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到文件 (GUID={guid})";
                    return new JsonResult(returnData);
                }

                var doc = rows[0].SQLToClass<BomDocumentClass, enum_bom_documents>();

                // 檔案路徑
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
                string filePath = Path.Combine(folder, doc.fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    returnData.Code = -200;
                    returnData.Result = "檔案不存在於伺服器";
                    return new JsonResult(returnData);
                }

                var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                string contentType = "application/octet-stream";

                // 原始檔名（可能有中文）
                string originalName = doc.originalName ?? doc.fileName;

                // 安全 ASCII 檔名 (fallback)
                string asciiFileName = ToEnglishOrAscii(Path.GetFileNameWithoutExtension(originalName)) + Path.GetExtension(originalName);
                if (string.IsNullOrWhiteSpace(asciiFileName))
                    asciiFileName = "download" + Path.GetExtension(originalName);

                // UTF-8 檔名（正確編碼）
                string utf8FileName = Uri.EscapeDataString(originalName);

                // 設定 Content-Disposition header
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{asciiFileName}\"; filename*=UTF-8''{utf8FileName}");

                // 確保前端能讀到 header
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition, Content-Length, Content-Type");

                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return new JsonResult(returnData);
            }
        }



        /// <summary>
        /// 清理檔名，移除非法字元並將空白轉換為底線
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), "_");
            }
            return fileName.Replace(" ", "_");
        }
        /// <summary>
        /// 將字串轉為 ASCII（移除中文與特殊字元）
        /// </summary>
        public static string ToEnglishOrAscii(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // 1. 正規化字元（去掉重音符號）
            string normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark && c < 128)
                {
                    sb.Append(c);
                }
            }

            // 移除空白，避免 filename 有問題
            return sb.ToString().Replace(" ", "_");
        }
    }
}
