using Basic;
using HsonAPILib; // 這裡引用你的 enum_clients, enum_client_contacts
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

                var messages = new List<string>();
                var output = new List<clientClass>();

                foreach (var cli in input)
                {
                    string now = Now();
                    string targetGuid = cli.GUID;
                    object[] row = null;

                    // 依 GUID 找
                    if (!targetGuid.StringIsEmpty())
                    {
                        var existed = sqlClients.GetRowsByDefult(null, (int)enum_clients.GUID, targetGuid);
                        if (existed.Count > 0) row = existed[0];
                        else targetGuid = "";
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
                        row[(int)enum_clients.更新時間] = now;
                        sqlClients.UpdateByDefulteExtra(null, row);
                        messages.Add($"更新客戶：{cli.名稱}（GUID={targetGuid}）");
                    }

                    // ===== Upsert 聯絡人 =====
                    var outContacts = new List<client_contactClass>();

                    if (cli.聯絡人清單 != null && cli.聯絡人清單.Count > 0)
                    {
                        // 確保只有一位主要聯絡人
                        var primarySet = false;
                        foreach (var ct in cli.聯絡人清單)
                        {
                            if (ct.主要聯絡人 == "1")
                            {
                                if (!primarySet)
                                {
                                    primarySet = true;
                                }
                                else
                                {
                                    ct.主要聯絡人 = "0"; // 取消多餘的主要聯絡人
                                }
                            }
                        }

                        foreach (var ct in cli.聯絡人清單)
                        {
                            ct.ClientGUID = targetGuid;
                            string cGuid = ct.GUID;
                            object[] cRow = null;

                            // 以 GUID 找
                            if (!cGuid.StringIsEmpty())
                            {
                                var existed = sqlContacts.GetRowsByDefult(null, (int)enum_client_contacts.GUID, cGuid);
                                if (existed.Count > 0) cRow = existed[0];
                                else cGuid = "";
                            }

                            // 若無 GUID → 以姓名 + Email 比對
                            if (cRow == null)
                            {
                                string sql = $@"
                            SELECT * FROM {conf.DBName}.{contactsTable}
                            WHERE ClientGUID = '{Esc(targetGuid)}'
                              AND 姓名 = '{Esc(ct.姓名)}'
                              AND 電子郵件 = '{Esc(ct.電子郵件)}'
                            LIMIT 1";
                                var dt = sqlContacts.WtrteCommandAndExecuteReader(sql);
                                if (dt.Rows.Count > 0) cRow = dt.Rows[0].ItemArray;
                            }

                            if (cRow == null)
                            {
                                // 新增
                                cGuid = Guid.NewGuid().ToString();
                                var insCt = new object[Enum.GetValues(typeof(enum_client_contacts)).Length];
                                insCt[(int)enum_client_contacts.GUID] = cGuid;
                                insCt[(int)enum_client_contacts.ClientGUID] = targetGuid;
                                insCt[(int)enum_client_contacts.姓名] = ct.姓名 ?? "";
                                insCt[(int)enum_client_contacts.電話] = ct.電話 ?? "";
                                insCt[(int)enum_client_contacts.電子郵件] = ct.電子郵件 ?? "";
                                insCt[(int)enum_client_contacts.職稱] = ct.職稱 ?? "";
                                insCt[(int)enum_client_contacts.主要聯絡人] = ct.主要聯絡人;
                                insCt[(int)enum_client_contacts.備註] = ct.備註 ?? "";
                                sqlContacts.AddRow(null, insCt);
                                messages.Add($"新增聯絡人：{ct.姓名}（客戶={cli.名稱}）");
                            }
                            else
                            {
                                // 更新
                                cGuid = cRow[(int)enum_client_contacts.GUID].ObjectToString();
                                cRow[(int)enum_client_contacts.ClientGUID] = targetGuid;
                                cRow[(int)enum_client_contacts.姓名] = ct.姓名 ?? cRow[(int)enum_client_contacts.姓名].ObjectToString();
                                cRow[(int)enum_client_contacts.電話] = ct.電話 ?? cRow[(int)enum_client_contacts.電話].ObjectToString();
                                cRow[(int)enum_client_contacts.電子郵件] = ct.電子郵件 ?? cRow[(int)enum_client_contacts.電子郵件].ObjectToString();
                                cRow[(int)enum_client_contacts.職稱] = ct.職稱 ?? cRow[(int)enum_client_contacts.職稱].ObjectToString();
                                cRow[(int)enum_client_contacts.主要聯絡人] = ct.主要聯絡人;
                                cRow[(int)enum_client_contacts.備註] = ct.備註 ?? cRow[(int)enum_client_contacts.備註].ObjectToString();
                                sqlContacts.UpdateByDefulteExtra(null, cRow);
                                messages.Add($"更新聯絡人：{ct.姓名}（客戶={cli.名稱}）");
                            }

                            ct.GUID = cGuid;
                            outContacts.Add(ct);
                        }

                        // ===== 刪除未傳入的聯絡人 =====
                        var existingContacts = sqlContacts.GetRowsByDefult(null, (int)enum_client_contacts.ClientGUID, targetGuid);
                        var incomingGuids = outContacts.Select(c => c.GUID).ToList();
                        foreach (var ec in existingContacts)
                        {
                            string ecGuid = ec[(int)enum_client_contacts.GUID].ObjectToString();
                            if (!incomingGuids.Contains(ecGuid))
                            {
                                sqlContacts.DeleteExtra(null, ec);
                                messages.Add($"刪除多餘聯絡人（客戶={cli.名稱}）：{ec[(int)enum_client_contacts.姓名]}");
                            }
                        }
                    }

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
        /// 此 API 用於查詢客戶清單並支援多條件濾器與分頁，系統會依據 <c>keyword</c>、<c>type</c>、<c>status</c> 進行過濾，
        /// 可透過 <c>includeContacts</c> 控制是否同時帶出各客戶的聯絡人清單。
        ///
        /// <b>邏輯流程：</b>
        /// 1. 驗證 ServerName / ServerType 並載入對應資料庫設定。  
        /// 2. 解析 <c>ValueAry</c> 參數（<c>key=value</c> 形式）：<c>keyword</c>、<c>type</c>、<c>status</c>、<c>includeContacts</c>、<c>page</c>、<c>pageSize</c>。  
        /// 3. 依條件組合查詢（<c>名稱/地址/類型</c> 模糊比對，類型與狀態為等值比對）。  
        /// 4. 以 <c>更新時間 DESC, 名稱 ASC</c> 排序後，依 <c>page</c>/<c>pageSize</c> 回傳當頁資料。  
        /// 5. 若 <c>includeContacts=true</c>，則批次撈取所有當頁客戶之聯絡人並依 <c>ClientGUID</c> 映射至各客戶。  
        ///
        /// <b>注意事項：</b>
        /// - <c>page</c> 最小值為 1；<c>pageSize</c> 最小值為 1；未提供時預設 <c>page=1</c>、<c>pageSize=50</c>。  
        /// - <c>keyword</c> 會對 <c>名稱</c>、<c>地址</c>、<c>類型</c> 做模糊比對。  
        /// - <c>includeContacts</c> 未提供時預設為 <c>false</c>。  
        /// - 僅回傳當頁資料；若需總筆數或總頁數，請於前端或另一路徑補充查詢。  
        ///
        /// <b>JSON 請求範例（英文化欄位）：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "Web",
        ///   "ValueAry": [
        ///     "keyword=Hospital",
        ///     "type=Government",
        ///     "status=Enabled",
        ///     "includeContacts=true",
        ///     "page=1",
        ///     "pageSize=50"
        ///   ]
        /// }
        /// ```
        /// ]]>
        ///
        /// <b>JSON 回應範例（英文化欄位；示意）：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_clients",
        ///   "Result": "OK",
        ///   "TimeTaken": "32.8ms",
        ///   "Data": [{
        ///     "guid": "f9b0a8d8-0fd9-4a3e-9e07-1c9e4f2f1a23",
        ///     "name": "Taipei General Hospital",
        ///     "type": "Government",
        ///     "status": "Enabled",
        ///     "phone": "02-1234-5678",
        ///     "email": "contact@tgh.gov.tw",
        ///     "address": "No. 1, Sec. 1, Renai Rd, Taipei",
        ///     "notes": "Key account",
        ///     "updatedAt": "2025-08-14T09:30:25",
        ///     "contacts": [{
        ///       "guid": "0ce9e1b2-7a2e-45a1-8c6b-4e2d9f8c1a77",
        ///       "ClientGUID": "f9b0a8d8-0fd9-4a3e-9e07-1c9e4f2f1a23",
        ///       "name": "Dr. Chen",
        ///       "title": "Pharmacy Director",
        ///       "phone": "02-2222-3333",
        ///       "email": "dr.chen@tgh.gov.tw",
        ///       "isPrimary": "1"
        ///     }]
        ///   }]
        /// }
        /// ```
        /// ]]>
        ///
        /// <b>錯誤回應範例：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_clients",
        ///   "Result": "找不到 Server 設定",
        ///   "TimeTaken": "1.3ms",
        ///   "Data": null
        /// }
        /// ```
        /// ]]>
        ///
        /// </remarks>
        /// <param name="returnData">
        /// 請求物件：
        /// - <c>ServerName</c>：伺服器名稱。  
        /// - <c>ServerType</c>：伺服器類型。  
        /// - <c>ValueAry</c>：查詢參數陣列（<c>key=value</c> 形式）：  
        ///   - <c>keyword</c>（string，可空）  
        ///   - <c>type</c>（string，可空）  
        ///   - <c>status</c>（string，可空）  
        ///   - <c>includeContacts</c>（bool，預設 false）  
        ///   - <c>page</c>（int，預設 1）  
        ///   - <c>pageSize</c>（int，預設 50）  
        /// </param>
        /// <returns>回傳序列化後的 JSON 字串；<c>Data</c> 為當頁客戶清單（可含聯絡人）。</returns>
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

                var sqlClients = new SQLControl(conf.Server, conf.DBName, clientsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);
                var sqlContacts = new SQLControl(conf.Server, conf.DBName, contactsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

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

                // 3) 查詢客戶資料
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

                int offset = (page - 1) * pageSize;
                string mainSql = $@"
            SELECT * 
            FROM {conf.DBName}.{clientsTable}
            {where}
            ORDER BY 更新時間 DESC, 名稱 ASC
            LIMIT {offset}, {pageSize}";
                var dtMain = sqlClients.WtrteCommandAndExecuteReader(mainSql);
                var clients = dtMain.DataTableToRowList().SQLToClass<clientClass, enum_clients>() ?? new List<clientClass>();

                // 4) 聯絡人
                if (clients.Count > 0)
                {
                    var guidList = clients.Select(c => c.GUID).Where(g => !g.StringIsEmpty()).Distinct().ToList();
                    string inList = string.Join(",", guidList.Select(g => $"'{Esc(g)}'"));

                    if (includeContacts)
                    {
                        string listSql = $@"
                    SELECT *
                    FROM {conf.DBName}.{contactsTable}
                    WHERE ClientGUID IN ({inList})
                    ORDER BY 主要聯絡人 DESC, 姓名 ASC";
                        var dtList = sqlContacts.WtrteCommandAndExecuteReader(listSql);
                        var list_value = dtList.DataTableToRowList();
                        var allContacts = list_value.SQLToClass<client_contactClass, enum_client_contacts>();

                        var listMap = allContacts.GroupBy(x => x.ClientGUID, StringComparer.OrdinalIgnoreCase)
                                                 .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                        foreach (var c in clients)
                        {
                            c.聯絡人清單 = listMap.ContainsKey(c.GUID) ? listMap[c.GUID] : new List<client_contactClass>();
                        }
                    }
                }

                // 5) 回傳
                returnData.Code = 200;
                returnData.Result = "OK";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = clients;
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
        /// 依 GUID 取得單一客戶資料（可選擇是否帶出聯絡人）
        /// </summary>
        /// <remarks>
        /// 此 API 用於依指定 <c>GUID</c> 取得單一客戶的完整資料，並可透過 <c>includeContacts</c> 控制是否同時帶出該客戶的聯絡人清單。
        ///
        /// <para><b>Endpoint：</b><c>/api/clients/get_by_guid</c></para>
        /// <para><b>Method：</b><c>POST</c>（Body: JSON）</para>
        /// <para><b>Content-Type：</b><c>application/json; charset=utf-8</c></para>
        ///
        /// <b>邏輯流程：</b>
        /// 1. 驗證並載入 <c>ServerName</c>、<c>ServerType</c> 對應的資料庫設定；若不存在，回傳 <c>Code=-200</c>。  
        /// 2. 解析 <c>ValueAry</c>：讀取 <c>GUID</c> 與 <c>includeContacts</c>（預設 <c>true</c>）。  
        /// 3. 以 <c>GUID</c> 查詢客戶主檔，若查無資料，回傳 <c>Code=404</c>。  
        /// 4. 若 <c>includeContacts=true</c>，則查詢該客戶的聯絡人清單並依「主要聯絡人 DESC、姓名 ASC」排序。  
        /// 5. 若 <c>includeContacts=false</c>，僅查詢聯絡人筆數並建立等量的空白占位物件於 <c>contacts</c>（供前端顯示數量用途）。  
        ///
        /// <b>注意事項：</b>
        /// - <c>GUID</c> 為必要參數，缺少時回傳 <c>Code=-200</c>。  
        /// - <c>includeContacts</c> 未提供時預設為 <c>true</c>。  
        /// - 聯絡人回傳順序：主要聯絡人優先，其次依姓名排序。  
        ///
        /// <b>JSON 請求範例（英文化欄位）：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "Web",
        ///   "ValueAry": [
        ///     "GUID=f9b0a8d8-0fd9-4a3e-9e07-1c9e4f2f1a23",
        ///     "includeContacts=true"
        ///   ]
        /// }
        /// ```
        /// ]]>
        ///
        /// <b>JSON 回應範例（含聯絡人，英文化欄位；示意）：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_by_guid",
        ///   "Result": "OK",
        ///   "TimeTaken": "15.7ms",
        ///   "Data": {
        ///     "guid": "f9b0a8d8-0fd9-4a3e-9e07-1c9e4f2f1a23",
        ///     "name": "Taipei General Hospital",
        ///     "type": "Government",
        ///     "status": "Enabled",
        ///     "phone": "02-1234-5678",
        ///     "email": "contact@tgh.gov.tw",
        ///     "address": "No. 1, Sec. 1, Renai Rd, Taipei",
        ///     "notes": "Key account",
        ///     "updatedAt": "2025-08-14T09:30:25",
        ///     "contacts": [
        ///       {
        ///         "guid": "0ce9e1b2-7a2e-45a1-8c6b-4e2d9f8c1a77",
        ///         "ClientGUID": "f9b0a8d8-0fd9-4a3e-9e07-1c9e4f2f1a23",
        ///         "name": "Dr. Chen",
        ///         "title": "Pharmacy Director",
        ///         "phone": "02-2222-3333",
        ///         "email": "dr.chen@tgh.gov.tw",
        ///         "isPrimary": "1"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        /// ]]>
        ///
        /// <b>JSON 回應範例（不含聯絡人明細；僅占位數量，英文化欄位；示意）：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_by_guid",
        ///   "Result": "OK",
        ///   "TimeTaken": "11.2ms",
        ///   "Data": {
        ///     "guid": "f9b0a8d8-0fd9-4a3e-9e07-1c9e4f2f1a23",
        ///     "name": "Taipei General Hospital",
        ///     "type": "Government",
        ///     "status": "Enabled",
        ///     "phone": "02-1234-5678",
        ///     "email": "contact@tgh.gov.tw",
        ///     "address": "No. 1, Sec. 1, Renai Rd, Taipei",
        ///     "notes": "Key account",
        ///     "updatedAt": "2025-08-14T09:30:25",
        ///     "contacts": [{}, {}, {}]
        ///   }
        /// }
        /// ```
        /// ]]>
        ///
        /// <b>錯誤回應範例：</b>
        /// <![CDATA[
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_by_guid",
        ///   "Result": "缺少 GUID 參數",
        ///   "TimeTaken": "0.6ms",
        ///   "Data": null
        /// }
        /// ```
        /// ```json
        /// {
        ///   "Code": 404,
        ///   "Method": "get_by_guid",
        ///   "Result": "查無 GUID = f9b0a8d8-0fd9-4a3e-9e07-1c9e4f2f1a23 的客戶",
        ///   "TimeTaken": "1.1ms",
        ///   "Data": null
        /// }
        /// ```
        /// ]]>
        ///
        /// </remarks>
        /// <param name="returnData">
        /// 請求物件：
        /// - <c>ServerName</c>：伺服器名稱。  
        /// - <c>ServerType</c>：伺服器類型。  
        /// - <c>ValueAry</c>：參數陣列（<c>key=value</c> 形式）：  
        ///   - <c>GUID</c>（string，必要）  
        ///   - <c>includeContacts</c>（bool，選填；預設 <c>true</c>）  
        /// </param>
        /// <returns>回傳序列化後的 JSON 字串；<c>Data</c> 為單一客戶物件（依設定可含聯絡人清單）。</returns>
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
        /// 2) 若未提供 GUID，使用「名稱」比對單筆刪除  
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
        ///     { "name": "台北市立醫院" }
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

                string Esc(string s) => (s ?? "").Replace("'", "''");

                var toDeleteClientRows = new List<object[]>();
                var willDeleteGuids = new List<string>();
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
                            deletedMsgs.Add($"GUID={item.GUID}");
                        }
                        else
                        {
                            notFound.Add($"GUID={item.GUID} 查無");
                        }
                        continue;
                    }

                    // 2) 單純用名稱比對
                    if (!item.名稱.StringIsEmpty())
                    {
                        string sql = $@"
                    SELECT * FROM {conf.DBName}.{clientsTable}
                    WHERE 名稱 = '{Esc(item.名稱)}'
                    LIMIT 1";
                        var dt = sqlClients.WtrteCommandAndExecuteReader(sql);
                        if (dt.Rows.Count > 0)
                        {
                            var row = dt.Rows[0].ItemArray;
                            toDeleteClientRows.Add(row);
                            willDeleteGuids.Add(row[(int)enum_clients.GUID].ObjectToString());
                            deletedMsgs.Add($"name={item.名稱}");
                        }
                        else
                        {
                            notFound.Add($"名稱查無：name={item.名稱}");
                        }
                        continue;
                    }

                    notFound.Add("(缺少 GUID 或 名稱)");
                }

                // 刪聯絡人
                int deletedContactCount = 0;
                if (deleteContacts && willDeleteGuids.Count > 0)
                {
                    string inList = string.Join(",", willDeleteGuids.Where(g => !g.StringIsEmpty())
                                                                    .Select(g => $"'{Esc(g)}'"));
                    if (!inList.StringIsEmpty())
                    {
                        string cntSql = $@"SELECT COUNT(*) AS cnt
                                   FROM {conf.DBName}.{contactsTable}
                                   WHERE ClientGUID IN ({inList})";
                        var dtCnt = sqlContacts.WtrteCommandAndExecuteReader(cntSql);
                        deletedContactCount = dtCnt.Rows[0]["cnt"].ToString().StringToInt32();

                        string delSql = $@"DELETE FROM {conf.DBName}.{contactsTable}
                                   WHERE ClientGUID IN ({inList})";
                        sqlContacts.WtrteCommand(delSql);
                    }
                }

                // 刪客戶
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
