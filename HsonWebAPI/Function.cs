using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HsonAPILib;
using Microsoft.AspNetCore.Http;
using Basic;
namespace HsonAPI
{
    public static class Method
    {
        static public (string Server, string DB, string UserName, string Password, uint Port) GetServerInfo(string Name, string Type, string Content)
        {
            List<sys_serverSettingClass> _serverSetting = serverSetting.GetAllServerSetting();
            sys_serverSettingClass sys_serverSettingClass = _serverSetting.MyFind(Name, Type, Content).FirstOrDefault();
            if (sys_serverSettingClass == null)
            {
                throw new Exception("找無Server資料");
            }
            return (sys_serverSettingClass.Server, sys_serverSettingClass.DBName, sys_serverSettingClass.User, sys_serverSettingClass.Password, (uint)sys_serverSettingClass.Port.StringToInt32());
        }
        static public string GetServerAPI(string Name, string Type, string Content)
        {
            List<sys_serverSettingClass> _serverSetting = serverSetting.GetAllServerSetting();
            sys_serverSettingClass sys_serverSettingClass = _serverSetting.MyFind(Name, Type, Content).FirstOrDefault();
            if (sys_serverSettingClass == null)
            {
                return null;
                throw new Exception("找無Server資料");
            }
            return sys_serverSettingClass.Server;
        }
        /// <summary>
        /// 取得目前請求的相對路徑（可選是否包含查詢字串）。若非 HTTP 呼叫則回傳 "[InternalCall]"。
        /// </summary>
        public static string GetRequestPath(HttpContext? context, bool includeQuery = false)
        {
            if (context?.Request == null)
                return "[InternalCall]";

            if (includeQuery)
                return context.Request.Path + context.Request.QueryString;

            return context.Request.Path;
        }
    }
}
