using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Basic;
namespace HsonAPILib
{
    /// <summary>
    /// 醫院名稱枚舉
    /// </summary>
    public enum enum_hospital_nameClass
    {
        GUID,
        名稱,
        棟名,
        診別,
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
        public string 診別 { get; set; }
    }
}
