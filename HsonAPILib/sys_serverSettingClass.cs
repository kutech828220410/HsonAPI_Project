using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Basic;
using System.ComponentModel;
using System.Reflection;

namespace HsonAPILib
{
    [EnumDescription("serversetting")]
    public enum enum_sys_serverSetting
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,
        [Description("單位,VARCHAR,200,None")]
        單位,
        [Description("設備名稱,VARCHAR,200,None")]
        設備名稱,
        [Description("類別,VARCHAR,200,None")]
        類別,
        [Description("程式類別,VARCHAR,200,INDEX")]
        程式類別,
        [Description("內容,VARCHAR,200,INDEX")]
        內容,
        [Description("Server,VARCHAR,200,None")]
        Server,
        [Description("Port,VARCHAR,200,None")]
        Port,
        [Description("DBName,VARCHAR,200,None")]
        DBName,
        [Description("TableName,VARCHAR,200,None")]
        TableName,
        [Description("User,VARCHAR,200,None")]
        User,
        [Description("Password,VARCHAR,200,None")]
        Password,
        [Description("Value,TEXT,65535,None")]
        Value,
    }
    public static class sys_serverSettingClassMethod
    {
        public static sys_serverSettingClass myFind(this List<sys_serverSettingClass> sys_serverSettingClasses, string Name, string Type, string Content)
        {
            if (sys_serverSettingClasses == null) return null;
            List<sys_serverSettingClass> sys_serverSettingClasses_buf = (from value in sys_serverSettingClasses
                                                                         where value.類別 == Type
                                                                         where value.設備名稱.ToUpper() == Name.ToUpper()
                                                                         where value.內容 == Content
                                                                         select value).ToList();
            if (sys_serverSettingClasses_buf.Count == 0) return null;
            return sys_serverSettingClasses_buf[0];
        }
        public static sys_serverSettingClass myFind(this List<sys_serverSettingClass> sys_serverSettingClasses, string GUID)
        {
            if (sys_serverSettingClasses == null) return null;
            List<sys_serverSettingClass> sys_serverSettingClasses_buf = (from value in sys_serverSettingClasses
                                                                         where value.GUID == GUID
                                                                         select value).ToList();
            if (sys_serverSettingClasses_buf.Count == 0) return null;
            return sys_serverSettingClasses_buf[0];
        }

        public static string GetUrl(this List<sys_serverSettingClass> sys_serverSettingClasses, string Name, string Type, string Content)
        {
            if (sys_serverSettingClasses == null) return null;
            List<sys_serverSettingClass> sys_serverSettingClasses_buf = (from value in sys_serverSettingClasses
                                                                         where value.類別 == Type
                                                                         where value.設備名稱.ToUpper() == Name.ToUpper()
                                                                         where value.內容 == Content
                                                                         select value).ToList();
            if (sys_serverSettingClasses_buf.Count == 0) return null;
            return sys_serverSettingClasses_buf[0].Server;
        }


        public static List<sys_serverSettingClass> MyFind(this List<sys_serverSettingClass> sys_serverSettingClasses, string Name, string Type, string Content)
        {
            if (sys_serverSettingClasses == null) return null;
            List<sys_serverSettingClass> sys_serverSettingClasses_buf = (from value in sys_serverSettingClasses
                                                                         where value.類別 == Type
                                                                         where value.設備名稱.ToUpper() == Name.ToUpper()
                                                                         where value.內容 == Content
                                                                         select value).ToList();
            return sys_serverSettingClasses_buf;
        }
        public static List<sys_serverSettingClass> MyFind(this List<sys_serverSettingClass> sys_serverSettingClasses, string Name, string Type)
        {
            if (sys_serverSettingClasses == null) return null;
            List<sys_serverSettingClass> sys_serverSettingClasses_buf = (from value in sys_serverSettingClasses
                                                                         where value.類別 == Type
                                                                         where value.設備名稱.ToUpper() == Name.ToUpper()
                                                                         select value).ToList();
            return sys_serverSettingClasses_buf;
        }





  
        public static List<sys_serverSettingClass> SetValue(this List<sys_serverSettingClass> sys_serverSettingClasses, sys_serverSettingClass sys_serverSettingClass)
        {
            for (int i = 0; i < sys_serverSettingClasses.Count; i++)
            {
                if (sys_serverSettingClasses[i].類別 != sys_serverSettingClass.類別) continue;
                if (sys_serverSettingClasses[i].設備名稱 != sys_serverSettingClass.設備名稱) continue;
                if (sys_serverSettingClasses[i].內容 != sys_serverSettingClass.內容) continue;
                sys_serverSettingClasses[i] = sys_serverSettingClass;
            }
            return sys_serverSettingClasses;
        }
        public static List<sys_serverSettingClass> WebApiGet(string url)
        {
            string jsonstring = Basic.Net.WEBApiGet(url);
            if (jsonstring.StringIsEmpty()) return null;
            //Console.WriteLine(jsonstring);
            returnData returnData = jsonstring.JsonDeserializet<returnData>();

            List<sys_serverSettingClass> sys_serverSettingClasses = returnData.Data.ObjToListClass<sys_serverSettingClass>();
            return sys_serverSettingClasses;
        }

        public static void Set_department_type(this List<sys_serverSettingClass> sys_serverSettingClasses, string department_type)
        {
            for (int i = 0; i < sys_serverSettingClasses.Count; i++)
            {
                sys_serverSettingClasses[i].單位 = department_type;
            }
        }
        public static List<string> Get_department_types(this List<sys_serverSettingClass> sys_serverSettingClasses)
        {
            List<string> department_types = (from temp in sys_serverSettingClasses
                                             select temp.單位).Distinct().ToList();
            department_types.Remove(null);
            department_types.Remove("");

            return department_types;
        }


    }
    public class sys_serverSettingClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }
        [JsonPropertyName("department_type ")]
        public string 單位 { get; set; }
        [JsonPropertyName("name")]
        public string 設備名稱 { get; set; }
        [JsonPropertyName("type")]
        public string 類別 { get; set; }
        [JsonPropertyName("porgram_type")]
        public string 程式類別 { get; set; }
        [JsonPropertyName("content")]
        public string 內容 { get; set; }
        [JsonPropertyName("server")]
        public string Server { get; set; }
        [JsonPropertyName("Port")]
        public string Port { get; set; }
        [JsonPropertyName("DBName")]
        public string DBName { get; set; }
        [JsonPropertyName("tableName")]
        public string TableName { get; set; }
        [JsonPropertyName("user")]
        public string User { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }

        public sys_serverSettingClass()
        {

        }
      

        static public List<string> get_department_type(string API_Server)
        {
            return get_department_type(API_Server, "調劑台");
        }
        static public List<string> get_department_type(string API_Server, string serverType)
        {
            string url = $"{API_Server}/api/ServerSetting/get_department_type";

            returnData returnData = new returnData();
            returnData.ValueAry.Add(serverType);
            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_out = json_out.JsonDeserializet<returnData>();
            if (returnData_out == null)
            {
                return null;
            }
            if (returnData_out.Data == null)
            {
                return null;
            }
            Console.WriteLine($"[{returnData_out.Method}]:{returnData_out.Result}");
            List<string> strs = returnData_out.Data.ObjToClass<List<string>>();
            return strs;
        }
        static public sys_serverSettingClass get_VM_Server(string API_Server)
        {
            string url = $"{API_Server}/api/ServerSetting/get_VM_Server";

            returnData returnData = new returnData();

            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_out = json_out.JsonDeserializet<returnData>();
            if (returnData_out == null)
            {
                return null;
            }
            if (returnData_out.Data == null)
            {
                return null;
            }
            Console.WriteLine($"[{returnData_out.Method}]:{returnData_out.Result}");
            sys_serverSettingClass sys_serverSettingClass = returnData_out.Data.ObjToClass<sys_serverSettingClass>();
            return sys_serverSettingClass;
        }
        static public List<string> get_name_by_department_type(string API_Server, string 單位)
        {
            List<sys_serverSettingClass> sys_serverSettingClasses = get_serversetting_by_department_type(API_Server, 單位);

            List<string> list_str = (from temp in sys_serverSettingClasses
                                     select temp.設備名稱).ToList();
            return list_str;
        }
        static public List<sys_serverSettingClass> get_serversetting_by_department_type(string API_Server, string 單位)
        {
            string url = $"{API_Server}/api/ServerSetting/get_serversetting_by_department_type";

            returnData returnData = new returnData();
            returnData.ValueAry.Add(單位);
            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_out = json_out.JsonDeserializet<returnData>();
            if (returnData_out == null)
            {
                return null;
            }
            if (returnData_out.Data == null)
            {
                return null;
            }
            Console.WriteLine($"[{returnData_out.Method}]:{returnData_out.Result}");
            List<sys_serverSettingClass> value = returnData_out.Data.ObjToClass<List<sys_serverSettingClass>>();
            return value;
        }
        static public List<sys_serverSettingClass> get_serversetting_by_type(string API_Server, string 類別)
        {
            string url = $"{API_Server}/api/ServerSetting/get_serversetting_by_type";

            returnData returnData = new returnData();
            returnData.ValueAry.Add(類別);
            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_out = json_out.JsonDeserializet<returnData>();
            if (returnData_out == null)
            {
                return null;
            }
            if (returnData_out.Data == null)
            {
                return null;
            }
            Console.WriteLine($"[{returnData_out.Method}]:{returnData_out.Result}");
            List<sys_serverSettingClass> value = returnData_out.Data.ObjToClass<List<sys_serverSettingClass>>();
            return value;
        }
        static public sys_serverSettingClass get_server(string API_Server, string 名稱, string 類別, string 內容)
        {
            string url = $"{API_Server}/api/ServerSetting/get_server";

            returnData returnData = new returnData();
            returnData.ValueAry.Add(名稱);
            returnData.ValueAry.Add(類別);
            returnData.ValueAry.Add(內容);
            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_out = json_out.JsonDeserializet<returnData>();
            if (returnData_out == null)
            {
                return null;
            }
            if (returnData_out.Data == null)
            {
                return null;
            }
            Console.WriteLine($"[{returnData_out.Method}]:{returnData_out.Result}");
            sys_serverSettingClass value = returnData_out.Data.ObjToClass<sys_serverSettingClass>();
            return value;
        }
        static public string get_api_url(string API_Server, string 名稱, string 類別, string 內容)
        {
            string url = $"{API_Server}/api/ServerSetting/get_server";

            returnData returnData = new returnData();
            returnData.ValueAry.Add(名稱);
            returnData.ValueAry.Add(類別);
            returnData.ValueAry.Add(內容);
            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_out = json_out.JsonDeserializet<returnData>();
            if (returnData_out == null)
            {
                return null;
            }
            if (returnData_out.Data == null)
            {
                return null;
            }
            Console.WriteLine($"[{returnData_out.Method}]:{returnData_out.Result}");
            sys_serverSettingClass value = returnData_out.Data.ObjToClass<sys_serverSettingClass>();
            if (value == null) return null;
            return value.Server;
        }
        static public DateTime GetServerTime(string API_Server)
        {
            string date_str = Basic.Net.WEBApiGet($"{API_Server}/api/time");
            if (date_str.Check_Date_String())
            {
                return date_str.StringToDateTime();
            }
            return DateTime.MinValue;
        }

        static public SQLUI.Table Init(string API_Server)
        {
            string url = $"{API_Server}/api/ServerSetting/init";

            returnData returnData = new returnData();

            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            SQLUI.Table table = json_out.JsonDeserializet<SQLUI.Table>();
            return table;
        }


        public class ICP_By_dps_name : IComparer<sys_serverSettingClass>
        {
            public int Compare(sys_serverSettingClass x, sys_serverSettingClass y)
            {
                return (x.設備名稱).CompareTo(y.設備名稱);
            }
        }

    }
}
