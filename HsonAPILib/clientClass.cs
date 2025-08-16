using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Basic;

namespace HsonAPILib
{
   

    /// <summary>
    /// 客戶欄位枚舉
    /// </summary>
    [EnumDescription("clients")]
    public enum enum_clients
    {
        /// <summary>唯一識別碼</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>客戶名稱</summary>
        [Description("名稱,VARCHAR,255,NONE")]
        名稱,

        /// <summary>地址</summary>
        [Description("地址,VARCHAR,500,NONE")]
        地址,

        /// <summary>客戶類型（政府機關、民間企業、學術機構、其他）</summary>
        [Description("類型,VARCHAR,50,NONE")]
        類型,

        /// <summary>啟用狀態（啟用、停用）</summary>
        [Description("啟用狀態,VARCHAR,10,NONE")]
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
    /// 客戶資料類別
    /// </summary>
    public class clientClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        [JsonPropertyName("name")]
        public string 名稱 { get; set; }

        [JsonPropertyName("contacts")]
        public List<client_contactClass> 聯絡人清單 { get; set; }

        [JsonPropertyName("address")]
        public string 地址 { get; set; }

        [JsonPropertyName("type")]
        public string 類型 { get; set; }

        [JsonPropertyName("status")]
        public string 啟用狀態 { get; set; }

        [JsonPropertyName("createdAt")]
        public string 建立時間 { get; set; }

        [JsonPropertyName("updatedAt")]
        public string 更新時間 { get; set; }

        [JsonPropertyName("notes")]
        public string 備註 { get; set; }
    }

    /// <summary>
    /// 客戶聯絡人欄位枚舉
    /// </summary>
    [EnumDescription("client_contacts")]
    public enum enum_client_contacts
    {
        /// <summary>唯一識別碼</summary>
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,

        /// <summary>所屬客戶 GUID（外鍵）</summary>
        [Description("ClientGUID,VARCHAR,50,INDEX")]
        ClientGUID,

        /// <summary>聯絡人姓名</summary>
        [Description("姓名,VARCHAR,100,NONE")]
        姓名,

        /// <summary>聯絡電話</summary>
        [Description("電話,VARCHAR,50,NONE")]
        電話,

        /// <summary>Email</summary>
        [Description("電子郵件,VARCHAR,255,NONE")]
        電子郵件,

        /// <summary>職稱</summary>
        [Description("職稱,VARCHAR,100,NONE")]
        職稱,

        /// <summary>是否為主要聯絡人（1=是,0=否）</summary>
        [Description("主要聯絡人,VARCHAR,1,NONE")]
        主要聯絡人,

        /// <summary>備註</summary>
        [Description("備註,VARCHAR,500,NONE")]
        備註
    }
    /// <summary>
    /// 客戶聯絡人資料類別
    /// </summary>
    public class client_contactClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        [JsonPropertyName("ClientGUID")]
        public string ClientGUID { get; set; } // 對應客戶 GUID（外鍵）

        [JsonPropertyName("name")]
        public string 姓名 { get; set; }

        [JsonPropertyName("phone")]
        public string 電話 { get; set; }

        [JsonPropertyName("email")]
        public string 電子郵件 { get; set; }

        [JsonPropertyName("title")]
        public string 職稱 { get; set; }

        [JsonPropertyName("isPrimary")]
        public string 主要聯絡人 { get; set; }

        [JsonPropertyName("notes")]
        public string 備註 { get; set; }
    }

    
  
}
