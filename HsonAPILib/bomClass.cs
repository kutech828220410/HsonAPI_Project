using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Basic;
using System.ComponentModel;
using System.Text.Json;

namespace HsonAPILib
{
    /// <summary>
    /// 專案 BOM 欄位枚舉
    /// </summary>
    [EnumDescription("project_boms")]
    public enum enum_project_boms
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>顯示編號</summary>
        [Description("ID,VARCHAR,50,INDEX")]
        ID,

        /// <summary>專案 GUID</summary>
        [Description("ProjectGUID,VARCHAR,50,INDEX")]
        ProjectGUID,

        /// <summary>專案顯示編號</summary>
        [Description("ProjectID,VARCHAR,50,NONE")]
        ProjectID,

        /// <summary>BOM 名稱</summary>
        [Description("name,VARCHAR,255,NONE")]
        name,

        /// <summary>BOM 描述</summary>
        [Description("description,VARCHAR,500,NONE")]
        description,

        /// <summary>版本號</summary>
        [Description("version,VARCHAR,50,NONE")]
        version,

        /// <summary>BOM 狀態（草稿/審核中/已核准/已發佈/已停用）</summary>
        [Description("status,VARCHAR,50,NONE")]
        status,

        /// <summary>BOM 類型（產品BOM/研發BOM/採購BOM）</summary>
        [Description("bomType,VARCHAR,50,NONE")]
        bomType,

        /// <summary>總品項數</summary>
        [Description("totalItems,VARCHAR,50,NONE")]
        totalItems,

        /// <summary>總成本</summary>
        [Description("totalCost,VARCHAR,50,NONE")]
        totalCost,

        /// <summary>建立者</summary>
        [Description("createdBy,VARCHAR,100,NONE")]
        createdBy,

        /// <summary>建立時間</summary>
        [Description("createdAt,DATETIME,20,INDEX")]
        createdAt,

        /// <summary>更新時間</summary>
        [Description("updatedAt,DATETIME,20,INDEX")]
        updatedAt,

        /// <summary>核准者</summary>
        [Description("approvedBy,VARCHAR,100,NONE")]
        approvedBy,

        /// <summary>核准時間</summary>
        [Description("approvedAt,DATETIME,20,NONE")]
        approvedAt,

