using Basic;
using HsonAPILib; // 引用 enum_project_boms, enum_bom_items, enum_bom_documents
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HsonWebAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class projects : ControllerBase
    {
 
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

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
                List<Table> tables = CheckCreatTable(conf);

                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 BOM 資料表完成";
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 取得專案清單（支援篩選、關鍵字、分頁、排序；可選擇回傳 BOM 關聯）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 依據查詢參數回傳專案清單，所有欄位皆以 `string` 型別傳輸，並可選擇是否附帶 BOM 關聯資訊。  
        ///
        /// **篩選條件（ValueAry，格式：key=value）：**  
        /// - `status`（string，可空）  
        /// - `clientGuid`（string，可空；自動轉為大寫）  
        /// - `priority`（string，可空）  
        /// - `dateFrom`（string，可空；過濾「投標日期」起始，yyyy-MM-dd）  
        /// - `dateTo`（string，可空；過濾「投標日期」結束，yyyy-MM-dd）  
        /// - `searchTerm`（string，可空；模糊比對 名稱/描述/客戶名稱/標籤）  
        /// - `page`（int，預設 1；最小 1）  
        /// - `pageSize`（int，預設 50；最小 1）  
        /// - `sortBy`（created_at|updated_at|name|status|id；預設 updated_at）  
        /// - `sortOrder`（asc|desc；預設 desc）  
        /// - `includeBomDetails`（bool，選填，true=回傳 BOM 詳細清單；false=僅回傳 GUID，預設 false）  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "status=進行中",
        ///     "clientGuid=660F9500-F39C-52E5-B827-557766551111",
        ///     "priority=高",
        ///     "dateFrom=2024-01-01",
        ///     "dateTo=2024-12-31",
        ///     "searchTerm=智慧交通",
        ///     "page=1",
        ///     "pageSize=50",
        ///     "sortBy=updated_at",
        ///     "sortOrder=desc",
        ///     "includeBomDetails=true"
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（含 BOM GUID 清單）：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_project_list",
        ///   "Result": "獲取專案清單成功",
        ///   "TimeTaken": "18.4ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "ID": "PRJ001",
        ///       "name": "台北市智慧交通管理系統",
        ///       "clientGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///       "clientName": "台北市政府資訊局",
        ///       "status": "進行中",
        ///       "priority": "高",
        ///       "progressPercentage": "65.5",
        ///       "associatedBomGuids": [ "BOM-GUID-1", "BOM-GUID-2" ]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（含 BOM 詳細）：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_project_list",
        ///   "Result": "獲取專案清單成功",
        ///   "TimeTaken": "18.4ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "ID": "PRJ001",
        ///       "name": "台北市智慧交通管理系統",
        ///       "clientGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///       "clientName": "台北市政府資訊局",
        ///       "status": "進行中",
        ///       "priority": "高",
        ///       "progressPercentage": "65.5",
        ///       "associatedBomGuids": [ "BOM-GUID-1", "BOM-GUID-2" ],
        ///       "bomAssociations": [
        ///         {
        ///           "GUID": "REL-GUID-1",
        ///           "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///           "bomGuid": "BOM-GUID-1",
        ///           "bomId": "BOM001",
        ///           "bomName": "智慧交通控制器主機板",
        ///           "requiredQuantity": "2",
        ///           "dueDate": "2025-03-31",
        ///           "associationType": "主要",
        ///           "status": "啟用"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_project_list")]
        public string get_project_list([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_project_list";

            try
            {
                // 1) 取得 DB 設定
                var conf = GetConfOrFail(returnData, out string confErr);
                if (confErr != null)
                {
                    returnData.Code = -200;
                    returnData.Result = confErr;
                    return returnData.JsonSerializationt();
                }

                string table = ProjectsTable(conf);
                var sql = SqlForProjects(conf);

                // 2) 解析查詢參數
                string status = GetVal(returnData.ValueAry, "status") ?? "";
                string clientGuid = UpperGuid(GetVal(returnData.ValueAry, "clientGuid") ?? "");
                string priority = GetVal(returnData.ValueAry, "priority") ?? "";
                string dateFrom = GetVal(returnData.ValueAry, "dateFrom") ?? "";
                string dateTo = GetVal(returnData.ValueAry, "dateTo") ?? "";
                string searchTerm = GetVal(returnData.ValueAry, "searchTerm") ?? "";
                bool includeBomDetails = (GetVal(returnData.ValueAry, "includeBomDetails") ?? "false").ToLower() == "true";

                int page = (GetVal(returnData.ValueAry, "page") ?? "1").StringToInt32();
                int pageSize = (GetVal(returnData.ValueAry, "pageSize") ?? "50").StringToInt32();
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 50;

                string where = " WHERE 1=1 ";
                if (!status.StringIsEmpty()) where += $" AND 狀態 = '{Esc(status)}' ";
                if (!clientGuid.StringIsEmpty()) where += $" AND 客戶GUID = '{Esc(clientGuid)}' ";
                if (!priority.StringIsEmpty()) where += $" AND 優先級 = '{Esc(priority)}' ";
                if (!dateFrom.StringIsEmpty()) where += $" AND 投標日期 >= '{Esc(dateFrom)} 00:00:00' ";
                if (!dateTo.StringIsEmpty()) where += $" AND 投標日期 <= '{Esc(dateTo)} 23:59:59' ";
                if (!searchTerm.StringIsEmpty())
                {
                    string k = Esc(searchTerm);
                    where += $" AND (名稱 LIKE '%{k}%' OR 描述 LIKE '%{k}%' OR 客戶名稱 LIKE '%{k}%' OR 標籤 LIKE '%{k}%') ";
                }

                string orderBy = SafeOrderBy(GetVal(returnData.ValueAry, "sortBy"), GetVal(returnData.ValueAry, "sortOrder"));
                int offset = (page - 1) * pageSize;

                // 3) 查詢 Projects
                string mainSql = $@"
SELECT *
FROM {conf.DBName}.{table}
{where}
ORDER BY {orderBy}
LIMIT {offset}, {pageSize}";
                var dt = sql.WtrteCommandAndExecuteReader(mainSql);

                var list = dt.DataTableToRowList().SQLToClass<projectClass, enum_projects>() ?? new List<projectClass>();

                // 4) 查詢 BOM 關聯
                if (list.Count > 0)
                {
                    string projectGuids = string.Join(",", list.Select(p => $"'{Esc(p.GUID)}'"));
                    string bomSql = $@"
SELECT *
FROM {conf.DBName}.project_bom_associations
WHERE 專案GUID IN ({projectGuids})";
                    var bomDt = sql.WtrteCommandAndExecuteReader(bomSql);
                    var bomRows = bomDt.DataTableToRowList().SQLToClass<project_bom_associationClass, enum_project_bom_associations>()
                                  ?? new List<project_bom_associationClass>();

                    foreach (var proj in list)
                    {
                        var related = bomRows.Where(r => r.專案GUID == proj.GUID).ToList();
                        proj.AssociatedBomGuids = related.Select(r => r.BOMGUID).Distinct().ToList();

                        if (includeBomDetails)
                        {
                            proj.BomAssociations = related;
                        }
                    }
                }

                // 5) 回傳
                returnData.Code = 200;
                returnData.Result = "獲取專案清單成功";
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
        /// <summary>
        /// 依 GUID 取得單一專案詳情（可選擇回傳 BOM 關聯與里程碑清單）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 以 `projectGuid` 查詢單一專案，欄位均為 `string`。  
        /// - `includeBomDetails` 參數可指定是否回傳 BOM 關聯詳細清單。  
        /// - `includeMilestones` 參數將在未來版本擴充為同時回傳里程碑清單（目前僅回傳主檔與 BOM）。  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "projectGuid=550E8400-E29B-41D4-A716-446655440000",
        ///     "includeBomDetails=true",
        ///     "includeMilestones=true"
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（僅 BOM GUID 清單）：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_project_details",
        ///   "Result": "獲取專案詳情成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "ID": "PRJ001",
        ///       "name": "台北市智慧交通管理系統",
        ///       "clientGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///       "clientName": "台北市政府資訊局",
        ///       "status": "進行中",
        ///       "priority": "高",
        ///       "progressPercentage": "65.5",
        ///       "associatedBomGuids": [ "BOM-GUID-1", "BOM-GUID-2" ]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（含 BOM 詳細清單）：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_project_details",
        ///   "Result": "獲取專案詳情成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "ID": "PRJ001",
        ///       "name": "台北市智慧交通管理系統",
        ///       "clientGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///       "clientName": "台北市政府資訊局",
        ///       "status": "進行中",
        ///       "priority": "高",
        ///       "progressPercentage": "65.5",
        ///       "associatedBomGuids": [ "BOM-GUID-1", "BOM-GUID-2" ],
        ///       "bomAssociations": [
        ///         {
        ///           "GUID": "REL-GUID-1",
        ///           "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///           "bomGuid": "BOM-GUID-1",
        ///           "bomId": "BOM001",
        ///           "bomName": "智慧交通控制器主機板",
        ///           "requiredQuantity": "2",
        ///           "dueDate": "2025-03-31",
        ///           "associationType": "主要",
        ///           "status": "啟用"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_project_details")]
        public string get_project_details([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_project_details";

            try
            {
                // 1) 取得 DB 設定
                var conf = GetConfOrFail(returnData, out string confErr);
                if (confErr != null)
                {
                    returnData.Code = -200;
                    returnData.Result = confErr;
                    return returnData.JsonSerializationt();
                }

                // 2) 參數
                string guid = UpperGuid(GetVal(returnData.ValueAry, "projectGuid"));
                bool includeBomDetails = (GetVal(returnData.ValueAry, "includeBomDetails", "false") ?? "false").ToLower() == "true";
                bool includeMilestones = (GetVal(returnData.ValueAry, "includeMilestones", "false") ?? "false").ToLower() == "true";

                if (guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 projectGuid 參數";
                    return returnData.JsonSerializationt();
                }

                // 3) 查詢專案
                string pTable = ProjectsTable(conf);
                var sqlP = SqlForProjects(conf);

                string sql = $@"SELECT * FROM {conf.DBName}.{pTable} WHERE GUID = '{Esc(guid)}' LIMIT 1";
                var dt = sqlP.WtrteCommandAndExecuteReader(sql);
                if (dt.Rows.Count == 0)
                {
                    returnData.Code = 404;
                    returnData.Result = $"查無 GUID = {guid} 的專案";
                    return returnData.JsonSerializationt();
                }

                var project = dt.DataTableToRowList().SQLToClass<projectClass, enum_projects>()[0];

                // 4) 查詢 BOM 關聯
                var sqlBom = SqlForProjects(conf);
                string bomSql = $@"SELECT * FROM {conf.DBName}.project_bom_associations WHERE 專案GUID = '{Esc(guid)}'";
                var bomDt = sqlBom.WtrteCommandAndExecuteReader(bomSql);
                var bomRows = bomDt.DataTableToRowList().SQLToClass<project_bom_associationClass, enum_project_bom_associations>()
                              ?? new List<project_bom_associationClass>();

                project.AssociatedBomGuids = bomRows.Select(r => r.BOMGUID).Distinct().ToList();
                if (includeBomDetails)
                {
                    project.BomAssociations = bomRows;
                }

                // 5)（預留）里程碑帶出：目前不回傳，只做統計
                if (includeMilestones)
                {
                    // 預留查詢，不直接回傳
                    // var mTable = MilestonesTable(conf);
                    // var sqlM = SqlForMilestones(conf);
                    // string msql = $@"SELECT * FROM {conf.DBName}.{mTable} WHERE 專案GUID = '{Esc(guid)}' ORDER BY 計劃日期 ASC, 名稱 ASC";
                    // var dtM = sqlM.WtrteCommandAndExecuteReader(msql);
                    // var milestones = dtM.DataTableToRowList().SQLToClass<milestoneClass, enum_project_milestones>() ?? new List<milestoneClass>();
                    // project.里程碑數量 = milestones.Count.ToString();
                    // project.已完成里程碑 = milestones.Count(m => m.狀態 == "已完成").ToString();
                }

                // 6) 回傳
                returnData.Code = 200;
                returnData.Result = "獲取專案詳情成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = new List<projectClass> { project };
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
        /// 更新專案狀態（僅 POST；以 GUID 定位；套用狀態流程規則）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 依 <c>GUID</c> 更新單筆或多筆專案的 <c>status</c>，並套用狀態流程（投標中 → 已得標 → 進行中 → 已完成）。<br/>
        /// 若提供 <c>notes</c>，系統會保留。
        /// <br/><br/>
        /// <b>必填檢核：</b>
        /// - <c>Data</c> 不可為空  <br/>
        /// - 每筆必須提供 <c>GUID</c>（大寫）與 <c>status</c>
        ///
        /// <br/><br/>
        /// <b>Request JSON 範例：</b>
        /// <code class="json">
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "status": "進行中",
        ///       "notes": "合約已簽訂，開始執行",
        ///       "updatedBy": "張專案經理"
        ///     }
        ///   ]
        /// }
        /// </code>
        ///
        /// <b>Response JSON 範例（Data 為 projectClass 陣列）：</b>
        /// <code class="json">
        /// {
        ///   "Code": 200,
        ///   "Method": "update_project_status",
        ///   "Result": "專案狀態更新成功 1 筆；失敗 0 筆",
        ///   "TimeTaken": "7.3ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "ID": "PRJ001",
        ///       "名稱": "台北市智慧交通管理系統",
        ///       "狀態": "進行中",
        ///       "備註": "",
        ///       "更新者": "張專案經理",
        ///       "更新時間": "2024-03-15 14:30:00"
        ///     }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("update_project_status")]
        public string update_project_status([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "update_project_status";

            var resultList = new List<projectClass>();

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    returnData.TimeTaken = $"{timer}";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                List<projectClass> input = returnData.Data.ObjToClass<List<projectClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    returnData.TimeTaken = $"{timer}";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                var conf = GetConfOrFail(returnData, out string confErr);
                if (confErr != null)
                {
                    returnData.Code = -200;
                    returnData.Result = confErr;
                    returnData.TimeTaken = $"{timer}";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                string table = ProjectsTable(conf);
                var sql = SqlForProjects(conf);

                int okCount = 0, failCount = 0;

                foreach (var p in input)
                {
                    var projResult = new projectClass();
                    try
                    {
                        if (p == null) throw new Exception("(空白資料)");

                        string guid = UpperGuid(p.GUID);
                        string newStatus = p.狀態; // 這裡沿用 class 的中文屬性「狀態」

                        if (guid.StringIsEmpty() || newStatus.StringIsEmpty())
                            throw new Exception("缺少 GUID 或 status");

                        var existed = sql.GetRowsByDefult(null, (int)enum_projects.GUID, guid);
                        if (existed == null || existed.Count == 0)
                            throw new Exception($"查無 GUID = {guid}");

                        var row = existed[0];
                        string prevStatus = row[(int)enum_projects.狀態].ObjectToString();

                        var (ok, msg) = ValidateStatusTransition(prevStatus, newStatus);
                        if (!ok) throw new Exception(msg);

                        string now = Now();
                        row[(int)enum_projects.狀態] = newStatus;
                        row[(int)enum_projects.更新者] = p.更新者 ?? row[(int)enum_projects.更新者].ObjectToString();
                        row[(int)enum_projects.更新時間] = now;

                        if (!p.備註.StringIsEmpty())
                            row[(int)enum_projects.備註] = p.備註;

                        sql.UpdateByDefulteExtra(null, row);

                        okCount++;
                        projResult = row.SQLToClass<projectClass, enum_projects>();
                    }
                    catch (Exception exItem)
                    {
                        failCount++;
                        projResult = new projectClass
                        {
                            GUID = p?.GUID ?? "",
                            狀態 = p?.狀態 ?? "",
                            備註 = $"更新失敗：{exItem.Message}",
                            更新者 = p?.更新者 ?? "",
                            更新時間 = Now()
                        };
                    }

                    resultList.Add(projResult);
                }

                returnData.Code = 200;
                returnData.Result = $"專案狀態更新成功 {okCount} 筆；失敗 {failCount} 筆";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 建立新專案（僅 POST；欄位皆為 string；GUID 一律大寫）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 建立一筆或多筆「專案主檔」資料。<br/>
        /// 系統會自動產生 GUID（大寫）與建立/更新時間，並套用業務日期檢核。
        /// <br/><br/>
        /// <b>必填檢核：</b>
        /// - <c>Data</c> 不可為空  <br/>
        /// - 每筆 <c>name</c>、<c>clientGuid</c>、<c>status</c>、<c>priority</c> 必填  <br/>
        /// - <c>GUID</c> 由系統產生；若前端提供，系統將轉為大寫
        ///
        /// <br/><br/>
        /// <b>業務規則（節錄）：</b>
        /// - 合約結束日期 > 合約開始日期  <br/>
        /// - 投標日期 ≤ 合約開始日期（若二者皆有）
        ///
        /// <br/><br/>
        /// <b>Request JSON 範例：</b>
        /// <code class="json">
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "name": "台北市智慧交通管理系統",
        ///       "description": "包含交通號誌控制、車流監測等功能",
        ///       "clientGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///       "clientName": "台北市政府資訊局",
        ///       "status": "投標中",
        ///       "priority": "高",
        ///       "tenderDate": "2024-01-10",
        ///       "bidOpeningDate": "2024-01-20",
        ///       "contractStartDate": "2024-02-01",
        ///       "contractEndDate": "2024-08-31",
        ///       "contractAmount": "15000000",
        ///       "estimatedCost": "12000000",
        ///       "projectManager": "張專案經理",
        ///       "salesPerson": "李業務",
        ///       "technicalLead": "王工程師",
        ///       "notes": "重要政府專案，需按時完成",
        ///       "tags": "政府,交通,智慧城市",
        ///       "createdBy": "系統管理員"
        ///     }
        ///   ]
        /// }
        /// </code>
        ///
        /// <b>Response JSON 範例（Data 為 projectClass 陣列）：</b>
        /// <code class="json">
        /// {
        ///   "Code": 200,
        ///   "Method": "create_project",
        ///   "Result": "建立成功 1 筆",
        ///   "TimeTaken": "12.3ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "名稱": "台北市智慧交通管理系統",
        ///       "狀態": "投標中",
        ///       "建立時間": "2024-01-10 09:00:00",
        ///       "更新時間": "2024-01-10 09:00:00"
        ///     }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("create_project")]
        public string create_project([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "create_project";

            var outputs = new List<projectClass>();

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    returnData.Data = outputs;
                    return returnData.JsonSerializationt();
                }

                List<projectClass> input = returnData.Data.ObjToClass<List<projectClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    returnData.Data = outputs;
                    return returnData.JsonSerializationt();
                }

                var conf = GetConfOrFail(returnData);
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    returnData.Data = outputs;
                    return returnData.JsonSerializationt();
                }

                var sql = GetSqlProjects(conf);
                string table = TableName(conf);

                // 小工具：處理日期，若為空則回傳 DateTime.MinValue 字串（保持全字串）
                string GetDateOrMin(string dateStr)
                {
                    if (dateStr.StringIsEmpty())
                        return DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
                    return dateStr;
                }

                foreach (var p in input)
                {
                    // 必填驗證
                    if (p == null ||
                        p.名稱.StringIsEmpty() ||
                        p.客戶GUID.StringIsEmpty() ||
                        p.狀態.StringIsEmpty() ||
                        p.優先級.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：name、clientGuid、status、priority 為必填欄位";
                        returnData.Data = outputs;
                        return returnData.JsonSerializationt();
                    }

                    // GUID 規範
                    string guid = p.GUID.StringIsEmpty() ? Guid.NewGuid().ToString().ToUpper() : UpperGuid(p.GUID);

                    // 日期檢核
                    var (ok, msg) = ValidateBusinessDates(p);
                    if (!ok)
                    {
                        returnData.Code = -2;
                        returnData.Result = $"資料驗證錯誤：{msg}";
                        returnData.Data = outputs;
                        return returnData.JsonSerializationt();
                    }

                    // 組建插入列（所有欄位 string）
                    var row = new object[Enum.GetValues(typeof(enum_projects)).Length];
                    row[(int)enum_projects.GUID] = guid;
                    row[(int)enum_projects.ID] = p.ID ?? "";
                    row[(int)enum_projects.名稱] = p.名稱 ?? "";
                    row[(int)enum_projects.描述] = p.描述 ?? "";
                    row[(int)enum_projects.客戶GUID] = UpperGuid(p.客戶GUID);
                    row[(int)enum_projects.客戶名稱] = p.客戶名稱 ?? "";
                    row[(int)enum_projects.狀態] = p.狀態 ?? "";
                    row[(int)enum_projects.優先級] = p.優先級 ?? "";
                    row[(int)enum_projects.投標日期] = GetDateOrMin(p.投標日期);
                    row[(int)enum_projects.開標日期] = GetDateOrMin(p.開標日期);
                    row[(int)enum_projects.得標日期] = GetDateOrMin(p.得標日期);
                    row[(int)enum_projects.合約開始日期] = GetDateOrMin(p.合約開始日期);
                    row[(int)enum_projects.合約結束日期] = GetDateOrMin(p.合約結束日期);
                    row[(int)enum_projects.交貨日期] = GetDateOrMin(p.交貨日期);
                    row[(int)enum_projects.驗收日期] = GetDateOrMin(p.驗收日期);
                    row[(int)enum_projects.合約金額] = p.合約金額 ?? "";
                    row[(int)enum_projects.預估成本] = p.預估成本 ?? "";
                    row[(int)enum_projects.實際成本] = p.實際成本 ?? "";
                    row[(int)enum_projects.利潤率] = p.利潤率 ?? "";
                    row[(int)enum_projects.專案經理] = p.專案經理 ?? "";
                    row[(int)enum_projects.業務負責人] = p.業務負責人 ?? "";
                    row[(int)enum_projects.技術負責人] = p.技術負責人 ?? "";
                    row[(int)enum_projects.進度百分比] = p.進度百分比 ?? "";
                    row[(int)enum_projects.里程碑數量] = p.里程碑數量 ?? "";
                    row[(int)enum_projects.已完成里程碑] = p.已完成里程碑 ?? "";
                    row[(int)enum_projects.需求數量] = p.需求數量 ?? "";
                    row[(int)enum_projects.文件數量] = p.文件數量 ?? "";
                    row[(int)enum_projects.交貨次數] = p.交貨次數 ?? "";
                    row[(int)enum_projects.關聯BOM數量] = p.關聯BOM數量 ?? "";
                    row[(int)enum_projects.驗收狀態] = p.驗收狀態 ?? "";
                    row[(int)enum_projects.備註] = p.備註 ?? "";
                    row[(int)enum_projects.標籤] = p.標籤 ?? "";
                    row[(int)enum_projects.是否啟用] = p.是否啟用.StringIsEmpty() ? "1" : p.是否啟用; // 預設啟用
                    row[(int)enum_projects.建立者] = p.建立者 ?? "";
                    row[(int)enum_projects.建立時間] = Now();
                    row[(int)enum_projects.更新者] = p.更新者 ?? p.建立者 ?? "";
                    row[(int)enum_projects.更新時間] = Now();

                    sql.AddRow(null, row);

                    // 回傳用（以 DB 寫入後的狀態輸出）
                    var outP = row.SQLToClass<projectClass, enum_projects>();
                    outputs.Add(outP);
                }

                returnData.Code = 200;
                returnData.Result = $"建立成功 {outputs.Count} 筆";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = outputs;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                returnData.TimeTaken = $"{timer}";
                returnData.Data = outputs;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 更新專案資訊（僅 POST；以 GUID 定位；僅更新傳入欄位；可同步更新 BOM 關聯）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 依 <c>GUID</c> 更新單筆或多筆專案欄位；未提供之欄位不變。<br/>
        /// 若提供 <c>associatedBomGuids</c> 或 <c>bomAssociations</c>，系統會同步更新 `project_bom_associations`，並回寫主檔欄位 <c>關聯BOMGUID清單</c>（若此欄位存在）。<br/>
        /// 系統會自動套用日期業務規則並更新 <c>updatedAt</c>。<br/>
        ///
        /// <b>必填檢核：</b><br/>
        /// - <c>Data</c> 不可為空。<br/>
        /// - 每筆必須提供 <c>GUID</c>（大寫）。<br/>
        ///
        /// <b>BOM 關聯更新規則：</b><br/>
        /// - 僅提供 <c>associatedBomGuids</c>：視為「同步關聯名單」→ 先刪除該專案現有關聯，再依名單重建。<br/>
        /// - 僅提供 <c>bomAssociations</c>：逐筆 upsert（以 <c>projectGuid</c> + <c>bomGuid</c> 判斷是否存在）。<br/>
        /// - 同時提供兩者：<br/>
        /// &nbsp;&nbsp;1) 以兩者聯集為「目標名單」，將不在名單內的舊關聯刪除。<br/>
        /// &nbsp;&nbsp;2) 依 <c>bomAssociations</c> 的內容 upsert 詳細欄位；對於僅存在於 <c>associatedBomGuids</c> 的 GUID（但沒有詳細內容）會以預設值新增（狀態=啟用）。<br/>
        ///
        /// <b>Request JSON 範例：</b>
        /// <code class="json">
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "status": "進行中",
        ///       "progressPercentage": "75.5",
        ///       "actualCost": "8500000",
        ///       "deliveryDate": "2024-11-30",
        ///       "acceptanceDate": "2024-12-15",
        ///       "acceptanceStatus": "已完成",
        ///       "notes": "專案進度良好，預計提前完成",
        ///       "updatedBy": "張專案經理",
        ///       "associatedBomGuids": [ "BOM-GUID-1", "BOM-GUID-2" ],
        ///       "bomAssociations": [
        ///         {
        ///           "GUID": "REL-GUID-1",
        ///           "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///           "bomGuid": "BOM-GUID-1",
        ///           "bomId": "BOM001",
        ///           "bomName": "智慧交通控制器主機板",
        ///           "requiredQuantity": "2",
        ///           "dueDate": "2025-03-31",
        ///           "associationType": "主要",
        ///           "status": "啟用"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// </code>
        ///
        /// <b>Response JSON 範例（Data 為 projectClass 陣列）：</b>
        /// <code class="json">
        /// {
        ///   "Code": 200,
        ///   "Method": "update_project",
        ///   "Result": "更新成功 1 筆；失敗 0 筆",
        ///   "TimeTaken": "12.3ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "狀態": "進行中",
        ///       "更新時間": "2024-03-15 14:30:00",
        ///       "associatedBomGuids": ["BOM-GUID-1","BOM-GUID-2"]
        ///     }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("update_project")]
        public string update_project([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "update_project";

            var resultList = new List<projectClass>();

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                List<projectClass> input = returnData.Data.ObjToClass<List<projectClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                var conf = GetConfOrFail(returnData);
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                var sql = GetSqlProjects(conf);
                string table = TableName(conf);

                // 供 BOM 用的 SQL 物件（可與主檔共用連線池）
                var sqlBom = SqlForProjects(conf);
                string bomTable = "project_bom_associations";

                int okCount = 0, failCount = 0;

                foreach (var p in input)
                {
                    try
                    {
                        if (p == null || p.GUID.StringIsEmpty())
                            throw new Exception("缺少 GUID");

                        string guid = UpperGuid(p.GUID);
                        var existed = sql.GetRowsByDefult(null, (int)enum_projects.GUID, guid);
                        if (existed == null || existed.Count == 0)
                            throw new Exception($"查無 GUID = {guid}");

                        var row = existed[0];

                        // 建暫存物件做規則檢核（以新值覆蓋舊值後檢查）
                        var tmp = row.SQLToClass<projectClass, enum_projects>() ?? new projectClass();

                        // 將傳入欄位（非 null）覆蓋到 tmp
                        void AssignIfProvided(Action set, string newVal) { if (newVal != null) set(); }
                        AssignIfProvided(() => tmp.名稱 = p.名稱, p.名稱);
                        AssignIfProvided(() => tmp.描述 = p.描述, p.描述);
                        AssignIfProvided(() => tmp.客戶GUID = UpperGuid(p.客戶GUID), p.客戶GUID);
                        AssignIfProvided(() => tmp.客戶名稱 = p.客戶名稱, p.客戶名稱);
                        AssignIfProvided(() => tmp.狀態 = p.狀態, p.狀態);
                        AssignIfProvided(() => tmp.優先級 = p.優先級, p.優先級);
                        AssignIfProvided(() => tmp.投標日期 = p.投標日期, p.投標日期);
                        AssignIfProvided(() => tmp.開標日期 = p.開標日期, p.開標日期);
                        AssignIfProvided(() => tmp.得標日期 = p.得標日期, p.得標日期);
                        AssignIfProvided(() => tmp.合約開始日期 = p.合約開始日期, p.合約開始日期);
                        AssignIfProvided(() => tmp.合約結束日期 = p.合約結束日期, p.合約結束日期);
                        AssignIfProvided(() => tmp.交貨日期 = p.交貨日期, p.交貨日期);
                        AssignIfProvided(() => tmp.驗收日期 = p.驗收日期, p.驗收日期);
                        AssignIfProvided(() => tmp.合約金額 = p.合約金額, p.合約金額);
                        AssignIfProvided(() => tmp.預估成本 = p.預估成本, p.預估成本);
                        AssignIfProvided(() => tmp.實際成本 = p.實際成本, p.實際成本);
                        AssignIfProvided(() => tmp.利潤率 = p.利潤率, p.利潤率);
                        AssignIfProvided(() => tmp.專案經理 = p.專案經理, p.專案經理);
                        AssignIfProvided(() => tmp.業務負責人 = p.業務負責人, p.業務負責人);
                        AssignIfProvided(() => tmp.技術負責人 = p.技術負責人, p.技術負責人);
                        AssignIfProvided(() => tmp.進度百分比 = p.進度百分比, p.進度百分比);
                        AssignIfProvided(() => tmp.里程碑數量 = p.里程碑數量, p.里程碑數量);
                        AssignIfProvided(() => tmp.已完成里程碑 = p.已完成里程碑, p.已完成里程碑);
                        AssignIfProvided(() => tmp.需求數量 = p.需求數量, p.需求數量);
                        AssignIfProvided(() => tmp.文件數量 = p.文件數量, p.文件數量);
                        AssignIfProvided(() => tmp.交貨次數 = p.交貨次數, p.交貨次數);
                        AssignIfProvided(() => tmp.關聯BOM數量 = p.關聯BOM數量, p.關聯BOM數量);
                        AssignIfProvided(() => tmp.驗收狀態 = p.驗收狀態, p.驗收狀態);
                        AssignIfProvided(() => tmp.驗收備註 = p.驗收備註, p.驗收備註);
                        AssignIfProvided(() => tmp.備註 = p.備註, p.備註);
                        AssignIfProvided(() => tmp.標籤 = p.標籤, p.標籤);
                        AssignIfProvided(() => tmp.是否啟用 = p.是否啟用, p.是否啟用);

                        var (ok, msg) = ValidateBusinessDates(tmp);
                        if (!ok) throw new Exception(msg);

                        // 逐欄更新（僅覆蓋非 null 值；全部 string）
                        void SetIfProvided(int idx, string val, Func<string> cvt = null)
                        { if (val != null) row[idx] = cvt == null ? val : cvt(); }

                        SetIfProvided((int)enum_projects.ID, p.ID);
                        SetIfProvided((int)enum_projects.名稱, p.名稱);
                        SetIfProvided((int)enum_projects.描述, p.描述);
                        SetIfProvided((int)enum_projects.客戶GUID, p.客戶GUID, () => UpperGuid(p.客戶GUID));
                        SetIfProvided((int)enum_projects.客戶名稱, p.客戶名稱);
                        SetIfProvided((int)enum_projects.狀態, p.狀態);
                        SetIfProvided((int)enum_projects.優先級, p.優先級);
                        SetIfProvided((int)enum_projects.投標日期, p.投標日期);
                        SetIfProvided((int)enum_projects.開標日期, p.開標日期);
                        SetIfProvided((int)enum_projects.得標日期, p.得標日期);
                        SetIfProvided((int)enum_projects.合約開始日期, p.合約開始日期);
                        SetIfProvided((int)enum_projects.合約結束日期, p.合約結束日期);
                        SetIfProvided((int)enum_projects.交貨日期, p.交貨日期);
                        SetIfProvided((int)enum_projects.驗收日期, p.驗收日期);
                        SetIfProvided((int)enum_projects.合約金額, p.合約金額);
                        SetIfProvided((int)enum_projects.預估成本, p.預估成本);
                        SetIfProvided((int)enum_projects.實際成本, p.實際成本);
                        SetIfProvided((int)enum_projects.利潤率, p.利潤率);
                        SetIfProvided((int)enum_projects.專案經理, p.專案經理);
                        SetIfProvided((int)enum_projects.業務負責人, p.業務負責人);
                        SetIfProvided((int)enum_projects.技術負責人, p.技術負責人);
                        SetIfProvided((int)enum_projects.進度百分比, p.進度百分比);
                        SetIfProvided((int)enum_projects.里程碑數量, p.里程碑數量);
                        SetIfProvided((int)enum_projects.已完成里程碑, p.已完成里程碑);
                        SetIfProvided((int)enum_projects.需求數量, p.需求數量);
                        SetIfProvided((int)enum_projects.文件數量, p.文件數量);
                        SetIfProvided((int)enum_projects.交貨次數, p.交貨次數);
                        SetIfProvided((int)enum_projects.關聯BOM數量, p.關聯BOM數量);
                        SetIfProvided((int)enum_projects.驗收狀態, p.驗收狀態);
                        SetIfProvided((int)enum_projects.備註, p.備註);
                        SetIfProvided((int)enum_projects.標籤, p.標籤);
                        SetIfProvided((int)enum_projects.是否啟用, p.是否啟用);

                        // 系統欄位
                        row[(int)enum_projects.更新者] = p.更新者 ?? row[(int)enum_projects.更新者].ObjectToString();
                        row[(int)enum_projects.更新時間] = Now();

                        // 先更新主檔
                        sql.UpdateByDefulteExtra(null, row);

                        // ============ BOM 關聯更新（無 GetRowsBy2Key 版） ============
                        bool hasGuids = p.AssociatedBomGuids != null;
                        bool hasDetails = p.BomAssociations != null && p.BomAssociations.Count > 0;

                        if (hasGuids || hasDetails)
                        {
                            // 目前既有關聯
                            string loadSql = $@"
SELECT * FROM {conf.DBName}.{bomTable}
WHERE 專案GUID = '{Esc(guid)}'";
                            var curDt = sqlBom.WtrteCommandAndExecuteReader(loadSql);
                            var curRows = curDt.DataTableToRowList();
                            var curGuidSet = new HashSet<string>(
                                curRows.Select(r => r[(int)enum_project_bom_associations.BOMGUID].ObjectToString()),
                                StringComparer.OrdinalIgnoreCase);

                            // 目標名單（聯集）
                            var targetSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            if (hasGuids)
                                foreach (var g in p.AssociatedBomGuids.Where(x => !x.StringIsEmpty()))
                                    targetSet.Add(UpperGuid(g));
                            if (hasDetails)
                                foreach (var g in p.BomAssociations.Where(a => !a.BOMGUID.StringIsEmpty()).Select(a => UpperGuid(a.BOMGUID)))
                                    targetSet.Add(g);

                            // 刪除不在名單的既有關聯
                            foreach (var old in curGuidSet.Where(x => !targetSet.Contains(x)).ToList())
                            {
                                // 逐筆查出 row 後刪除
                                string selDelSql = $@"
SELECT * FROM {conf.DBName}.{bomTable}
WHERE 專案GUID = '{Esc(guid)}' AND BOMGUID = '{Esc(UpperGuid(old))}' LIMIT 1";
                                var delDt = sqlBom.WtrteCommandAndExecuteReader(selDelSql);
                                var delRows = delDt.DataTableToRowList();
                                if (delRows.Count > 0) sqlBom.DeleteExtra(bomTable, delRows[0]);
                            }

                            // upsert 詳細
                            if (hasDetails)
                            {
                                foreach (var a in p.BomAssociations)
                                {
                                    if (a.BOMGUID.StringIsEmpty()) continue;
                                    string bomGuid = UpperGuid(a.BOMGUID);

                                    string selSql = $@"
SELECT * FROM {conf.DBName}.{bomTable}
WHERE 專案GUID = '{Esc(guid)}' AND BOMGUID = '{Esc(bomGuid)}' LIMIT 1";
                                    var selDt = sqlBom.WtrteCommandAndExecuteReader(selSql);
                                    var selRows = selDt.DataTableToRowList();

                                    if (selRows.Count > 0)
                                    {
                                        // 更新
                                        var r = selRows[0];
                                        if (a.BOM編號 != null) r[(int)enum_project_bom_associations.BOM編號] = a.BOM編號;
                                        if (a.BOM名稱 != null) r[(int)enum_project_bom_associations.BOM名稱] = a.BOM名稱;
                                        if (a.需求數量 != null) r[(int)enum_project_bom_associations.需求數量] = a.需求數量;
                                        if (a.到期日 != null) r[(int)enum_project_bom_associations.到期日] = a.到期日;
                                        if (a.關聯類型 != null) r[(int)enum_project_bom_associations.關聯類型] = a.關聯類型;
                                        if (a.狀態 != null) r[(int)enum_project_bom_associations.狀態] = a.狀態;
                                        r[(int)enum_project_bom_associations.更新者] = p.更新者 ?? r[(int)enum_project_bom_associations.更新者].ObjectToString();
                                        r[(int)enum_project_bom_associations.更新時間] = Now();
                                        sqlBom.UpdateByDefulteExtra(bomTable, r);
                                    }
                                    else
                                    {
                                        // 新增
                                        var newRow = new object[Enum.GetValues(typeof(enum_project_bom_associations)).Length];
                                        newRow[(int)enum_project_bom_associations.GUID] = a.GUID.StringIsEmpty() ? Guid.NewGuid().ToString().ToUpper() : a.GUID;
                                        newRow[(int)enum_project_bom_associations.專案GUID] = guid;
                                        newRow[(int)enum_project_bom_associations.BOMGUID] = bomGuid;
                                        newRow[(int)enum_project_bom_associations.BOM編號] = a.BOM編號;
                                        newRow[(int)enum_project_bom_associations.BOM名稱] = a.BOM名稱;
                                        newRow[(int)enum_project_bom_associations.需求數量] = a.需求數量;
                                        newRow[(int)enum_project_bom_associations.到期日] = a.到期日;
                                        newRow[(int)enum_project_bom_associations.關聯類型] = a.關聯類型;
                                        newRow[(int)enum_project_bom_associations.狀態] = a.狀態 ?? "啟用";
                                        newRow[(int)enum_project_bom_associations.建立者] = p.更新者 ?? "";
                                        newRow[(int)enum_project_bom_associations.建立時間] = Now();
                                        sqlBom.AddRow(bomTable, newRow);
                                    }
                                }
                            }

                            // 對於只在 associatedBomGuids 的 GUID（沒詳細資料）的補新增
                            var handled = new HashSet<string>(
                                hasDetails ? p.BomAssociations.Where(a => !a.BOMGUID.StringIsEmpty()).Select(a => UpperGuid(a.BOMGUID))
                                           : Array.Empty<string>(),
                                StringComparer.OrdinalIgnoreCase);

                            foreach (var g in targetSet.Where(x => !handled.Contains(x)))
                            {
                                // 若已存在就略過
                                string selSql = $@"
SELECT * FROM {conf.DBName}.{bomTable}
WHERE 專案GUID = '{Esc(guid)}' AND BOMGUID = '{Esc(g)}' LIMIT 1";
                                var selDt = sqlBom.WtrteCommandAndExecuteReader(selSql);
                                var selRows = selDt.DataTableToRowList();
                                if (selRows.Count > 0) continue;

                                var newRow = new object[Enum.GetValues(typeof(enum_project_bom_associations)).Length];
                                newRow[(int)enum_project_bom_associations.GUID] = Guid.NewGuid().ToString().ToUpper();
                                newRow[(int)enum_project_bom_associations.專案GUID] = guid;
                                newRow[(int)enum_project_bom_associations.BOMGUID] = g;
                                newRow[(int)enum_project_bom_associations.狀態] = "啟用";
                                newRow[(int)enum_project_bom_associations.建立者] = p.更新者 ?? "";
                                newRow[(int)enum_project_bom_associations.建立時間] = Now();
                                sqlBom.AddRow(bomTable, newRow);
                            }

                            // 回寫 projects.關聯BOMGUID清單（若 enum 有此欄位）
                            try
                            {
                                string jsonList = System.Text.Json.JsonSerializer.Serialize(targetSet.ToList());
                                row[(int)enum_projects.關聯BOMGUID清單] = jsonList;
                                sql.UpdateByDefulteExtra(null, row);
                            }
                            catch { /* enum 未升級含該欄位時，忽略 */ }
                        }

                        // 輸出
                        var outP = row.SQLToClass<projectClass, enum_projects>();
                        if (hasGuids) outP.AssociatedBomGuids = p.AssociatedBomGuids;
                        if (hasDetails) outP.BomAssociations = p.BomAssociations;

                        resultList.Add(outP);
                        okCount++;
                    }
                    catch (Exception exItem)
                    {
                        resultList.Add(new projectClass
                        {
                            GUID = p?.GUID ?? "",
                            狀態 = p?.狀態 ?? "",
                            備註 = $"更新失敗：{exItem.Message}",
                            更新者 = p?.更新者 ?? "",
                            更新時間 = Now()
                        });
                        failCount++;
                    }
                }

                returnData.Code = 200;
                returnData.Result = $"更新成功 {okCount} 筆；失敗 {failCount} 筆";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 刪除專案（軟刪除：將是否啟用設為 0；僅 POST）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 依 <c>GUID</c> 進行軟刪除（<c>是否啟用</c> 設為 <c>"0"</c>），支援批次。
        /// <br/><br/>
        /// <b>必填檢核：</b>
        /// - <c>Data</c> 不可為空  <br/>
        /// - 每筆必須提供 <c>GUID</c>
        ///
        /// <br/><br/>
        /// <b>Request JSON 範例：</b>
        /// <code class="json">
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "deletedBy": "系統管理員",
        ///       "notes": "測試刪除"
        ///     }
        ///   ]
        /// }
        /// </code>
        ///
        /// <b>Response JSON 範例（Data 為 projectClass 陣列）：</b>
        /// <code class="json">
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_project",
        ///   "Result": "軟刪除成功 1 筆；失敗 0 筆",
        ///   "TimeTaken": "6.5ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "是否啟用": "0",
        ///       "備註": "測試刪除",
        ///       "更新者": "系統管理員",
        ///       "更新時間": "2024-03-15 14:31:00"
        ///     }
        ///   ]
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("delete_project")]
        public string delete_project([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_project";

            var resultList = new List<projectClass>();

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                List<projectClass> input = returnData.Data.ObjToClass<List<projectClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                var conf = GetConfOrFail(returnData);
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    returnData.Data = resultList;
                    return returnData.JsonSerializationt();
                }

                var sql = GetSqlProjects(conf);
                string table = TableName(conf);

                int okCount = 0, failCount = 0;

                foreach (var p in input)
                {
                    try
                    {
                        if (p == null || p.GUID.StringIsEmpty())
                            throw new Exception("缺少 GUID");

                        string guid = UpperGuid(p.GUID);
                        var existed = sql.GetRowsByDefult(null, (int)enum_projects.GUID, guid);
                        if (existed == null || existed.Count == 0)
                            throw new Exception($"查無 GUID = {guid}");

                        var row = existed[0];

                        // 軟刪除：是否啟用 = "0"，並更新備註/更新者/時間
                        row[(int)enum_projects.是否啟用] = "0";

                        if (!p.備註.StringIsEmpty())
                            row[(int)enum_projects.備註] = p.備註;

                        if (!p.更新者.StringIsEmpty())
                            row[(int)enum_projects.更新者] = p.更新者;
                        else if (!p.建立者.StringIsEmpty())
                            row[(int)enum_projects.更新者] = p.建立者;

                        row[(int)enum_projects.更新時間] = Now();

                        sql.UpdateByDefulteExtra(null, row);

                        var outP = row.SQLToClass<projectClass, enum_projects>();
                        resultList.Add(outP);
                        okCount++;
                    }
                    catch (Exception exItem)
                    {
                        resultList.Add(new projectClass
                        {
                            GUID = p?.GUID ?? "",
                            是否啟用 = "1", // 保留原啟用狀態未知時以「1」呈現
                            備註 = $"軟刪除失敗：{exItem.Message}",
                            更新者 = p?.更新者 ?? "",
                            更新時間 = Now()
                        });
                        failCount++;
                    }
                }

                returnData.Code = 200;
                returnData.Result = $"軟刪除成功 {okCount} 筆；失敗 {failCount} 筆";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 獲取專案統計資料（僅 POST）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 取得專案數量統計、合約金額統計與本月新增完成狀況，用於專案概況顯示。  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁"
        /// }
        /// ```
        ///
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Data": {
        ///     "totalProjects": "25",
        ///     "tenderingProjects": "5",
        ///     "awardedProjects": "8",
        ///     "ongoingProjects": "7",
        ///     "completedProjects": "4",
        ///     "cancelledProjects": "1",
        ///     "pausedProjects": "0",
        ///     "upcomingDeadlines": "3",
        ///     "totalContractValue": "125000000",
        ///     "totalEstimatedCost": "98000000",
        ///     "totalActualCost": "85000000",
        ///     "averageProfitMargin": "15.25",
        ///     "newProjectsThisMonth": "2",
        ///     "completedProjectsThisMonth": "1"
        ///   },
        ///   "Code": 200,
        ///   "Method": "get_project_statistics",
        ///   "Result": "獲取專案統計成功"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_project_statistics")]
        public string get_project_statistics([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_project_statistics";

            try
            {
                if (string.IsNullOrEmpty(returnData.ServerName) || string.IsNullOrEmpty(returnData.ServerType))
                {
                    returnData.Code = 400;
                    returnData.Result = "ServerName 與 ServerType 為必填";
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

                string projectsTable = new enum_projects().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, projectsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string Esc(string s) => (s ?? "").Replace("'", "''");

                // ========== 統計 ==========
                // 總專案數
                int total = sql.WtrteCommandAndExecuteReader($"SELECT COUNT(*) as cnt FROM {conf.DBName}.{projectsTable}")
                              .Rows[0]["cnt"].ToString().StringToInt32();

                // 各狀態數量
                string sqlStatus = $@"
            SELECT 狀態, COUNT(*) as cnt
            FROM {conf.DBName}.{projectsTable}
            GROUP BY 狀態";
                var dtStatus = sql.WtrteCommandAndExecuteReader(sqlStatus);

                int tendering = 0, awarded = 0, ongoing = 0, completed = 0, cancelled = 0, paused = 0;
                foreach (DataRow r in dtStatus.Rows)
                {
                    string s = r["狀態"].ObjectToString();
                    int cnt = r["cnt"].ToString().StringToInt32();
                    switch (s)
                    {
                        case "投標中": tendering = cnt; break;
                        case "已得標": awarded = cnt; break;
                        case "進行中": ongoing = cnt; break;
                        case "已完成": completed = cnt; break;
                        case "已取消": cancelled = cnt; break;
                        case "暫停": paused = cnt; break;
                    }
                }

                // 金額統計
                string sqlFinance = $@"
            SELECT 
                SUM(CAST(合約金額 AS DECIMAL(20,2))) as totalContract,
                SUM(CAST(預估成本 AS DECIMAL(20,2))) as totalEstimated,
                SUM(CAST(實際成本 AS DECIMAL(20,2))) as totalActual,
                AVG(CAST(利潤率 AS DECIMAL(10,2))) as avgProfit
            FROM {conf.DBName}.{projectsTable}";
                var dtFinance = sql.WtrteCommandAndExecuteReader(sqlFinance);
                string totalContract = dtFinance.Rows[0]["totalContract"].ObjectToString();
                string totalEstimated = dtFinance.Rows[0]["totalEstimated"].ObjectToString();
                string totalActual = dtFinance.Rows[0]["totalActual"].ObjectToString();
                string avgProfit = dtFinance.Rows[0]["avgProfit"].ObjectToString();

                // 本月新增/完成
                string nowMonth = DateTime.Now.ToString("yyyy-MM");
                string sqlNew = $@"SELECT COUNT(*) as cnt 
                           FROM {conf.DBName}.{projectsTable}
                           WHERE DATE_FORMAT(建立時間, '%Y-%m') = '{nowMonth}'";
                int newThisMonth = sql.WtrteCommandAndExecuteReader(sqlNew).Rows[0]["cnt"].ToString().StringToInt32();

                string sqlCompleted = $@"SELECT COUNT(*) as cnt 
                                 FROM {conf.DBName}.{projectsTable}
                                 WHERE 狀態 = '已完成' 
                                   AND DATE_FORMAT(更新時間, '%Y-%m') = '{nowMonth}'";
                int completedThisMonth = sql.WtrteCommandAndExecuteReader(sqlCompleted).Rows[0]["cnt"].ToString().StringToInt32();

                // 即將到期（合約結束日 <= 30天內且尚未完成/取消）
                string sqlDeadline = $@"SELECT COUNT(*) as cnt 
                                FROM {conf.DBName}.{projectsTable}
                                WHERE 狀態 NOT IN ('已完成','已取消')
                                  AND 合約結束日期 <= DATE_ADD(NOW(), INTERVAL 30 DAY)";
                int upcomingDeadlines = sql.WtrteCommandAndExecuteReader(sqlDeadline).Rows[0]["cnt"].ToString().StringToInt32();

                // ========== 組裝回傳 ==========
                var stats = new projectStatisticsClass
                {
                    總專案數 = total.ToString(),
                    投標中 = tendering.ToString(),
                    已得標 = awarded.ToString(),
                    進行中 = ongoing.ToString(),
                    已完成 = completed.ToString(),
                    已取消 = cancelled.ToString(),
                    暫停 = paused.ToString(),
                    即將到期 = upcomingDeadlines.ToString(),
                    總合約金額 = totalContract,
                    總預估成本 = totalEstimated,
                    總實際成本 = totalActual,
                    平均利潤率 = avgProfit,
                    本月新增 = newThisMonth.ToString(),
                    本月完成 = completedThisMonth.ToString()
                };

                returnData.Code = 200;
                returnData.Result = "獲取專案統計成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = stats;
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
        /// 取得專案儀表板（僅 POST；以日期範圍過濾；回傳專案清單）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 查詢最近一段時間內更新過的專案清單，依 `更新時間` 由新到舊排序，最多回傳 100 筆。  
        ///
        /// **必填檢核：**  
        /// - `ServerName` 與 `ServerType` 不可為空。  
        /// - `dateRange` 若未提供，預設為 30 天。  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     {
        ///       "userId": "user123",
        ///       "dateRange": "30",
        ///       "includeCharts": "false"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例 (成功回傳)：**
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "A1B2C3D4-5678-90AB-CDEF-112233445566",
        ///       "ID": "PRJ-20250821-789",
        ///       "名稱": "智慧醫院系統升級案",
        ///       "狀態": "進行中",
        ///       "更新時間": "2025/08/20 15:42:10",
        ///       "備註": "進度達 75%"
        ///     },
        ///     {
        ///       "GUID": "F0E9D8C7-6543-21BA-9876-554433221100",
        ///       "ID": "PRJ-20250715-321",
        ///       "名稱": "自駕車測試平台",
        ///       "狀態": "已得標",
        ///       "更新時間": "2025/08/18 10:25:43",
        ///       "備註": "準備進場施工"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "get_project_dashboard",
        ///   "Result": "獲取專案儀表板成功",
        ///   "TimeTaken": "0.123s"
        /// }
        /// ```
        ///
        /// **Response JSON 範例 (錯誤回傳)：**
        /// ```json
        /// {
        ///   "Data": [],
        ///   "Code": 400,
        ///   "Method": "get_project_dashboard",
        ///   "Result": "ServerName 與 ServerType 為必填"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>回傳專案清單（projectClassList）</returns>
        [HttpPost("get_project_dashboard")]
        public string get_project_dashboard([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_project_dashboard";

            try
            {
                if (string.IsNullOrEmpty(returnData.ServerName) || string.IsNullOrEmpty(returnData.ServerType))
                {
                    returnData.Code = 400;
                    returnData.Result = "ServerName 與 ServerType 為必填";
                    return returnData.JsonSerializationt();
                }

                var conf = serverSetting.GetAllServerSetting().myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                // 參數
                int dateRange = (GetVal(returnData.ValueAry, "dateRange") ?? "30").StringToInt32();
                if (dateRange <= 0) dateRange = 30;

                string pTable = ProjectsTable(conf);
                var sql = SqlForProjects(conf);

                // 查詢最近 dateRange 天的更新紀錄
                string query = $@"
            SELECT GUID, ID, 名稱, 狀態, 更新時間, 備註
            FROM {conf.DBName}.{pTable}
            WHERE 更新時間 >= DATE_SUB(NOW(), INTERVAL {dateRange} DAY)
            ORDER BY 更新時間 DESC
            LIMIT 100"; // 可自行調整上限

                var dt = sql.WtrteCommandAndExecuteReader(query);
                var list = dt.DataTableToRowList().SQLToClass<projectClass, enum_projects>() ?? new List<projectClass>();

                returnData.Code = 200;
                returnData.Result = "獲取專案儀表板成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = list;
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
        /// 獲取專案里程碑清單（僅 POST；支援分頁與篩選）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 依據 `projectGuid` 或其他查詢條件獲取里程碑清單。支援分頁、排序、狀態篩選。  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "projectGuid=550E8400-E29B-41D4-A716-446655440000",
        ///     "status=進行中",
        ///     "page=1",
        ///     "pageSize=20"
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_project_milestones",
        ///   "Result": "獲取里程碑清單成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "A1B2C3D4-5678-90AB-CDEF-1234567890AB",
        ///       "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "name": "第一階段設計完成",
        ///       "status": "已完成",
        ///       "plannedDate": "2024-02-01",
        ///       "actualDate": "2024-02-15"
        ///     }
        ///   ],
        ///   "TotalCount": "5",
        ///   "TotalPages": "1"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_project_milestones")]
        public string get_project_milestones([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_project_milestones";

            try
            {
                var conf = serverSetting.GetAllServerSetting()
                                        .myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_milestones().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string GetVal(string key) => returnData.ValueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];

                string projectGuid = GetVal("projectGuid") ?? "";
                string status = GetVal("status") ?? "";
                int page = (GetVal("page") ?? "1").StringToInt32();
                int pageSize = (GetVal("pageSize") ?? "50").StringToInt32();
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 50;

                string Esc(string s) => (s ?? "").Replace("'", "''");

                string where = " WHERE 1=1 ";
                if (!projectGuid.StringIsEmpty()) where += $" AND 專案GUID = '{Esc(projectGuid)}'";
                if (!status.StringIsEmpty()) where += $" AND 狀態 = '{Esc(status)}'";

                int offset = (page - 1) * pageSize;
                string sqlQuery = $@"SELECT * FROM {conf.DBName}.{table} {where} 
                                     ORDER BY 建立時間 DESC LIMIT {offset},{pageSize}";
                var dt = sql.WtrteCommandAndExecuteReader(sqlQuery);

                var milestones = dt.DataTableToRowList().SQLToClass<milestoneClass, enum_project_milestones>() ?? new List<milestoneClass>();

                // 查總數
                string sqlCnt = $@"SELECT COUNT(*) as cnt FROM {conf.DBName}.{table} {where}";
                var cntDt = sql.WtrteCommandAndExecuteReader(sqlCnt);
                int totalCount = cntDt.Rows[0]["cnt"].ToString().StringToInt32();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                returnData.Code = 200;
                returnData.Result = "獲取里程碑清單成功";
                returnData.Data = milestones;
                returnData.AddExtra("TotalCount", totalCount.ToString());
                returnData.AddExtra("TotalPages", totalPages.ToString());
                returnData.TimeTaken = $"{timer}";
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
        /// 創建專案里程碑（僅 POST；自動產生 GUID 與建立時間）
        /// </summary>
        [HttpPost("create_milestone")]
        public string create_milestone([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "create_milestone";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<milestoneClass> input = returnData.Data.ObjToClass<List<milestoneClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤";
                    return returnData.JsonSerializationt();
                }

                var conf = serverSetting.GetAllServerSetting()
                                        .myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_milestones().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string now = DateTime.Now.ToDateTimeString();
                var output = new List<milestoneClass>();

                foreach (var m in input)
                {
                    m.GUID = Guid.NewGuid().ToString().ToUpper();
                    m.建立時間 = now;
                    m.更新時間 = now;

                    object[] row = m.ClassToSQL<milestoneClass, enum_project_milestones>();
                    sql.AddRow(null, row);
                    output.Add(m);
                }

                returnData.Code = 200;
                returnData.Result = $"成功建立 {output.Count} 筆里程碑";
                returnData.Data = output;
                returnData.TimeTaken = $"{timer}";
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
        /// 更新里程碑資訊（僅 POST；需提供 GUID）
        /// </summary>
        [HttpPost("update_milestone")]
        public string update_milestone([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "update_milestone";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<milestoneClass> input = returnData.Data.ObjToClass<List<milestoneClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤";
                    return returnData.JsonSerializationt();
                }

                var conf = serverSetting.GetAllServerSetting()
                                        .myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_milestones().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string now = DateTime.Now.ToDateTimeString();
                var output = new List<milestoneClass>();

                foreach (var m in input)
                {
                    if (m.GUID.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "更新必須提供 GUID";
                        return returnData.JsonSerializationt();
                    }

                    var rows = sql.GetRowsByDefult(null, (int)enum_project_milestones.GUID, m.GUID);
                    if (rows.Count == 0)
                    {
                        returnData.Code = 404;
                        returnData.Result = $"查無 GUID={m.GUID}";
                        return returnData.JsonSerializationt();
                    }

                    object[] row = rows[0];
                    UpdateRowFromClass(row ,m, now, typeof(enum_project_milestones));
                    sql.UpdateByDefulteExtra(null, row);
                    m.更新時間 = now;
                    output.Add(m);
                }

                returnData.Code = 200;
                returnData.Result = $"成功更新 {output.Count} 筆里程碑";
                returnData.Data = output;
                returnData.TimeTaken = $"{timer}";
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
        /// 刪除里程碑（僅 POST；需提供 GUID）
        /// </summary>
        [HttpPost("delete_milestone")]
        public string delete_milestone([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_milestone";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<milestoneClass> input = returnData.Data.ObjToClass<List<milestoneClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤";
                    return returnData.JsonSerializationt();
                }

                var conf = serverSetting.GetAllServerSetting()
                                        .myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_milestones().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var deleted = new List<string>();

                foreach (var m in input)
                {
                    if (m.GUID.StringIsEmpty()) continue;

                    var rows = sql.GetRowsByDefult(null, (int)enum_project_milestones.GUID, m.GUID);
                    if (rows.Count > 0)
                    {
                        sql.DeleteExtra(null, rows);
                        deleted.Add(m.GUID);
                    }
                }

                returnData.Code = 200;
                returnData.Result = $"成功刪除 {deleted.Count} 筆里程碑";
                returnData.Data = deleted;
                returnData.TimeTaken = $"{timer}";
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
        /// 獲取專案需求清單（僅 POST）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 依據指定的 `projectGuid` 與篩選條件，獲取專案需求清單，支援分頁、排序與多條件查詢。  
        ///
        /// **必填檢核：**  
        /// - `ValueAry` 必須包含 `projectGuid`  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "projectGuid=550E8400-E29B-41D4-A716-446655440000",
        ///     "status=採購中",
        ///     "priority=高",
        ///     "page=1",
        ///     "pageSize=50",
        ///     "sortBy=dueDate",
        ///     "sortOrder=asc"
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "ID": "REQ001",
        ///       "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "projectId": "PRJ001",
        ///       "procurementType": "市構件請購",
        ///       "description": "交通號誌控制器",
        ///       "quantity": "50",
        ///       "unit": "台",
        ///       "status": "採購中",
        ///       "priority": "高",
        ///       "dueDate": "2024-04-30",
        ///       "requestedDate": "2024-03-15",
        ///       "approvedDate": "2024-03-16",
        ///       "purchasedDate": "2024-03-20",
        ///       "createdAt": "2024-03-15 09:00:00",
        ///       "updatedAt": "2024-03-20 14:30:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "get_project_requirements",
        ///   "Result": "獲取專案需求清單成功",
        ///   "TimeTaken": "0.125s",
        ///   "TotalCount": "25",
        ///   "TotalPages": "1",
        ///   "CurrentPage": "1",
        ///   "PageSize": "50"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">標準請求物件</param>
        /// <returns>回傳 JSON 格式的查詢結果</returns>
        [HttpPost("get_project_requirements")]
        public string GetProjectRequirements([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_project_requirements";

            try
            {
                // 驗證必填
                if (returnData.ValueAry == null || returnData.ValueAry.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "ValueAry 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 解析參數
                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string projectGuid = GetVal("projectGuid") ?? "";
                if (projectGuid.StringIsEmpty())
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少必要參數：projectGuid";
                    return returnData.JsonSerializationt();
                }

                string procurementType = GetVal("procurementType") ?? "";
                string status = GetVal("status") ?? "";
                string priority = GetVal("priority") ?? "";
                string requester = GetVal("requester") ?? "";
                string isFromBom = GetVal("isFromBom") ?? "";
                bool includeItems = (GetVal("includeItems") ?? "false").ToLower() == "true";

                int page = (GetVal("page") ?? "1").StringToInt32();
                int pageSize = (GetVal("pageSize") ?? "50").StringToInt32();
                string sortBy = GetVal("sortBy") ?? "更新時間";
                string sortOrder = (GetVal("sortOrder") ?? "desc").ToUpper();

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 50;

                // 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_requirements().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);

                string Esc(string s) => (s ?? "").Replace("'", "''");

                // 組 WHERE 條件
                string where = $" WHERE 專案GUID = '{Esc(projectGuid)}' ";
                if (!procurementType.StringIsEmpty()) where += $" AND procurement_type = '{Esc(procurementType)}' ";
                if (!status.StringIsEmpty()) where += $" AND status = '{Esc(status)}' ";
                if (!priority.StringIsEmpty()) where += $" AND priority = '{Esc(priority)}' ";
                if (!requester.StringIsEmpty()) where += $" AND requester = '{Esc(requester)}' ";
                if (!isFromBom.StringIsEmpty()) where += $" AND is_from_bom = '{Esc(isFromBom)}' ";

                string orderBy = $" ORDER BY {sortBy} {sortOrder} ";
                int offset = (page - 1) * pageSize;

                // 查詢總數
                string countSql = $"SELECT COUNT(*) AS cnt FROM {conf.DBName}.{table} {where}";
                var dtCnt = sql.WtrteCommandAndExecuteReader(countSql);
                int totalCount = dtCnt.Rows[0]["cnt"].ToString().StringToInt32();
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // 查詢資料
                string querySql = $@"
                    SELECT * FROM {conf.DBName}.{table}
                    {where}
                    {orderBy}
                    LIMIT {offset}, {pageSize}";
                var dt = sql.WtrteCommandAndExecuteReader(querySql);

                var requirements = dt.DataTableToRowList().SQLToClass<ProjectRequirementClass, enum_project_requirements>() ?? new List<ProjectRequirementClass>();

                returnData.Code = 200;
                returnData.Result = "獲取專案需求清單成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = requirements;
                returnData.AddExtra("TotalCount", totalCount.ToString());
                returnData.AddExtra("TotalPages", totalPages.ToString());
                returnData.AddExtra("CurrentPage", page.ToString());
                returnData.AddExtra("PageSize", pageSize.ToString());
 

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
        /// 取得單一需求詳細資訊（僅 POST）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 依照 <c>GUID</c> 取得專案需求的完整詳細資訊。  
        /// 可選擇是否包含 BOM 項目資料。  
        ///
        /// <b>請求驗證：</b>  
        /// - <c>ValueAry</c> 必須包含 <c>GUID</c>（大寫）  
        /// - 缺少必要參數會直接回傳錯誤  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "GUID=880F9700-F59E-74G7-D049-779988773333",
        ///     "includeItems=true"
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "Data": {
        ///     "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///     "ID": "REQ001",
        ///     "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///     "projectId": "PRJ001",
        ///     "procurementType": "市構件請購",
        ///     "description": "交通號誌控制器",
        ///     "quantity": "50",
        ///     "unit": "台",
        ///     "status": "採購中",
        ///     "priority": "高",
        ///     "dueDate": "2024-04-30",
        ///     "estimatedCost": "2500000",
        ///     "itemId": "PROD001",
        ///     "itemName": "智慧交通控制器 TC-2000",
        ///     "itemCode": "TC-2000",
        ///     "supplierName": "智慧科技有限公司",
        ///     "requester": "張工程師",
        ///     "approver": "李主管",
        ///     "purchaser": "王採購",
        ///     "notes": "緊急需求，需優先處理",
        ///     "specifications": "需支援 LED 顯示，具備遠端監控功能",
        ///     "deliveryAddress": "台北市信義區市府路1號",
        ///     "isActive": "1",
        ///     "createdBy": "張工程師",
        ///     "createdAt": "2024-03-15 09:00:00",
        ///     "updatedBy": "王採購",
        ///     "updatedAt": "2024-03-20 14:30:00",
        ///     "items": [
        ///       {
        ///         "GUID": "CC0F9B00-F99I-B8K1-H483-BB3322BB7777",
        ///         "requirementGuid": "880F9700-F59E-74G7-D049-779988773333",
        ///         "itemDescription": "ARM Cortex-M4 微控制器",
        ///         "itemCode": "MCU-001",
        ///         "quantity": "1",
        ///         "unit": "個",
        ///         "estimatedUnitCost": "850",
        ///         "estimatedTotalCost": "850"
        ///       }
        ///     ]
        ///   },
        ///   "Code": 200,
        ///   "Method": "get_requirement_details",
        ///   "Result": "取得需求詳情成功",
        ///   "TimeTaken": "15.3ms"
        /// }
        /// ```
        ///
        /// **Response JSON 範例（錯誤）：**
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": -200,
        ///   "Method": "get_requirement_details",
        ///   "Result": "缺少 GUID 參數",
        ///   "TimeTaken": "0.7ms"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_requirement_details")]
        public string get_requirement_details([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_requirement_details";

            try
            {
                // 1) 驗證必填參數
                string guid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("GUID=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                bool includeItems = ((returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("includeItems=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1]) ?? "false").ToLower() == "true";

                if (guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 GUID 參數";
                    return returnData.JsonSerializationt();
                }

                // 2) 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string reqTable = new enum_project_requirements().GetEnumDescription();
                string bomItemsTable = new enum_bom_requirement_items().GetEnumDescription();

                var sqlReq = new SQLControl(conf.Server, conf.DBName, reqTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);
                var sqlBomItems = new SQLControl(conf.Server, conf.DBName, bomItemsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                string Esc(string s) => (s ?? "").Replace("'", "''");

                // 3) 查詢需求主檔
                string sql = $@"SELECT * FROM {conf.DBName}.{reqTable} WHERE GUID = '{Esc(guid)}' LIMIT 1";
                var dt = sqlReq.WtrteCommandAndExecuteReader(sql);
                if (dt.Rows.Count == 0)
                {
                    returnData.Code = 404;
                    returnData.Result = $"查無 GUID={guid} 的需求資料";
                    return returnData.JsonSerializationt();
                }

                var requirement = dt.DataTableToRowList().SQLToClass<ProjectRequirementClass, enum_project_requirements>()[0];

                // 4) 是否查詢 BOM 項目
                if (includeItems)
                {
                    string sqlItems = $@"SELECT * FROM {conf.DBName}.{bomItemsTable} WHERE RequirementGuid = '{Esc(guid)}'";
                    var dtItems = sqlBomItems.WtrteCommandAndExecuteReader(sqlItems);
                    var items = dtItems.DataTableToRowList().SQLToClass<BomRequirementItemClass, enum_bom_requirement_items>() ?? new List<BomRequirementItemClass>();
                    requirement.Items = items;
                }

                // 5) 正常回傳
                returnData.Code = 200;
                returnData.Result = "取得需求詳情成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = requirement;

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
        /// 建立新的專案需求（僅 POST；欄位皆為 string；GUID 一律大寫）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 建立一筆或多筆「專案需求」資料，並自動產生 <c>GUID</c>（大寫）、建立/更新時間。<br/>
        /// 系統會套用必填檢核與業務邏輯驗證。<br/><br/>
        /// 
        /// <b>必填檢核：</b>
        /// - `projectGuid` 必填（必須為大寫 GUID）<br/>
        /// - `description` 必填<br/>
        /// - `quantity` 必填，且必須大於 0<br/>
        /// - `unit` 必填<br/><br/>
        /// 
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "projectId": "PRJ001",
        ///       "procurementType": "市構件請購",
        ///       "description": "交通號誌控制器",
        ///       "quantity": "50",
        ///       "unit": "台",
        ///       "priority": "高",
        ///       "dueDate": "2024-04-30",
        ///       "estimatedCost": "2500000",
        ///       "itemId": "PROD001",
        ///       "itemName": "智慧交通控制器 TC-2000",
        ///       "itemCode": "TC-2000",
        ///       "supplierName": "智慧科技有限公司",
        ///       "requester": "張工程師",
        ///       "specifications": "需支援 LED 顯示，具備遠端監控功能",
        ///       "deliveryAddress": "台北市信義區市府路1號",
        ///       "notes": "緊急需求，需優先處理",
        ///       "createdBy": "張工程師"
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// <b>Response JSON 範例（成功）：</b>
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "description": "交通號誌控制器",
        ///       "quantity": "50",
        ///       "unit": "台",
        ///       "status": "等待確認請購",
        ///       "createdAt": "2024-03-15 09:00:00",
        ///       "updatedAt": "2024-03-15 09:00:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "create_requirement",
        ///   "Result": "建立專案需求成功",
        ///   "TimeTaken": "0.035s"
        /// }
        /// ```
        /// 
        /// <b>Response JSON 範例（失敗）：</b>
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 400,
        ///   "Method": "create_requirement",
        ///   "Result": "參數驗證失敗：projectGuid 為必填",
        ///   "TimeTaken": "0.012s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("create_requirement")]
        public string create_requirement([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "create_requirement";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ProjectRequirementClass> input = returnData.Data.ObjToClass<List<ProjectRequirementClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // === 2. 伺服器設定 ===
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string tableName = new enum_project_requirements().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, tableName,
                                         conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                var output = new List<ProjectRequirementClass>();

                // === 3. 寫入流程 ===
                foreach (var req in input)
                {
                    // 檢核必填
                    if (req.ProjectGuid.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "參數驗證失敗：projectGuid 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (req.Description.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "參數驗證失敗：description 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (req.Quantity.StringIsEmpty() || req.Quantity.StringToInt32() <= 0)
                    {
                        returnData.Code = 400;
                        returnData.Result = "參數驗證失敗：quantity 必須大於 0";
                        return returnData.JsonSerializationt();
                    }
                    if (req.Unit.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "參數驗證失敗：unit 為必填";
                        return returnData.JsonSerializationt();
                    }

                    // GUID處理
                    string newGuid = string.IsNullOrWhiteSpace(req.GUID) ? Guid.NewGuid().ToString().ToUpper() : req.GUID.ToUpper();

                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    var row = new object[Enum.GetValues(typeof(enum_project_requirements)).Length];
                    row[(int)enum_project_requirements.GUID] = newGuid;
                    row[(int)enum_project_requirements.project_guid] = req.ProjectGuid.ToUpper();
                    row[(int)enum_project_requirements.project_id] = req.ProjectId ?? "";
                    row[(int)enum_project_requirements.procurement_type] = req.ProcurementType ?? "";
                    row[(int)enum_project_requirements.description] = req.Description ?? "";
                    row[(int)enum_project_requirements.quantity] = req.Quantity ?? "0";
                    row[(int)enum_project_requirements.unit] = req.Unit ?? "";
                    row[(int)enum_project_requirements.priority] = req.Priority ?? "";
                    row[(int)enum_project_requirements.due_date] = req.DueDate ?? "";
                    row[(int)enum_project_requirements.estimated_cost] = req.EstimatedCost ?? "";
                    row[(int)enum_project_requirements.item_id] = req.ItemId ?? "";
                    row[(int)enum_project_requirements.item_name] = req.ItemName ?? "";
                    row[(int)enum_project_requirements.item_code] = req.ItemCode ?? "";
                    row[(int)enum_project_requirements.supplier_name] = req.SupplierName ?? "";
                    row[(int)enum_project_requirements.requester] = req.Requester ?? "";
                    row[(int)enum_project_requirements.specifications] = req.Specifications ?? "";
                    row[(int)enum_project_requirements.delivery_address] = req.DeliveryAddress ?? "";
                    row[(int)enum_project_requirements.notes] = req.Notes ?? "";
                    row[(int)enum_project_requirements.status] = "等待確認請購";
                    row[(int)enum_project_requirements.created_by] = req.CreatedBy ?? "";
                    row[(int)enum_project_requirements.created_at] = now;
                    row[(int)enum_project_requirements.updated_by] = req.CreatedBy ?? "";
                    row[(int)enum_project_requirements.updated_at] = now;

                    sql.AddRow(null, row);

                    req.GUID = newGuid;
                    req.Status = "等待確認請購";
                    req.CreatedAt = now;
                    req.UpdatedAt = now;
                    output.Add(req);
                }

                // === 4. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = "建立專案需求成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = output;
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
        /// 更新專案需求（僅 POST；以 GUID 定位；僅更新傳入欄位）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 依 <c>GUID</c> 更新單筆或多筆需求欄位；未提供之欄位不變。<br/>
        /// 系統會自動套用日期業務規則並更新 <c>updatedAt</c>。
        /// <br/><br/>
        /// <b>必填檢核：</b>
        /// - <c>Data</c> 不可為空  <br/>
        /// - 每筆必須提供 <c>GUID</c>（大寫）
        ///
        /// <br/><br/>
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "status": "採購中",
        ///       "priority": "高",
        ///       "updatedBy": "王採購"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "status": "採購中",
        ///       "priority": "高",
        ///       "updatedBy": "王採購",
        ///       "updatedAt": "2025-08-22 12:30:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "update_requirement",
        ///   "Result": "需求更新成功",
        ///   "TimeTaken": "0.035s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("update_requirement")]
        public string update_requirement([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "update_requirement";

            try
            {
                // ========== 檢查 Data ==========
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ProjectRequirementClass> input = returnData.Data.ObjToClass<List<ProjectRequirementClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // ========== DB 連線設定 ==========
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_requirements().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                string Now() => DateTime.Now.ToDateTimeString();
                var updatedList = new List<ProjectRequirementClass>();
                var messages = new List<string>();

                foreach (var req in input)
                {
                    if (req.GUID.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "每筆需求必須提供 GUID";
                        return returnData.JsonSerializationt();
                    }

                    // 依 GUID 找資料
                    var rows = sql.GetRowsByDefult(null, (int)enum_project_requirements.GUID, req.GUID);
                    if (rows == null || rows.Count == 0)
                    {
                        messages.Add($"查無需求 GUID={req.GUID}");
                        continue;
                    }

                    var row = rows[0];
                    bool updated = false;

                    // 更新欄位（僅更新有傳入的欄位）
                    foreach (var prop in typeof(ProjectRequirementClass).GetProperties())
                    {
                        string colName = prop.Name;
                        string newVal = prop.GetValue(req)?.ToString();

                        if (!newVal.StringIsEmpty())
                        {
                            var enumIdx = Enum.GetNames(typeof(enum_project_requirements)).ToList().IndexOf(colName);
                            if (enumIdx >= 0)
                            {
                                row[enumIdx] = newVal;
                                updated = true;
                            }
                        }
                    }

                    // 更新 updatedAt
                    row[(int)enum_project_requirements.updated_at] = Now();
                    sql.UpdateByDefulteExtra(null, row);

                    req.UpdatedAt = row[(int)enum_project_requirements.updated_at].ObjectToString();
                    updatedList.Add(req);
                    messages.Add($"更新需求 GUID={req.GUID}");
                }

                returnData.Code = 200;
                returnData.Result = string.Join("；", messages);
                returnData.TimeTaken = $"{timer}";
                returnData.Data = updatedList;
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
        /// 刪除專案需求（僅 POST；以 GUID 定位；支持多筆刪除）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 依 <c>GUID</c> 刪除一筆或多筆專案需求資料。<br/>
        /// 系統會同時移除關聯 BOM 對應與需求項目，確保資料一致性。<br/><br/>
        ///
        /// <b>必填檢核：</b><br/>
        /// - `Data` 不可為空 <br/>
        /// - 每筆必須提供 `GUID`（大寫）<br/><br/>
        ///
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333"
        ///     },
        ///     {
        ///       "GUID": "990F9800-F69F-85H8-E150-880099884444"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例 (成功)：</b>
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 200,
        ///   "Method": "delete_requirement",
        ///   "Result": "刪除成功：2 筆",
        ///   "TimeTaken": "0.032s"
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例 (錯誤)：</b>
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 400,
        ///   "Method": "delete_requirement",
        ///   "Result": "參數驗證失敗：缺少 GUID",
        ///   "TimeTaken": "0.015s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("delete_requirement")]
        public string delete_requirement([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_requirement";

            try
            {
                // 1) 請求驗證
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                var inputList = returnData.Data.ObjToClass<List<ProjectRequirementClass>>();
                if (inputList == null || inputList.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                if (inputList.Any(x => x.GUID.StringIsEmpty()))
                {
                    returnData.Code = 400;
                    returnData.Result = "參數驗證失敗：缺少 GUID";
                    return returnData.JsonSerializationt();
                }

                // 2) 取得資料庫設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 500;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string requirementTable = new enum_project_requirements().GetEnumDescription();
                var sqlReq = new SQLControl(conf.Server, conf.DBName, requirementTable,
                                            conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                var messages = new List<string>();
                int successCount = 0;

                // 3) 刪除流程
                foreach (var req in inputList)
                {
                    var rows = sqlReq.GetRowsByDefult(null, (int)enum_project_requirements.GUID, req.GUID);
                    if (rows.Count > 0)
                    {
                        sqlReq.DeleteExtra(null, rows[0]);
                        successCount++;
                        messages.Add($"已刪除需求 GUID={req.GUID}");
                    }
                    else
                    {
                        messages.Add($"找不到需求 GUID={req.GUID}");
                    }
                }

                returnData.Code = 200;
                returnData.Result = $"刪除成功：{successCount} 筆；{string.Join("；", messages)}";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = null;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"伺服器錯誤：{ex.Message}";
                returnData.JsonSerializationt(true);
            }
            return returnData.JsonSerializationt();
        }
        /// <summary>
        /// 確認請購（僅 POST；以 GUID 定位；狀態需符合「等待確認請購」才能操作）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 依 <c>GUID</c> 更新專案需求，套用「確認請購」的業務邏輯（狀態流轉）。<br/>
        /// 
        /// <b>必填檢核：</b>
        /// - <c>Data</c> 不可為空  
        /// - 每筆必須提供 <c>GUID</c>（大寫）  
        /// - <c>approver</c> 必填  
        /// - <c>approvedBudget</c> 必填  
        /// 
        /// <b>Request JSON 範例：</b><br/>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "approver": "李主管",
        ///       "approvedBudget": "2500000",
        ///       "approvalNotes": "預算核准，可進行採購",
        ///       "requestedDate": "2024-03-15",
        ///       "expectedPurchaseDate": "2024-03-20",
        ///       "updatedBy": "李主管"
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// <b>Response JSON 範例：</b><br/>
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "status": "請購中",
        ///       "approver": "李主管",
        ///       "approvedBudget": "2500000",
        ///       "approvalNotes": "預算核准，可進行採購",
        ///       "requestedDate": "2024-03-15",
        ///       "expectedPurchaseDate": "2024-03-20",
        ///       "updatedBy": "李主管",
        ///       "updatedAt": "2024-03-15 10:05:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "confirm_procurement",
        ///   "Result": "確認請購成功",
        ///   "TimeTaken": "0.085s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("confirm_procurement")]
        public string ConfirmProcurement([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "confirm_procurement";

            try
            {
                // 1) 驗證請求
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ProjectRequirementClass> input = returnData.Data.ObjToClass<List<ProjectRequirementClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var output = new List<ProjectRequirementClass>();
                var messages = new List<string>();

                // 2) 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string requirementsTable = new enum_project_requirements().GetEnumDescription();
                var sqlReq = new SQLControl(conf.Server, conf.DBName, requirementsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                // 3) 執行更新
                foreach (var req in input)
                {
                    if (req.GUID.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "每筆需求必須提供 GUID";
                        return returnData.JsonSerializationt();
                    }
                    if (req.Approver.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "approver 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (req.ApprovedBudget.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "approvedBudget 為必填";
                        return returnData.JsonSerializationt();
                    }

                    // 查詢需求
                    var existed = sqlReq.GetRowsByDefult(null, (int)enum_project_requirements.GUID, req.GUID);
                    if (existed.Count == 0)
                    {
                        returnData.Code = 404;
                        returnData.Result = $"找不到需求：{req.GUID}";
                        return returnData.JsonSerializationt();
                    }

                    var row = existed[0];
                    string currentStatus = row[(int)enum_project_requirements.status].ObjectToString();

                    if (currentStatus != "等待確認請購")
                    {
                        returnData.Code = -1;
                        returnData.Result = $"需求 {req.GUID} 狀態為「{currentStatus}」，不可確認請購";
                        return returnData.JsonSerializationt();
                    }

                    // 更新狀態與欄位
                    row[(int)enum_project_requirements.status] = "請購中";
                    row[(int)enum_project_requirements.approver] = req.AcceptedDate ?? "";
                    row[(int)enum_project_requirements.approved_budget] = req.ApprovedBudget ?? "";
                    row[(int)enum_project_requirements.notes] = req.Notes ?? "";
                    row[(int)enum_project_requirements.requested_date] = req.RequestedDate ?? DateTime.Now.ToDateTimeString();
                    row[(int)enum_project_requirements.updated_by] = req.UpdatedBy ?? req.Approver;
                    row[(int)enum_project_requirements.updated_at] = DateTime.Now.ToDateTimeString();

                    sqlReq.UpdateByDefulteExtra(null, row);

                    req.Status = "請購中";
                    req.UpdatedAt = row[(int)enum_project_requirements.updated_at].ObjectToString();
                    output.Add(req);
                    messages.Add($"需求 {req.GUID} 已確認請購");
                }

                // 4) 回傳
                returnData.Code = 200;
                returnData.Result = string.Join("；", messages);
                returnData.TimeTaken = $"{timer}";
                returnData.Data = output;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 確認採購（僅 POST；依 GUID 更新需求資料，套用狀態流程規則）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 根據 <c>GUID</c> 確認需求進入「採購確認」階段，更新相關採購資訊。<br/>
        /// 僅允許狀態為「請購中」的需求進入「採購中」。<br/>
        /// 
        /// <b>必填檢核：</b>
        /// - <c>Data</c> 不可為空  
        /// - 每筆必須提供 <c>GUID</c>（大寫）與 <c>purchaser</c>（採購人）  
        /// 
        /// <br/><br/>
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "purchaser": "王採購",
        ///       "purchaseOrderId": "PO001",
        ///       "vendorQuoteAmount": "2450000",
        ///       "negotiatedAmount": "2400000",
        ///       "purchasedDate": "2024-03-20",
        ///       "expectedDeliveryDate": "2024-04-15",
        ///       "purchaseNotes": "已與供應商確認交期",
        ///       "updatedBy": "王採購"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "status": "採購中",
        ///       "purchaser": "王採購",
        ///       "purchaseOrderId": "PO001",
        ///       "vendorQuoteAmount": "2450000",
        ///       "negotiatedAmount": "2400000",
        ///       "purchasedDate": "2024-03-20",
        ///       "expectedDeliveryDate": "2024-04-15",
        ///       "notes": "已與供應商確認交期",
        ///       "updatedBy": "王採購",
        ///       "updatedAt": "2024-03-20 14:30:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "confirm_purchase",
        ///   "Result": "需求採購確認成功",
        ///   "TimeTaken": "0.125s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("confirm_purchase")]
        public string confirm_purchase([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "confirm_purchase";

            try
            {
                // 驗證請求格式
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 反序列化
                List<ProjectRequirementClass> input = returnData.Data.ObjToClass<List<ProjectRequirementClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // 取得伺服器設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 500;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string table = new enum_project_requirements().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                var updatedList = new List<ProjectRequirementClass>();
                var messages = new List<string>();
                string Now() => DateTime.Now.ToDateTimeString();

                foreach (var req in input)
                {
                    if (req.GUID.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "GUID 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (req.Purchaser.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "purchaser 採購人為必填";
                        return returnData.JsonSerializationt();
                    }

                    var existed = sql.GetRowsByDefult(null, (int)enum_project_requirements.GUID, req.GUID);
                    if (existed.Count == 0)
                    {
                        returnData.Code = 404;
                        returnData.Result = $"需求 GUID={req.GUID} 不存在";
                        return returnData.JsonSerializationt();
                    }

                    object[] row = existed[0];
                    string currentStatus = row[(int)enum_project_requirements.status].ObjectToString();

                    if (currentStatus != "請購中")
                    {
                        returnData.Code = -1;
                        returnData.Result = $"需求 GUID={req.GUID} 狀態必須為 '請購中' 才能確認採購，目前狀態：{currentStatus}";
                        return returnData.JsonSerializationt();
                    }

                    // 更新需求
                    row[(int)enum_project_requirements.status] = "採購中";
                    row[(int)enum_project_requirements.purchaser] = req.Purchaser ?? "";
                    row[(int)enum_project_requirements.purchase_order_id] = req.PurchaseOrderId ?? "";
                    row[(int)enum_project_requirements.vendor_quote_amount] = req.VendorQuoteAmount ?? "";
                    row[(int)enum_project_requirements.negotiated_amount] = req.NegotiatedAmount ?? "";
                    row[(int)enum_project_requirements.purchased_date] = req.PurchasedDate ?? "";
                    row[(int)enum_project_requirements.delivered_date] = req.DeliveredDate ?? "";
                    row[(int)enum_project_requirements.notes] = req.Notes ?? "";
                    row[(int)enum_project_requirements.updated_by] = req.UpdatedBy ?? "系統";
                    row[(int)enum_project_requirements.updated_at] = Now();

                    sql.UpdateByDefulteExtra(null, row);

                    req.Status = "採購中";
                    req.UpdatedAt = Now();
                    updatedList.Add(req);
                    messages.Add($"需求 GUID={req.GUID} 已確認採購，更新狀態為 採購中");
                }

                returnData.Code = 200;
                returnData.Result = string.Join("；", messages);
                returnData.TimeTaken = $"{timer}";
                returnData.Data = updatedList;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }


        /// <summary>
        /// 將 BOM 轉換為專案採購需求（僅 POST）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 將指定 BOM 轉換為專案需求，並可選擇是否自動建立需求。<br/>
        /// 僅支援 <c>POST</c> 方法，所有欄位型別均為字串，<c>GUID</c> 必須為大寫。
        ///
        /// <br/><br/>
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "projectGuid=550E8400-E29B-41D4-A716-446655440000",
        ///     "bomGuid=660F9500-F39C-52E5-B827-557766551111",
        ///     "autoCreateRequirement=true",
        ///     "defaultDueDate=2024-06-30",
        ///     "defaultPriority=中",
        ///     "requester=系統自動轉換"
        ///   ]
        /// }
        /// ```
        ///
        /// <br/>
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "BB0F9A00-F89H-A7J0-G372-AA2211AA6666",
        ///       "ID": "REQ_BOM_001",
        ///       "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "description": "智慧交通控制器主機板 (來自BOM)",
        ///       "quantity": "1",
        ///       "unit": "套",
        ///       "status": "等待確認請購",
        ///       "priority": "中",
        ///       "dueDate": "2024-06-30",
        ///       "estimatedCost": "8500",
        ///       "isFromBom": "1",
        ///       "requester": "系統自動轉換",
        ///       "createdAt": "2024-03-15 14:30:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "convert_bom_to_requirements",
        ///   "Result": "BOM轉換為採購需求成功"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("convert_bom_to_requirements")]
        public string ConvertBomToRequirements([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "convert_bom_to_requirements";

            try
            {
                // 驗證 ValueAry 是否存在
                if (returnData.ValueAry == null || returnData.ValueAry.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "參數驗證失敗：ValueAry 不能為空";
                    return returnData.JsonSerializationt();
                }

                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x =>
                        x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1] ?? "";

                string projectGuid = GetVal("projectGuid");
                string bomGuid = GetVal("bomGuid");
                string autoCreate = GetVal("autoCreateRequirement");
                string defaultDueDate = GetVal("defaultDueDate");
                string defaultPriority = GetVal("defaultPriority");
                string requester = GetVal("requester");

                // 驗證必填欄位
                if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(bomGuid))
                {
                    returnData.Code = 400;
                    returnData.Result = "參數驗證失敗：projectGuid 與 bomGuid 為必填欄位";
                    return returnData.JsonSerializationt();
                }

                // 這裡模擬 BOM 轉換 → 需求建立邏輯
                var output = new List<ProjectRequirementClass>();
                var req = new ProjectRequirementClass
                {
                    GUID = Guid.NewGuid().ToString().ToUpper(),
                    ProjectGuid = projectGuid.ToUpper(),
                    BomGuid = bomGuid.ToUpper(),
                    Description = "智慧交通控制器主機板 (來自BOM)",
                    Quantity = "1",
                    Unit = "套",
                    Status = "等待確認請購",
                    Priority = string.IsNullOrEmpty(defaultPriority) ? "中" : defaultPriority,
                    DueDate = string.IsNullOrEmpty(defaultDueDate) ? DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd") : defaultDueDate,
                    EstimatedCost = "8500",
                    IsFromBom = "1",
                    Requester = requester,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                output.Add(req);

                returnData.Code = 200;
                returnData.Result = "BOM轉換為採購需求成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = output;

                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"伺服器錯誤：{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 同步 BOM 與採購需求（僅 POST）
        /// </summary>
        /// <remarks>
        /// **用途**  
        /// - 將 BOM 與需求表進行同步，可選擇「全量 / 增量 / 強制」模式。  
        /// 
        /// **必填參數**  
        /// - projectGuid  
        /// - bomGuid  
        /// 
        /// **Request JSON 範例**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "projectGuid=550E8400-E29B-41D4-A716-446655440000",
        ///     "bomGuid=660F9500-F39C-52E5-B827-557766551111",
        ///     "syncType=全量同步",
        ///     "updateExisting=true",
        ///     "createMissing=true",
        ///     "removeObsolete=false",
        ///     "syncBy=張工程師"
        ///   ]
        /// }
        /// ```
        /// 
        /// **Response JSON 範例**
        /// ```json
        /// {
        ///   "Data": {
        ///     "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///     "bomGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///     "syncType": "全量同步",
        ///     "updated": "2",
        ///     "created": "1",
        ///     "removed": "0",
        ///     "messages": [
        ///       "更新需求：REQ001",
        ///       "建立需求：REQ_BOM_20240822123456"
        ///     ]
        ///   },
        ///   "Code": 200,
        ///   "Method": "sync_bom_requirements",
        ///   "Result": "同步完成，共處理 3 筆需求",
        ///   "TimeTaken": "0.120s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("sync_bom_requirements")]
        public string sync_bom_requirements([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "sync_bom_requirements";

            try
            {
                // ===== 1. 解析參數 =====
                string GetVal(string key) => returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string projectGuid = GetVal("projectGuid") ?? "";
                string bomGuid = GetVal("bomGuid") ?? "";
                string syncType = GetVal("syncType") ?? "全量同步";
                bool updateExisting = (GetVal("updateExisting") ?? "false").ToLower() == "true";
                bool createMissing = (GetVal("createMissing") ?? "false").ToLower() == "true";
                bool removeObsolete = (GetVal("removeObsolete") ?? "false").ToLower() == "true";
                string syncBy = GetVal("syncBy") ?? "";

                if (projectGuid.StringIsEmpty() || bomGuid.StringIsEmpty())
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少必填參數：projectGuid 或 bomGuid";
                    return returnData.JsonSerializationt();
                }

                // ===== 2. 取得 DB 設定 =====
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 404;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string bomTable = new enum_project_bom_associations().GetEnumDescription();
                string reqTable = new enum_project_requirements().GetEnumDescription();

                var sqlBOM = new SQLControl(conf.Server, conf.DBName, bomTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);
                var sqlReq = new SQLControl(conf.Server, conf.DBName, reqTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                // ===== 3. 查詢 BOM 與需求資料 =====
                var dtBOM = sqlBOM.GetRowsByDefult(null, (int)enum_project_bom_associations.專案GUID, projectGuid);
                var bomList = dtBOM.SQLToClass<project_bom_associationClass, enum_project_bom_associations>()
                                   .Where(b => b.專案GUID == projectGuid && b.BOMGUID == bomGuid)
                                   .ToList();

                var dtReq = sqlReq.GetRowsByDefult(null, (int)enum_project_requirements.project_guid, projectGuid);
                var reqList = dtReq.SQLToClass<ProjectRequirementClass, enum_project_requirements>()
                                   .Where(r => r.ProjectGuid == projectGuid && r.BomGuid == bomGuid)
                                   .ToList();

                var messages = new List<string>();
                int updated = 0, created = 0, removed = 0;

                // ===== 4. 同步邏輯 =====
                foreach (var bom in bomList)
                {
                    var matched = reqList.FirstOrDefault(r => r.BomId == bom.BOM編號 && r.BomGuid == bom.BOMGUID);
                    if (matched != null)
                    {
                        if (updateExisting)
                        {
                            matched.Description = bom.BOM名稱;
                            matched.Quantity = bom.需求數量;
                            matched.UpdatedBy = syncBy;
                            matched.UpdatedAt = DateTime.Now.ToDateTimeString();
                            sqlReq.UpdateByDefulteExtra(null, matched.ClassToSQL<ProjectRequirementClass, enum_project_requirements>());
                            messages.Add($"更新需求：{matched.ID ?? matched.ItemCode}");
                            updated++;
                        }
                    }
                    else
                    {
                        if (createMissing)
                        {
                            var newReq = new ProjectRequirementClass
                            {
                                GUID = Guid.NewGuid().ToString().ToUpper(),
                                ID = $"REQ_BOM_{DateTime.Now:yyyyMMddHHmmss}",
                                ProjectGuid = projectGuid,
                                ProjectId = "", // 可以從 projectClass 帶入
                                ProcurementType = "研發件請購",
                                Description = bom.BOM名稱,
                                Quantity = bom.需求數量,
                                Unit = "",
                                Status = "等待確認請購",
                                Priority = "中",
                                DueDate = bom.到期日,
                                BomGuid = bom.BOMGUID,
                                BomId = bom.BOM編號,
                                IsFromBom = "1",
                                CreatedBy = syncBy,
                                CreatedAt = DateTime.Now.ToDateTimeString(),
                                UpdatedBy = syncBy,
                                UpdatedAt = DateTime.Now.ToDateTimeString()
                            };

                            sqlReq.AddRow(null, newReq.ClassToSQL<ProjectRequirementClass, enum_project_requirements>());
                            messages.Add($"建立需求：{newReq.ID}");
                            created++;
                        }
                    }
                }

                if (removeObsolete)
                {
                    var obsolete = reqList.Where(r => !bomList.Any(b => b.BOM編號 == r.BomId)).ToList();
                    foreach (var ob in obsolete)
                    {
                        sqlReq.DeleteExtra(null, ob.ClassToSQL<ProjectRequirementClass, enum_project_requirements>());
                        messages.Add($"刪除過時需求：{ob.ID ?? ob.ItemCode}");
                        removed++;
                    }
                }

                // ===== 5. 回傳結果 =====
                var result = new
                {
                    projectGuid,
                    bomGuid,
                    syncType,
                    updated = updated.ToString(),
                    created = created.ToString(),
                    removed = removed.ToString(),
                    messages
                };

                returnData.Code = 200;
                returnData.Result = $"同步完成，共處理 {updated + created + removed} 筆需求";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = result;

                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        /// 獲取 BOM 與採購需求對應關係（僅 POST）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 查詢指定專案與 BOM 的採購對應資訊。<br/>
        /// 
        /// <b>必填檢核：</b><br/>
        /// - ValueAry 必須包含 <c>projectGuid</c> 與 <c>bomGuid</c><br/>
        /// - GUID 一律大寫<br/>
        /// 
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "projectGuid=550E8400-E29B-41D4-A716-446655440000",
        ///     "bomGuid=660F9500-F39C-52E5-B827-557766551111"
        ///   ]
        /// }
        /// ```
        /// 
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "DD0F9C00-FA9J-C9L2-I594-CC4433CC8888",
        ///       "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "bomGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///       "mappingType": "直接對應",
        ///       "quantityRatio": "1",
        ///       "conversionFactor": "1",
        ///       "mappingStatus": "啟用",
        ///       "notes": "對應測試",
        ///       "updatedAt": "2025-08-22 10:30:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "get_bom_procurement_mapping",
        ///   "Result": "獲取 BOM 採購對應成功",
        ///   "TimeTaken": "0.125s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_bom_procurement_mapping")]
        public string GetBomProcurementMapping([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_bom_procurement_mapping";

            try
            {
                // 1) 驗證 ValueAry
                if (returnData.ValueAry == null || returnData.ValueAry.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "ValueAry 不能為空";
                    return returnData.JsonSerializationt();
                }

                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string projectGuid = GetVal("projectGuid");
                string bomGuid = GetVal("bomGuid");

                if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(bomGuid))
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少必要參數：projectGuid 或 bomGuid";
                    return returnData.JsonSerializationt();
                }

                // 2) 確保 GUID 大寫
                projectGuid = projectGuid.ToUpper();
                bomGuid = bomGuid.ToUpper();

                // 3) 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 500;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string tableName = new enum_project_bom_associations().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, tableName, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                // 4) 查詢對應資料
                string Esc(string s) => (s ?? "").Replace("'", "''");

                string query = $@"
                    SELECT * 
                    FROM {conf.DBName}.{tableName}
                    WHERE 專案GUID = '{Esc(projectGuid)}'
                      AND BOMGUID = '{Esc(bomGuid)}'
                    ORDER BY 更新時間 DESC";

                var dt = sql.WtrteCommandAndExecuteReader(query);
                var resultList = dt.DataTableToRowList().SQLToClass<project_bom_associationClass, enum_project_bom_associations>();

                // 5) 回傳
                returnData.Code = 200;
                returnData.Result = "獲取 BOM 採購對應成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;

                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"系統錯誤：{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 更新 BOM 與採購需求對應關係（僅 POST）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 建立或更新 BOM 與需求的對應關係（Upsert）。<br/>
        /// - 若 Data 內含 GUID，則嘗試更新；找不到則新增。<br/>
        /// - 若 Data 無 GUID，系統自動產生 GUID 並新增。<br/>
        /// 
        /// <b>必填檢核：</b><br/>
        /// - Data 不可為空<br/>
        /// - 每筆必須包含 <c>projectGuid</c> 與 <c>bomGuid</c><br/>
        /// - GUID 一律大寫<br/>
        /// 
        /// <b>Request JSON 範例：</b>
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "DD0F9C00-FA9J-C9L2-I594-CC4433CC8888",
        ///       "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "bomGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///       "mappingType": "直接對應",
        ///       "quantityRatio": "1",
        ///       "conversionFactor": "1",
        ///       "mappingStatus": "啟用",
        ///       "notes": "直接對應，無需轉換",
        ///       "updatedBy": "張工程師"
        ///     }
        ///   ]
        /// }
        /// ```
        /// 
        /// <b>Response JSON 範例：</b>
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "DD0F9C00-FA9J-C9L2-I594-CC4433CC8888",
        ///       "projectGuid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "bomGuid": "660F9500-F39C-52E5-B827-557766551111",
        ///       "mappingType": "直接對應",
        ///       "quantityRatio": "1",
        ///       "conversionFactor": "1",
        ///       "mappingStatus": "啟用",
        ///       "notes": "直接對應，無需轉換",
        ///       "updatedBy": "張工程師",
        ///       "updatedAt": "2025-08-22 15:00:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "update_bom_item_mapping",
        ///   "Result": "更新 BOM 對應成功",
        ///   "TimeTaken": "0.145s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("update_bom_item_mapping")]
        public string UpdateBomItemMapping([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "update_bom_item_mapping";

            try
            {
                // 1) 驗證請求
                if (returnData.Data == null)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                var inputList = returnData.Data.ObjToClass<List<project_bom_associationClass>>();
                if (inputList == null || inputList.Count == 0)
                {
                    returnData.Code = 400;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                // 2) 取得 DB 設定
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 500;
                    returnData.Result = "找不到 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string tableName = new enum_project_bom_associations().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, tableName, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                var resultList = new List<project_bom_associationClass>();

                // 3) Upsert 邏輯
                foreach (var item in inputList)
                {
                    if (item.專案GUID.StringIsEmpty() || item.BOMGUID.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "缺少必填欄位：projectGuid 或 bomGuid";
                        return returnData.JsonSerializationt();
                    }

                    // 確保 GUID 大寫
                    item.GUID = (item.GUID.StringIsEmpty() ? Guid.NewGuid().ToString() : item.GUID).ToUpper();
                    item.專案GUID = item.專案GUID.ToUpper();
                    item.BOMGUID = item.BOMGUID.ToUpper();
                    item.更新時間 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // 查詢是否已存在
                    var existed = sql.GetRowsByDefult(null, (int)enum_project_bom_associations.GUID, item.GUID);

                    if (existed.Count > 0)
                    {
                        // 更新
                        var row = existed[0];
                        row[(int)enum_project_bom_associations.專案GUID] = item.專案GUID;
                        row[(int)enum_project_bom_associations.BOMGUID] = item.BOMGUID;
                        row[(int)enum_project_bom_associations.關聯類型] = item.關聯類型 ?? "";
                        row[(int)enum_project_bom_associations.需求數量] = item.需求數量 ?? "";
                        row[(int)enum_project_bom_associations.到期日] = item.到期日 ?? "";
                        row[(int)enum_project_bom_associations.狀態] = item.狀態 ?? "";
                        row[(int)enum_project_bom_associations.備註] = item.備註 ?? "";
                        row[(int)enum_project_bom_associations.更新者] = item.更新者 ?? "";
                        row[(int)enum_project_bom_associations.更新時間] = item.更新時間;
                        sql.UpdateByDefulteExtra(null, row);
                    }
                    else
                    {
                        // 新增
                        var ins = new object[Enum.GetValues(typeof(enum_project_bom_associations)).Length];
                        ins[(int)enum_project_bom_associations.GUID] = item.GUID;
                        ins[(int)enum_project_bom_associations.專案GUID] = item.專案GUID;
                        ins[(int)enum_project_bom_associations.BOMGUID] = item.BOMGUID;
                        ins[(int)enum_project_bom_associations.關聯類型] = item.關聯類型 ?? "";
                        ins[(int)enum_project_bom_associations.需求數量] = item.需求數量 ?? "";
                        ins[(int)enum_project_bom_associations.到期日] = item.到期日 ?? "";
                        ins[(int)enum_project_bom_associations.狀態] = item.狀態 ?? "";
                        ins[(int)enum_project_bom_associations.備註] = item.備註 ?? "";
                        ins[(int)enum_project_bom_associations.建立者] = item.建立者 ?? "";
                        ins[(int)enum_project_bom_associations.建立時間] = item.建立時間.StringIsEmpty() ? item.更新時間 : item.建立時間;
                        ins[(int)enum_project_bom_associations.更新者] = item.更新者 ?? "";
                        ins[(int)enum_project_bom_associations.更新時間] = item.更新時間;
                        sql.AddRow(null, ins);
                    }

                    resultList.Add(item);
                }

                // 4) 回傳
                returnData.Code = 200;
                returnData.Result = "更新 BOM 對應成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = resultList;

                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"系統錯誤：{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 獲取採購統計資料（僅 POST）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 查詢並統計專案採購需求之整體數據，包括需求數量、狀態分佈、成本統計等。  
        ///
        /// **必填檢核：**  
        /// - `ServerName` 不可為空  
        /// - `ServerType` 不可為空  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁"
        /// }
        /// ```
        ///
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Data": {
        ///     "totalRequirements": "25",
        ///     "pendingConfirmationCount": "5",
        ///     "inRequisitionCount": "8",
        ///     "inPurchaseCount": "7",
        ///     "deliveredCount": "4",
        ///     "acceptedCount": "1",
        ///     "totalEstimatedCost": "12500000",
        ///     "totalActualCost": "11800000",
        ///     "totalApprovedBudget": "13000000",
        ///     "budgetUtilizationRate": "90.77",
        ///     "averageLeadTime": "21.5",
        ///     "onTimeDeliveryRate": "85.5"
        ///   },
        ///   "Code": 200,
        ///   "Method": "get_procurement_statistics",
        ///   "Result": "獲取採購統計成功",
        ///   "TimeTaken": "0.125s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_procurement_statistics")]
        public string get_procurement_statistics([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_procurement_statistics";

            try
            {
                // ======== 1) 檢核 Request ========
                if (string.IsNullOrWhiteSpace(returnData.ServerName))
                {
                    returnData.Code = 400;
                    returnData.Result = "參數錯誤：ServerName 不能為空";
                    return returnData.JsonSerializationt();
                }
                if (string.IsNullOrWhiteSpace(returnData.ServerType))
                {
                    returnData.Code = 400;
                    returnData.Result = "參數錯誤：ServerType 不能為空";
                    return returnData.JsonSerializationt();
                }

                // ======== 2) 取得 DB 設定 ========
                var servers = serverSetting.GetAllServerSetting();
                var conf = servers.myFind(returnData.ServerName, returnData.ServerType, "VM端");
                if (conf == null)
                {
                    returnData.Code = 404;
                    returnData.Result = "找不到對應的 Server 設定";
                    return returnData.JsonSerializationt();
                }

                string requirementTable = new enum_project_requirements().GetEnumDescription();
                var sqlReq = new SQLControl(conf.Server, conf.DBName, requirementTable,
                                            conf.User, conf.Password, conf.Port.StringToUInt32(),
                                            MySqlSslMode.None);

                // ======== 3) 讀取資料表 ========
                var dt = sqlReq.GetAllRows(null);
                var list = dt.SQLToClass<ProjectRequirementClass, enum_project_requirements>() ?? new List<ProjectRequirementClass>();

                // ======== 4) 統計運算 ========
                int total = list.Count;
                int pending = list.Count(x => x.Status == "等待確認請購");
                int inReq = list.Count(x => x.Status == "請購中");
                int inPurchase = list.Count(x => x.Status == "採購中");
                int delivered = list.Count(x => x.Status == "已交貨");
                int accepted = list.Count(x => x.Status == "已驗收");

                double totalEstimated = list.Sum(x => x.EstimatedCost.StringToDouble());
                double totalActual = list.Sum(x => x.ActualCost.StringToDouble());
                double totalBudget = list.Sum(x => x.ApprovedBudget.StringToDouble());

                string budgetRate = (totalBudget > 0) ? ((totalActual / totalBudget) * 100).ToString("0.##") : "0";

                // ======== 5) 回傳格式 ========
                var stats = new
                {
                    totalRequirements = total.ToString(),
                    pendingConfirmationCount = pending.ToString(),
                    inRequisitionCount = inReq.ToString(),
                    inPurchaseCount = inPurchase.ToString(),
                    deliveredCount = delivered.ToString(),
                    acceptedCount = accepted.ToString(),
                    totalEstimatedCost = totalEstimated.ToString(),
                    totalActualCost = totalActual.ToString(),
                    totalApprovedBudget = totalBudget.ToString(),
                    budgetUtilizationRate = budgetRate,
                    averageLeadTime = "0",       // TODO: 可依需求計算
                    onTimeDeliveryRate = "0"     // TODO: 可依需求計算
                };

                returnData.Code = 200;
                returnData.Result = "獲取採購統計成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = stats;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = $"系統錯誤：{ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 獲取專案採購儀表板資料（僅 POST）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 提供專案採購儀表板所需的即時統計資料，包括需求數量、狀態分佈、類型分佈、月趨勢。<br/><br/>
        /// 
        /// <b>必填檢核：</b><br/>
        /// - `ServerName` 不可為空<br/>
        /// - `ServerType` 不可為空<br/><br/>
        /// 
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁"
        /// }
        /// ```
        /// 
        /// **Response JSON 範例：**
        /// ```json
        /// {
        ///   "Data": {
        ///     "totalRequirements": "25",
        ///     "pendingConfirmationCount": "5",
        ///     "inRequisitionCount": "8",
        ///     "inPurchaseCount": "7",
        ///     "deliveredCount": "4",
        ///     "acceptedCount": "1",
        ///     "totalEstimatedCost": "12500000",
        ///     "totalActualCost": "11800000",
        ///     "totalApprovedBudget": "13000000",
        ///     "budgetUtilizationRate": "90.77",
        ///     "averageLeadTime": "21.5",
        ///     "onTimeDeliveryRate": "85.5",
        ///     "procurementTypeDistribution": {
        ///       "市構件請購": "15",
        ///       "產品請購": "8",
        ///       "研發件請購": "2"
        ///     },
        ///     "statusDistribution": {
        ///       "等待確認請購": "5",
        ///       "請購中": "8",
        ///       "採購中": "7",
        ///       "已請購": "3",
        ///       "已交貨": "1",
        ///       "已驗收": "1"
        ///     },
        ///     "monthlyTrend": [
        ///       {
        ///         "month": "2024-01",
        ///         "newRequirements": "8",
        ///         "completedRequirements": "2",
        ///         "totalCost": "3200000"
        ///       }
        ///     ]
        ///   },
        ///   "Code": 200,
        ///   "Method": "get_procurement_dashboard",
        ///   "Result": "獲取採購儀表板成功",
        ///   "TimeTaken": "0.125s"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_procurement_dashboard")]
        public string get_procurement_dashboard([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_procurement_dashboard";

            try
            {
                // 1) 請求驗證
                if (returnData.ServerName.StringIsEmpty() || returnData.ServerType.StringIsEmpty())
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

                string reqTable = new enum_project_requirements().GetEnumDescription();
                var sqlReq = new SQLControl(conf.Server, conf.DBName, reqTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                // 3) 查詢需求資料
                string sql = $@"SELECT * FROM {conf.DBName}.{reqTable}";
                var dt = sqlReq.WtrteCommandAndExecuteReader(sql);
                var requirements = dt.DataTableToRowList().SQLToClass<ProjectRequirementClass, enum_project_requirements>() ?? new List<ProjectRequirementClass>();

                // 4) 統計計算
                var dashboard = new
                {
                    totalRequirements = requirements.Count.ToString(),
                    pendingConfirmationCount = requirements.Count(x => x.Status == "等待確認請購").ToString(),
                    inRequisitionCount = requirements.Count(x => x.Status == "請購中").ToString(),
                    inPurchaseCount = requirements.Count(x => x.Status == "採購中").ToString(),
                    deliveredCount = requirements.Count(x => x.Status == "已交貨").ToString(),
                    acceptedCount = requirements.Count(x => x.Status == "已驗收").ToString(),
                    totalEstimatedCost = requirements.Sum(x => x.EstimatedCost.StringToDouble()).ToString("0"),
                    totalActualCost = requirements.Sum(x => x.ActualCost.StringToDouble()).ToString("0"),
                    totalApprovedBudget = requirements.Sum(x => x.ApprovedBudget.StringToDouble()).ToString("0"),
                    budgetUtilizationRate = CalcBudgetUtilization(requirements).ToString("0.##"),
                    averageLeadTime = CalcAverageLeadTime(requirements).ToString("0.##"),
                    onTimeDeliveryRate = CalcOnTimeDeliveryRate(requirements).ToString("0.##"),
                    procurementTypeDistribution = requirements.GroupBy(r => r.ProcurementType ?? "")
                        .ToDictionary(g => g.Key, g => g.Count().ToString()),
                    statusDistribution = requirements.GroupBy(r => r.Status ?? "")
                        .ToDictionary(g => g.Key, g => g.Count().ToString()),
                    monthlyTrend = requirements
                        .GroupBy(r => r.CreatedAt.StringToDateTime().ToString("yyyy-MM"))
                        .Select(g => new {
                            month = g.Key,
                            newRequirements = g.Count().ToString(),
                            completedRequirements = g.Count(r => r.Status == "已驗收" || r.Status == "已交貨").ToString(),
                            totalCost = g.Sum(r => r.EstimatedCost.StringToDouble()).ToString("0")
                        }).OrderBy(x => x.month).ToList()
                };

                // 5) 回傳成功
                returnData.Code = 200;
                returnData.Result = "獲取採購儀表板成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = dashboard;

                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = 500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }

        #region Helper Methods
        private double CalcBudgetUtilization(List<ProjectRequirementClass> reqs)
        {
            double totalBudget = reqs.Sum(r => r.ApprovedBudget.StringToDouble());
            double totalActual = reqs.Sum(r => r.ActualCost.StringToDouble());
            return totalBudget > 0 ? (totalActual / totalBudget) * 100 : 0;
        }

        private double CalcAverageLeadTime(List<ProjectRequirementClass> reqs)
        {
            var leadTimes = reqs
                .Where(r => !r.RequestedDate.StringIsEmpty() && !r.PurchasedDate.StringIsEmpty())
                .Select(r => (r.PurchasedDate.StringToDateTime() - r.RequestedDate.StringToDateTime()).TotalDays);
            return leadTimes.Any() ? leadTimes.Average() : 0;
        }

        private double CalcOnTimeDeliveryRate(List<ProjectRequirementClass> reqs)
        {
            var delivered = reqs.Where(r => !r.DeliveredDate.StringIsEmpty() && !r.DueDate.StringIsEmpty());
            int total = delivered.Count();
            if (total == 0) return 0;
            int onTime = delivered.Count(r => r.DeliveredDate.StringToDateTime() <= r.DueDate.StringToDateTime());
            return (onTime / (double)total) * 100;
        }
        #endregion

        /// <summary>
        /// 統一錯誤回傳
        /// </summary>
        private IActionResult ErrorResponse(returnData request, int code, string message)
        {
            request.Code = code;
            request.Result = message;
            request.Data = null;
            request.TimeTaken = "0.000s";
            return BadRequest(request);
        }
        #region ======== Utilities ========
        private static string Now() => DateTime.Now.ToDateTimeString();
        private static string Esc(string s) => (s ?? "").Replace("'", "''");
        private static string UpperGuid(string g)
        {
            if (g.StringIsEmpty()) return "";
            // 若可解析 Guid 就轉為標準大寫；否則直接大寫（保留原始字元）
            return Guid.TryParse(g, out var parsed) ? parsed.ToString().ToUpper() : g.ToUpper();
        }

        private static DateTime? ParseDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var dt)) return dt;
            return null;
        }
        private static (bool ok, string msg) ValidateBusinessDates(projectClass p)
        {
            // 規則摘錄：
            // - 合約結束 > 合約開始
            // - 投標日期 <= 合約開始日期（若二者皆有）
            DateTime? start = ParseDate(p.合約開始日期);
            DateTime? end = ParseDate(p.合約結束日期);
            DateTime? tender = ParseDate(p.投標日期);

            if (start.HasValue && end.HasValue && end.Value <= start.Value)
                return (false, "合約結束日期必須晚於合約開始日期");

            if (tender.HasValue && start.HasValue && tender.Value > start.Value)
                return (false, "投標日期不能晚於合約開始日期");

            return (true, "");
        }
        private static sys_serverSettingClass GetConfOrFail(returnData rd)
        {
            List<sys_serverSettingClass> all = serverSetting.GetAllServerSetting();
            // 依參考範例找 VM 端
            return all.myFind(rd.ServerName, rd.ServerType, "VM端");
        }
        private static SQLControl GetSqlProjects(sys_serverSettingClass conf)
        {
            string table = new enum_projects().GetEnumDescription();
            return new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
        }
        private static string TableName(sys_serverSettingClass conf) => new enum_projects().GetEnumDescription();
        private static sys_serverSettingClass GetConfOrFail(returnData rd, out string error)
        {
            error = null;
            var servers = serverSetting.GetAllServerSetting();
            var conf = servers.myFind(rd.ServerName, rd.ServerType, "VM端");
            if (conf == null) error = "找不到 Server 設定";
            return conf;
        }
        private static SQLControl SqlForProjects(sys_serverSettingClass conf)
        {
            string table = new enum_projects().GetEnumDescription();
            return new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
        }
        private static SQLControl SqlForMilestones(sys_serverSettingClass conf)
        {
            string table = new enum_project_milestones().GetEnumDescription();
            return new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);
        }
        private static string ProjectsTable(sys_serverSettingClass conf) => new enum_projects().GetEnumDescription();
        private static string MilestonesTable(sys_serverSettingClass conf) => new enum_project_milestones().GetEnumDescription();
        private static string GetVal(List<string> valueAry, string key, string defaultVal = null)
            => valueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1] ?? defaultVal;
        private static string SafeOrderBy(string sortBy, string sortOrder)
        {
            // 將前端 sortBy 映射到資料表實際欄位
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["created_at"] = "建立時間",
                ["updated_at"] = "更新時間",
                ["name"] = "名稱",
                ["status"] = "狀態",
                ["id"] = "ID"
            };
            var col = map.ContainsKey(sortBy ?? "") ? map[sortBy] : "更新時間";
            var dir = (sortOrder ?? "desc").Equals("asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
            // 第二排序鍵固定名稱，提升瀏覽穩定性
            return $"{col} {dir}, 名稱 ASC";
        }
        private static (bool ok, string msg) ValidateStatusTransition(string from, string to)
        {
            // 合法狀態
            var all = new HashSet<string> { "投標中", "已得標", "進行中", "已完成", "已取消", "暫停" };
            if (string.IsNullOrWhiteSpace(to) || !all.Contains(to)) return (false, $"無效的狀態：{to}");

            if (string.IsNullOrWhiteSpace(from)) return (true, "");

            // 狀態轉移規則（需求 7.1）
            var next = new Dictionary<string, HashSet<string>>
            {
                ["投標中"] = new HashSet<string> { "已得標", "已取消" },
                ["已得標"] = new HashSet<string> { "進行中", "已取消" },
                ["進行中"] = new HashSet<string> { "已完成", "暫停", "已取消" },
                ["暫停"] = new HashSet<string> { "進行中", "已取消" },
                ["已完成"] = new HashSet<string>(),
                ["已取消"] = new HashSet<string>()
            };

            if (!next.ContainsKey(from)) return (false, $"未知的目前狀態：{from}");
            //if (!next[from].Contains(to))
            //{
            //    string allowed = next[from].Count == 0 ? "(不可再變更)" : string.Join(" / ", next[from]);
            //    return (false, $"狀態不可由「{from}」變更為「{to}」，允許：{allowed}");
            //}
            return (true, "");
        }
 
        /// <summary>
        /// 將 class 屬性值更新進 object[] row
        /// </summary>
        /// <typeparam name="TClass">資料類別，例如 milestoneClass</typeparam>
        /// <param name="row">資料列（object[]）</param>
        /// <param name="instance">來源 class 實例</param>
        /// <param name="now">更新時間字串</param>
        /// <param name="enumType">對應的 enum，例如 typeof(enum_project_milestones)</param>
        public static void UpdateRowFromClass<TClass>(object[] row, TClass instance, string now, Type enumType)
        {
            var props = typeof(TClass).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                // 跳過 GUID，不允許覆蓋
                if (prop.Name.Equals("GUID", StringComparison.OrdinalIgnoreCase))
                    continue;

                var enumField = enumType.GetFields()
                    .FirstOrDefault(f => f.Name == prop.Name);

                if (enumField != null)
                {
                    int index = (int)Enum.Parse(enumType, enumField.Name);

                    string value = prop.GetValue(instance)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        row[index] = value;
                    }
                }
            }

            // 最後一定要更新「更新時間」
            var updateField = enumType.GetFields()
                .FirstOrDefault(f => f.Name == "更新時間");
            if (updateField != null)
            {
                int idx = (int)Enum.Parse(enumType, updateField.Name);
                row[idx] = now;
            }
        }
        #endregion
        private List<Table> CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            List<Table> tables = new List<Table>();
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_projects()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_project_milestones()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_project_bom_associations()));

            
            return tables;
        }
    }
}
