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
    /// 產品主檔欄位枚舉
    /// </summary>
    [EnumDescription("products")]
    public enum enum_products
    {
        /// <summary>唯一識別碼</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>產品代碼（公司內部唯一代碼）</summary>
        [Description("產品代碼,VARCHAR,50,INDEX")]
        產品代碼,

        /// <summary>產品名稱（對外名稱）</summary>
        [Description("產品名稱,VARCHAR,255,NONE")]
        產品名稱,

        /// <summary>產品類型（例如 single=單品, bundle=組合品）</summary>
        [Description("產品類型,VARCHAR,10,NONE")]
        產品類型,

        /// <summary>產品規格（如劑量、尺寸等）</summary>
        [Description("規格,VARCHAR,255,NONE")]
        規格,

        /// <summary>品牌名稱</summary>
        [Description("廠牌,VARCHAR,100,NONE")]
        廠牌,

        /// <summary>產品分類（如藥品、食品、器材等）</summary>
        [Description("類別,VARCHAR,100,NONE")]
        類別,

        /// <summary>單位（如盒、瓶、組等）</summary>
        [Description("單位,VARCHAR,50,NONE")]
        單位,

        /// <summary>售價（文字儲存，可含貨幣單位）</summary>
        [Description("售價,VARCHAR,50,NONE")]
        售價,

        /// <summary>產品狀態（啟用、停用、停售等）</summary>
        [Description("狀態,VARCHAR,50,NONE")]
        狀態,

        /// <summary>額外備註</summary>
        [Description("備註,VARCHAR,500,NONE")]
        備註,

        /// <summary>條碼清單（JSON 字串存多條碼）</summary>
        [Description("條碼清單,TEXT,300,NONE")]
        條碼清單,

        /// <summary>建立時間</summary>
        [Description("建立時間,DATETIME,20,INDEX")]
        建立時間,

        /// <summary>更新時間</summary>
        [Description("更新時間,DATETIME,20,INDEX")]
        更新時間,

        /// <summary>文件名稱</summary>
        [Description("文件名稱,VARCHAR,255,NONE")]
        文件名稱,

        /// <summary>文件連結</summary>
        [Description("文件連結,VARCHAR,500,NONE")]
        文件連結,

        /// <summary>文件版本</summary>
        [Description("文件版本,VARCHAR,50,NONE")]
        文件版本,

        /// <summary>圖片連結</summary>
        [Description("圖片連結,VARCHAR,500,NONE")]
        圖片連結
    }

    /// <summary>
    /// 產品資料類別（含子項與所屬組合品）
    /// </summary>
    public class productsClass
    {
        /// <summary>唯一識別碼</summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>產品代碼</summary>
        [JsonPropertyName("product_code")]
        public string 產品代碼 { get; set; }

        /// <summary>產品名稱</summary>
        [JsonPropertyName("product_name")]
        public string 產品名稱 { get; set; }

        /// <summary>產品類型</summary>
        [JsonPropertyName("product_type")]
        public string 產品類型 { get; set; }

        /// <summary>產品規格</summary>
        [JsonPropertyName("spec")]
        public string 規格 { get; set; }

        /// <summary>品牌名稱</summary>
        [JsonPropertyName("brand")]
        public string 廠牌 { get; set; }

        /// <summary>產品分類</summary>
        [JsonPropertyName("category")]
        public string 類別 { get; set; }

        /// <summary>單位</summary>
        [JsonPropertyName("unit")]
        public string 單位 { get; set; }

        /// <summary>售價</summary>
        [JsonPropertyName("price")]
        public string 售價 { get; set; }

        /// <summary>狀態</summary>
        [JsonPropertyName("status")]
        public string 狀態 { get; set; }

        /// <summary>備註</summary>
        [JsonPropertyName("note")]
        public string 備註 { get; set; }

        /// <summary>
        /// 條碼清單（DB 實際存 JSON 字串）
        /// </summary>
        [JsonIgnore]
        public string 條碼清單 { get; set; } = "[]";

        /// <summary>
        /// 條碼清單（API 使用，List 格式）
        /// </summary>
        [JsonPropertyName("BARCODE")]
        public List<string> Barcode
        {
            get => DeserializeBarcode(條碼清單);
            set => 條碼清單 = SerializeBarcode(value);
        }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        public string 建立時間 { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        public string 更新時間 { get; set; }

        /// <summary>文件名稱</summary>
        [JsonPropertyName("doc_name")]
        public string 文件名稱 { get; set; }

        /// <summary>文件連結</summary>
        [JsonPropertyName("doc_url")]
        public string 文件連結 { get; set; }

        /// <summary>文件版本</summary>
        [JsonPropertyName("doc_version")]
        public string 文件版本 { get; set; }

        /// <summary>圖片連結</summary>
        [JsonPropertyName("img_url")]
        public string 圖片連結 { get; set; }

        [JsonPropertyName("child_count")]
        public int child_count { get; set; }

        [JsonPropertyName("parent_count")]
        public int parent_count { get; set; }

        /// <summary>子項清單（此產品包含哪些子項）</summary>
        [JsonPropertyName("child_components")]
        public List<product_componentsClass> child_components { get; set; } = new List<product_componentsClass>();

        /// <summary>所屬組合品清單（此產品被哪些組合品使用）</summary>
        [JsonPropertyName("parent_products")]
        public List<product_componentsClass> parent_products { get; set; } = new List<product_componentsClass>();

        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private List<string> DeserializeBarcode(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json, jsonSerializerOptions) ?? new List<string>();
                return list.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct().ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private string SerializeBarcode(List<string> barcodes)
        {
            if (barcodes == null) return "[]";
            var filtered = barcodes.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct().ToList();
            return JsonSerializer.Serialize(filtered, jsonSerializerOptions);
        }
    }

    /// <summary>
    /// 產品組成關聯表欄位枚舉（使用產品代碼做關聯）
    /// </summary>
    [EnumDescription("product_components")]
    public enum enum_product_components
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,
        /// <summary>父產品代碼</summary>
        [Description("parent_code,VARCHAR,50,INDEX")]
        parent_code,
        /// <summary>子項代碼</summary>
        [Description("child_code,VARCHAR,50,INDEX")]
        child_code,
        [Description("數量,VARCHAR,50,NONE")]
        數量,
        [Description("備註,VARCHAR,500,NONE")]
        備註,
        [Description("建立時間,DATETIME,20,INDEX")]
        建立時間,
        [Description("更新時間,DATETIME,20,INDEX")]
        更新時間
    }
    /// <summary>
    /// 產品組成關聯資料類別（使用產品代碼做關聯）
    /// </summary>
    public class product_componentsClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>父產品代碼</summary>
        [JsonPropertyName("parent_code")]
        public string parent_code { get; set; }

        /// <summary>子項代碼</summary>
        [JsonPropertyName("child_code")]
        public string child_code { get; set; }

        [JsonPropertyName("qty")]
        public string 數量 { get; set; }

        [JsonPropertyName("note")]
        public string 備註 { get; set; }

        [JsonPropertyName("created_at")]
        public string 建立時間 { get; set; }

        [JsonPropertyName("updated_at")]
        public string 更新時間 { get; set; }

        // === JOIN 後的產品資訊 ===
        [JsonPropertyName("product_name")]
        public string 產品名稱 { get; set; }

        [JsonPropertyName("brand")]
        public string 廠牌 { get; set; }

        [JsonPropertyName("price")]
        public string 售價 { get; set; }

        [JsonPropertyName("status")]
        public string 狀態 { get; set; }

        [JsonPropertyName("child_components")]
        public List<product_componentsClass> child_components { get; set; } = new List<product_componentsClass>();
    }


}