        /// <summary>備註</summary>
        [Description("notes,VARCHAR,500,NONE")]
        notes
    }

    /// <summary>
    /// 專案 BOM 類別
    /// </summary>
    public class ProjectBomClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>顯示編號</summary>
        [JsonPropertyName("ID")]
        public string ID { get; set; }

        /// <summary>專案 GUID</summary>
        [JsonPropertyName("ProjectGUID")]
        public string ProjectGUID { get; set; }

        /// <summary>專案顯示編號</summary>
        [JsonPropertyName("ProjectID")]
        public string ProjectID { get; set; }

        /// <summary>BOM 名稱</summary>
        [JsonPropertyName("name")]
        public string name { get; set; }

        /// <summary>BOM 描述</summary>
        [JsonPropertyName("description")]
        public string description { get; set; }

        /// <summary>版本號</summary>
        [JsonPropertyName("version")]
        public string version { get; set; }

        /// <summary>BOM 狀態</summary>
        [JsonPropertyName("status")]
        public string status { get; set; }

        /// <summary>BOM 類型</summary>
        [JsonPropertyName("bomType")]
        public string bomType { get; set; }

        /// <summary>總品項數</summary>
        [JsonPropertyName("totalItems")]
        public string totalItems { get; set; }

        /// <summary>總成本</summary>
        [JsonPropertyName("totalCost")]
        public string totalCost { get; set; }

        /// <summary>建立者</summary>
        [JsonPropertyName("createdBy")]
        public string createdBy { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("createdAt")]
        public string createdAt { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updatedAt")]
        public string updatedAt { get; set; }

        /// <summary>核准者</summary>
        [JsonPropertyName("approvedBy")]
        public string approvedBy { get; set; }

        /// <summary>核准時間</summary>
        [JsonPropertyName("approvedAt")]
        public string approvedAt { get; set; }

        /// <summary>備註</summary>
        [JsonPropertyName("notes")]
        public string notes { get; set; }

        /// <summary>BOM 項目清單</summary>
        [JsonPropertyName("items")]
        public List<BomItemClass> items { get; set; } = new List<BomItemClass>();

        /// <summary>相關文件</summary>
        [JsonPropertyName("documents")]
        public List<BomDocumentClass> documents { get; set; } = new List<BomDocumentClass>();
    }


    /// <summary>
    /// BOM 項目欄位枚舉
    /// </summary>
    [EnumDescription("bom_items")]
    public enum enum_bom_items
    {
        /// <summary>唯一識別碼</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>顯示編號</summary>
        [Description("ID,VARCHAR,50,INDEX")]
        ID,

        /// <summary>所屬 BOM GUID</summary>
        [Description("BomGUID,VARCHAR,50,INDEX")]
        BomGUID,

        /// <summary>所屬 BOM 顯示編號</summary>
        [Description("BomID,VARCHAR,50,NONE")]
        BomID,

        /// <summary>料號</summary>
        [Description("itemCode,VARCHAR,100,NONE")]
        itemCode,

        /// <summary>品項名稱</summary>
        [Description("itemName,VARCHAR,255,NONE")]
        itemName,

        /// <summary>描述</summary>
        [Description("description,VARCHAR,500,NONE")]
        description,

        /// <summary>規格</summary>
        [Description("specification,VARCHAR,255,NONE")]
        specification,

        /// <summary>數量</summary>
        [Description("quantity,VARCHAR,50,NONE")]
        quantity,

        /// <summary>單位</summary>
        [Description("unit,VARCHAR,50,NONE")]
        unit,

        /// <summary>交期</summary>
        [Description("leadTime,VARCHAR,50,NONE")]
        leadTime,

        /// <summary>備註</summary>
        [Description("notes,VARCHAR,500,NONE")]
        notes,

        /// <summary>項目類型（貨品/子項目）</summary>
        [Description("itemType,VARCHAR,50,NONE")]
        itemType,

        /// <summary>父項目 GUID</summary>
        [Description("ParentGUID,VARCHAR,50,INDEX")]
        ParentGUID,

        /// <summary>父項目顯示編號</summary>
        [Description("ParentID,VARCHAR,50,NONE")]
        ParentID,

        /// <summary>建立時間</summary>
        [Description("createdAt,DATETIME,20,INDEX")]
        createdAt,

        /// <summary>更新時間</summary>
        [Description("updatedAt,DATETIME,20,INDEX")]
        updatedAt
    }
    /// <summary>
    /// BOM 項目資料類別
    /// </summary>
    public class BomItemClass
    {
        /// <summary>唯一識別碼</summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>顯示編號</summary>
        [JsonPropertyName("ID")]
        public string ID { get; set; }

        /// <summary>所屬 BOM GUID</summary>
        [JsonPropertyName("BomGUID")]
        public string BomGUID { get; set; }

        /// <summary>所屬 BOM 顯示編號</summary>
        [JsonPropertyName("BomID")]
        public string BomID { get; set; }

        /// <summary>料號</summary>
        [JsonPropertyName("itemCode")]
        public string itemCode { get; set; }

        /// <summary>品項名稱</summary>
        [JsonPropertyName("itemName")]
        public string itemName { get; set; }

        /// <summary>描述</summary>
        [JsonPropertyName("description")]
        public string description { get; set; }

        /// <summary>規格</summary>
        [JsonPropertyName("specification")]
        public string specification { get; set; }

        /// <summary>數量</summary>
        [JsonPropertyName("quantity")]
        public string quantity { get; set; }

        /// <summary>單位</summary>
        [JsonPropertyName("unit")]
        public string unit { get; set; }

        /// <summary>交期</summary>
        [JsonPropertyName("leadTime")]
        public string leadTime { get; set; }

        /// <summary>備註</summary>
        [JsonPropertyName("notes")]
        public string notes { get; set; }

        /// <summary>項目類型（貨品/子項目）</summary>
        [JsonPropertyName("itemType")]
        public string itemType { get; set; }

        /// <summary>父項目 GUID</summary>
        [JsonPropertyName("ParentGUID")]
        public string ParentGUID { get; set; }

        /// <summary>父項目顯示編號</summary>
        [JsonPropertyName("ParentID")]
        public string ParentID { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("createdAt")]
        public string createdAt { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updatedAt")]
        public string updatedAt { get; set; }
    }


    /// <summary>
    /// BOM 文件欄位枚舉
    /// </summary>
    [EnumDescription("bom_documents")]
    public enum enum_bom_documents
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>文件編號</summary>
        [Description("ID,VARCHAR,50,INDEX")]
        ID,

        /// <summary>所屬 BOM GUID</summary>
        [Description("BomGUID,VARCHAR,50,INDEX")]
        BomGUID,

        /// <summary>所屬 BOM 項目 GUID</summary>
        [Description("BomItemGUID,VARCHAR,50,NONE")]
        BomItemGUID,

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
    public class BomDocumentClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>文件編號</summary>
        [JsonPropertyName("ID")]
        public string ID { get; set; }

        /// <summary>所屬 BOM GUID</summary>
        [JsonPropertyName("BomGUID")]
        public string BomGUID { get; set; }

        /// <summary>所屬 BOM 項目 GUID</summary>
        [JsonPropertyName("BomItemGUID")]
        public string BomItemGUID { get; set; }

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

    /// <summary>
    /// BOM 統計資料
    /// </summary>
    public class BomStatsClass
    {
        /// <summary>總 BOM 數量</summary>
        [JsonPropertyName("totalBoms")]
        public string totalBoms { get; set; }

        /// <summary>草稿 BOM 數量</summary>
        [JsonPropertyName("draftBoms")]
        public string draftBoms { get; set; }

        /// <summary>已核准 BOM 數量</summary>
        [JsonPropertyName("approvedBoms")]
        public string approvedBoms { get; set; }

        /// <summary>已發佈 BOM 數量</summary>
        [JsonPropertyName("publishedBoms")]
        public string publishedBoms { get; set; }

        /// <summary>總品項數量</summary>
        [JsonPropertyName("totalItems")]
        public string totalItems { get; set; }

        /// <summary>總成本</summary>
        [JsonPropertyName("totalCost")]
        public string totalCost { get; set; }
    }
    /// <summary>
    /// BOM 篩選條件
    /// </summary>
    public class BomFiltersClass
    {
        /// <summary>搜尋關鍵字</summary>
        [JsonPropertyName("search")]
        public string search { get; set; }

        /// <summary>狀態</summary>
        [JsonPropertyName("status")]
        public string status { get; set; }

        /// <summary>BOM 類型</summary>
        [JsonPropertyName("bomType")]
        public string bomType { get; set; }

        /// <summary>版本號</summary>
        [JsonPropertyName("version")]
        public string version { get; set; }
    }
    /// <summary>
    /// 專案 BOM API 回應
    /// </summary>
    public class BomResponseClass
    {
        /// <summary>專案 BOM 清單</summary>
        [JsonPropertyName("Data")]
        public List<ProjectBomClass> Data { get; set; }

        /// <summary>狀態代碼</summary>
        [JsonPropertyName("Code")]
        public int Code { get; set; }

        /// <summary>結果訊息</summary>
        [JsonPropertyName("Result")]
        public string Result { get; set; }
    }

    /// <summary>
    /// BOM 項目 API 回應
    /// </summary>
    public class BomItemResponseClass
    {
        /// <summary>BOM 項目清單</summary>
        [JsonPropertyName("Data")]
        public List<BomItemClass> Data { get; set; }

        /// <summary>狀態代碼</summary>
        [JsonPropertyName("Code")]
        public int Code { get; set; }

        /// <summary>結果訊息</summary>
        [JsonPropertyName("Result")]
        public string Result { get; set; }
    }

}
