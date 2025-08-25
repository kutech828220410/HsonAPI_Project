using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Basic;
using System.ComponentModel;
using System.Text.Json;

namespace HsonAPILib
{
    public enum CountryRegionType
    {
        台灣,
        中國,
        美國,
        日本,
        韓國,
        其他
    }
    public enum SupplierType
    {
        實體公司,
        平台,
        通路
    }
    public enum PlatformType
    {
        淘寶,
        蝦皮,
        露天,
        阿里巴巴,
        PCHome,
        Momo,
        其他
    }
    /// <summary>
    /// 供應商欄位枚舉
    /// </summary>
    [EnumDescription("suppliers")]
    public enum enum_suppliers
    {
        /// <summary>供應商唯一識別碼 (GUID)</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>供應商名稱</summary>
        [Description("suppliername,VARCHAR,200,NONE")]
        suppliername,

        /// <summary>供應商類型 (實體公司/平台/通路)</summary>
        [Description("suppliertype,VARCHAR,50,NONE")]
        suppliertype,

        /// <summary>國家或地區</summary>
        [Description("countryregion,VARCHAR,50,NONE")]
        countryregion,

        /// <summary>城市名稱</summary>
        [Description("city,VARCHAR,100,NONE")]
        city,

        /// <summary>所屬平台 (蝦皮/淘寶/露天)</summary>
        [Description("platform,VARCHAR,50,NONE")]
        platform,

        /// <summary>平台店舖代號/帳號</summary>
        [Description("platformshopid,VARCHAR,100,NONE")]
        platformshopid,

        /// <summary>平台店舖網址</summary>
        [Description("platformurl,VARCHAR,500,NONE")]
        platformurl,

        /// <summary>統一編號/營業執照號</summary>
        [Description("companyregistrationid,VARCHAR,50,NONE")]
        companyregistrationid,

        /// <summary>聯絡人姓名</summary>
        [Description("contactperson,VARCHAR,100,NONE")]
        contactperson,

        /// <summary>聯絡電話</summary>
        [Description("phone,VARCHAR,50,NONE")]
        phone,

        /// <summary>聯絡信箱</summary>
        [Description("email,VARCHAR,100,NONE")]
        email,

        /// <summary>公司地址</summary>
        [Description("address,TEXT,20,NONE")]
        address,

        /// <summary>付款銀行帳號資訊</summary>
        [Description("bankaccountinfo,VARCHAR,200,NONE")]
        bankaccountinfo,

        /// <summary>公司官網網址</summary>
        [Description("companywebsite,VARCHAR,500,NONE")]
        companywebsite,

        /// <summary>供應範圍 (藥品/耗材/設備等)</summary>
        [Description("categoryscope,VARCHAR,200,NONE")]
        categoryscope,

        /// <summary>配送方式</summary>
        [Description("deliverymethod,VARCHAR,100,NONE")]
        deliverymethod,

        /// <summary>付款條件</summary>
        [Description("paymentterm,VARCHAR,100,NONE")]
        paymentterm,

        /// <summary>內部信用評分</summary>
        [Description("rating,VARCHAR,10,NONE")]
        rating,

        /// <summary>合約狀態</summary>
        [Description("contractstatus,VARCHAR,50,NONE")]
        contractstatus,

        /// <summary>建立時間</summary>
        [Description("createddate,DATETIME,20,NONE")]
        createddate,

        /// <summary>最後更新時間</summary>
        [Description("updateddate,DATETIME,20,NONE")]
        updateddate
    }
    /// <summary>
    /// 供應商資料類別
    /// </summary>
    public class supplierClass
    {
        /// <summary>供應商唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>供應商名稱</summary>
        [JsonPropertyName("suppliername")]
        public string suppliername { get; set; }

        /// <summary>供應商類型 (實體公司/平台/通路)</summary>
        [JsonPropertyName("suppliertype")]
        public string suppliertype { get; set; }

        /// <summary>國家或地區</summary>
        [JsonPropertyName("countryregion")]
        public string countryregion { get; set; }

        /// <summary>城市名稱</summary>
        [JsonPropertyName("city")]
        public string city { get; set; }

        /// <summary>所屬平台 (蝦皮/淘寶/露天)</summary>
        [JsonPropertyName("platform")]
        public string platform { get; set; }

        /// <summary>平台店舖代號/帳號</summary>
        [JsonPropertyName("platformshopid")]
        public string platformshopid { get; set; }

        /// <summary>平台店舖網址</summary>
        [JsonPropertyName("platformurl")]
        public string platformurl { get; set; }

        /// <summary>統一編號/營業執照號</summary>
        [JsonPropertyName("companyregistrationid")]
        public string companyregistrationid { get; set; }

        /// <summary>聯絡人姓名</summary>
        [JsonPropertyName("contactperson")]
        public string contactperson { get; set; }

        /// <summary>聯絡電話</summary>
        [JsonPropertyName("phone")]
        public string phone { get; set; }

        /// <summary>聯絡信箱</summary>
        [JsonPropertyName("email")]
        public string email { get; set; }

        /// <summary>公司地址</summary>
        [JsonPropertyName("address")]
        public string address { get; set; }

        /// <summary>付款銀行帳號資訊</summary>
        [JsonPropertyName("bankaccountinfo")]
        public string bankaccountinfo { get; set; }

        /// <summary>公司官網網址</summary>
        [JsonPropertyName("companywebsite")]
        public string companywebsite { get; set; }

        /// <summary>供應範圍 (藥品/耗材/設備等)</summary>
        [JsonPropertyName("categoryscope")]
        public string categoryscope { get; set; }

        /// <summary>配送方式</summary>
        [JsonPropertyName("deliverymethod")]
        public string deliverymethod { get; set; }

