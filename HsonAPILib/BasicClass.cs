using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Basic;
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
        public string Result
        {
            get => _result;
            set
            {
                _result = value;
            }
        }
        public string Value { get => _value; set => _value = value; }
        public string TimeTaken { get => _timeTaken; set => _timeTaken = value; }
        public string Method { get => _method; set => _method = value; }
        public string Server { get => _server; set => _server = value; }
        public string DbName { get => _dbName; set => _dbName = value; }
        public string TableName { get => _tableName; set => _tableName = value; }
        public string ServerType { get => _serverType; set => _serverType = value; }
        public string ServerName { get => _serverName; set => _serverName = value; }
        public string ServerContent { get => _serverContent; set => _serverContent = value; }
        public uint Port { get => _port; set => _port = value; }
        public string UserName { get => _userName; set => _userName = value; }
        public string Password { get => _password; set => _password = value; }
        public List<string> ValueAry { get; set; }

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
    }

    public static class BasicClassMethod
    {
        static public object[] ClassToSQL<T, E>(this T _class) where E : Enum
        {
            object[] value = new object[Enum.GetNames(typeof(E)).Length];
            Type enumType = typeof(E);
            E _enum = Activator.CreateInstance<E>();
            foreach (var field in enumType.GetFields())
            {
                if (field.FieldType.IsEnum)
                {
                    string enumName = field.Name;
                    int enumIndex = (int)field.GetValue(_enum);

                    // 使用 enumIndex 來填入對應的屬性值
                    value[enumIndex] = typeof(T).GetProperty(enumName)?.GetValue(_class);
                }
            }

            return value;
        }
        static public T SQLToClass<T, E>(this object[] values)
        {
            T obj = Activator.CreateInstance<T>();
            E _enum = Activator.CreateInstance<E>();
            Type enumType = typeof(E);

            foreach (var field in enumType.GetFields())
            {
                string enumName = field.Name;
                if (field.FieldType.IsEnum)
                {

                    int enumIndex = (int)field.GetValue(_enum);

                    // 使用 enumIndex 來取得對應的屬性值
                    object value = values[enumIndex];

                    // 使用 enumName 來取得對應的屬性
                    var property = typeof(T).GetProperty(enumName);
                    if (value is DateTime)
                    {
                        property?.SetValue(obj, value.ToDateTimeString());
                    }
                    else
                    {
                        // 將值填入對應的屬性
                        property?.SetValue(obj, value.ObjectToString());
                    }

                }
            }

            return obj;
        }

        static public List<object[]> ClassToSQL<T, E>(this List<T> _classes) where E : Enum, new()
        {
            List<object[]> list_value = new List<object[]>();
            E _enum = Activator.CreateInstance<E>();
            for (int i = 0; i < _classes.Count; i++)
            {
                object[] value = _classes[i].ClassToSQL<T, E>();
                list_value.Add(value);
            }
            return list_value;
        }
        static public List<T> SQLToClass<T, E>(this List<object[]> values) where E : Enum, new()
        {
            List<T> list_value = new List<T>();
            E _enum = Activator.CreateInstance<E>();

            for (int i = 0; i < values.Count; i++)
            {
                object[] value = values[i];
                T _class = values[i].SQLToClass<T, E>();
                list_value.Add(_class);
            }
            return list_value;
        }

        static public T ObjToClass<T>(this object data)
        {
            string jsondata = data.JsonSerializationt();
            return jsondata.JsonDeserializet<T>();
        }
        static public List<T> ObjToListClass<T>(this object data)
        {
            string jsondata = data.JsonSerializationt();
            return jsondata.JsonDeserializet<List<T>>();
        }

        static public List<object[]> ObjToListSQL<T, E>(this object data) where E : Enum, new()
        {
            List<T> list_T = data.ObjToListClass<T>();

            return list_T.ClassToSQL<T, E>();
        }
    }
}
