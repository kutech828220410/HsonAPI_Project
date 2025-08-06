using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Basic;
using System.ComponentModel;

namespace HsonAPILib
{
    [EnumDescription("product_items")]
    public enum enum_product_items
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,
        [Description("product_guid,VARCHAR,50,INDEX")]
        product_guid,
        [Description("庫存數量,VARCHAR,50,NONE")]
        庫存數量,
        [Description("類別,VARCHAR,100,NONE")]
        類別,
        [Description("批號,VARCHAR,100,NONE")]
        批號,
        [Description("效期,DATETIME,20,NONE")]
        效期,
        [Description("建立時間,DATETIME,20,INDEX")]
        建立時間,
        [Description("更新時間,DATETIME,20,INDEX")]
        更新時間
    }
    public class product_itemsClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        [JsonPropertyName("product_guid")]
        public string product_guid { get; set; }

        [JsonPropertyName("sku")]
        public string SKU { get; set; }

        [JsonPropertyName("barcode")]
        public string 條碼 { get; set; }

        [JsonPropertyName("stock_qty")]
        public string 庫存數量 { get; set; }

        [JsonPropertyName("category")]
        public string 類別 { get; set; }

        [JsonPropertyName("batch_no")]
        public string 批號 { get; set; }

        [JsonPropertyName("expiry_date")]
        public string 效期 { get; set; }

        [JsonPropertyName("created_at")]
        public string 建立時間 { get; set; }

        [JsonPropertyName("updated_at")]
        public string 更新時間 { get; set; }
    }
}
