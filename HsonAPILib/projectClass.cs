using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Basic;

namespace HsonAPILib
{
    
    /// <summary>
    /// 專案主檔欄位枚舉
    /// </summary>
    [EnumDescription("projects")]
    public enum enum_projects
    {
        /// <summary>唯一識別碼（大寫 GUID）</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>專案顯示用編號</summary>
        [Description("ID,VARCHAR,50,NONE")]
        ID,

        /// <summary>專案名稱</summary>
        [Description("名稱,VARCHAR,255,NONE")]
        名稱,

        /// <summary>專案描述</summary>
        [Description("描述,TEXT,20,NONE")]
        描述,

        /// <summary>客戶 GUID（大寫）</summary>
        [Description("客戶GUID,VARCHAR,50,NONE")]
        客戶GUID,

        /// <summary>客戶名稱</summary>
        [Description("客戶名稱,VARCHAR,255,NONE")]
        客戶名稱,

        /// <summary>專案狀態（投標中/已得標/進行中/已完成/已取消/暫停）</summary>
        [Description("狀態,VARCHAR,20,NONE")]
        狀態,

        /// <summary>專案優先級（低/中/高/緊急）</summary>
        [Description("優先級,VARCHAR,10,NONE")]
        優先級,

        /// <summary>投標日期</summary>
        [Description("投標日期,DATETIME,20,NONE")]
        投標日期,

        /// <summary>開標日期</summary>
        [Description("開標日期,DATETIME,20,NONE")]
        開標日期,

        /// <summary>得標日期</summary>
        [Description("得標日期,DATETIME,20,NONE")]
        得標日期,

        /// <summary>合約開始日期</summary>
        [Description("合約開始日期,DATETIME,20,NONE")]
        合約開始日期,

        /// <summary>合約結束日期</summary>
        [Description("合約結束日期,DATETIME,20,NONE")]
        合約結束日期,

        /// <summary>交貨日期</summary>
        [Description("交貨日期,DATETIME,20,NONE")]
        交貨日期,

        /// <summary>驗收日期</summary>
        [Description("驗收日期,DATETIME,20,NONE")]
        驗收日期,

        /// <summary>合約金額（字串）</summary>
        [Description("合約金額,VARCHAR,50,NONE")]
        合約金額,

        /// <summary>預估成本（字串）</summary>
        [Description("預估成本,VARCHAR,50,NONE")]
        預估成本,

        /// <summary>實際成本（字串）</summary>
        [Description("實際成本,VARCHAR,50,NONE")]
        實際成本,

        /// <summary>利潤率（字串）</summary>
        [Description("利潤率,VARCHAR,20,NONE")]
        利潤率,

        /// <summary>專案經理</summary>
        [Description("專案經理,VARCHAR,50,NONE")]
        專案經理,

        /// <summary>業務負責人</summary>
        [Description("業務負責人,VARCHAR,50,NONE")]
        業務負責人,

        /// <summary>技術負責人</summary>
        [Description("技術負責人,VARCHAR,50,NONE")]
        技術負責人,

        /// <summary>進度百分比（字串）</summary>
        [Description("進度百分比,VARCHAR,20,NONE")]
        進度百分比,

        /// <summary>里程碑數量（字串）</summary>
        [Description("里程碑數量,VARCHAR,20,NONE")]
        里程碑數量,

        /// <summary>已完成里程碑數（字串）</summary>
        [Description("已完成里程碑,VARCHAR,20,NONE")]
        已完成里程碑,

        /// <summary>需求數量（字串）</summary>
        [Description("需求數量,VARCHAR,20,NONE")]
        需求數量,

        /// <summary>文件數量（字串）</summary>
        [Description("文件數量,VARCHAR,20,NONE")]
        文件數量,

        /// <summary>交貨次數（字串）</summary>
        [Description("交貨次數,VARCHAR,20,NONE")]
        交貨次數,

        /// <summary>關聯 BOM 數量（字串）</summary>
        [Description("關聯BOM數量,VARCHAR,20,NONE")]
        關聯BOM數量,

        /// <summary>關聯 BOM GUID 清單（JSON 陣列字串）</summary>
        [Description("關聯BOMGUID清單,TEXT,20,NONE")]
        關聯BOMGUID清單,

        /// <summary>驗收狀態（未開始/進行中/已完成/有問題）</summary>
        [Description("驗收狀態,VARCHAR,20,NONE")]
        驗收狀態,

        /// <summary>驗收備註</summary>
        [Description("驗收備註,TEXT,20,NONE")]
        验收備註,

        /// <summary>備註</summary>
        [Description("備註,TEXT,20,NONE")]
        備註,

        /// <summary>標籤（逗號分隔）</summary>
        [Description("標籤,VARCHAR,500,NONE")]
        標籤,

