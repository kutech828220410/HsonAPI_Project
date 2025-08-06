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
    /// <summary>
    /// 醫院名稱枚舉
    /// </summary>
    public enum enum_hospital_nameClass
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,
        [Description("名稱,VARCHAR,50,NONE")]
        名稱,
        [Description("棟名,VARCHAR,50,NONE")]
        棟名,
        [Description("單位,VARCHAR,50,NONE")]
        單位,
    }
    /// <summary>
    /// 醫院名稱類別
    /// </summary>
    public class hospital_nameClass
    {
        /// <summary>
        /// 唯一KEY
        /// </summary>
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }
        /// <summary>
        /// 醫院名稱
        /// </summary>
        [JsonPropertyName("name")]
        public string 名稱 { get; set; }
        /// <summary>
        /// 分院及樓棟註記
        /// </summary>
        [JsonPropertyName("building_names")]
        public string 棟名 { get; set; }
        /// <summary>
        /// 住院、門診、急診註記
        /// </summary>
        [JsonPropertyName("hcasetyp")]
        public string 單位 { get; set; }
    }
}