        /// <summary>付款條件</summary>
        [JsonPropertyName("paymentterm")]
        public string paymentterm { get; set; }

        /// <summary>內部信用評分</summary>
        [JsonPropertyName("rating")]
        public string rating { get; set; }

        /// <summary>合約狀態</summary>
        [JsonPropertyName("contractstatus")]
        public string contractstatus { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("createddate")]
        public string createddate { get; set; }

        /// <summary>最後更新時間</summary>
        [JsonPropertyName("updateddate")]
        public string updateddate { get; set; }
    }


    /// <summary>
    /// 供應商與產品關聯表欄位枚舉
    /// </summary>
    [EnumDescription("supplier_products")]
    public enum enum_supplierproducts
    {
        /// <summary>供應商商品關聯唯一識別碼 (GUID)</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>對應供應商 GUID</summary>
        [Description("supplier_guid,VARCHAR,50,INDEX")]
        supplier_guid,

        /// <summary>對應產品 GUID</summary>
        [Description("product_guid,VARCHAR,50,INDEX")]
        product_guid,

        /// <summary>報價 (文字格式，若需要換算可轉數字)</summary>
        [Description("price,VARCHAR,50,NONE")]
        price,

        /// <summary>交貨時間 (如: 7天內)</summary>
        [Description("leadtime,VARCHAR,100,NONE")]
        leadtime,

        /// <summary>最小訂購量 (Minimum Order Quantity)</summary>
        [Description("moq,VARCHAR,50,NONE")]
        moq,

        /// <summary>是否為首選供應商 (0/1 或 Yes/No)</summary>
        [Description("ispreferred,VARCHAR,10,NONE")]
        ispreferred,

        /// <summary>建立時間</summary>
        [Description("createddate,DATETIME,20,NONE")]
        createddate,

        /// <summary>最後更新時間</summary>
        [Description("updateddate,DATETIME,20,NONE")]
        updateddate
    }
    /// <summary>
    /// 供應商與產品關聯資料類別
    /// </summary>
    public class supplierProductClass
    {
        /// <summary>供應商商品關聯唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>對應供應商 GUID</summary>
        [JsonPropertyName("supplier_guid")]
        public string supplier_guid { get; set; }

        /// <summary>對應產品 GUID</summary>
        [JsonPropertyName("product_guid")]
        public string product_guid { get; set; }

        /// <summary>報價 (文字格式，若需要換算可轉數字)</summary>
        [JsonPropertyName("price")]
        public string price { get; set; }

        /// <summary>交貨時間 (如: 7天內)</summary>
        [JsonPropertyName("leadtime")]
        public string leadtime { get; set; }

        /// <summary>最小訂購量 (Minimum Order Quantity)</summary>
        [JsonPropertyName("moq")]
        public string moq { get; set; }

        /// <summary>是否為首選供應商 (0/1 或 Yes/No)</summary>
        [JsonPropertyName("ispreferred")]
        public string ispreferred { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("createddate")]
        public string createddate { get; set; }

        /// <summary>最後更新時間</summary>
        [JsonPropertyName("updateddate")]
        public string updateddate { get; set; }
    }


    /// <summary>
    /// 供應商商品歷史報價表欄位枚舉
    /// </summary>
    [EnumDescription("supplier_productpricehistory")]
    public enum enum_supplierproductpricehistory
    {
        /// <summary>歷史報價唯一識別碼 (GUID)</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>對應供應商商品關聯 GUID</summary>
        [Description("supplierproduct_guid,VARCHAR,50,INDEX")]
        supplierproduct_guid,

        /// <summary>歷史報價金額</summary>
        [Description("price,VARCHAR,50,NONE")]
        price,

        /// <summary>幣別 (TWD/CNY/USD...)</summary>
        [Description("currency,VARCHAR,10,NONE")]
        currency,

        /// <summary>價格生效日期</summary>
        [Description("validfrom,DATETIME,20,NONE")]
        validfrom,

        /// <summary>價格失效日期 (NULL 表示目前仍有效)</summary>
        [Description("validto,DATETIME,20,NONE")]
        validto,

        /// <summary>備註 (如促銷/議價原因等)</summary>
        [Description("remark,TEXT,20,NONE")]
        remark,

        /// <summary>建立時間</summary>
        [Description("createddate,DATETIME,20,NONE")]
        createddate,

        /// <summary>最後更新時間</summary>
        [Description("updateddate,DATETIME,20,NONE")]
        updateddate
    }
    /// <summary>
    /// 供應商商品歷史報價資料類別
    /// </summary>
    public class supplierProductPriceHistoryClass
    {
        /// <summary>歷史報價唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        /// <summary>對應供應商商品關聯 GUID</summary>
        [JsonPropertyName("supplierproduct_guid")]
        public string supplierproduct_guid { get; set; }

        /// <summary>歷史報價金額</summary>
        [JsonPropertyName("price")]
        public string price { get; set; }

        /// <summary>幣別 (TWD/CNY/USD...)</summary>
        [JsonPropertyName("currency")]
        public string currency { get; set; }

        /// <summary>價格生效日期</summary>
        [JsonPropertyName("validfrom")]
        public string validfrom { get; set; }

        /// <summary>價格失效日期 (NULL 表示目前仍有效)</summary>
        [JsonPropertyName("validto")]
        public string validto { get; set; }

        /// <summary>備註 (如促銷/議價原因等)</summary>
        [JsonPropertyName("remark")]
        public string remark { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("createddate")]
        public string createddate { get; set; }

        /// <summary>最後更新時間</summary>
        [JsonPropertyName("updateddate")]
        public string updateddate { get; set; }
    }

}
