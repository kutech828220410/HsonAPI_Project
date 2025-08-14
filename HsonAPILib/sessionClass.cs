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
    [EnumDescription("login_session")]
    public enum enum_login_session
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,
        [Description("ID,VARCHAR,50,INDEX")]
        ID,
        [Description("Name,VARCHAR,50,NONE")]
        Name,
        [Description("Employer,VARCHAR,200,NONE")]
        Employer,
        [Description("loginTime,DATETIME,50,NONE")]
        loginTime,
        [Description("verifyTime,DATETIME,50,NONE")]
        verifyTime,
    }

    public class sessionClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }
        [JsonPropertyName("ID")]
        public string ID { get; set; }
        [JsonPropertyName("Password")]
        public string Password { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set; }
        [JsonPropertyName("Employer")]
        public string Employer { get; set; }
        [JsonPropertyName("loginTime")]
        public string loginTime { get; set; }
        [JsonPropertyName("verifyTime")]
        public string verifyTime { get; set; }
        [JsonPropertyName("check_sec")]
        public string check_sec { get; set; }
        [JsonPropertyName("color")]
        public string Color { get; set; }
        [JsonPropertyName("level")]
        public string level { get; set; }
        [JsonPropertyName("UID")]
        public string UID { get; set; }
        [JsonPropertyName("BARCODE")]
        public string BARCODE { get; set; }
        [JsonPropertyName("license")]
        public string license { get; set; }

        public List<PermissionsClass> Permissions { get => permissions; set => permissions = value; }
        private List<PermissionsClass> permissions = new List<PermissionsClass>();

        static public sessionClass LoginByUID(string API_Server, string UID)
        {
            List<sys_serverSettingClass> sys_serverSettingClasses = sys_serverSettingClassMethod.WebApiGet($"{API_Server}/api/serversetting");
            sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "API_Login");
            if (sys_serverSettingClasses.Count == 0)
            {
                Console.WriteLine($"{DateTime.Now.ToDateTimeString()} Login失敗,查無API網址,請檢查[API_Login]網址是否設定");
                return null;
            }
            string url = $"{sys_serverSettingClasses[0].Server}";

            returnData returnData = new returnData();
            sessionClass _sessionClass = new sessionClass();

            _sessionClass.UID = UID;
            returnData.Data = _sessionClass;

            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_result = json_out.JsonDeserializet<returnData>();
            if (returnData_result == null)
            {
                return null;
            }
            if (returnData_result.Data == null)
            {
                return null;
            }

            Console.WriteLine($"{returnData_result}");

            return returnData_result.Data.ObjToClass<sessionClass>();

        }
        static public sessionClass LoginByBarCode(string API_Server, string BARCODE)
        {
            List<sys_serverSettingClass> sys_serverSettingClasses = sys_serverSettingClassMethod.WebApiGet($"{API_Server}/api/serversetting");
            sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "API_Login");
            if (sys_serverSettingClasses.Count == 0)
            {
                Console.WriteLine($"{DateTime.Now.ToDateTimeString()} Login失敗,查無API網址,請檢查[API_Login]網址是否設定");
                return null;
            }
            string url = $"{sys_serverSettingClasses[0].Server}";

            returnData returnData = new returnData();
            sessionClass _sessionClass = new sessionClass();

            _sessionClass.BARCODE = BARCODE;
            returnData.Data = _sessionClass;

            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_result = json_out.JsonDeserializet<returnData>();
            if (returnData_result == null)
            {
                return null;
            }
            if (returnData_result.Data == null)
            {
                return null;
            }

            Console.WriteLine($"{returnData_result}");

            return returnData_result.Data.ObjToClass<sessionClass>();

        }
        static public sessionClass LoginByID(string API_Server, string userID, string password)
        {
            List<sys_serverSettingClass> sys_serverSettingClasses = sys_serverSettingClassMethod.WebApiGet($"{API_Server}/api/serversetting");
            sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "API_Login");
            if (sys_serverSettingClasses.Count == 0)
            {
                Console.WriteLine($"{DateTime.Now.ToDateTimeString()} Login失敗,查無API網址,請檢查[API_Login]網址是否設定");
                return null;
            }
            string url = $"{sys_serverSettingClasses[0].Server}";

            returnData returnData = new returnData();
            sessionClass _sessionClass = new sessionClass();

            _sessionClass.ID = userID;
            _sessionClass.Password = password;
            returnData.Data = _sessionClass;

            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData returnData_result = json_out.JsonDeserializet<returnData>();
            if (returnData_result == null)
            {
                return null;
            }
            if (returnData_result.Data == null)
            {
                return null;
            }

            Console.WriteLine($"{returnData_result}");

            return returnData_result.Data.ObjToClass<sessionClass>();

        }
    }
    static public class sessionClassMethod
    {
        static public PermissionsClass GetPermission(this sessionClass sessionclass, string type, string name)
        {
            List<PermissionsClass> permissionsClasses = (from temp in sessionclass.Permissions
                                                         where temp.類別 == type
                                                         where temp.名稱 == name
                                                         select temp).ToList();
            if (permissionsClasses.Count > 0) return permissionsClasses[0];
            return null;
        }
    }
    [EnumDescription("login_data")]
    public enum enum_login_data
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,
        [Description("權限等級,VARCHAR,50,NONE")]
        權限等級,
        [Description("Data01,VARCHAR,50,NONE")]
        Data01,
        [Description("Data02,VARCHAR,50,NONE")]
        Data02,
        [Description("Data03,VARCHAR,50,NONE")]
        Data03,
        [Description("Data04,VARCHAR,50,NONE")]
        Data04
    }
    [EnumDescription("login_data_index")]
    public enum enum_login_data_index
    {
        [Description("GUID,VARCHAR,50,PRIMARY")]
        GUID,
        [Description("索引,VARCHAR,50,NONE")]
        索引,
        [Description("Name,VARCHAR,50,NONE")]
        Name,
        [Description("Type,VARCHAR,50,NONE")]
        Type,
        [Description("群組,VARCHAR,50,NONE")]
        群組,
        [Description("描述,VARCHAR,200,NONE")]
        描述,
    }
    public class loginDataIndexClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }
        [JsonPropertyName("index")]
        public string 索引 { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("group")]
        public string 群組 { get; set; }
        [JsonPropertyName("description")]
        public string 描述 { get; set; }
        public class ICP_By_index : IComparer<loginDataIndexClass>
        {
            public int Compare(loginDataIndexClass x, loginDataIndexClass y)
            {
                int xIndex = x.索引.StringToInt32();
                int yIndex = y.索引.StringToInt32();

                return xIndex.CompareTo(yIndex);
            }
        }
        static public List<loginDataIndexClass> update_login_data_index(string API_Server, string data)
        {
            string url = $"{API_Server}/api/session/update_login_data_index";

            returnData returnData = new returnData();

            string json_in = data;
            string json_out = Net.WEBApiPostJson(url, json_in);
            returnData = json_out.JsonDeserializet<returnData>();
            //if (returnData == null) return null;
            //if (returnData.Code != 200) return null;
            List<loginDataIndexClass> loginDataIndexClasses = returnData.Data.ObjToClass<List<loginDataIndexClass>>();
            return loginDataIndexClasses;
        }
    }
    public class loginDataClass
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }
        [JsonPropertyName("level")]
        public string 權限等級 { get; set; }
        [JsonPropertyName("Data01")]
        public string Data01 { get; set; }
        [JsonPropertyName("Data02")]
        public string Data02 { get; set; }
        [JsonPropertyName("Data03")]
        public string Data03 { get; set; }
        [JsonPropertyName("Data04")]
        public string Data04 { get; set; }
        static public List<loginDataClass> get_permission_index(string API_Server)
        {
            string url = $"{API_Server}/api/session/get_permission_index";

            returnData returnData = new returnData();

            string json_in = returnData.JsonSerializationt();
            string json_out = Net.WEBApiPostJson(url, json_in);

            returnData = json_out.JsonDeserializet<returnData>();
            List<loginDataClass> loginDataClasses = returnData.Data.ObjToClass<List<loginDataClass>>();
            return loginDataClasses;
        }
    }
    public class PermissionsClass
    {
        [JsonPropertyName("name")]
        public string 名稱 { get; set; }
        [JsonPropertyName("index")]
        public int 索引 { get; set; }
        [JsonPropertyName("type")]
        public string 類別 { get; set; }
        [JsonPropertyName("state")]
        public bool 狀態 { get; set; }
        [JsonPropertyName("group")]
        public string 群組 { get; set; }
        [JsonPropertyName("description")]
        public string 描述 { get; set; }
    }
}
