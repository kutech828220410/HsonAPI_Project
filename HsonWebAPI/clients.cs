using Basic;
using HsonAPILib; // 這裡引用你的 enum_clients, enum_client_contacts
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace HsonAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class clients : ControllerBase
    {
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

        /// <summary>
        /// 初始化 dbvm.clients 與 dbvm.client_contacts 資料表
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 會檢查並建立 `clients` 與 `client_contacts` 資料表  
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
                returnData.Result = $"初始化 clients 與 client_contacts 資料表完成";
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
        /// 批次新增或更新客戶與其聯絡人（使用 ClientGUID 為外鍵）
        /// </summary>
        /// <remarks>
        /// **JSON 範例**
        /// <code>
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "",
        ///       "name": "臺北市立聯合醫院",
        ///       "address": "台北市中正區xx路xx號",
        ///       "type": "政府機關",
        ///       "status": "啟用",
        ///       "notes": "優先客戶",
        ///       "contacts": [
        ///         {
        ///           "GUID": "",
        ///           "clientGUID": "",               // 可省略，後端會自動帶入對應客戶 GUID
        ///           "name": "王小明",
        ///           "phone": "02-1234-5678",
        ///           "email": "ming@example.com",
        ///           "title": "資訊課長",
        ///           "isPrimary": true,
        ///           "notes": "採購窗口"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("add_or_update_clients")]
        public string add_or_update_clients([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_or_update_clients";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 1) 反序列化
                List<clientClass> input = returnData.Data.ObjToClass<List<clientClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // 2) 取得資料庫設定
                List<sys_serverSettingClass> all = serverSetting.GetAllServerSetting();
                var conf = all.myFind("Main", "網頁", "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無 Server 資料";
                    return returnData.JsonSerializationt();
                }

                string clientsTable = new enum_clients().GetEnumDescription();
                string contactsTable = new enum_client_contacts().GetEnumDescription();

                var sqlClients = new SQLControl(conf.Server, conf.DBName, clientsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                var sqlContacts = new SQLControl(conf.Server, conf.DBName, contactsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                // 3) 確保 client_contacts 有 ClientGUID 欄位（舊表相容）
                EnsureContactHasClientGUID(sqlContacts, conf.DBName, contactsTable);

                string Now() => DateTime.Now.ToDateTimeString();
                string Esc(string s) => (s ?? "").Replace("'", "''");
                string BoolTo10(bool? b) => (b.HasValue && b.Value) ? "1" : "0";

                var messages = new List<string>();
                var output = new List<clientClass>();

                foreach (var cli in input)
                {
                    // ===== Upsert 客戶 =====
                    string now = Now();
                    string targetGuid = cli.GUID;

                    object[] row = null;

                    // 依 GUID 找
                    if (!targetGuid.StringIsEmpty())
                    {
                        var existed = sqlClients.GetRowsByDefult(null, (int)enum_clients.GUID, targetGuid);
                        if (existed.Count > 0) row = existed[0];
                        else targetGuid = ""; // 指到不存在 → 視同新建
                    }

                    // 若無 → 以 名稱+地址 比對
                    if (row == null)
                    {
                        if (!cli.名稱.StringIsEmpty() && !cli.地址.StringIsEmpty())
                        {
                            string sql = $@"
                                SELECT * FROM {conf.DBName}.{clientsTable}
                                WHERE 名稱 = '{Esc(cli.名稱)}'
                                  AND 地址 = '{Esc(cli.地址)}'
                                LIMIT 1";
                            var dt = sqlClients.WtrteCommandAndExecuteReader(sql);
                            if (dt.Rows.Count > 0) row = dt.Rows[0].ItemArray;
                        }
                    }

                    bool isNew = row == null;

                    if (isNew)
                    {
                        targetGuid = Guid.NewGuid().ToString();
                        var ins = new object[Enum.GetValues(typeof(enum_clients)).Length];
                        ins[(int)enum_clients.GUID] = targetGuid;
                        ins[(int)enum_clients.名稱] = cli.名稱 ?? "";
                        ins[(int)enum_clients.地址] = cli.地址 ?? "";
                        ins[(int)enum_clients.類型] = cli.類型 ?? "";
                        ins[(int)enum_clients.啟用狀態] = cli.啟用狀態 ?? "啟用";
                        ins[(int)enum_clients.備註] = cli.備註 ?? "";
                        ins[(int)enum_clients.建立時間] = now;
                        ins[(int)enum_clients.更新時間] = now;

                        sqlClients.AddRow(null, ins);
                        messages.Add($"新增客戶：{cli.名稱}");
                    }
                    else
                    {
                        targetGuid = row[(int)enum_clients.GUID].ObjectToString();

                        row[(int)enum_clients.名稱] = cli.名稱 ?? row[(int)enum_clients.名稱].ObjectToString();
                        row[(int)enum_clients.地址] = cli.地址 ?? row[(int)enum_clients.地址].ObjectToString();
                        row[(int)enum_clients.類型] = cli.類型 ?? row[(int)enum_clients.類型].ObjectToString();
                        row[(int)enum_clients.啟用狀態] = cli.啟用狀態 ?? row[(int)enum_clients.啟用狀態].ObjectToString();
                        row[(int)enum_clients.備註] = cli.備註 ?? row[(int)enum_clients.備註].ObjectToString();
                        // 建立時間保留
                        row[(int)enum_clients.更新時間] = now;

                        sqlClients.UpdateByDefulteExtra(null, row);
                        messages.Add($"更新客戶：{cli.名稱}（GUID={targetGuid}）");
                    }

                    // ===== Upsert 聯絡人（掛在 ClientGUID）=====
                    var outContacts = new List<client_contactClass>();

                    if (cli.聯絡人清單 != null && cli.聯絡人清單.Count > 0)
                    {
                        foreach (var ct in cli.聯絡人清單)
                        {
                            ct.ClientGUID = targetGuid; // 強制綁定上層客戶

                            string cGuid = ct.GUID;
                            object[] cRow = null;

                            // 先以聯絡人 GUID 找
                            if (!cGuid.StringIsEmpty())
                            {
                                var existed = sqlContacts.GetRowsByDefult(null, (int)enum_client_contacts.GUID, cGuid);
                                if (existed.Count > 0) cRow = existed[0];
                                else cGuid = "";
                            }

                            // 再以 (ClientGUID + 姓名 + 電子郵件) 匹配
                            if (cRow == null)
                            {
                                string sql = $@"
                                    SELECT * FROM {conf.DBName}.{contactsTable}
                                    WHERE ClientGUID = '{Esc(targetGuid)}'
                                      AND 姓名       = '{Esc(ct.姓名)}'
                                      AND 電子郵件   = '{Esc(ct.電子郵件)}'
                                    LIMIT 1";
                                var dt = sqlContacts.WtrteCommandAndExecuteReader(sql);
                                if (dt.Rows.Count > 0) cRow = dt.Rows[0].ItemArray;
                            }

                            string isPrimary = BoolTo10(ct.主要聯絡人);

                            if (cRow == null)
                            {
                                // 新建聯絡人
                                cGuid = Guid.NewGuid().ToString();
                                var insCt = new object[Enum.GetValues(typeof(enum_client_contacts)).Length];
                                insCt[(int)enum_client_contacts.GUID] = cGuid;
                                insCt[(int)enum_client_contacts.ClientGUID] = targetGuid;
                                insCt[(int)enum_client_contacts.姓名] = ct.姓名 ?? "";
                                insCt[(int)enum_client_contacts.電話] = ct.電話 ?? "";
                                insCt[(int)enum_client_contacts.電子郵件] = ct.電子郵件 ?? "";
                                insCt[(int)enum_client_contacts.職稱] = ct.職稱 ?? "";
                                insCt[(int)enum_client_contacts.主要聯絡人] = isPrimary;
                                insCt[(int)enum_client_contacts.備註] = ct.備註 ?? "";

                                sqlContacts.AddRow(null, insCt);
                                messages.Add($"新增聯絡人：{ct.姓名}（客戶={cli.名稱}）");
                            }
                            else
                            {
                                // 更新聯絡人
                                cGuid = cRow[(int)enum_client_contacts.GUID].ObjectToString();

                                cRow[(int)enum_client_contacts.ClientGUID] = targetGuid;
                                cRow[(int)enum_client_contacts.姓名] = ct.姓名 ?? cRow[(int)enum_client_contacts.姓名].ObjectToString();
                                cRow[(int)enum_client_contacts.電話] = ct.電話 ?? cRow[(int)enum_client_contacts.電話].ObjectToString();
                                cRow[(int)enum_client_contacts.電子郵件] = ct.電子郵件 ?? cRow[(int)enum_client_contacts.電子郵件].ObjectToString();
                                cRow[(int)enum_client_contacts.職稱] = ct.職稱 ?? cRow[(int)enum_client_contacts.職稱].ObjectToString();
                                cRow[(int)enum_client_contacts.主要聯絡人] = isPrimary;
                                cRow[(int)enum_client_contacts.備註] = ct.備註 ?? cRow[(int)enum_client_contacts.備註].ObjectToString();

                                sqlContacts.UpdateByDefulteExtra(null, cRow);
                                messages.Add($"更新聯絡人：{ct.姓名}（客戶={cli.名稱}）");
                            }

                            // 回填 GUID 與 ClientGUID
                            ct.GUID = cGuid;
                            ct.ClientGUID = targetGuid;
                            outContacts.Add(ct);
                        }
                    }

                    // 回填客戶資訊
                    cli.GUID = targetGuid;
                    cli.建立時間 = cli.建立時間.StringIsEmpty() ? now : cli.建立時間;
                    cli.更新時間 = now;
                    cli.聯絡人清單 = outContacts;

                    output.Add(cli);
                }

                returnData.Code = 200;
                returnData.Result = string.Join("；", messages);
                returnData.TimeTaken = $"{timer}";
                returnData.Data = output;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 取得客戶列表（支援關鍵字、類型、狀態、分頁；可選帶出聯絡人）
        /// </summary>
        /// <remarks>
        /// **JSON 範例**
        /// <code>
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "keyword=醫院",
        ///     "type=政府機關",
        ///     "status=啟用",
        ///     "includeContacts=true",
        ///     "page=1",
        ///     "pageSize=50"
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("get_clients")]
        public string get_clients([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_clients";

            try
            {
                // 1) 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string clientsTable = new enum_clients().GetEnumDescription();
                string contactsTable = new enum_client_contacts().GetEnumDescription();

                var sqlClients = new SQLControl(conf.Server, conf.DBName, clientsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                var sqlContacts = new SQLControl(conf.Server, conf.DBName, contactsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                // 2) 解析查詢參數
                string GetVal(string key) => returnData.ValueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];

                string keyword = GetVal("keyword") ?? "";
                string type = GetVal("type") ?? "";
                string status = GetVal("status") ?? "";
                bool includeContacts = (GetVal("includeContacts") ?? "false").ToLower() == "true";

                int page = (GetVal("page") ?? "1").StringToInt32();
                int pageSize = (GetVal("pageSize") ?? "50").StringToInt32();
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 50;

                string Esc(string s) => (s ?? "").Replace("'", "''");

                // 3) 組主要查詢（先算總數、再取當頁資料）
                string where = " WHERE 1=1 ";
                if (!keyword.StringIsEmpty())
                {
                    string k = Esc(keyword);
                    where += $" AND (名稱 LIKE '%{k}%' OR 地址 LIKE '%{k}%' OR 類型 LIKE '%{k}%') ";
                }
                if (!type.StringIsEmpty())
                {
                    where += $" AND 類型 = '{Esc(type)}' ";
                }
                if (!status.StringIsEmpty())
                {
                    where += $" AND 啟用狀態 = '{Esc(status)}' ";
                }

                // 總數
                string countSql = $@"SELECT COUNT(*) AS cnt FROM {conf.DBName}.{clientsTable} {where}";
                var dtCount = sqlClients.WtrteCommandAndExecuteReader(countSql);
                int total = dtCount.Rows[0]["cnt"].ToString().StringToInt32();

                // 分頁
                int offset = (page - 1) * pageSize;
                string mainSql = $@"
                    SELECT * 
                    FROM {conf.DBName}.{clientsTable}
                    {where}
                    ORDER BY 更新時間 DESC, 名稱 ASC
                    LIMIT {offset}, {pageSize}";
                var dtMain = sqlClients.WtrteCommandAndExecuteReader(mainSql);

                var clients = dtMain.DataTableToRowList().SQLToClass<clientClass, enum_clients>() ?? new List<clientClass>();

                // 4) 取聯絡人（一次性 IN 子查詢）
                if (clients.Count > 0)
                {
                    var guidList = clients.Select(c => c.GUID).Where(g => !g.StringIsEmpty()).Distinct().ToList();
                    string inList = string.Join(",", guidList.Select(g => $"'{Esc(g)}'"));

                    // 先撈數量
                    string cntSql = $@"
                        SELECT ClientGUID, COUNT(*) AS cnt
                        FROM {conf.DBName}.{contactsTable}
                        WHERE ClientGUID IN ({inList})
                        GROUP BY ClientGUID";
                    var dtCnt = sqlContacts.WtrteCommandAndExecuteReader(cntSql);
                    var cntMap = dtCnt.AsEnumerable()
                                      .ToDictionary(r => r["ClientGUID"].ObjectToString(),
                                                    r => r["cnt"].ToString().StringToInt32(),
                                                    StringComparer.OrdinalIgnoreCase);

                    // 需要完整清單時再撈
                    Dictionary<string, List<client_contactClass>> listMap = null;
                    if (includeContacts)
                    {
                        string listSql = $@"
                            SELECT *
                            FROM {conf.DBName}.{contactsTable}
                            WHERE ClientGUID IN ({inList})
                            ORDER BY 主要聯絡人 DESC, 姓名 ASC";
                        var dtList = sqlContacts.WtrteCommandAndExecuteReader(listSql);
                        var allContacts = dtList.DataTableToRowList().SQLToClass<client_contactClass, enum_client_contacts>() ?? new List<client_contactClass>();
                        listMap = allContacts.GroupBy(x => x.ClientGUID, StringComparer.OrdinalIgnoreCase)
                                             .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
                    }

                    // 回填到客戶物件
                    foreach (var c in clients)
                    {
                        int contactCount = cntMap.TryGetValue(c.GUID ?? "", out var cc) ? cc : 0;

                        if (includeContacts)
                        {
                            c.聯絡人清單 = listMap != null && listMap.TryGetValue(c.GUID ?? "", out var lst)
                                ? lst
                                : new List<client_contactClass>();
                        }
                        else
                        {
                            // 不帶清單，只用數量占位（可視需要留空）
                            c.聯絡人清單 = Enumerable.Range(0, contactCount).Select(_ => new client_contactClass()).ToList();
                        }
                    }
                }

                // 5) 回傳（含分頁資訊）
                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = new
                {
                    page,
                    pageSize,
                    total,
                    items = clients
                };
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
        /// 依 GUID 取得單一客戶（預設帶出聯絡人清單）
        /// </summary>
        /// <remarks>
        /// **JSON 範例**
        /// <code>
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [ "GUID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", "includeContacts=true" ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("get_by_guid")]
        public string get_by_guid([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_by_guid";

            try
            {
                // 1) 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string clientsTable = new enum_clients().GetEnumDescription();
                string contactsTable = new enum_client_contacts().GetEnumDescription();

                var sqlClients = new SQLControl(conf.Server, conf.DBName, clientsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                var sqlContacts = new SQLControl(conf.Server, conf.DBName, contactsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                // 2) 解析參數
                string guid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("GUID=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                bool includeContacts = ((returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("includeContacts=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1]) ?? "true").ToLower() == "true";

                if (guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 GUID 參數";
                    return returnData.JsonSerializationt();
                }

                string Esc(string s) => (s ?? "").Replace("'", "''");

                // 3) 撈客戶
                string sql = $@"SELECT * FROM {conf.DBName}.{clientsTable} WHERE GUID = '{Esc(guid)}' LIMIT 1";
                var dt = sqlClients.WtrteCommandAndExecuteReader(sql);
                if (dt.Rows.Count == 0)
                {
                    returnData.Code = 404;
                    returnData.Result = $"查無 GUID = {guid} 的客戶";
                    return returnData.JsonSerializationt();
                }
                var client = dt.DataTableToRowList().SQLToClass<clientClass, enum_clients>()[0];

                // 4) 撈聯絡人
                if (includeContacts)
                {
                    string sqlC = $@"
                        SELECT * FROM {conf.DBName}.{contactsTable}
                        WHERE ClientGUID = '{Esc(guid)}'
                        ORDER BY 主要聯絡人 DESC, 姓名 ASC";
                    var dtC = sqlContacts.WtrteCommandAndExecuteReader(sqlC);
                    client.聯絡人清單 = dtC.DataTableToRowList().SQLToClass<client_contactClass, enum_client_contacts>() ?? new List<client_contactClass>();
                }
                else
                {
                    string sqlCnt = $@"
                        SELECT COUNT(*) AS cnt FROM {conf.DBName}.{contactsTable}
                        WHERE ClientGUID = '{Esc(guid)}'";
                    var dtCnt = sqlContacts.WtrteCommandAndExecuteReader(sqlCnt);
                    int cnt = dtCnt.Rows[0]["cnt"].ToString().StringToInt32();
                    client.聯絡人清單 = Enumerable.Range(0, cnt).Select(_ => new client_contactClass()).ToList();
                }

                // 5) 回傳
                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = client;
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
        /// 批次刪除聯絡人
        /// </summary>
        /// <remarks>
        /// 支援兩種匹配刪除：
        /// 1) 以 GUID 直接刪除  
        /// 2) 以 ClientGUID + 姓名 + 電子郵件 匹配刪除  
        /// 
        /// **JSON 請求範例**
        /// <code>
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     { "GUID": "11111111-aaaa-bbbb-cccc-222222222222" },
        ///     { "ClientGUID": "99999999-xxxx-yyyy-zzzz-888888888888", "name": "王小明", "email": "ming@example.com" }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("delete_contacts")]
        public string delete_contacts([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_contacts";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 解析輸入
                List<client_contactClass> input = returnData.Data.ObjToClass<List<client_contactClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
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

                string contactsTable = new enum_client_contacts().GetEnumDescription();
                var sqlContacts = new SQLControl(conf.Server, conf.DBName, contactsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                // util
                string Esc(string s) => (s ?? "").Replace("'", "''");

                var toDelete = new List<object[]>();   // 直接要刪的資料列
                var deletedMsgs = new List<string>();  // 成功清單
                var notFound = new List<string>();   // 找不到清單

                foreach (var item in input)
                {
                    if (item == null)
                    {
                        notFound.Add("(空白輸入)");
                        continue;
                    }

                    // 優先用 GUID
                    if (!item.GUID.StringIsEmpty())
                    {
                        var rows = sqlContacts.GetRowsByDefult(null, (int)enum_client_contacts.GUID, item.GUID);
                        if (rows != null && rows.Count > 0)
                        {
                            toDelete.AddRange(rows);
                            deletedMsgs.Add(item.GUID);
                        }
                        else
                        {
                            notFound.Add($"GUID={item.GUID} 查無");
                        }
                        continue;
                    }

                    // 次之用 ClientGUID + 姓名 + 電子郵件
                    bool hasComposite =
                        !item.ClientGUID.StringIsEmpty() &&
                        !item.姓名.StringIsEmpty() &&
                        !item.電子郵件.StringIsEmpty();

                    if (hasComposite)
                    {
                        string sql = $@"
                            SELECT * 
                            FROM {conf.DBName}.{contactsTable}
                            WHERE ClientGUID = '{Esc(item.ClientGUID)}'
                              AND 姓名       = '{Esc(item.姓名)}'
                              AND 電子郵件   = '{Esc(item.電子郵件)}'";
                        var dt = sqlContacts.WtrteCommandAndExecuteReader(sql);
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow r in dt.Rows) toDelete.Add(r.ItemArray);
                            deletedMsgs.Add($"由組合鍵刪除: {item.姓名} <{item.電子郵件}> @{item.ClientGUID}");
                        }
                        else
                        {
                            notFound.Add($"組合鍵查無: {item.姓名} <{item.電子郵件}> @{item.ClientGUID}");
                        }
                        continue;
                    }

                    // 兩種方式都不符合
                    notFound.Add("(缺少 GUID 或 (ClientGUID+姓名+電子郵件))");
                }

                // 執行刪除
                if (toDelete.Count > 0)
                {
                    sqlContacts.DeleteExtra(contactsTable, toDelete);
                }

                returnData.Code = 200;
                returnData.Result = $"刪除完成：成功 {deletedMsgs.Count} 筆，失敗 {notFound.Count} 筆";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = new
                {
                    deleted = deletedMsgs,
                    not_found = notFound
                };
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
        /// 批次刪除客戶資料（可選擇是否一併刪除該客戶的所有聯絡人）
        /// </summary>
        /// <remarks>
        /// **匹配規則：**
        /// 1) 優先使用 GUID 刪除  
        /// 2) 若未提供 GUID，使用「名稱 + 地址」比對單筆刪除  
        /// 
        /// **ValueAry 參數：**
        /// - deleteContacts=true|false（預設 false）是否一併刪除聯絡人
        /// 
        /// **JSON 請求範例：**
        /// <code>
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": ["deleteContacts=true"],
        ///   "Data": [
        ///     { "GUID": "11111111-aaaa-bbbb-cccc-222222222222" },
        ///     { "name": "台北市立醫院", "address": "台北市信義區健康路88號" }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("delete_clients")]
        public string delete_clients([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_clients";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 解析輸入資料
                List<clientClass> input = returnData.Data.ObjToClass<List<clientClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // 解析參數：是否一併刪除聯絡人
                bool deleteContacts = (returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith("deleteContacts=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1] ?? "false").ToLower() == "true";

                // 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string clientsTable = new enum_clients().GetEnumDescription();
                string contactsTable = new enum_client_contacts().GetEnumDescription();

                var sqlClients = new SQLControl(conf.Server, conf.DBName, clientsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
                var sqlContacts = new SQLControl(conf.Server, conf.DBName, contactsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                // util
                string Esc(string s) => (s ?? "").Replace("'", "''");

                var toDeleteClientRows = new List<object[]>(); // 準備要刪除的客戶資料列
                var willDeleteGuids = new List<string>();    // 紀錄要刪的 GUID（後續刪聯絡人會用）
                var deletedMsgs = new List<string>();
                var notFound = new List<string>();

                foreach (var item in input)
                {
                    if (item == null)
                    {
                        notFound.Add("(空白輸入)");
                        continue;
                    }

                    // 1) 優先以 GUID 查
                    if (!item.GUID.StringIsEmpty())
                    {
                        var rows = sqlClients.GetRowsByDefult(null, (int)enum_clients.GUID, item.GUID);
                        if (rows != null && rows.Count > 0)
                        {
                            toDeleteClientRows.AddRange(rows);
                            willDeleteGuids.Add(item.GUID);
                            deletedMsgs.Add(item.GUID);
                        }
                        else
                        {
                            notFound.Add($"GUID={item.GUID} 查無");
                        }
                        continue;
                    }

                    // 2) 名稱 + 地址 匹配
                    bool hasComposite = !item.名稱.StringIsEmpty() && !item.地址.StringIsEmpty();
                    if (hasComposite)
                    {
                        string sql = $@"
                            SELECT * FROM {conf.DBName}.{clientsTable}
                            WHERE 名稱 = '{Esc(item.名稱)}'
                              AND 地址 = '{Esc(item.地址)}'
                            LIMIT 1";
                        var dt = sqlClients.WtrteCommandAndExecuteReader(sql);
                        if (dt.Rows.Count > 0)
                        {
                            var row = dt.Rows[0].ItemArray;
                            toDeleteClientRows.Add(row);
                            willDeleteGuids.Add(row[(int)enum_clients.GUID].ObjectToString());
                            deletedMsgs.Add($"name={item.名稱},address={item.地址}");
                        }
                        else
                        {
                            notFound.Add($"組合鍵查無：name={item.名稱},address={item.地址}");
                        }
                        continue;
                    }

                    notFound.Add("(缺少 GUID 或 (名稱+地址))");
                }

                // 先刪聯絡人（若指定級聯刪除）
                int deletedContactCount = 0;
                if (deleteContacts && willDeleteGuids.Count > 0)
                {
                    // IN (...) 打包一次刪除
                    string inList = string.Join(",", willDeleteGuids.Where(g => !g.StringIsEmpty())
                                                                    .Select(g => $"'{Esc(g)}'"));
                    if (!inList.StringIsEmpty())
                    {
                        // 先查出要刪的數量（回報用）
                        string cntSql = $@"SELECT COUNT(*) AS cnt
                                           FROM {conf.DBName}.{contactsTable}
                                           WHERE ClientGUID IN ({inList})";
                        var dtCnt = sqlContacts.WtrteCommandAndExecuteReader(cntSql);
                        deletedContactCount = dtCnt.Rows[0]["cnt"].ToString().StringToInt32();

                        // 真正刪除
                        string delSql = $@"DELETE FROM {conf.DBName}.{contactsTable}
                                           WHERE ClientGUID IN ({inList})";
                        sqlContacts.WtrteCommand(delSql);
                    }
                }

                // 再刪客戶
                if (toDeleteClientRows.Count > 0)
                {
                    sqlClients.DeleteExtra(clientsTable, toDeleteClientRows);
                }

                returnData.Code = 200;
                returnData.Result = $"刪除完成：客戶成功 {deletedMsgs.Count} 筆，失敗 {notFound.Count} 筆" +
                                    (deleteContacts ? $"；一併刪除聯絡人 {deletedContactCount} 筆" : "");
                returnData.TimeTaken = $"{timer}";
                returnData.Data = new
                {
                    deleted_clients = deletedMsgs,
                    not_found = notFound,
                    deleted_contacts = deleteContacts ? deletedContactCount : 0
                };
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
        /// 確保 client_contacts 具有 ClientGUID 欄位（外鍵），若無則自動新增
        /// </summary>
        private void EnsureContactHasClientGUID(SQLControl sqlContacts, string dbName, string contactsTable)
        {
            string checkSql = $@"
                SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = '{dbName.Replace("'", "''")}'
                  AND TABLE_NAME   = '{contactsTable.Replace("'", "''")}'
                  AND COLUMN_NAME  = 'ClientGUID'";
            var dt = sqlContacts.WtrteCommandAndExecuteReader(checkSql);
            if (dt.Rows.Count == 0)
            {
                string alter = $@"ALTER TABLE {dbName}.{contactsTable}
                                  ADD COLUMN ClientGUID VARCHAR(50) NULL COMMENT '對應 clients.GUID'";
                sqlContacts.WtrteCommand(alter);

                // 索引（部分版本不支援 IF NOT EXISTS，包 try）
                try
                {
                    string idx = $@"CREATE INDEX idx_{contactsTable}_ClientGUID
                                    ON {dbName}.{contactsTable}(ClientGUID)";
                    sqlContacts.WtrteCommand(idx);
                }
                catch { }
            }
        }
        /// <summary>
        /// 檢查並建立 clients 與 client_contacts 資料表
        /// </summary>
        private List<Table> CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            List<Table> tables = new List<Table>();
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_clients()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_client_contacts()));
            return tables;
        }
    }
}
