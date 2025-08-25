using Basic;
using HsonAPILib; // 引用 enum_project_boms, enum_bom_items, enum_project_documents
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Ocsp;
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
using static NPOI.HSSF.Util.HSSFColor;
using static Org.BouncyCastle.Math.EC.ECCurve;

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
                var conf = GetConfOrFail(returnData, out string confErr);
                if (confErr != null)
                {
                    returnData.Code = -200;
                    returnData.Result = confErr;
                    return returnData.JsonSerializationt();
                }

                // 解析 ValueAry
                string status = GetVal(returnData.ValueAry, "status") ?? "";
                string clientGuid = GetVal(returnData.ValueAry, "clientGuid") ?? "";
                string priority = GetVal(returnData.ValueAry, "priority") ?? "";
                string dateFrom = GetVal(returnData.ValueAry, "dateFrom") ?? "";
                string dateTo = GetVal(returnData.ValueAry, "dateTo") ?? "";
                string searchTerm = GetVal(returnData.ValueAry, "searchTerm") ?? "";
                int page = (GetVal(returnData.ValueAry, "page") ?? "1").StringToInt32();
                int pageSize = (GetVal(returnData.ValueAry, "pageSize") ?? "50").StringToInt32();
                string sortBy = GetVal(returnData.ValueAry, "sortBy") ?? "updated_at";
                string sortOrder = GetVal(returnData.ValueAry, "sortOrder") ?? "desc";
                bool includeBomDetails = (GetVal(returnData.ValueAry, "includeBomDetails") ?? "false").ToLower() == "true";

                // 呼叫靜態函式
                var list = GetProjectList(
                    status, clientGuid, priority,
                    dateFrom, dateTo, searchTerm,
                    page, pageSize, sortBy, sortOrder,
                    includeBomDetails, conf
                );

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
                var conf = GetConfOrFail(returnData, out string confErr);
                if (confErr != null)
                {
                    returnData.Code = -200;
                    returnData.Result = confErr;
                    return returnData.JsonSerializationt();
                }

                string guid = GetVal(returnData.ValueAry, "projectGuid") ?? "";
                bool includeBomDetails = (GetVal(returnData.ValueAry, "includeBomDetails") ?? "false").ToLower() == "true";
                bool includeMilestones = (GetVal(returnData.ValueAry, "includeMilestones") ?? "false").ToLower() == "true";

                if (guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 projectGuid 參數";
                    return returnData.JsonSerializationt();
                }

                // 呼叫靜態函式
                var project = GetProjectDetails(guid, includeBomDetails, includeMilestones, conf);

                returnData.Code = 200;
                returnData.Result = "獲取專案詳情成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = new List<projectClass> { project };
                return returnData.JsonSerializationt();
            }
            catch (KeyNotFoundException ex)
            {
                returnData.Code = 404;
                returnData.Result = ex.Message;
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
                var sql = conf.GetSQLControl(new enum_projects().GetEnumDescription());

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
                var sqlBom = conf.GetSQLControl(new enum_projects().GetEnumDescription());
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

                string pTable = new enum_projects().GetEnumDescription();
                var sql = conf.GetSQLControl(new enum_projects().GetEnumDescription());

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
        /// 用途:  
        /// 依據指定的 <c>project_guid</c> 與篩選條件，獲取專案需求清單，支援分頁、排序與多條件查詢。
        ///
        /// 必填檢核:  
        /// - <c>ValueAry</c> 必須包含 <c>project_guid</c>
        ///
        /// Request JSON 範例:
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "project_guid=E3568F7E-6737-4FEA-913E-75FA21F3C934",
        ///     "includeItems=true",
        ///     "status=等待確認請購",
        ///     "priority=中",
        ///     "page=1",
        ///     "pageSize=50",
        ///     "sortBy=due_date",
        ///     "sortOrder=asc"
        ///   ]
        /// }
        /// ```
        ///
        /// Response JSON 範例（成功）:
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "4BEBA8E7-81D2-41B2-8671-0DB24D89E096",
        ///       "project_guid": "E3568F7E-6737-4FEA-913E-75FA21F3C934",
        ///       "procurement_type": "BOM",
        ///       "description": "111",
        ///       "quantity": "2",
        ///       "unit": "套",
        ///       "status": ProcurementStatuType.等待確認請購.GetEnumName(),
        ///       "priority": "中",
        ///       "due_date": "2024/06/30 00:00:00",
        ///       "item_name": "TEST",
        ///       "requester": "系統自動轉換",
        ///       "created_at": "2025/08/24 01:07:57",
        ///       "updated_at": "2025/08/24 01:07:57",
        ///       "items": [
        ///         {
        ///           "GUID": "2226FEB5-6827-40D5-932C-D32BBA38A333",
        ///           "requirement_guid": "4BEBA8E7-81D2-41B2-8671-0DB24D89E096",
        ///           "item_description": "電腦組合包",
        ///           "item_name": "電腦組合包",
        ///           "item_code": "P001",
        ///           "quantity": "2",
        ///           "unit": "個",
        ///           "specifications": "含顯示器與鍵鼠組",
        ///           "lead_time_days": "20",
        ///           "created_by": "系統自動轉換",
        ///           "created_at": "2025/08/24 01:07:57"
        ///         },
        ///         {
        ///           "GUID": "5B9163AE-19EB-4F8F-AC74-8EB806015713",
        ///           "item_name": "無線滑鼠",
        ///           "item_code": "P004",
        ///           "quantity": "10",
        ///           "unit": "個",
        ///           "specifications": "藍牙5.0",
        ///           "lead_time_days": "80"
        ///         }
        ///       ]
        ///     },
        ///     {
        ///       "GUID": "2B057B9C-65BC-4A12-A9F2-321052653AF3",
        ///       "project_guid": "E3568F7E-6737-4FEA-913E-75FA21F3C934",
        ///       "procurement_type": "產品請購",
        ///       "description": "無線滑鼠",
        ///       "quantity": "1",
        ///       "unit": "個",
        ///       "status": ProcurementStatuType.等待確認請購.GetEnumName(),
        ///       "due_date": "2025/08/30 00:00:00",
        ///       "item_id": "P004",
        ///       "item_name": "無線滑鼠",
        ///       "created_by": "開發測試用戶",
        ///       "created_at": "2025/08/24 02:25:34",
        ///       "items": []
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "get_project_requirements",
        ///   "Result": "獲取專案需求清單成功",
        ///   "TimeTaken": "5798.596ms",
        ///   "TotalCount": "3",
        ///   "TotalPages": "1",
        ///   "CurrentPage": "1",
        ///   "PageSize": "50"
        /// }
        /// ```
        ///
        /// Response JSON 範例（失敗）:
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 400,
        ///   "Method": "get_project_requirements",
        ///   "Result": "缺少必要參數：project_guid",
        ///   "TimeTaken": "10ms"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">標準請求物件</param>
        /// <returns>回傳 JSON 格式的查詢結果，包含需求清單與分頁資訊</returns>
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

                string projectGuid = GetVal("project_guid") ?? "";
                if (projectGuid.StringIsEmpty())
                {
                    returnData.Code = 400;
                    returnData.Result = "缺少必要參數：project_guid";
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
                string sortBy = GetVal("sortBy") ?? "updated_at";
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
                string where = $" WHERE project_guid = '{Esc(projectGuid)}' ";
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

                if (includeItems || true)
                {
                    string bomItemsTable = new enum_bom_requirement_items().GetEnumDescription();
                    var sqlBomItems = new SQLControl(conf.Server, conf.DBName, bomItemsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                    foreach (var req in requirements)
                    {
                        
                        string sqlItems = $@"SELECT * FROM {conf.DBName}.{bomItemsTable} WHERE RequirementGuid = '{Esc(req.GUID)}'";
                        var dtItems = sqlBomItems.GetRowsByDefult(null, (int)enum_bom_requirement_items.RequirementGuid, req.GUID);
                        var items = dtItems.SQLToClass<BomRequirementItemClass, enum_bom_requirement_items>() ?? new List<BomRequirementItemClass>();
                        req.items = items;
                        UpdateRequirementStatus(req);
                    }
                }

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
        /// 取得單一專案需求詳情（僅 POST）
        /// </summary>
        /// <remarks>
        /// 用途:  
        /// 根據指定的 <c>requirement_guid</c> 查詢單一專案需求的完整詳細資訊。  
        /// 可選擇是否包含 BOM 子項目（items）。  
        ///
        /// 必填檢核:  
        /// - <c>ValueAry</c> 必須包含 <c>requirement_guid</c>
        /// - 缺少必要參數會直接回傳錯誤
        ///
        /// Request JSON 範例:
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "requirement_guid=4BEBA8E7-81D2-41B2-8671-0DB24D89E096",
        ///     "includeItems=true"
        ///   ]
        /// }
        /// ```
        ///
        /// Response JSON 範例（成功）:
        /// ```json
        /// {
        ///   "Data": {
        ///     "GUID": "4BEBA8E7-81D2-41B2-8671-0DB24D89E096",
        ///     "project_guid": "E3568F7E-6737-4FEA-913E-75FA21F3C934",
        ///     "procurement_type": "BOM",
        ///     "description": "111",
        ///     "quantity": "2",
        ///     "unit": "套",
        ///     "status": ProcurementStatuType.等待確認請購.GetEnumName(),
        ///     "priority": "中",
        ///     "due_date": "2024/06/30 00:00:00",
        ///     "item_name": "TEST",
        ///     "requester": "系統自動轉換",
        ///     "created_at": "2025/08/24 01:07:57",
        ///     "updated_at": "2025/08/24 01:07:57",
        ///     "items": [
        ///       {
        ///         "GUID": "2226FEB5-6827-40D5-932C-D32BBA38A333",
        ///         "requirement_guid": "4BEBA8E7-81D2-41B2-8671-0DB24D89E096",
        ///         "item_description": "電腦組合包",
        ///         "item_name": "電腦組合包",
        ///         "item_code": "P001",
        ///         "quantity": "2",
        ///         "unit": "個",
        ///         "specifications": "含顯示器與鍵鼠組",
        ///         "lead_time_days": "20",
        ///         "created_by": "系統自動轉換",
        ///         "created_at": "2025/08/24 01:07:57"
        ///       },
        ///       {
        ///         "GUID": "5B9163AE-19EB-4F8F-AC74-8EB806015713",
        ///         "item_name": "無線滑鼠",
        ///         "item_code": "P004",
        ///         "quantity": "10",
        ///         "unit": "個",
        ///         "specifications": "藍牙5.0",
        ///         "lead_time_days": "80"
        ///       }
        ///     ]
        ///   },
        ///   "Code": 200,
        ///   "Method": "get_requirement_details",
        ///   "Result": "取得需求詳情成功",
        ///   "TimeTaken": "42.613ms"
        /// }
        /// ```
        ///
        /// Response JSON 範例（失敗）:
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 400,
        ///   "Method": "get_requirement_details",
        ///   "Result": "缺少必要參數：requirement_guid",
        ///   "TimeTaken": "5ms"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">標準請求物件</param>
        /// <returns>回傳 JSON 格式的單一需求詳情，包含 BOM 子項目（若指定 includeItems=true）</returns>
        [HttpPost("get_requirement_details")]
        public string get_requirement_details([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_requirement_details";

            try
            {
                // 1) 驗證必填參數
                string requirement_guid = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("requirement_guid=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                bool includeItems = ((returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("includeItems=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1]) ?? "false").ToLower() == "true";

                if (requirement_guid.StringIsEmpty())
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
                string projectsTable = new enum_projects().GetEnumDescription();

                var sqlReq = new SQLControl(conf.Server, conf.DBName, reqTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);
                var sqlBomItems = new SQLControl(conf.Server, conf.DBName, bomItemsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);
                var sqlprojects = new SQLControl(conf.Server, conf.DBName, projectsTable, conf.User, conf.Password, conf.Port.StringToUInt32(), MySqlSslMode.None);

                string Esc(string s) => (s ?? "").Replace("'", "''");

                // 3) 查詢需求主檔
                string sql = $@"SELECT * FROM {conf.DBName}.{reqTable} WHERE GUID = '{Esc(requirement_guid)}' LIMIT 1";
                var dt = sqlReq.WtrteCommandAndExecuteReader(sql);
                if (dt.Rows.Count == 0)
                {
                    returnData.Code = 404;
                    returnData.Result = $"查無 requirement_guid={requirement_guid} 的需求資料";
                    return returnData.JsonSerializationt();
                }

                var requirement = dt.DataTableToRowList().SQLToClass<ProjectRequirementClass, enum_project_requirements>()[0];
                var projectRows = sqlprojects.GetRowsByDefult(null, (int)enum_projects.GUID, requirement.project_guid);
                var project = projectRows.Count > 0 ? projectRows[0].SQLToClass<projectClass, enum_projects>() : null;
                requirement.project_id = project.ID;
                // 4) 是否查詢 BOM 項目
                if (includeItems)
                {

                    string sqlItems = $@"SELECT * FROM {conf.DBName}.{bomItemsTable} WHERE RequirementGuid = '{Esc(requirement.GUID)}'";
                    var dtItems = sqlBomItems.GetRowsByDefult(null, (int)enum_bom_requirement_items.RequirementGuid, requirement.GUID);
                    var items = dtItems.SQLToClass<BomRequirementItemClass, enum_bom_requirement_items>() ?? new List<BomRequirementItemClass>();
                    requirement.items = items;
                }
                UpdateRequirementStatus(requirement);
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
        /// 用途:
        /// 建立一筆或多筆「專案需求」資料，並自動產生 GUID（大寫）、建立/更新時間。系統會套用必填檢核與業務邏輯驗證。
        ///
        /// 必填檢核:
        /// - project_guid 必填（必須為大寫 GUID）
        /// - description 必填
        /// - quantity 必填，且必須大於 0
        /// - unit 必填
        ///
        /// Request JSON 範例:
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "project_guid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "project_id": "PRJ001",
        ///       "procurement_type": "市構件請購",
        ///       "description": "交通號誌控制器",
        ///       "quantity": "50",
        ///       "unit": "台",
        ///       "priority": "高",
        ///       "due_date": "2024-04-30",
        ///       "estimated_cost": "2500000",
        ///       "item_id": "PROD001",
        ///       "item_name": "智慧交通控制器 TC-2000",
        ///       "item_code": "TC-2000",
        ///       "supplier_name": "智慧科技有限公司",
        ///       "requester": "張工程師",
        ///       "specifications": "需支援 LED 顯示，具備遠端監控功能",
        ///       "delivery_address": "台北市信義區市府路1號",
        ///       "notes": "緊急需求，需優先處理",
        ///       "created_by": "張工程師"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// Response JSON 範例（成功）:
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "project_guid": "550E8400-E29B-41D4-A716-446655440000",
        ///       "description": "交通號誌控制器",
        ///       "quantity": "50",
        ///       "unit": "台",
        ///       "status": ProcurementStatuType.等待確認請購.GetEnumName(),
        ///       "created_at": "2024-03-15 09:00:00",
        ///       "updated_at": "2024-03-15 09:00:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "create_requirement",
        ///   "Result": "建立專案需求成功",
        ///   "TimeTaken": "0.035s"
        /// }
        /// ```
        ///
        /// Response JSON 範例（失敗）:
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 400,
        ///   "Method": "create_requirement",
        ///   "Result": "參數驗證失敗：project_guid 為必填",
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
                var sql_projects = conf.GetSQLControl(new enum_projects().GetEnumDescription());
           
                var sql_req = conf.GetSQLControl(new enum_project_requirements().GetEnumDescription());
                var sql_bom_req_items = conf.GetSQLControl(new enum_bom_requirement_items().GetEnumDescription());


                var output = new List<ProjectRequirementClass>();

                // === 3. 寫入流程 ===
                foreach (var req in input)
                {
                    // 檢核必填
                    if (req.project_guid.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "參數驗證失敗：projectGuid 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (req.description.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "參數驗證失敗：description 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (req.quantity.StringIsEmpty() || req.quantity.StringToInt32() <= 0)
                    {
                        returnData.Code = 400;
                        returnData.Result = "參數驗證失敗：quantity 必須大於 0";
                        return returnData.JsonSerializationt();
                    }
                    if (req.unit.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "參數驗證失敗：unit 為必填";
                        return returnData.JsonSerializationt();
                    }
                    productsClass productsClass = products.GetProductByCode(conf, req.item_code);
                    projectClass projectClass = GetProjectDetails(req.project_guid, false, false, conf);
                    if (productsClass == null)
                    {
                        returnData.Code = 400;
                        returnData.Result = $"參數驗證失敗：item_code '{req.item_code}' 不存在於產品清單";
                        return returnData.JsonSerializationt();
                    }
                    if (projectClass == null)
                    {
                        returnData.Code = 400;
                        returnData.Result = $"參數驗證失敗：project_guid '{req.project_guid}' 不存在於專案列表中";
                        return returnData.JsonSerializationt();
                    }
                    // GUID處理
                    string newGuid = string.IsNullOrWhiteSpace(req.GUID) ? Guid.NewGuid().ToString().ToUpper() : req.GUID.ToUpper();

                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    var row = new object[Enum.GetValues(typeof(enum_project_requirements)).Length];
                    row[(int)enum_project_requirements.GUID] = newGuid;
                    row[(int)enum_project_requirements.ID] = $"REQ-{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                    row[(int)enum_project_requirements.project_guid] = projectClass.GUID;
                    row[(int)enum_project_requirements.project_id] = projectClass.ID ?? "";
                    row[(int)enum_project_requirements.procurement_type] = req.procurement_type ?? "";
                    row[(int)enum_project_requirements.description] = req.description ?? "";
                    row[(int)enum_project_requirements.quantity] = req.quantity ?? "0";
                    row[(int)enum_project_requirements.unit] = productsClass.單位 ?? "";
                    row[(int)enum_project_requirements.priority] = req.priority ?? "";
                    row[(int)enum_project_requirements.due_date] = req.due_date ?? "";
                    row[(int)enum_project_requirements.estimated_cost] = req.estimated_cost ?? "";
                    row[(int)enum_project_requirements.item_id] = req.item_id ?? "";
                    row[(int)enum_project_requirements.item_name] = productsClass.產品名稱 ?? "";
                    row[(int)enum_project_requirements.item_code] = productsClass.產品代碼 ?? "";
                    row[(int)enum_project_requirements.is_from_bom] = "false";
                    row[(int)enum_project_requirements.supplier_name] = req.supplier_name ?? "";
                    row[(int)enum_project_requirements.requester] = req.requester ?? "";
                    row[(int)enum_project_requirements.specifications] = req.specifications ?? "";
                    row[(int)enum_project_requirements.delivery_address] = req.delivery_address ?? "";
                    row[(int)enum_project_requirements.notes] = req.notes ?? "";
                    row[(int)enum_project_requirements.status] = ProcurementStatuType.等待確認請購.GetEnumName();
                    row[(int)enum_project_requirements.created_by] = req.created_by ?? "";
                    row[(int)enum_project_requirements.created_at] = now;
                    row[(int)enum_project_requirements.updated_by] = req.updated_by ?? "";
                    row[(int)enum_project_requirements.updated_at] = now;

                    ProjectRequirementClass req_out = row.SQLToClass<ProjectRequirementClass, enum_project_requirements>();

                    List<BomRequirementItemClass> bomRequirementItemClasses = new List<BomRequirementItemClass>();
                    foreach (var item in productsClass.child_components)
                    {
          
                        var bomRequirementItemClass = new BomRequirementItemClass
                        {
                            GUID = Guid.NewGuid().ToString().ToUpper(),
                            ID = $"REQ-PRODUCT_ITEM_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{item.child_code}",
                            RequirementGuid = req_out.GUID,
                            RequirementId = req_out.ID,
                            ItemDescription = item.規格,
                            ItemCode = item.child_code,
                            ItemName = item.產品名稱,
                            Quantity = (item.數量.StringToDouble() * req_out.quantity.StringToDouble()).ToString(),
                            EstimatedUnitCost = "0",
                            EstimatedTotalCost = "0",
                            ActualUnitCost = "0",
                            ActualTotalCost = "0",
                            ItemStatus = "啟用",
                            ProcurementProgress = ProcurementStatuType.等待確認請購.GetEnumName(),
                            Specifications = item.規格,
                            LeadTimeDays = "-",
                            dueDate = req_out.due_date.StringToDateTime().AddDays(0).ToString("yyyy-MM-dd"),
                            Notes = item.備註,
                            IsActive = "true",
                            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            CreatedBy = req.created_by,

                        };
                        bomRequirementItemClasses.Add(bomRequirementItemClass);
                    }
                    req_out.items = bomRequirementItemClasses;
                    output.Add(req_out);

                    sql_req.AddRow(null, row);
                    var bom_items_rows = bomRequirementItemClasses.ClassToSQL<BomRequirementItemClass , enum_bom_requirement_items>();
                    sql_bom_req_items.AddRows(null, bom_items_rows);
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
        /// **用途：**  
        /// 依 <c>GUID</c> 更新單筆或多筆需求欄位；未提供之欄位不會變更。  
        /// 系統會自動套用業務規則並更新 <c>updated_at</c>。  
        ///
        /// **必填檢核：**  
        /// - <c>Data</c> 不可為空  
        /// - 每筆必須提供 <c>GUID</c>  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "status": "採購中",
        ///       "priority": "高",
        ///       "updated_by": "王採購"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "status": "採購中",
        ///       "priority": "高",
        ///       "updated_by": "王採購",
        ///       "updated_at": "2025-08-22 12:30:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "update_requirement",
        ///   "Result": "需求更新成功",
        ///   "TimeTaken": "0.035s"
        /// }
        /// ```
        ///
        /// **Response JSON 範例（錯誤）：**
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": -200,
        ///   "Method": "update_requirement",
        ///   "Result": "每筆需求必須提供 GUID",
        ///   "TimeTaken": "0.5ms"
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

                    req.updated_at = row[(int)enum_project_requirements.updated_at].ObjectToString();
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
        /// 刪除專案需求（僅 POST；以 GUID 定位；支援多筆刪除）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 依 <c>GUID</c> 刪除一筆或多筆專案需求資料。  
        /// 系統會同時移除關聯 BOM 對應與需求項目，確保資料一致性。  
        ///
        /// **必填檢核：**  
        /// - <c>Data</c> 不可為空  
        /// - 每筆必須提供 <c>GUID</c>  
        ///
        /// **Request JSON 範例：**
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
        /// **Response JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 200,
        ///   "Method": "delete_requirement",
        ///   "Result": "刪除成功：2 筆；已刪除需求 GUID=880F9700-F59E-74G7-D049-779988773333；已刪除需求 GUID=990F9800-F69F-85H8-E150-880099884444",
        ///   "TimeTaken": "0.032s"
        /// }
        /// ```
        ///
        /// **Response JSON 範例（錯誤）：**
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 400,
        ///   "Method": "delete_requirement",
        ///   "Result": "參數驗證失敗：缺少 GUID",
        ///   "TimeTaken": "0.015s"
        /// }
        /// ```
        ///
        /// **Response JSON 範例（伺服器錯誤）：**
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 500,
        ///   "Method": "delete_requirement",
        ///   "Result": "伺服器錯誤：連線逾時",
        ///   "TimeTaken": "0.020s"
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
                var sqlReq = conf.GetSQLControl(new enum_project_requirements().GetEnumDescription());
                var sql_bom_Req = conf.GetSQLControl(new enum_bom_requirement_items().GetEnumDescription());


                var messages = new List<string>();
                int successCount = 0;

                // 3) 刪除流程
                foreach (var req in inputList)
                {
                    sqlReq.DeleteByDefult(null, (int)enum_project_requirements.GUID, req.GUID);
                    var bom_req_rows = sql_bom_Req.GetRowsByDefult(null, (int)enum_bom_requirement_items.RequirementGuid, req.GUID);
                    sql_bom_Req.DeleteExtra(null, bom_req_rows);
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
        /// **用途：**  
        /// 依 <c>GUID</c> 更新專案需求，套用「確認請購」業務邏輯（狀態流轉為「請購中」）。  
        ///
        /// **必填檢核：**  
        /// - <c>Data</c> 不可為空  
        /// - 每筆必須提供 <c>GUID</c>  
        /// - <c>approver</c> 必填  
        /// - <c>approved_budget</c> 必填  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "approver": "李主管",
        ///       "approved_budget": "2500000",
        ///       "notes": "預算核准，可進行採購",
        ///       "requested_date": "2024-03-15",
        ///       "updated_by": "李主管"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "Data": [
        ///     {
        ///       "GUID": "880F9700-F59E-74G7-D049-779988773333",
        ///       "status": ProcurementStatuType.請購中.GetEnumName(),
        ///       "approver": "李主管",
        ///       "approved_budget": "2500000",
        ///       "notes": "預算核准，可進行採購",
        ///       "requested_date": "2024-03-15",
        ///       "updated_by": "李主管",
        ///       "updated_at": "2025-08-22 10:05:00"
        ///     }
        ///   ],
        ///   "Code": 200,
        ///   "Method": "confirm_procurement",
        ///   "Result": "需求 880F9700-F59E-74G7-D049-779988773333 已確認請購",
        ///   "TimeTaken": "0.085s"
        /// }
        /// ```
        ///
        /// **Response JSON 範例（錯誤 - 缺少欄位）：**
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 400,
        ///   "Method": "confirm_procurement",
        ///   "Result": "approved_budget 為必填",
        ///   "TimeTaken": "0.012s"
        /// }
        /// ```
        ///
        /// **Response JSON 範例（錯誤 - 狀態不符）：**
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": -1,
        ///   "Method": "confirm_procurement",
        ///   "Result": "需求 880F9700-F59E-74G7-D049-779988773333 狀態為「採購中」，不可確認請購",
        ///   "TimeTaken": "0.010s"
        /// }
        /// ```
        ///
        /// **Response JSON 範例（伺服器錯誤）：**
        /// ```json
        /// {
        ///   "Data": null,
        ///   "Code": 500,
        ///   "Method": "confirm_procurement",
        ///   "Result": "伺服器錯誤：連線逾時",
        ///   "TimeTaken": "0.025s"
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
                var bomRequirementItemClasses = new List<BomRequirementItemClass>();
                var bomRequirementItemClasses_update = new List<BomRequirementItemClass>();
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

                var sqlReq = conf.GetSQLControl(new enum_project_requirements().GetEnumDescription());
                var sqlbom_Req_items = conf.GetSQLControl(new enum_bom_requirement_items().GetEnumDescription());

                // 3) 執行更新
                foreach (var req in input)
                {
                    if (req.GUID.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "每筆需求必須提供 GUID";
                        return returnData.JsonSerializationt();
                    }
                    if (req.approver.StringIsEmpty())
                    {
                        returnData.Code = 400;
                        returnData.Result = "approver 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (req.approved_budget.StringIsEmpty())
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

                    //if (currentStatus != ProcurementStatuType.等待確認請購.GetEnumName())
                    //{
                    //    returnData.Code = -1;
                    //    returnData.Result = $"需求 {req.GUID} 狀態為「{currentStatus}」，不可確認請購";
                    //    return returnData.JsonSerializationt();
                    //}

                    // 更新狀態與欄位
                    if (req.approved_budget.StringIsEmpty()) req.approved_budget = req.estimated_cost;
                    row[(int)enum_project_requirements.status] = ProcurementStatuType.請購中.GetEnumName();
                    row[(int)enum_project_requirements.approver] = req.approver ?? "";
                    row[(int)enum_project_requirements.approved_budget] = req.approved_budget ?? "";
                    //row[(int)enum_project_requirements.notes] = req.notes ?? "";
                    row[(int)enum_project_requirements.requested_date] = req.requested_date ?? DateTime.Now.ToDateTimeString();
                    row[(int)enum_project_requirements.updated_by] = req.updated_by ?? req.approver;
                    row[(int)enum_project_requirements.updated_at] = DateTime.Now.ToDateTimeString();

                    var req_out = row.SQLToClass<ProjectRequirementClass, enum_project_requirements>();

                    sqlbom_Req_items.GetRowsByDefult(null, (int)enum_bom_requirement_items.RequirementGuid, req.GUID)
                        .ForEach(itemRow =>
                        {
                            BomRequirementItemClass bomRequirementItemClass = itemRow.SQLToClass<BomRequirementItemClass, enum_bom_requirement_items>();
                            if(bomRequirementItemClass.ProcurementProgress == ProcurementStatuType.等待確認請購.GetEnumName())
                            {
                                bomRequirementItemClass.ProcurementProgress = ProcurementStatuType.請購中.GetEnumName();
                                bomRequirementItemClass.UpdatedAt = DateTime.Now.ToDateTimeString();
                                bomRequirementItemClasses_update.Add(bomRequirementItemClass);
                            }                   
                            bomRequirementItemClasses.Add(bomRequirementItemClass);
                        });
                    req_out.items = bomRequirementItemClasses;

                    sqlReq.UpdateByDefulteExtra(null, row);
                    sqlbom_Req_items.UpdateByDefulteExtra(null, bomRequirementItemClasses_update.ClassToSQL<BomRequirementItemClass, enum_bom_requirement_items>());
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
        /// 將 BOM 轉換為專案採購需求（僅 POST）
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 依據指定的 BOM 與專案 GUID，將 BOM 項目轉換為專案採購需求，並可選擇是否自動建立需求與需求項目。<br/>
        /// 僅支援 <c>POST</c> 方法，所有欄位型別均為字串，<c>GUID</c> 必須為大寫。<br/>
        ///
        /// <br/><b>必填檢核：</b><br/>
        /// - `projectGuid` (必填，需為大寫 GUID)<br/>
        /// - `bomGuid` (必填，需為大寫 GUID)<br/>
        /// - 其他欄位如 `autoCreateRequirement`、`defaultDueDate`、`defaultPriority`、`quantity`、`unit`、`requester` 可選填<br/>
        ///
        /// <br/><b>參數說明（ValueAry）：</b><br/>
        /// - `projectGuid`：專案 GUID<br/>
        /// - `bomGuid`：BOM GUID<br/>
        /// - `autoCreateRequirement`：是否自動建立需求 (true/false，預設 false)<br/>
        /// - `defaultDueDate`：需求預設到期日 (yyyy-MM-dd，若未填則預設為 +1 個月)<br/>
        /// - `defaultPriority`：需求優先順序 (高/中/低，預設：中)<br/>
        /// - `quantity`：需求數量 (預設 1)<br/>
        /// - `unit`：需求單位 (預設：套)<br/>
        /// - `requester`：需求申請人 (可空，建議填寫)<br/>
        ///
        /// <br/><b>Request JSON 範例：</b>
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
        ///     "quantity=1",
        ///     "unit=套",
        ///     "estimated_cost=8500",
        ///     "requester=系統自動轉換",
        ///     "notes=備註內容"
        ///   ]
        /// }
        /// ```
        ///
        /// <br/><b>Response JSON 範例 (成功)：</b>
        /// ```json
        /// {
        ///   "Data": {
        ///     "GUID": "BB0F9A00-F89H-A7J0-G372-AA2211AA6666",
        ///     "ID": "REQ_BOM_001",
        ///     "project_guid": "550E8400-E29B-41D4-A716-446655440000",
        ///     "bom_guid": "660F9500-F39C-52E5-B827-557766551111",
        ///     "item_name": "智慧交通控制器主機板",
        ///     "description": "智慧交通控制器主機板 (來自BOM)",
        ///     "quantity": "1",
        ///     "unit": "套",
        ///     "status": ProcurementStatuType.等待確認請購.GetEnumName(),
        ///     "priority": "中",
        ///     "due_date": "2024-06-30",
        ///     "estimated_cost": "8500",
        ///     "is_from_bom": "true",
        ///     "requester": "系統自動轉換",
        ///     "created_at": "2024-03-15 14:30:00",
        ///     "updated_at": "2024-03-15 14:30:00",
        ///     "items": [
        ///       {
        ///         "GUID": "CC1F9A00-F88Z-B7C0-G472-AB3311BB7777",
        ///         "RequirementGuid": "BB0F9A00-F89H-A7J0-G372-AA2211AA6666",
        ///         "ItemCode": "MCU-001",
        ///         "ItemName": "ARM Cortex-M4 微控制器",
        ///         "Quantity": "1",
        ///         "Unit": "個",
        ///         "ItemStatus": "啟用",
        ///         "ProcurementProgress": ProcurementStatuType.等待確認請購.GetEnumName()
        ///       }
        ///     ]
        ///   },
        ///   "Code": 200,
        ///   "Method": "convert_bom_to_requirements",
        ///   "Result": "BOM轉換為採購需求成功",
        ///   "TimeTaken": "25.6ms"
        /// }
        /// ```
        ///
        /// <br/><b>Response JSON 範例 (失敗)：</b>
        /// ```json
        /// {
        ///   "Code": 400,
        ///   "Method": "convert_bom_to_requirements",
        ///   "Result": "參數驗證失敗：projectGuid 與 bomGuid 為必填欄位"
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

                string projectGuid = (GetVal("projectGuid") ?? "").ToUpper();
                string bomGuid = (GetVal("bomGuid") ?? "").ToUpper();
                bool autoCreate = (GetVal("autoCreateRequirement") ?? "false").ToLower() == "true";
                string defaultDueDate = GetVal("defaultDueDate");
                string defaultPriority = GetVal("defaultPriority");
                string requester = GetVal("requester");
                string quantity = GetVal("quantity");
                string unit = GetVal("unit");
                string estimated_cost = GetVal("estimated_cost");
                string notes = GetVal("notes");

                if (string.IsNullOrWhiteSpace(projectGuid) || string.IsNullOrWhiteSpace(bomGuid))
                {
                    returnData.Code = 400;
                    returnData.Result = "參數驗證失敗：projectGuid 與 bomGuid 為必填欄位";
                    return returnData.JsonSerializationt();
                }

                // 1) 取得 DB 設定
                var conf = GetConfOrFail(returnData, out string confErr);
                if (confErr != null)
                {
                    returnData.Code = -200;
                    returnData.Result = confErr;
                    return returnData.JsonSerializationt();
                }

                var sql_projects = conf.GetSQLControl(new enum_projects().GetEnumDescription());
                var sql_req = conf.GetSQLControl(new enum_project_requirements().GetEnumDescription());
                var sql_bom_req_items = conf.GetSQLControl(new enum_bom_requirement_items().GetEnumDescription());

                projectClass projectClass = GetProjectDetails(projectGuid, false, false, conf);
                List<BomItemClass> bomItemClasses = bom.GetBomItems(conf, bomGuid);
                ProjectBomClass projectBomClass = bom.GetBomDetails(conf, bomGuid);


                if (bomItemClasses.Count == 0 || projectBomClass == null)
                {
                    returnData.Code = 404;
                    returnData.Result = $"查無 BOM 資料 (Project={projectGuid}, BOM={bomGuid})";
                    return returnData.JsonSerializationt();
                }
                if (projectClass == null)
                {
                    returnData.Code = 404;
                    returnData.Result = $"查無專案資料 (Project={projectGuid})";
                    return returnData.JsonSerializationt();
                }
                    // 3) BOM → Requirement 轉換
                    var req = new ProjectRequirementClass
                    {
                        GUID = Guid.NewGuid().ToString().ToUpper(),
                        ID = $"REQ-BOM_{DateTime.Now.ToString("yyyyMMddHHmmss")}",
                        project_guid = projectClass.GUID,
                        project_id = projectClass.ID,
                        bom_guid = bomGuid,
                        item_name = projectBomClass.name,
                        procurement_type = "BOM",
                        description = $"{projectBomClass.description}",
                        quantity = quantity,
                        unit = unit.StringIsEmpty() ? "套" : unit, // TODO: 若有單位欄位可直接帶入
                        status = ProcurementStatuType.等待確認請購.GetEnumName(),
                        priority = string.IsNullOrWhiteSpace(defaultPriority) ? "中" : defaultPriority,
                        due_date = string.IsNullOrWhiteSpace(defaultDueDate)
                             ? DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd")
                             : defaultDueDate,
                        estimated_cost = estimated_cost, // TODO: 可連結成本估算表
                        is_from_bom = "true",
                        requester = requester ?? "",
                        created_by = requester ?? "",
                        created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        notes = notes ?? "",
                    };
                List<BomRequirementItemClass> bomRequirementItemClasses = new List<BomRequirementItemClass>();
                foreach (var bomItemClass in bomItemClasses)
                {
                    if(bomItemClass.leadTime.StringIsInt32() == false)
                    {
                        bomItemClass.leadTime = "7";
                    }
                    var bomRequirementItemClass = new BomRequirementItemClass
                    {
                        GUID = Guid.NewGuid().ToString().ToUpper(),
                        ID = $"REQ-BOM_ITEM_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{bomItemClass.itemCode}",
                        RequirementGuid = req.GUID,
                        RequirementId = req.ID,
                        ItemDescription = bomItemClass.description,
                        ItemCode = bomItemClass.itemCode,
                        ItemName = bomItemClass.itemName,
                        Quantity = (bomItemClass.quantity.StringToDouble() * quantity.StringToDouble()).ToString(),
                        Unit = bomItemClass.unit,
                        EstimatedUnitCost = "0",
                        EstimatedTotalCost = "0",
                        ActualUnitCost = "0",
                        ActualTotalCost = "0",
                        ItemStatus = "啟用",
                        ProcurementProgress = ProcurementStatuType.等待確認請購.GetEnumName(),
                        Specifications = bomItemClass.specification,
                        LeadTimeDays = bomItemClass.leadTime,
                        dueDate = req.due_date.StringToDateTime().AddDays(0).ToString("yyyy-MM-dd"),
                        Notes = bomItemClass.notes,
                        IsActive = "true",
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        CreatedBy = requester,

                    };
                    bomRequirementItemClasses.Add(bomRequirementItemClass);
                }
                req.items = bomRequirementItemClasses;
                if (autoCreate)
                {
                    var req_row = req.ClassToSQL<ProjectRequirementClass, enum_project_requirements>();
                    sql_req.AddRow(null, req_row);

                    var req_item_rows = req.items.ClassToSQL<BomRequirementItemClass, enum_bom_requirement_items>();
                    sql_bom_req_items.AddRows(null, req_item_rows);

                }

                // 5) 回傳
                returnData.Code = 200;
                returnData.Result = "BOM轉換為採購需求成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = req;

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
                int pending = list.Count(x => x.status == ProcurementStatuType.等待確認請購.GetEnumName());
                int inReq = list.Count(x => x.status == ProcurementStatuType.請購中.GetEnumName());
                int inPurchase = list.Count(x => x.status == "採購中");
                int delivered = list.Count(x => x.status == "已交貨");
                int accepted = list.Count(x => x.status == "已驗收");

                double totalEstimated = list.Sum(x => x.estimated_cost.StringToDouble());
                double totalActual = list.Sum(x => x.actual_cost.StringToDouble());
                double totalBudget = list.Sum(x => x.approved_budget.StringToDouble());

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
        ///       ProcurementStatuType.等待確認請購.GetEnumName(): "5",
        ///       ProcurementStatuType.請購中.GetEnumName(): "8",
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
                    pendingConfirmationCount = requirements.Count(x => x.status == ProcurementStatuType.等待確認請購.GetEnumName()).ToString(),
                    inRequisitionCount = requirements.Count(x => x.status == ProcurementStatuType.請購中.GetEnumName()).ToString(),
                    inPurchaseCount = requirements.Count(x => x.status == "採購中").ToString(),
                    deliveredCount = requirements.Count(x => x.status == "已交貨").ToString(),
                    acceptedCount = requirements.Count(x => x.status == "已驗收").ToString(),
                    totalEstimatedCost = requirements.Sum(x => x.estimated_cost.StringToDouble()).ToString("0"),
                    totalActualCost = requirements.Sum(x => x.actual_cost.StringToDouble()).ToString("0"),
                    totalApprovedBudget = requirements.Sum(x => x.approved_budget.StringToDouble()).ToString("0"),
                    budgetUtilizationRate = CalcBudgetUtilization(requirements).ToString("0.##"),
                    averageLeadTime = CalcAverageLeadTime(requirements).ToString("0.##"),
                    onTimeDeliveryRate = CalcOnTimeDeliveryRate(requirements).ToString("0.##"),
                    procurementTypeDistribution = requirements.GroupBy(r => r.procurement_type ?? "")
                        .ToDictionary(g => g.Key, g => g.Count().ToString()),
                    statusDistribution = requirements.GroupBy(r => r.status ?? "")
                        .ToDictionary(g => g.Key, g => g.Count().ToString()),
                    monthlyTrend = requirements
                        .GroupBy(r => r.created_at.StringToDateTime().ToString("yyyy-MM"))
                        .Select(g => new {
                            month = g.Key,
                            newRequirements = g.Count().ToString(),
                            completedRequirements = g.Count(r => r.status == "已驗收" || r.status == "已交貨").ToString(),
                            totalCost = g.Sum(r => r.estimated_cost.StringToDouble()).ToString("0")
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


        /// <summary>
        /// 取得專案相關文件清單（僅 POST）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 依據指定的 <c>project_GUID</c>，查詢並回傳專案所屬的文件清單，按上傳日期由新到舊排序。  
        ///
        /// **必填檢核：**  
        /// - <c>ValueAry</c> 必須包含 <c>project_GUID</c>  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "ValueAry": [
        ///     "project_GUID=550E8400-E29B-41D4-A716-446655440000"
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_bom_documents",
        ///   "Result": "OK",
        ///   "TimeTaken": "0.035s",
        ///   "Data": [
        ///     {
        ///       "GUID": "D1234567-89AB-4CDE-F012-3456789ABCDE",
        ///       "BomGUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "fileName": "需求文件_v1.pdf",
        ///       "fileType": "pdf",
        ///       "fileUrl": "https://example.com/uploads/projects/550E8400-E29B-41D4-A716-446655440000/需求文件_v1.pdf",
        ///       "uploader": "王小明",
        ///       "uploadDate": "2025-08-24 10:15:00",
        ///       "notes": "初版需求文件"
        ///     },
        ///     {
        ///       "GUID": "E2234567-89AB-4CDE-F012-987654321000",
        ///       "BomGUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "fileName": "設計圖.png",
        ///       "fileType": "png",
        ///       "fileUrl": "https://example.com/uploads/projects/550E8400-E29B-41D4-A716-446655440000/設計圖.png",
        ///       "uploader": "李工程師",
        ///       "uploadDate": "2025-08-23 09:45:00",
        ///       "notes": "系統架構設計圖"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（錯誤 - 缺少參數）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_bom_documents",
        ///   "Result": "缺少 project_GUID 參數",
        ///   "TimeTaken": "0.010s",
        ///   "Data": null
        /// }
        /// ```
        ///
        /// **Response JSON 範例（伺服器錯誤）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_bom_documents",
        ///   "Result": "伺服器錯誤：連線逾時",
        ///   "TimeTaken": "0.025s",
        ///   "Data": null
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">標準請求物件，需包含 <c>project_GUID</c></param>
        /// <returns>回傳 JSON 格式的專案文件清單</returns>
        [HttpPost("get_project_documents")]
        public string get_bom_documents([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_project_documents";

            try
            {
                // 驗證參數
                string project_GUID = returnData.ValueAry?.FirstOrDefault(x => x.StartsWith("project_GUID=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
                if (project_GUID.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少 project_GUID 參數";
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

                var sql_doc = conf.GetSQLControl(new enum_project_documents().GetEnumDescription());

                string Esc(string s) => (s ?? "").Replace("'", "''");
                string query = $"SELECT * FROM {conf.DBName}.{new enum_project_documents().GetEnumDescription()} WHERE project_GUID='{Esc(project_GUID)}' ORDER BY uploadDate DESC";
                var dt = sql_doc.WtrteCommandAndExecuteReader(query);

                var docs = dt.DataTableToRowList().SQLToClass<projectDocumentClass, enum_project_documents>();

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
        /// 上傳專案文件（僅 POST）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 使用 multipart/form-data 上傳指定專案的文件（圖面、規格書、技術文件或其他），  
        /// 系統會自動將檔案存放於 `/wwwroot/documents/project/{projectID}/` 目錄下，並建立對應的文件紀錄。  
        ///
        /// **必填檢核：**  
        /// - <c>project_GUID</c>：專案 GUID  
        /// - <c>userName</c>：上傳者名稱  
        /// - <c>file</c>：檔案物件  
        /// - <c>type</c>：文件類型（必填值：圖面 | 規格書 | 技術文件 | 其他）  
        ///
        /// **Request 格式（multipart/form-data）：**  
        /// - project_GUID: "550E8400-E29B-41D4-A716-446655440000"  
        /// - userName: "王小明"  
        /// - type: "圖面"  
        /// - notes: "第一版設計圖"  
        /// - file: (上傳的檔案物件，例如 設計圖.pdf)  
        ///
        /// **Response JSON 範例（成功）：**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "upload_project_document",
        ///   "Result": "檔案上傳成功",
        ///   "TimeTaken": "50.1ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "NEW-DOC-GUID-001",
        ///       "project_GUID": "550E8400-E29B-41D4-A716-446655440000",
        ///       "ID": "12345",
        ///       "fileName": "12345_設計圖_20250816_193000.pdf",
        ///       "originalName": "設計圖.pdf",
        ///       "type": "圖面",
        ///       "fileSize": "2048576",
        ///       "url": "/documents/project/12345/12345_設計圖_20250816_193000.pdf",
        ///       "uploadDate": "2025-08-16 19:30:00",
        ///       "uploadedBy": "王小明",
        ///       "notes": "第一版設計圖"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// **Response JSON 範例（錯誤 - 缺少參數）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "upload_project_document",
        ///   "Result": "缺少必要參數 (project_GUID / userName / file / type)",
        ///   "TimeTaken": "0.012s",
        ///   "Data": null
        /// }
        /// ```
        ///
        /// **Response JSON 範例（錯誤 - 找不到專案）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "upload_project_document",
        ///   "Result": "找不到對應的 project (GUID=550E8400-E29B-41D4-A716-446655440000)",
        ///   "TimeTaken": "0.010s",
        ///   "Data": null
        /// }
        /// ```
        ///
        /// **Response JSON 範例（伺服器錯誤）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "upload_project_document",
        ///   "Result": "伺服器錯誤：連線逾時",
        ///   "TimeTaken": "0.025s",
        ///   "Data": null
        /// }
        /// ```
        /// </remarks>
        /// <param name="project_GUID">專案 GUID</param>
        /// <param name="userName">上傳者名稱</param>
        /// <param name="type">文件類型（圖面 | 規格書 | 技術文件 | 其他）</param>
        /// <param name="notes">備註（可選填）</param>
        /// <param name="file">上傳的檔案</param>
        /// <returns>回傳 JSON 格式的上傳結果，包含新文件資訊</returns>
        [HttpPost("upload_project_document")]
        [RequestSizeLimit(30_000_000)] // 限制30MB
        public string upload_project_document([FromForm] string project_GUID, [FromForm] string userName, [FromForm] string type, [FromForm] string notes, [FromForm] IFormFile file)
        {
            var timer = new MyTimerBasic();
            var returnData = new returnData { Method = "upload_project_document" };

            try
            {
                var missingFields = new List<string>();

                if (project_GUID.StringIsEmpty()) missingFields.Add("project_GUID");
                if (userName.StringIsEmpty()) missingFields.Add("userName");
                if (file == null || file.Length == 0) missingFields.Add("file");
                if (type.StringIsEmpty()) missingFields.Add("type");

                if (missingFields.Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少必要參數：" + string.Join("、", missingFields);
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

                // === Step1: 用 projectID 找 BOM 主檔 GUID ===
                string projectTable = new enum_projects().GetEnumDescription();
                var sqlproject = conf.GetSQLControl(projectTable);

                List<object[]> projectRows = sqlproject.GetRowsByDefult(null, (int)enum_projects.GUID, project_GUID);
                if (projectRows.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到對應的 project (GUID={project_GUID})";
                    return returnData.JsonSerializationt();
                }
                string projectID = projectRows[0][(int)enum_project_boms.ID].ObjectToString();

                // === Step2: 檔案處理 ===
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "project", projectID);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string safeFileName = SanitizeFileName(Path.GetFileName(file.FileName));
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string ext = Path.GetExtension(safeFileName);
                string baseName = Path.GetFileNameWithoutExtension(safeFileName);
                string newFileName = $"{projectID}_{baseName}_{timestamp}{ext}";

                string filePath = Path.Combine(folder, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                FileInfo fi = new FileInfo(filePath);

                // === Step3: 寫入文件表 ===
                string docTable = new enum_project_documents().GetEnumDescription();
                var sqlDoc = new SQLControl(conf.Server, conf.DBName, docTable, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                string now = DateTime.Now.ToDateTimeString();
                string guid = Guid.NewGuid().ToString().ToUpper();

                var ins = new object[Enum.GetValues(typeof(enum_project_documents)).Length];
                ins[(int)enum_project_documents.GUID] = guid;
                ins[(int)enum_project_documents.project_GUID] = project_GUID; // ✅ 存 GUID，不是 ID
                ins[(int)enum_project_documents.ID] = projectID;
                ins[(int)enum_project_documents.fileName] = newFileName;
                ins[(int)enum_project_documents.originalName] = file.FileName;
                ins[(int)enum_project_documents.type] = type;
                ins[(int)enum_project_documents.fileSize] = fi.Length.ToString();
                ins[(int)enum_project_documents.uploadDate] = now;
                ins[(int)enum_project_documents.uploadedBy] = userName;
                ins[(int)enum_project_documents.url] = $"/documents/project/{projectID}/{newFileName}";
                ins[(int)enum_project_documents.notes] = notes ?? "";
                sqlDoc.AddRow(null, ins);

                var doc = ins.SQLToClass<projectDocumentClass, enum_project_documents>();

                returnData.Code = 200;
                returnData.Result = "檔案上傳成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = new List<projectDocumentClass> { doc };
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
        /// 下載專案文件（僅 POST）
        /// </summary>
        /// <remarks>
        /// **用途：**  
        /// 依據文件的 <c>GUID</c> 下載對應的專案文件。  
        /// 若檔案存在，回傳檔案串流並附帶正確的 Content-Disposition 標頭；  
        /// 若檔案或紀錄不存在，則回傳錯誤 JSON。  
        ///
        /// **必填檢核：**  
        /// - <c>Data</c> 不可為空  
        /// - <c>GUID</c> 必填  
        ///
        /// **Request JSON 範例：**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "網頁",
        ///   "Data": [
        ///     { "GUID": "DOC-GUID-001" }
        ///   ]
        /// }
        /// ```
        ///
        /// **成功回應（檔案下載）：**
        /// - Response Header:
        ///   - Content-Disposition: attachment; filename="fallback.pdf"; filename*=UTF-8''設計圖.pdf  
        ///   - Content-Type: application/octet-stream  
        ///   - Access-Control-Expose-Headers: Content-Disposition, Content-Length, Content-Type  
        /// - Response Body: 檔案二進位串流（例如 PDF、Word、Excel 等）  
        ///
        /// **錯誤回應 JSON 範例（缺少 GUID）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_project_document",
        ///   "Result": "缺少 GUID 參數",
        ///   "Data": null
        /// }
        /// ```
        ///
        /// **錯誤回應 JSON 範例（找不到文件紀錄）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_project_document",
        ///   "Result": "找不到文件 (GUID=DOC-GUID-001)",
        ///   "Data": null
        /// }
        /// ```
        ///
        /// **錯誤回應 JSON 範例（檔案不存在於伺服器）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_project_document",
        ///   "Result": "檔案不存在於伺服器",
        ///   "Data": null
        /// }
        /// ```
        ///
        /// **錯誤回應 JSON 範例（伺服器錯誤）：**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_project_document",
        ///   "Result": "伺服器錯誤：連線逾時",
        ///   "Data": null
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">標準請求物件，需包含文件 GUID</param>
        /// <returns>若成功，回傳檔案串流；若失敗，回傳 JSON 錯誤資訊</returns>
        [HttpPost("download_project_document")]
        public IActionResult download_project_document([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "download_project_document";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return new JsonResult(returnData);
                }

                var input = returnData.Data.ObjToClass<List<projectDocumentClass>>();
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

                string table = new enum_project_documents().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var rows = sql.GetRowsByDefult(null, (int)enum_project_documents.GUID, guid);
                if (rows == null || rows.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到文件 (GUID={guid})";
                    return new JsonResult(returnData);
                }

                var doc = rows[0].SQLToClass<projectDocumentClass, enum_project_documents>();

                // 檔案路徑
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", "project", doc.ID);
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
        /// 刪除 專案 文件
        /// </summary>
        /// <remarks>
        /// 依 <c>GUID</c> 刪除 Project 文件。
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
        ///   "Method": "delete_project_document",
        ///   "Result": "刪除成功 1 筆",
        ///   "TimeTaken": "8.9ms"
        /// }
        /// ```
        /// </remarks>
        [HttpPost("delete_project_document")]
        public string delete_project_document([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_project_document";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                var input = returnData.Data.ObjToClass<List<projectDocumentClass>>();
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

                string table = new enum_project_documents().GetEnumDescription();
                var sql = new SQLControl(conf.Server, conf.DBName, table, conf.User, conf.Password, conf.Port.StringToUInt32(), SSLMode);

                var deleted = new List<string>();
                foreach (var doc in input)
                {
                    if (doc.GUID.StringIsEmpty()) continue;

                    var rows = sql.GetRowsByDefult(null, (int)enum_project_documents.GUID, doc.GUID);
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

        #region Helper Methods
        private double CalcBudgetUtilization(List<ProjectRequirementClass> reqs)
        {
            double totalBudget = reqs.Sum(r => r.approved_budget.StringToDouble());
            double totalActual = reqs.Sum(r => r.actual_cost.StringToDouble());
            return totalBudget > 0 ? (totalActual / totalBudget) * 100 : 0;
        }
        private double CalcAverageLeadTime(List<ProjectRequirementClass> reqs)
        {
            var leadTimes = reqs
                .Where(r => !r.requested_date.StringIsEmpty() && !r.purchased_date.StringIsEmpty())
                .Select(r => (r.purchased_date.StringToDateTime() - r.requested_date.StringToDateTime()).TotalDays);
            return leadTimes.Any() ? leadTimes.Average() : 0;
        }
        private double CalcOnTimeDeliveryRate(List<ProjectRequirementClass> reqs)
        {
            var delivered = reqs.Where(r => !r.delivered_date.StringIsEmpty() && !r.due_date.StringIsEmpty());
            int total = delivered.Count();
            if (total == 0) return 0;
            int onTime = delivered.Count(r => r.delivered_date.StringToDateTime() <= r.due_date.StringToDateTime());
            return (onTime / (double)total) * 100;
        }
        #endregion
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
        /// <summary>
        /// 取得專案清單（支援篩選、關鍵字、分頁、排序；可選擇回傳 BOM 關聯）
        /// </summary>
        public static List<projectClass> GetProjectList(
            string status = "",
            string clientGuid = "",
            string priority = "",
            string dateFrom = "",
            string dateTo = "",
            string searchTerm = "",
            int page = 1,
            int pageSize = 50,
            string sortBy = "updated_at",
            string sortOrder = "desc",
        bool includeBomDetails = false,
            sys_serverSettingClass conf = null   // 可以選擇傳 DB 設定，或在內部抓取
        )
        {
            var timer = new MyTimerBasic();

            if (conf == null) throw new ArgumentNullException(nameof(conf));

            string table = ProjectsTable(conf);
            var sql = conf.GetSQLControl(new enum_projects().GetEnumDescription());

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            string where = " WHERE 1=1 ";
            if (!status.StringIsEmpty()) where += $" AND 狀態 = '{Esc(status)}' ";
            if (!clientGuid.StringIsEmpty()) where += $" AND 客戶GUID = '{Esc(clientGuid.ToUpper())}' ";
            if (!priority.StringIsEmpty()) where += $" AND 優先級 = '{Esc(priority)}' ";
            if (!dateFrom.StringIsEmpty()) where += $" AND 投標日期 >= '{Esc(dateFrom)} 00:00:00' ";
            if (!dateTo.StringIsEmpty()) where += $" AND 投標日期 <= '{Esc(dateTo)} 23:59:59' ";
            if (!searchTerm.StringIsEmpty())
            {
                string k = Esc(searchTerm);
                where += $" AND (名稱 LIKE '%{k}%' OR 描述 LIKE '%{k}%' OR 客戶名稱 LIKE '%{k}%' OR 標籤 LIKE '%{k}%') ";
            }

            string orderBy = SafeOrderBy(sortBy, sortOrder);
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

            return list;
        }

        /// <summary>
        /// 依 GUID 取得單一專案詳情（可選擇回傳 BOM 關聯與里程碑清單）
        /// </summary>
        public static projectClass GetProjectDetails(
            string projectGuid,
            bool includeBomDetails = false,
            bool includeMilestones = false,
            sys_serverSettingClass conf = null
        )
        {
            if (conf == null) throw new ArgumentNullException(nameof(conf));
            if (string.IsNullOrWhiteSpace(projectGuid)) throw new ArgumentException("缺少 projectGuid 參數");

            string guid = projectGuid.ToUpper();

            // 1) 查詢專案
            string pTable = ProjectsTable(conf);
            var sqlP = conf.GetSQLControl(new enum_projects().GetEnumDescription());

            string sql = $@"SELECT * FROM {conf.DBName}.{pTable} WHERE GUID = '{Esc(guid)}' LIMIT 1";
            var dt = sqlP.WtrteCommandAndExecuteReader(sql);
            if (dt.Rows.Count == 0)
            {
                throw new KeyNotFoundException($"查無 GUID = {guid} 的專案");
            }

            var project = dt.DataTableToRowList().SQLToClass<projectClass, enum_projects>()[0];

            // 2) 查詢 BOM 關聯
            var sqlBom = conf.GetSQLControl(new enum_projects().GetEnumDescription());
            string bomSql = $@"SELECT * FROM {conf.DBName}.project_bom_associations WHERE 專案GUID = '{Esc(guid)}'";
            var bomDt = sqlBom.WtrteCommandAndExecuteReader(bomSql);
            var bomRows = bomDt.DataTableToRowList().SQLToClass<project_bom_associationClass, enum_project_bom_associations>()
                          ?? new List<project_bom_associationClass>();

            project.AssociatedBomGuids = bomRows.Select(r => r.BOMGUID).Distinct().ToList();
            if (includeBomDetails)
            {
                project.BomAssociations = bomRows;
            }

            // 3)（預留）里程碑
            if (includeMilestones)
            {
                // 預留：可在此查 milestoneClass，現在只做擴充點
            }

            return project;
        }

        /// <summary>
        /// 依據 items 進度更新 ProjectRequirementClass 的 status
        /// </summary>
        public static void UpdateRequirementStatus(ProjectRequirementClass requirement)
        {
            if (requirement == null) return;

            // 若 items 有資料，依據 ProcurementProgress 更新狀態
            if (requirement.items != null && requirement.items.Count > 0)
            {
                // 取所有合法的 Enum 值 (忽略無效文字)
                var statusList = requirement.items
                    .Where(i => !string.IsNullOrWhiteSpace(i.ProcurementProgress))
                    .Select(i =>
                    {
                        if (Enum.TryParse(typeof(ProcurementStatuType), i.ProcurementProgress, out var result))
                            return (ProcurementStatuType)result;
                        return (ProcurementStatuType?)null;
                    })
                    .Where(e => e.HasValue)
                    .Select(e => e.Value)
                    .ToList();

                if (statusList.Any())
                {
                    // 取流程最前的狀態 (數字最小)
                    var minStatus = statusList.Min();
                    requirement.status = minStatus.ToString();
                }
            }
            else
            {
                // items 無資料 → 保留本身狀態 (不用處理)
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
        private List<Table> CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            List<Table> tables = new List<Table>();
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_projects()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_project_milestones()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_project_bom_associations()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_project_requirements()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_bom_requirement_items()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_requirement_status_history()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_project_documents()));

            
            return tables;
        }
    }
}