        /// <summary>是否啟用（1/0）</summary>
        [Description("是否啟用,VARCHAR,1,NONE")]
        是否啟用,

        /// <summary>建立者</summary>
        [Description("建立者,VARCHAR,50,NONE")]
        建立者,

        /// <summary>建立時間</summary>
        [Description("建立時間,DATETIME,20,NONE")]
        建立時間,

        /// <summary>更新者</summary>
        [Description("更新者,VARCHAR,50,NONE")]
        更新者,

        /// <summary>更新時間</summary>
        [Description("更新時間,DATETIME,20,NONE")]
        更新時間
    }
    /// <summary>
    /// 專案主檔資料類別（所有屬性為 string，對應前端 JSON 欄位）
    /// </summary>
    public class projectClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        [JsonPropertyName("id")]
        public string ID { get; set; }

        [JsonPropertyName("name")]
        public string 名稱 { get; set; }

        [JsonPropertyName("description")]
        public string 描述 { get; set; }

        [JsonPropertyName("clientGuid")]
        public string 客戶GUID { get; set; }

        [JsonPropertyName("clientName")]
        public string 客戶名稱 { get; set; }

        [JsonPropertyName("status")]
        public string 狀態 { get; set; }

        [JsonPropertyName("priority")]
        public string 優先級 { get; set; }

        [JsonPropertyName("tenderDate")]
        public string 投標日期 { get; set; }

        [JsonPropertyName("bidOpeningDate")]
        public string 開標日期 { get; set; }

        [JsonPropertyName("awardedDate")]
        public string 得標日期 { get; set; }

        [JsonPropertyName("contractStartDate")]
        public string 合約開始日期 { get; set; }

        [JsonPropertyName("contractEndDate")]
        public string 合約結束日期 { get; set; }

        [JsonPropertyName("deliveryDate")]
        public string 交貨日期 { get; set; }

        [JsonPropertyName("acceptanceDate")]
        public string 驗收日期 { get; set; }

        [JsonPropertyName("contractAmount")]
        public string 合約金額 { get; set; }

        [JsonPropertyName("estimatedCost")]
        public string 預估成本 { get; set; }

        [JsonPropertyName("actualCost")]
        public string 實際成本 { get; set; }

        [JsonPropertyName("profitMargin")]
        public string 利潤率 { get; set; }

        [JsonPropertyName("projectManager")]
        public string 專案經理 { get; set; }

        [JsonPropertyName("salesPerson")]
        public string 業務負責人 { get; set; }

        [JsonPropertyName("technicalLead")]
        public string 技術負責人 { get; set; }

        [JsonPropertyName("progressPercentage")]
        public string 進度百分比 { get; set; }

        [JsonPropertyName("milestoneCount")]
        public string 里程碑數量 { get; set; }

        [JsonPropertyName("completedMilestones")]
        public string 已完成里程碑 { get; set; }

        [JsonPropertyName("requirementsCount")]
        public string 需求數量 { get; set; }

        [JsonPropertyName("documentsCount")]
        public string 文件數量 { get; set; }

        [JsonPropertyName("deliveriesCount")]
        public string 交貨次數 { get; set; }

        [JsonPropertyName("bomCount")]
        public string 關聯BOM數量 { get; set; }

        /// <summary>關聯 BOM GUID 清單</summary>
        [JsonPropertyName("associatedBomGuids")]
        public List<string> AssociatedBomGuids { get; set; }

        /// <summary>關聯 BOM 詳細清單（選填；includeBomDetails=true 時才回傳）</summary>
        [JsonPropertyName("bomAssociations")]
        public List<project_bom_associationClass> BomAssociations { get; set; }

        [JsonPropertyName("acceptanceStatus")]
        public string 驗收狀態 { get; set; }

        [JsonPropertyName("acceptanceNotes")]
        public string 驗收備註 { get; set; }

        [JsonPropertyName("notes")]
        public string 備註 { get; set; }

        [JsonPropertyName("tags")]
        public string 標籤 { get; set; }

        [JsonPropertyName("isActive")]
        public string 是否啟用 { get; set; }

        [JsonPropertyName("createdBy")]
        public string 建立者 { get; set; }

        [JsonPropertyName("createdAt")]
        public string 建立時間 { get; set; }

        [JsonPropertyName("updatedBy")]
        public string 更新者 { get; set; }

