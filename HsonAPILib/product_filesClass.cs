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
    /// 產品檔案表欄位枚舉
    /// </summary>
    [EnumDescription("product_files")]
    public enum enum_product_files
    {
        /// <summary>唯一識別碼</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>產品代碼（對應 products.產品代碼）</summary>
        [Description("產品代碼,VARCHAR,50,INDEX")]
        產品代碼,

        /// <summary>檔案名稱（實際儲存檔案名）</summary>
        [Description("檔案名稱,VARCHAR,255,NONE")]
        檔案名稱,

        /// <summary>顯示名稱（給前端或使用者看的檔名）</summary>
        [Description("顯示名稱,VARCHAR,255,NONE")]
        顯示名稱,

        /// <summary>檔案類型（圖片、文件、製造方式等）</summary>
        [Description("檔案類型,VARCHAR,50,NONE")]
        檔案類型,

        /// <summary>內容類型（MIME Type，如 image/jpeg、application/pdf）</summary>
        [Description("內容類型,VARCHAR,100,NONE")]
        內容類型,

        /// <summary>檔案大小（文字儲存，含單位或純位元組數字）</summary>
        [Description("檔案大小,VARCHAR,50,NONE")]
        檔案大小,

        /// <summary>檔案雜湊值（例如 SHA256）</summary>
        [Description("檔案雜湊,VARCHAR,100,NONE")]
        檔案雜湊,

        /// <summary>儲存位置類型（local、s3、db 等）</summary>
        [Description("儲存方式,VARCHAR,50,NONE")]
        儲存方式,

        /// <summary>相對路徑（相對於檔案根目錄或 URL base）</summary>
        [Description("相對路徑,VARCHAR,500,NONE")]
        相對路徑,

        /// <summary>完整檔案網址</summary>
        [Description("檔案網址,VARCHAR,1000,NONE")]
        檔案網址,

        /// <summary>版本號（文字儲存，方便自訂版本格式）</summary>
        [Description("版本,VARCHAR,50,NONE")]
        版本,

        /// <summary>是否啟用（'1'=啟用，'0'=停用）</summary>
        [Description("啟用狀態,VARCHAR,1,NONE")]
        啟用狀態,

        /// <summary>備註</summary>
        [Description("備註,VARCHAR,500,NONE")]
        備註,

        /// <summary>建立時間</summary>
        [Description("建立時間,DATETIME,20,INDEX")]
        建立時間,

        /// <summary>更新時間</summary>
        [Description("更新時間,DATETIME,20,INDEX")]
        更新時間
    }

    /// <summary>
    /// 產品檔案資料類別
    /// </summary>
    public class product_filesClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>產品代碼</summary>
        [JsonPropertyName("product_code")]
        public string 產品代碼 { get; set; }

        /// <summary>檔案名稱（實際檔案名）</summary>
        [JsonPropertyName("file_name")]
        public string 檔案名稱 { get; set; }

        /// <summary>顯示名稱（給前端看的檔名）</summary>
        [JsonPropertyName("display_name")]
        public string 顯示名稱 { get; set; }

        /// <summary>檔案類型（圖片、文件、製造方式等）</summary>
        [JsonPropertyName("file_category")]
        public string 檔案類型 { get; set; }

        /// <summary>內容類型（MIME Type）</summary>
        [JsonPropertyName("content_type")]
        public string 內容類型 { get; set; }

        /// <summary>檔案大小（字串）</summary>
        [JsonPropertyName("size_text")]
        public string 檔案大小 { get; set; }

        /// <summary>檔案雜湊值</summary>
        [JsonPropertyName("hash")]
        public string 檔案雜湊 { get; set; }

        /// <summary>儲存方式（local、s3、db 等）</summary>
        [JsonPropertyName("storage_kind")]
        public string 儲存方式 { get; set; }

        /// <summary>相對路徑</summary>
        [JsonPropertyName("relative_path")]
        public string 相對路徑 { get; set; }

        /// <summary>完整檔案網址</summary>
        [JsonPropertyName("file_url")]
        public string 檔案網址 { get; set; }

        /// <summary>版本號</summary>
        [JsonPropertyName("version")]
        public string 版本 { get; set; }

        /// <summary>是否啟用</summary>
        [JsonPropertyName("is_active")]
        public string 啟用狀態 { get; set; }

        /// <summary>備註</summary>
        [JsonPropertyName("note")]
        public string 備註 { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        public string 建立時間 { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        public string 更新時間 { get; set; }
    }
}
