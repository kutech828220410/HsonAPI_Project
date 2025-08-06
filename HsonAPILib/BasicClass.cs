using Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace HsonAPILib
{
    public class returnData
    {
        private object _data = new object();
        private int _code = 0;
        private string _result = "";
        private string _value = "";
        private string _method = "";
        private string _timeTaken = "";
        private string _server = "";
        private uint _port = 0;
        private string _userName = "";
        private string _password = "";

        private string _dbName = "";
        private string _tableName = "";
        private string _serverName = "";
        private string _serverType = "";
        private string _serverContent = "";
        private string _RequestUrl = "";
        private string _token = "";
        private List<string> valueAry = new List<string>();

        /// <summary>
        /// 資料內容回傳或傳入,內容可為任意資料結構JSON格式,依據API自定義
        /// </summary>
        public object Data
        {
            get => _data;
            set
            {
                _data = value;
            }
        }
        /// <summary>
        /// 回傳結果碼
        /// </summary>
        public int Code { get => _code; set => _code = value; }
        /// <summary>
        /// 執行函式名稱
        /// </summary>
        public string Method { get => _method; set => _method = value; }
        /// <summary>
        /// 回傳結果說明
        /// </summary>
        public string Result
        {
            get => _result;
            set
            {
                _result = value;
            }
        }
        /// <summary>
        /// 傳入參數(string)由API自定義
        /// </summary>
        public string Value { get => _value; set => _value = value; }
        /// <summary>
        /// 傳入參數(stringAry)由API自定義
        /// </summary>
        public List<string> ValueAry
        {
            get
            {
                return this.valueAry;
            }
            set
            {
                this.valueAry = value;
            }
        }
        /// <summary>
        /// 執行花費時長
        /// </summary>
        public string TimeTaken { get => _timeTaken; set => _timeTaken = value; }
        /// <summary>
        /// 傳入API驗證token碼
        /// </summary>
        public string Token { get => _token; set => _token = value; }
        /// <summary>
        /// 傳入Database Server參數
        /// </summary>
        public string Server { get => _server; set => _server = value; }
        /// <summary>
        /// 傳入Database DbName參數
        /// </summary>
        public string DbName { get => _dbName; set => _dbName = value; }
        /// <summary>
        /// 傳入Database TableName參數
        /// </summary>
        public string TableName { get => _tableName; set => _tableName = value; }
        /// <summary>
        /// 傳入Database Port參數
        /// </summary>
        public uint Port { get => _port; set => _port = value; }
        /// <summary>
        /// 傳入Database UserName參數
        /// </summary>
        public string UserName { get => _userName; set => _userName = value; }
        /// <summary>
        /// 傳入Database Password參數
        /// </summary>
        public string Password { get => _password; set => _password = value; }
        /// <summary>
        /// 傳入伺服器 ServerType參數(網頁、調劑台、藥庫....)
        /// </summary>
        public string ServerType { get => _serverType; set => _serverType = value; }
        /// <summary>
        /// 傳入伺服器 ServerName參數(dps01、ds01...)
        /// </summary>
        public string ServerName { get => _serverName; set => _serverName = value; }
        /// <summary>
        /// 傳入伺服器 ServerContent參數(人員資料、藥品資料、一般資料...)
        /// </summary>
        public string ServerContent { get => _serverContent; set => _serverContent = value; }
        /// <summary>
        /// 回傳觸發請求網址
        /// </summary>
        public string RequestUrl { get => _RequestUrl; set => _RequestUrl = value; }




        [JsonIgnore]
        public string Url = "";
        [JsonIgnore]
        public string JsonInput
        {
            get
            {
                return InputData.JsonSerializationt();
            }
        }
        [JsonIgnore]
        public returnData InputData
        {
            get
            {
                return this;
            }
        }
        [JsonIgnore]
        public string JsonResult
        {
            get
            {
                return ResultData.JsonSerializationt();
            }
            set
            {
                this.resultData = value.JsonDeserializet<returnData>();
            }
        }
        private returnData resultData;
        [JsonIgnore]
        public returnData ResultData
        {
            get
            {
                return resultData;
            }
            set
            {
                resultData = value;
            }
        }



        public returnData()
        {
        }
        public returnData(string url)
        {
            this.Url = url;
        }


        public string ApiPostJson()
        {
            string json = Net.WEBApiPostJson(this.Url, this.JsonInput);
            JsonResult = json;
            return json;
        }
        public string ApiGet()
        {
            string json = Net.WEBApiGet(this.Url);
            JsonResult = json;
            return json;
        }

        public override string ToString()
        {
            return $"[{RequestUrl}] Result : {Result} ,{TimeTaken}";
        }
    }

    static public class BasicMethod
    {
       

    }

    public class ValidityClass
    {
        private List<validity> _validitys = new List<validity>();

        public List<validity> Validitys { get => _validitys; set => _validitys = value; }


        public void AddValidity(string validity_period, string lot)
        {
            validity _validity = new validity(validity_period, lot);
            if (_validity.Check()) _validitys.Add(_validity);
        }
        public string Value
        {
            set
            {
                _validitys.Clear();
                string[] text_ary = value.Split('\n');
                for (int i = 0; i < text_ary.Length; i++)
                {
                    validity _validity = new validity();
                    _validity.Value = text_ary[i];
                    if (_validity.Check()) _validitys.Add(_validity);
                }
            }
            get
            {
                return this.ToString();
            }
        }
        public override string ToString()
        {
            string text = "";
            for (int i = 0; i < _validitys.Count; i++)
            {
                text += _validitys[i].ToString();
                if (i != _validitys.Count) text += "\n";
            }
            return text;
        }

        public class validity
        {
            [JsonPropertyName("validity_period")]
            public string Validity_period { get; set; }
            [JsonPropertyName("lot_number")]
            public string Lot_number { get; set; }

            public validity(string validity_period, string lot)
            {
                if (validity_period.Check_Date_String())
                {
                    this.Validity_period = validity_period;
                    this.Lot_number = lot;
                }
            }
            public validity()
            {

            }

            public bool Check()
            {
                return Validity_period.Check_Date_String();
            }
            public string Value
            {
                set
                {
                    string 效期 = value.GetTextValue("效期");
                    string 批號 = value.GetTextValue("批號");
                    if (效期.Check_Date_String())
                    {
                        this.Validity_period = 效期;
                        this.Lot_number = 批號;
                    }
                }
                get
                {
                    return this.ToString();
                }
            }

            public override string ToString()
            {
                if (Lot_number.StringIsEmpty()) Lot_number = "無";
                return $"[效期]:{Validity_period},[批號]:{Lot_number}";
            }
        }
    }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonAliasAttribute : Attribute
    {
        public string[] Aliases { get; }

        public JsonAliasAttribute(params string[] aliases)
        {
            Aliases = aliases;
        }
    }
    public class GenericAliasJsonConverter<T> : JsonConverter<T> where T : new()
    {
        // 儲存每個屬性的主要名稱與別名對應（key 為小寫）
        private static readonly Dictionary<string, PropertyInfo> _propertyMap;

        // 儲存屬性對應的主要 JSON 名稱，供序列化時使用
        private static readonly Dictionary<PropertyInfo, string> _propertyPrimaryName;

        static GenericAliasJsonConverter()
        {
            _propertyMap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            _propertyPrimaryName = new Dictionary<PropertyInfo, string>();

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 忽略不可寫或標記 [JsonIgnore] 的屬性
                if (!prop.CanWrite || prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                // 取得主要 JSON 名稱，若有 JsonPropertyNameAttribute 則使用其設定
                string primaryName = prop.Name;
                var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (jsonPropAttr != null)
                    primaryName = jsonPropAttr.Name;

                // 儲存主要名稱
                _propertyPrimaryName[prop] = primaryName;

                // 加入主要名稱映射（轉小寫）
                _propertyMap[primaryName.ToLowerInvariant()] = prop;

                // 若有定義 JsonAliasAttribute，則也加入別名映射
                var aliasAttr = prop.GetCustomAttribute<JsonAliasAttribute>();
                if (aliasAttr != null)
                {
                    foreach (var alias in aliasAttr.Aliases)
                    {
                        _propertyMap[alias.ToLowerInvariant()] = prop;
                    }
                }
            }
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            T instance = new T();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return instance;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string jsonPropName = reader.GetString();
                    reader.Read(); // 移至屬性值

                    string lowerName = jsonPropName.ToLowerInvariant();
                    if (_propertyMap.TryGetValue(lowerName, out var propertyInfo))
                    {
                        // 利用 JsonSerializer.Deserialize 解析屬性值
                        object value = JsonSerializer.Deserialize(ref reader, propertyInfo.PropertyType, options);
                        propertyInfo.SetValue(instance, value);
                    }
                    else
                    {
                        // 當無對應屬性時，略過此值
                        reader.Skip();
                    }
                }
            }
            throw new JsonException("無法完成反序列化");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            // 利用反射取得所有公有實例屬性
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 略過不可讀或標記 [JsonIgnore] 的屬性
                if (!prop.CanRead || prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                // 取得主要名稱
                string jsonPropName = _propertyPrimaryName.TryGetValue(prop, out var primaryName)
                    ? primaryName
                    : prop.Name;

                writer.WritePropertyName(jsonPropName);
                var propValue = prop.GetValue(value);
                JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
            }
            writer.WriteEndObject();
        }
    }
}