        [JsonPropertyName("updatedAt")]
        public string 更新時間 { get; set; }
    }

    /// <summary>
    /// 專案 BOM 關聯欄位枚舉
    /// </summary>
    [EnumDescription("project_bom_associations")]
    public enum enum_project_bom_associations
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        [Description("專案GUID,VARCHAR,50,NONE")]
        專案GUID,

        [Description("BOMGUID,VARCHAR,50,NONE")]
        BOMGUID,

        [Description("BOM編號,VARCHAR,50,NONE")]
        BOM編號,

        [Description("BOM名稱,VARCHAR,255,NONE")]
        BOM名稱,

        [Description("需求數量,VARCHAR,20,NONE")]
        需求數量,

        [Description("到期日,DATETIME,20,NONE")]
        到期日,

        [Description("關聯類型,VARCHAR,50,NONE")]
        關聯類型,

        [Description("狀態,VARCHAR,20,NONE")]
        狀態,

        [Description("備註,TEXT,20,NONE")]
        備註,

        [Description("建立者,VARCHAR,50,NONE")]
        建立者,

        [Description("建立時間,DATETIME,20,NONE")]
        建立時間,

        [Description("更新者,VARCHAR,50,NONE")]
        更新者,

        [Description("更新時間,DATETIME,20,NONE")]
        更新時間
    }
    /// <summary>
    /// 專案 BOM 關聯資料類別（所有屬性為 string）
    /// </summary>
    public class project_bom_associationClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        [JsonPropertyName("projectGuid")]
        public string 專案GUID { get; set; }

        [JsonPropertyName("bomGuid")]
        public string BOMGUID { get; set; }

        [JsonPropertyName("bomId")]
        public string BOM編號 { get; set; }

        [JsonPropertyName("bomName")]
        public string BOM名稱 { get; set; }

        [JsonPropertyName("requiredQuantity")]
        public string 需求數量 { get; set; }

        [JsonPropertyName("dueDate")]
        public string 到期日 { get; set; }

        [JsonPropertyName("associationType")]
        public string 關聯類型 { get; set; }

        [JsonPropertyName("status")]
        public string 狀態 { get; set; }

        [JsonPropertyName("notes")]
        public string 備註 { get; set; }

        [JsonPropertyName("createdBy")]
        public string 建立者 { get; set; }

        [JsonPropertyName("createdAt")]
        public string 建立時間 { get; set; }

        [JsonPropertyName("updatedBy")]
        public string 更新者 { get; set; }

        [JsonPropertyName("updatedAt")]
        public string 更新時間 { get; set; }
    }

    /// <summary>
    /// 專案里程碑欄位枚舉
    /// </summary>
    [EnumDescription("project_milestones")]
    public enum enum_project_milestones
    {
        /// <summary>里程碑唯一識別碼（大寫 GUID）</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>里程碑顯示用編號</summary>
        [Description("ID,VARCHAR,50,NONE")]
        ID,

        /// <summary>所屬專案 GUID（大寫）</summary>
        [Description("專案GUID,VARCHAR,50,NONE")]
        專案GUID,

        /// <summary>所屬專案編號</summary>
        [Description("專案編號,VARCHAR,50,NONE")]
        專案編號,

        /// <summary>里程碑名稱</summary>
        [Description("名稱,VARCHAR,255,NONE")]
        名稱,

        /// <summary>里程碑描述</summary>
        [Description("描述,TEXT,20,NONE")]
        描述,

        /// <summary>里程碑類型</summary>
        [Description("里程碑類型,VARCHAR,50,NONE")]
        里程碑類型,

        /// <summary>計劃日期</summary>
        [Description("計劃日期,DATETIME,20,NONE")]
        計劃日期,

        /// <summary>實際日期</summary>
        [Description("實際日期,DATETIME,20,NONE")]
        實際日期,

        /// <summary>里程碑狀態（未開始/進行中/已完成/已延期/已取消）</summary>
        [Description("狀態,VARCHAR,20,NONE")]
        狀態,

        /// <summary>進度百分比（字串）</summary>
        [Description("進度百分比,VARCHAR,20,NONE")]
        進度百分比,

        /// <summary>負責人</summary>
        [Description("負責人,VARCHAR,50,NONE")]
        負責人,

        /// <summary>備註</summary>
        [Description("備註,TEXT,20,NONE")]
        備註,

        /// <summary>是否啟用（1/0）</summary>
        [Description("是否啟用,VARCHAR,1,NONE")]
        是否啟用,

        /// <summary>建立者</summary>
        [Description("建立者,VARCHAR,50,NONE")]
        建立者,

        /// <summary>建立時間</summary>
        [Description("建立時間,DATETIME,20,NONE")]
        建立時間,

        /// <summary>更新者</summary>
        [Description("更新者,VARCHAR,50,NONE")]
        更新者,

        /// <summary>更新時間</summary>
        [Description("更新時間,DATETIME,20,NONE")]
        更新時間
    }
    /// <summary>
    /// 專案里程碑資料類別（所有屬性為 string，對應前端 JSON 欄位）
    /// </summary>
    public class milestoneClass
    {
        /// <summary>里程碑唯一識別碼（大寫 GUID）</summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>里程碑顯示用編號</summary>
        [JsonPropertyName("ID")]
        public string ID { get; set; }

        /// <summary>所屬專案 GUID（大寫）</summary>
        [JsonPropertyName("projectGuid")]
        public string 專案GUID { get; set; }

        /// <summary>所屬專案編號</summary>
        [JsonPropertyName("projectId")]
        public string 專案編號 { get; set; }

        /// <summary>里程碑名稱</summary>
        [JsonPropertyName("name")]
        public string 名稱 { get; set; }

        /// <summary>里程碑描述</summary>
        [JsonPropertyName("description")]
        public string 描述 { get; set; }

        /// <summary>里程碑類型</summary>
        [JsonPropertyName("milestoneType")]
        public string 里程碑類型 { get; set; }

        /// <summary>計劃日期（yyyy-MM-dd）</summary>
        [JsonPropertyName("plannedDate")]
        public string 計劃日期 { get; set; }

        /// <summary>實際日期（yyyy-MM-dd）</summary>
        [JsonPropertyName("actualDate")]
        public string 實際日期 { get; set; }

        /// <summary>里程碑狀態（未開始/進行中/已完成/已延期/已取消）</summary>
        [JsonPropertyName("status")]
        public string 狀態 { get; set; }

        /// <summary>進度百分比（字串）</summary>
        [JsonPropertyName("progressPercentage")]
        public string 進度百分比 { get; set; }

        /// <summary>負責人</summary>
        [JsonPropertyName("responsiblePerson")]
        public string 負責人 { get; set; }

        /// <summary>備註</summary>
        [JsonPropertyName("notes")]
        public string 備註 { get; set; }

        /// <summary>是否啟用（1/0）</summary>
        [JsonPropertyName("isActive")]
        public string 是否啟用 { get; set; }

        /// <summary>建立者</summary>
        [JsonPropertyName("createdBy")]
        public string 建立者 { get; set; }

        /// <summary>建立時間（yyyy-MM-dd HH:mm:ss）</summary>
        [JsonPropertyName("createdAt")]
        public string 建立時間 { get; set; }

        /// <summary>更新者</summary>
        [JsonPropertyName("updatedBy")]
        public string 更新者 { get; set; }

        /// <summary>更新時間（yyyy-MM-dd HH:mm:ss）</summary>
        [JsonPropertyName("updatedAt")]
        public string 更新時間 { get; set; }
    }


    /// <summary>
    /// 專案統計資料類別（所有屬性為 string，對應前端 JSON 欄位）
    /// </summary>
    public class projectStatisticsClass
    {
        /// <summary>總專案數</summary>
        [JsonPropertyName("totalProjects")]
        public string 總專案數 { get; set; }

        /// <summary>投標中專案數</summary>
        [JsonPropertyName("tenderingProjects")]
        public string 投標中 { get; set; }

        /// <summary>已得標專案數</summary>
        [JsonPropertyName("awardedProjects")]
        public string 已得標 { get; set; }

        /// <summary>進行中專案數</summary>
        [JsonPropertyName("ongoingProjects")]
        public string 進行中 { get; set; }

        /// <summary>已完成專案數</summary>
        [JsonPropertyName("completedProjects")]
        public string 已完成 { get; set; }

        /// <summary>已取消專案數</summary>
        [JsonPropertyName("cancelledProjects")]
        public string 已取消 { get; set; }

        /// <summary>暫停專案數</summary>
        [JsonPropertyName("pausedProjects")]
        public string 暫停 { get; set; }

        /// <summary>即將到期專案數</summary>
        [JsonPropertyName("upcomingDeadlines")]
        public string 即將到期 { get; set; }

        /// <summary>總合約金額</summary>
        [JsonPropertyName("totalContractValue")]
        public string 總合約金額 { get; set; }

        /// <summary>總預估成本</summary>
        [JsonPropertyName("totalEstimatedCost")]
        public string 總預估成本 { get; set; }

        /// <summary>總實際成本</summary>
        [JsonPropertyName("totalActualCost")]
        public string 總實際成本 { get; set; }

        /// <summary>平均利潤率</summary>
        [JsonPropertyName("averageProfitMargin")]
        public string 平均利潤率 { get; set; }

        /// <summary>本月新增專案數</summary>
        [JsonPropertyName("newProjectsThisMonth")]
        public string 本月新增 { get; set; }

        /// <summary>本月完成專案數</summary>
        [JsonPropertyName("completedProjectsThisMonth")]
        public string 本月完成 { get; set; }
    }

    /// <summary>
    /// 專案需求欄位枚舉
    /// </summary>
    [EnumDescription("project_requirements")]
    public enum enum_project_requirements
    {
        /// <summary>唯一識別碼</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>需求編號</summary>
        [Description("ID,VARCHAR,50,UNIQUE")]
        ID,

        /// <summary>專案GUID</summary>
        [Description("project_guid,VARCHAR,50,NONE")]
        project_guid,

        /// <summary>專案編號</summary>
        [Description("project_id,VARCHAR,50,NONE")]
        project_id,

        /// <summary>請購類型</summary>
        [Description("procurement_type,VARCHAR,20,NONE")]
        procurement_type,

        /// <summary>需求描述</summary>
        [Description("description,TEXT,20,NONE")]
        description,

        /// <summary>數量</summary>
        [Description("quantity,VARCHAR,50,NONE")]
        quantity,

        /// <summary>單位</summary>
        [Description("unit,VARCHAR,20,NONE")]
        unit,

        /// <summary>需求狀態</summary>
        [Description("status,VARCHAR,20,NONE")]
        status,

        /// <summary>優先級</summary>
        [Description("priority,VARCHAR,10,NONE")]
        priority,

        /// <summary>需求期限</summary>
        [Description("due_date,DATETIME,20,NONE")]
        due_date,

        /// <summary>請購日期</summary>
        [Description("requested_date,DATETIME,20,NONE")]
        requested_date,

        /// <summary>核准日期</summary>
        [Description("approved_date,DATETIME,20,NONE")]
        approved_date,

        /// <summary>採購日期</summary>
        [Description("purchased_date,DATETIME,20,NONE")]
        purchased_date,

        /// <summary>交貨日期</summary>
        [Description("delivered_date,DATETIME,20,NONE")]
        delivered_date,

        /// <summary>驗收日期</summary>
        [Description("accepted_date,DATETIME,20,NONE")]
        accepted_date,

        /// <summary>預估成本</summary>
        [Description("estimated_cost,VARCHAR,50,NONE")]
        estimated_cost,

        /// <summary>實際成本</summary>
        [Description("actual_cost,VARCHAR,50,NONE")]
        actual_cost,

        /// <summary>核准預算</summary>
        [Description("approved_budget,VARCHAR,50,NONE")]
        approved_budget,

        /// <summary>貨品ID</summary>
        [Description("item_id,VARCHAR,50,NONE")]
        item_id,

        /// <summary>貨品名稱</summary>
        [Description("item_name,VARCHAR,200,NONE")]
        item_name,

        /// <summary>貨品代碼</summary>
        [Description("item_code,VARCHAR,50,NONE")]
        item_code,

        /// <summary>供應商名稱</summary>
        [Description("supplier_name,VARCHAR,200,NONE")]
        supplier_name,

        /// <summary>BOM GUID</summary>
        [Description("bom_guid,VARCHAR,50,NONE")]
        bom_guid,

        /// <summary>BOM 編號</summary>
        [Description("bom_id,VARCHAR,50,NONE")]
        bom_id,

        /// <summary>是否來自BOM</summary>
        [Description("is_from_bom,VARCHAR,5,NONE")]
        is_from_bom,

        /// <summary>採購單號</summary>
        [Description("purchase_order_id,VARCHAR,50,NONE")]
        purchase_order_id,

        /// <summary>廠商報價</summary>
        [Description("vendor_quote_amount,VARCHAR,50,NONE")]
        vendor_quote_amount,

        /// <summary>議價金額</summary>
        [Description("negotiated_amount,VARCHAR,50,NONE")]
        negotiated_amount,

        /// <summary>請購人</summary>
        [Description("requester,VARCHAR,100,NONE")]
        requester,

        /// <summary>核准者</summary>
        [Description("approver,VARCHAR,100,NONE")]
        approver,

        /// <summary>採購人</summary>
        [Description("purchaser,VARCHAR,100,NONE")]
        purchaser,

        /// <summary>規格要求</summary>
        [Description("specifications,TEXT,20,NONE")]
        specifications,

        /// <summary>交貨地址</summary>
        [Description("delivery_address,TEXT,20,NONE")]
        delivery_address,

        /// <summary>備註</summary>
        [Description("notes,TEXT,20,NONE")]
        notes,

        /// <summary>是否啟用</summary>
        [Description("is_active,VARCHAR,5,NONE")]
        is_active,

        /// <summary>建立者</summary>
        [Description("created_by,VARCHAR,100,NONE")]
        created_by,

        /// <summary>建立時間</summary>
        [Description("created_at,DATETIME,20,NONE")]
        created_at,

        /// <summary>更新者</summary>
        [Description("updated_by,VARCHAR,100,NONE")]
        updated_by,

        /// <summary>更新時間</summary>
        [Description("updated_at,DATETIME,20,NONE")]
        updated_at
    }
    /// <summary>
    /// 專案需求資料類別
    /// </summary>
    public class ProjectRequirementClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        [JsonPropertyName("ID")]
        public string ID { get; set; }

        [JsonPropertyName("project_guid")]
        public string project_guid { get; set; }

        [JsonPropertyName("project_id")]
        public string project_id { get; set; }

        [JsonPropertyName("procurement_type")]
        public string procurement_type { get; set; }

        [JsonPropertyName("description")]
        public string description { get; set; }

        [JsonPropertyName("quantity")]
        public string quantity { get; set; }

        [JsonPropertyName("unit")]
        public string unit { get; set; }

        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("priority")]
        public string priority { get; set; }

        [JsonPropertyName("due_date")]
        public string due_date { get; set; }

        [JsonPropertyName("requested_date")]
        public string requested_date { get; set; }

        [JsonPropertyName("approved_date")]
        public string approved_date { get; set; }

        [JsonPropertyName("purchased_date")]
        public string purchased_date { get; set; }

        [JsonPropertyName("delivered_date")]
        public string delivered_date { get; set; }

        [JsonPropertyName("accepted_date")]
        public string accepted_date { get; set; }

        [JsonPropertyName("estimated_cost")]
        public string estimated_cost { get; set; }

        [JsonPropertyName("actual_cost")]
        public string actual_cost { get; set; }

        [JsonPropertyName("approved_budget")]
        public string approved_budget { get; set; }

        [JsonPropertyName("item_id")]
        public string item_id { get; set; }

        [JsonPropertyName("item_name")]
        public string item_name { get; set; }

        [JsonPropertyName("item_code")]
        public string item_code { get; set; }

        [JsonPropertyName("supplier_name")]
        public string supplier_name { get; set; }

        [JsonPropertyName("bom_guid")]
        public string bom_guid { get; set; }

        [JsonPropertyName("bom_id")]
        public string bom_id { get; set; }

        [JsonPropertyName("is_from_bom")]
        public string is_from_bom { get; set; }

        [JsonPropertyName("purchase_order_id")]
        public string purchase_order_id { get; set; }

        [JsonPropertyName("vendor_quote_amount")]
        public string vendor_quote_amount { get; set; }

        [JsonPropertyName("negotiated_amount")]
        public string negotiated_amount { get; set; }

        [JsonPropertyName("requester")]
        public string requester { get; set; }

        [JsonPropertyName("approver")]
        public string approver { get; set; }

        [JsonPropertyName("purchaser")]
        public string purchaser { get; set; }

        [JsonPropertyName("specifications")]
        public string specifications { get; set; }

        [JsonPropertyName("delivery_address")]
        public string delivery_address { get; set; }

        [JsonPropertyName("notes")]
        public string notes { get; set; }

        [JsonPropertyName("is_active")]
        public string is_active { get; set; }

        [JsonPropertyName("created_by")]
        public string created_by { get; set; }

        [JsonPropertyName("created_at")]
        public string created_at { get; set; }

        [JsonPropertyName("updated_by")]
        public string updated_by { get; set; }

        [JsonPropertyName("updated_at")]
        public string updated_at { get; set; }

        [JsonPropertyName("items")]
        public List<BomRequirementItemClass> items { get; set; }
    }


    /// <summary>
    /// BOM 需求項目欄位枚舉
    /// </summary>
    [EnumDescription("bom_requirement_items")]
    public enum enum_bom_requirement_items
    {
        /// <summary>唯一識別碼</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>項目編號</summary>
        [Description("ID,VARCHAR,50,NONE")]
        ID,

        /// <summary>需求GUID</summary>
        [Description("requirement_guid,VARCHAR,50,NONE")]
        RequirementGuid,

        /// <summary>需求編號</summary>
        [Description("requirement_id,VARCHAR,50,NONE")]
        RequirementId,

        /// <summary>項目描述</summary>
        [Description("item_description,TEXT,20,NONE")]
        ItemDescription,

        /// <summary>料號</summary>
        [Description("item_name,VARCHAR,300,NONE")]
        ItemName,

        /// <summary>料號</summary>
        [Description("item_code,VARCHAR,50,NONE")]
        ItemCode,

        /// <summary>數量</summary>
        [Description("quantity,VARCHAR,50,NONE")]
        Quantity,

        /// <summary>單位</summary>
        [Description("unit,VARCHAR,20,NONE")]
        Unit,

        /// <summary>預估單價</summary>
        [Description("estimated_unit_cost,VARCHAR,50,NONE")]
        EstimatedUnitCost,

        /// <summary>預估總價</summary>
        [Description("estimated_total_cost,VARCHAR,50,NONE")]
        EstimatedTotalCost,

        /// <summary>實際單價</summary>
        [Description("actual_unit_cost,VARCHAR,50,NONE")]
        ActualUnitCost,

        /// <summary>實際總價</summary>
        [Description("actual_total_cost,VARCHAR,50,NONE")]
        ActualTotalCost,

        /// <summary>項目狀態</summary>
        [Description("item_status,VARCHAR,20,NONE")]
        ItemStatus,

        /// <summary>採購進度</summary>
        [Description("procurement_progress,VARCHAR,10,NONE")]
        ProcurementProgress,

        /// <summary>技術規格</summary>
        [Description("specifications,TEXT,20,NONE")]
        Specifications,

        /// <summary>技術要求</summary>
        [Description("technical_requirements,TEXT,20,NONE")]
        TechnicalRequirements,

        /// <summary>品質標準</summary>
        [Description("quality_standards,TEXT,20,NONE")]
        QualityStandards,

        /// <summary>偏好供應商</summary>
        [Description("preferred_supplier,VARCHAR,200,NONE")]
        PreferredSupplier,

        /// <summary>供應商料號</summary>
        [Description("supplier_part_number,VARCHAR,100,NONE")]
        SupplierPartNumber,

        /// <summary>交期</summary>
        [Description("due_date,VARCHAR,10,NONE")]
        dueDate,

        /// <summary>交期天數</summary>
        [Description("lead_time_days,VARCHAR,10,NONE")]
        LeadTimeDays,

        /// <summary>備註</summary>
        [Description("notes,TEXT,20,NONE")]
        Notes,

        /// <summary>是否啟用</summary>
        [Description("is_active,VARCHAR,5,NONE")]
        IsActive,

        /// <summary>建立者</summary>
        [Description("created_by,VARCHAR,100,NONE")]
        CreatedBy,

        /// <summary>建立時間</summary>
        [Description("created_at,DATETIME,20,NONE")]
        CreatedAt,

        /// <summary>更新者</summary>
        [Description("updated_by,VARCHAR,100,NONE")]
        UpdatedBy,

        /// <summary>更新時間</summary>
        [Description("updated_at,DATETIME,20,NONE")]
        UpdatedAt
    }
    /// <summary>
    /// BOM 需求項目資料類別
    /// </summary>
    public class BomRequirementItemClass
    {
        /// <summary>唯一識別碼</summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>項目編號</summary>
        [JsonPropertyName("id")]
        public string ID { get; set; }

        /// <summary>需求GUID</summary>
        [JsonPropertyName("requirement_guid")]
        public string RequirementGuid { get; set; }

        /// <summary>需求編號</summary>
        [JsonPropertyName("requirement_id")]
        public string RequirementId { get; set; }

        /// <summary>項目描述</summary>
        [JsonPropertyName("item_description")]
        public string ItemDescription { get; set; }

        /// <summary>料號</summary>
        [JsonPropertyName("item_code")]
        public string ItemCode { get; set; }

        /// <summary>項目名稱</summary>
        [JsonPropertyName("item_name")]
        public string ItemName { get; set; }

        /// <summary>數量</summary>
        [JsonPropertyName("quantity")]
        public string Quantity { get; set; }

        /// <summary>單位</summary>
        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        /// <summary>預估單價</summary>
        [JsonPropertyName("estimated_unit_cost")]
        public string EstimatedUnitCost { get; set; }

        /// <summary>預估總價</summary>
        [JsonPropertyName("estimated_total_cost")]
        public string EstimatedTotalCost { get; set; }

        /// <summary>實際單價</summary>
        [JsonPropertyName("actual_unit_cost")]
        public string ActualUnitCost { get; set; }

        /// <summary>實際總價</summary>
        [JsonPropertyName("actual_total_cost")]
        public string ActualTotalCost { get; set; }

        /// <summary>項目狀態</summary>
        [JsonPropertyName("item_status")]
        public string ItemStatus { get; set; }

        /// <summary>採購進度</summary>
        [JsonPropertyName("procurement_progress")]
        public string ProcurementProgress { get; set; }

        /// <summary>技術規格</summary>
        [JsonPropertyName("specifications")]
        public string Specifications { get; set; }

        /// <summary>技術要求</summary>
        [JsonPropertyName("technical_requirements")]
        public string TechnicalRequirements { get; set; }

        /// <summary>品質標準</summary>
        [JsonPropertyName("quality_standards")]
        public string QualityStandards { get; set; }

        /// <summary>偏好供應商</summary>
        [JsonPropertyName("preferred_supplier")]
        public string PreferredSupplier { get; set; }

        /// <summary>供應商料號</summary>
        [JsonPropertyName("supplier_part_number")]
        public string SupplierPartNumber { get; set; }

        /// <summary>交期天數</summary>
        [JsonPropertyName("lead_time_days")]
        public string LeadTimeDays { get; set; }

        /// <summary>交期天數</summary>
        [JsonPropertyName("due_date")]
        public string dueDate { get; set; }

        /// <summary>備註</summary>
        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        /// <summary>是否啟用</summary>
        [JsonPropertyName("is_active")]
        public string IsActive { get; set; }

        /// <summary>建立者</summary>
        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        /// <summary>更新者</summary>
        [JsonPropertyName("updated_by")]
        public string UpdatedBy { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }
    }

    /// <summary>
    /// 需求狀態歷程紀錄欄位枚舉
    /// </summary>
    [EnumDescription("requirement_status_history")]
    public enum enum_requirement_status_history
    {
        /// <summary>唯一識別碼</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>需求 GUID</summary>
        [Description("requirement_guid,VARCHAR,50,NONE")]
        RequirementGuid,

        /// <summary>前一狀態</summary>
        [Description("previous_status,VARCHAR,20,NONE")]
        PreviousStatus,

        /// <summary>新狀態</summary>
        [Description("new_status,VARCHAR,20,NONE")]
        NewStatus,

        /// <summary>變更原因</summary>
        [Description("change_reason,TEXT,20,NONE")]
        ChangeReason,

        /// <summary>變更人</summary>
        [Description("changed_by,VARCHAR,100,NONE")]
        ChangedBy,

        /// <summary>變更時間</summary>
        [Description("changed_at,DATETIME,20,NONE")]
        ChangedAt,

        /// <summary>變更備註</summary>
        [Description("notes,TEXT,20,NONE")]
        Notes
    }
    /// <summary>
    /// 需求狀態歷程紀錄資料類別
    /// </summary>
    public class RequirementStatusHistoryClass
    {
        /// <summary>唯一識別碼</summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>需求 GUID</summary>
        [JsonPropertyName("requirement_guid")]
        public string RequirementGuid { get; set; }

        /// <summary>前一狀態</summary>
        [JsonPropertyName("previous_status")]
        public string PreviousStatus { get; set; }

        /// <summary>新狀態</summary>
        [JsonPropertyName("new_status")]
        public string NewStatus { get; set; }

        /// <summary>變更原因</summary>
        [JsonPropertyName("change_reason")]
        public string ChangeReason { get; set; }

        /// <summary>變更人</summary>
        [JsonPropertyName("changed_by")]
        public string ChangedBy { get; set; }

        /// <summary>變更時間</summary>
        [JsonPropertyName("changed_at")]
        public string ChangedAt { get; set; }

        /// <summary>變更備註</summary>
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }


    /// <summary>
    /// BOM 文件欄位枚舉
    /// </summary>
    [EnumDescription("project_documents")]
    public enum enum_project_documents
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>文件編號</summary>
        [Description("ID,VARCHAR,50,INDEX")]
        ID,

        /// <summary>所屬 BOM GUID</summary>
        [Description("project_GUID,VARCHAR,50,INDEX")]
        project_GUID,

        /// <summary>檔案名稱</summary>
        [Description("fileName,VARCHAR,255,NONE")]
        fileName,

        /// <summary>原始檔案名稱</summary>
        [Description("originalName,VARCHAR,255,NONE")]
        originalName,

        /// <summary>檔案類型（圖面/規格書/技術文件/其他）</summary>
        [Description("type,VARCHAR,50,NONE")]
        type,

        /// <summary>檔案大小</summary>
        [Description("fileSize,VARCHAR,50,NONE")]
        fileSize,

        /// <summary>上傳日期</summary>
        [Description("uploadDate,DATETIME,20,INDEX")]
        uploadDate,

        /// <summary>上傳者</summary>
        [Description("uploadedBy,VARCHAR,100,NONE")]
        uploadedBy,

        /// <summary>檔案網址</summary>
        [Description("url,VARCHAR,500,NONE")]
        url,

        /// <summary>備註</summary>
        [Description("notes,VARCHAR,500,NONE")]
        notes
    }
    /// <summary>
    /// BOM 文件類別
    /// </summary>
    public class projectDocumentClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>文件編號</summary>
        [JsonPropertyName("ID")]
        public string ID { get; set; }

        /// <summary>所屬 project_GUID</summary>
        [JsonPropertyName("project_GUID")]
        public string project_GUID { get; set; }

        /// <summary>檔案名稱</summary>
        [JsonPropertyName("fileName")]
        public string fileName { get; set; }

        /// <summary>原始檔案名稱</summary>
        [JsonPropertyName("originalName")]
        public string originalName { get; set; }

        /// <summary>檔案類型（圖面/規格書/技術文件/其他）</summary>
        [JsonPropertyName("type")]
        public string type { get; set; }

        /// <summary>檔案大小</summary>
        [JsonPropertyName("fileSize")]
        public string fileSize { get; set; }

        /// <summary>上傳日期</summary>
        [JsonPropertyName("uploadDate")]
        public string uploadDate { get; set; }

        /// <summary>上傳者</summary>
        [JsonPropertyName("uploadedBy")]
        public string uploadedBy { get; set; }

        /// <summary>檔案網址</summary>
        [JsonPropertyName("url")]
        public string url { get; set; }

        /// <summary>備註</summary>
        [JsonPropertyName("notes")]
        public string notes { get; set; }
    }

}
