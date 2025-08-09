using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SQLUI;
using Basic;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Configuration;
using HsonAPILib;
using MySql.Data;

namespace HsonAPI
{
    /// <summary>
    /// 測試單元
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class test : ControllerBase
    {
        static string Version = "Ver1.0.0.1";
        [HttpGet]
        public string Get()
        {
            var localIpAddress = HttpContext.Connection.LocalIpAddress?.ToString();
            var localPort = HttpContext.Connection.LocalPort;
            var protocol = HttpContext.Request.IsHttps ? "https" : "http";
            returnData returnData = new returnData();
            returnData.Code = 200;
            returnData.Result = $"Api test sucess!{protocol}://{localIpAddress}:{localPort}";

            string DB = ConfigurationManager.AppSettings["DB"];
            string Server = ConfigurationManager.AppSettings["Server"];
            string VM_Server = ConfigurationManager.AppSettings["VM_Server"];
            string VM_DB = ConfigurationManager.AppSettings["VM_DB"];

            List<string> strs = new List<string>();
            strs.Add($"local Server : {Server}");
            strs.Add($"local Database : {DB}");
            strs.Add($"VM Server : {VM_Server}");
            strs.Add($"VM Database : {VM_DB}");
            //strs.Add($"uDP_Class PORT: {Startup.uDP_Class.Port}");
            strs.Add($"Version : {Version}");


            returnData.Data = strs;


            return returnData.JsonSerializationt(true);
        }
    }
}
