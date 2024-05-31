using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Basic;
namespace HsonAPILib
{
    public enum enum_hostpital_report
    {
        GUID,
        hospital_name_guid,
        標題,
        內容,
        回報時間,
        發生時間,
        完成時間,
        審核時間,
        是否完成,
        是否審核,
        回報人員,
        完成人員,
    }
    /// <summary>
    /// 醫院問題回報
    /// </summary>
    public class hostpital_reportClass
    {

        /// <summary>
        /// 唯一KEY
        /// </summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }
        /// <summary>
        /// 醫院名稱GUID索引鍵
        /// </summary>
        [JsonPropertyName("hospital_name_guid")]
        public string hospital_name_guid
        {
            get
            {
                if (hospital_NameClass == null) return "";
                return hospital_NameClass.GUID;
            }
            set
            {
                if (hospital_NameClass == null) hospital_NameClass = new hospital_nameClass();
                hospital_NameClass.GUID = value;
            }
        }
        /// <summary>
        /// 標題
        /// </summary>
        [JsonPropertyName("title")]
        public string 標題 { get; set; }
        /// <summary>
        /// 內容
        /// </summary>
        [JsonPropertyName("content")]
        public string 內容 { get; set; }
        /// <summary>
        /// 回報人員
        /// </summary>
        [JsonPropertyName("report_personnel")]
        public string 回報人員 { get; set; }
        /// <summary>
        /// 回報時間
        /// </summary>
        [JsonPropertyName("report_time")]
        public string 回報時間 { get; set; }
        /// <summary>
        /// 發生時間
        /// </summary>
        [JsonPropertyName("occurrence_time")]
        public string 發生時間 { get; set; }
        /// <summary>
        /// 完成人員
        /// </summary>
        [JsonPropertyName("finished_personnel")]
        public string 完成人員 { get; set; }
        /// <summary>
        /// 完成時間
        /// </summary>
        [JsonPropertyName("finished_time")]
        public string 完成時間 { get; set; }
        /// <summary>
        /// 審核時間
        /// </summary>
        [JsonPropertyName("review_time")]
        public string 審核時間 { get; set; }
        /// <summary>
        /// 是否完成
        /// </summary>
        [JsonPropertyName("is_finished")]
        public string 是否完成 { get; set; }
        /// <summary>
        /// 是否審核
        /// </summary>
        [JsonPropertyName("is_reviewed")]
        public string 是否審核 { get; set; }
        /// <summary>
        /// 醫院資訊
        /// </summary>
        [JsonPropertyName("hospital_information")]
        public hospital_nameClass hospital_NameClass { get; set; }
        /// <summary>
        /// 圖片Ary
        /// </summary>
        [JsonPropertyName("pictures")]
        public List<hostpital_report_picture_Class> pictures { get; set; }

   
    }
    public enum enum_hostpital_report_picture
    {
        GUID,
        hostpital_report_guid,
        圖片,
        加入時間,
    }
    public class hostpital_report_picture_Class
    {
        /// <summary>
        /// 唯一KEY
        /// </summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }
        /// <summary>
        /// 問題回報資料GUID索引鍵
        /// </summary>
        [JsonPropertyName("hostpital_report_guid")]
        public string hostpital_report_guid { get; set; }
        /// <summary>
        /// 圖片
        /// </summary>
        [JsonPropertyName("picture")]
        public string 圖片 { get; set; }
        /// <summary>
        /// 加入時間
        /// </summary>
        [JsonPropertyName("add_time")]
        public string 加入時間 { get; set; }
    }
}
