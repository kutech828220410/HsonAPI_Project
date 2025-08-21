using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Basic;

namespace HsonAPILib
{
    #region ===================== 專案主檔枚舉 =====================

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

    #endregion

    #region ===================== 專案主檔資料類別 =====================

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

    #endregion

    #region ===================== 專案 BOM 關聯枚舉 =====================

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

    #endregion

    #region ===================== 專案 BOM 關聯資料類別 =====================

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

    #endregion

    #region ===================== 專案里程碑枚舉 =====================

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

    #endregion

    #region ===================== 里程碑資料類別 =====================

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

    #endregion

    #region ===================== 專案統計資料類別（回傳用） =====================

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

    #endregion




}
