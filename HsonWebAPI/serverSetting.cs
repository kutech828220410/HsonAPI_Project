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

namespace HsonWebAPI
{
    /// <summary>
    /// Server 設定管理 API
    /// </summary>
    /// <remarks>
    /// 用於管理與查詢系統的伺服器連線設定資訊，例如：新增、刪除、初始化、依類別或單位查詢等。
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class serverSetting : Controller
    {
        static private string Server = ConfigurationManager.AppSettings["server"];
        static private uint Port = (uint)ConfigurationManager.AppSettings["port"].StringToInt32();
        static private string UserName = ConfigurationManager.AppSettings["user"];
        static private string Password = ConfigurationManager.AppSettings["password"];
        static private string DB = "hs_db";
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

        /// <summary>
        /// 取得所有伺服器連線資訊
        /// </summary>
        /// <remarks>
        /// 範例請求：
        /// <code>
        /// GET /api/serverSetting
        /// </code>
        /// </remarks>
        /// <returns>包含所有伺服器設定的資料清單</returns>
        [HttpGet]
        public string GET()
        {
            returnData returnData = new returnData();
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            returnData.Method = "get";

            try
            {
                this.CheckCreatTable();
                SQLControl sQLControl = new SQLControl(Server, DB, "ServerSetting", UserName, Password, Port, SSLMode);
                List<object[]> list_value = sQLControl.GetAllRows(null);

                List<sys_serverSettingClass> sys_serverSettingClasses = list_value.SQLToClass<sys_serverSettingClass, enum_sys_serverSetting>();

                returnData.Code = 200;
                returnData.Data = sys_serverSettingClasses;
                returnData.Result = $"取得伺服器設定成功! 共<{sys_serverSettingClasses.Count}>筆";
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 初始化資料表（建立 ServerSetting 資料表）
        /// </summary>
        /// <remarks>
        /// 範例請求：
        /// <code>
        /// GET /api/serverSetting/init
        /// </code>
        /// </remarks>
        /// <returns>資料表建立結果</returns>
        [Route("init")]
        [HttpGet]
        public string GET_init()
        {
            returnData returnData = new returnData();
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "GET_init";
            try
            {
                returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            }
            catch { }

            try
            {
                return CheckCreatTable();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 初始化資料表（POST 版本）
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "Data": {}
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>資料表建立結果</returns>
        [Route("post_init")]
        [HttpPost]
        public string POST_init([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);

            try
            {
                try
                {
                    returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
                }
                catch { }

                returnData.Method = "POST_init";
                return CheckCreatTable();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 新增或更新伺服器連線資訊
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "Data": [
        ///         {
        ///             "設備名稱": "Server01",
        ///             "類別": "調劑台",
        ///             "程式類別": "一般資料",
        ///             "內容": "xxx"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">包含伺服器設定的資料物件清單</param>
        /// <returns>新增或更新後的結果</returns>
        [Route("add")]
        [HttpPost]
        public string POST_add([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try
            {
                returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            }
            catch { }
            returnData.Method = "add";
            try
            {
                this.CheckCreatTable();
                SQLControl sQLControl = new SQLControl(Server, DB, "ServerSetting", UserName, Password, Port, SSLMode);
                List<object[]> list_value = sQLControl.GetAllRows(null);
                List<object[]> list_value_returnData = new List<object[]>();
                List<object[]> list_value_add = new List<object[]>();
                List<object[]> list_value_replace = new List<object[]>();
                List<object[]> list_value_buf = new List<object[]>();

                List<sys_serverSettingClass> sys_serverSettingClasses = returnData.Data.ObjToListClass<sys_serverSettingClass>();
                list_value_returnData = sys_serverSettingClasses.ClassToSQL<sys_serverSettingClass, enum_sys_serverSetting>();

                for (int i = 0; i < list_value_returnData.Count; i++)
                {
                    string 名稱 = list_value_returnData[i][(int)enum_sys_serverSetting.設備名稱].ObjectToString();
                    string 類別 = list_value_returnData[i][(int)enum_sys_serverSetting.類別].ObjectToString();
                    string 程式類別 = list_value_returnData[i][(int)enum_sys_serverSetting.程式類別].ObjectToString();
                    string 內容 = list_value_returnData[i][(int)enum_sys_serverSetting.內容].ObjectToString();

                    list_value_buf = list_value.GetRows((int)enum_sys_serverSetting.設備名稱, 名稱);
                    list_value_buf = list_value_buf.GetRows((int)enum_sys_serverSetting.類別, 類別);
                    list_value_buf = list_value_buf.GetRows((int)enum_sys_serverSetting.程式類別, 程式類別);
                    list_value_buf = list_value_buf.GetRows((int)enum_sys_serverSetting.內容, 內容);
                    if (list_value_buf.Count == 0)
                    {
                        object[] value = list_value_returnData[i];
                        value[(int)enum_sys_serverSetting.GUID] = Guid.NewGuid().ToString();
                        list_value_add.Add(value);
                    }
                    else
                    {
                        object[] value = list_value_returnData[i];
                        value[(int)enum_sys_serverSetting.GUID] = list_value_buf[0][(int)enum_sys_serverSetting.GUID].ObjectToString();
                        list_value_replace.Add(value);
                    }
                }
                sQLControl.AddRows(null, list_value_add);
                sQLControl.UpdateByDefulteExtra(null, list_value_replace);

                returnData.Code = 200;
                returnData.Result = "新增伺服器資料成功!";
                returnData.Data = sys_serverSettingClasses;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 刪除伺服器連線資訊
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "Data": [
        ///         {
        ///             "設備名稱": "Server01",
        ///             "類別": "調劑台",
        ///             "程式類別": "一般資料",
        ///             "內容": "xxx"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">包含伺服器設定的資料物件清單</param>
        /// <returns>刪除後的結果</returns>
        [Route("delete")]
        [HttpPost]
        public string POST_delete([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try
            {
                returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            }
            catch { }
            returnData.Method = "delete";
            try
            {
                this.CheckCreatTable();
                SQLControl sQLControl = new SQLControl(Server, DB, "ServerSetting", UserName, Password, Port, SSLMode);
                List<object[]> list_value = sQLControl.GetAllRows(null);
                List<object[]> list_value_returnData = new List<object[]>();
                List<sys_serverSettingClass> sys_serverSettingClasses = returnData.Data.ObjToListClass<sys_serverSettingClass>();
                list_value_returnData = sys_serverSettingClasses.ClassToSQL<sys_serverSettingClass, enum_sys_serverSetting>();

                sQLControl.DeleteExtra(null, list_value_returnData);

                returnData.Code = 200;
                returnData.Result = "刪除伺服器資料成功!";
                returnData.Data = sys_serverSettingClasses;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 依類別取得伺服器連線資訊
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "ValueAry": ["調劑台"]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">包含 ValueAry[0] = 類別</param>
        /// <returns>符合類別條件的伺服器設定清單</returns>
        [Route("get_serversetting_by_type")]
        [HttpPost]
        public string POST_get_serversetting_by_type([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try
            {
                returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            }
            catch { }
            returnData.Method = "get_serversetting_by_type";
            try
            {
                this.CheckCreatTable();
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 應包含 1 筆類別資料";
                    return returnData.JsonSerializationt(true);
                }
                string Type = returnData.ValueAry[0];

                List<sys_serverSettingClass> sys_serverSettingClasses = GetAllServerSetting();
                sys_serverSettingClasses = (from temp in sys_serverSettingClasses
                                            where temp.類別 == Type
                                            where temp.內容 == "一般資料"
                                            select temp).ToList();

                sys_serverSettingClasses.Sort(new sys_serverSettingClass.ICP_By_dps_name());
                returnData.Code = 200;
                returnData.Result = $"取得連線資訊, 共<{sys_serverSettingClasses.Count}>筆";
                returnData.Data = sys_serverSettingClasses;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 取得調劑台與藥庫的設備名稱清單
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>符合條件的設備名稱清單</returns>
        [Route("get_name")]
        [HttpPost]
        public string get_name([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try
            {
                returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
            }
            catch { }
            returnData.Method = "get_name";
            try
            {
                List<sys_serverSettingClass> sys_serverSettingClasses = GetAllServerSetting();
                sys_serverSettingClasses = sys_serverSettingClasses.Where(temp => (temp.類別 == "調劑台" || temp.類別 == "藥庫") && temp.內容 == "一般資料").ToList();
                sys_serverSettingClasses.Sort(new sys_serverSettingClass.ICP_By_dps_name());
                returnData.Code = 200;
                returnData.Result = $"取得連線資訊, 共<{sys_serverSettingClasses.Count}>筆";
                returnData.Data = sys_serverSettingClasses;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 依指定類別取得服務單位清單
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "ValueAry": ["調劑台"]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">ValueAry[0] = 類別名稱</param>
        /// <returns>符合條件的服務單位名稱清單</returns>
        [Route("get_department_type")]
        [HttpPost]
        public string POST_get_department_type([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try { returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}"; } catch { }
            returnData.Method = "get_department_type";
            try
            {
                this.CheckCreatTable();
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = "returnData.ValueAry 內容應為 [Type]";
                    return returnData.JsonSerializationt(true);
                }
                string Type = returnData.ValueAry[0];
                List<sys_serverSettingClass> sys_serverSettingClasses = GetAllServerSetting();
                List<string> department_types = (from temp in sys_serverSettingClasses
                                                 where temp.類別 == Type
                                                 where temp.內容 == "一般資料"
                                                 select temp.單位).Distinct().ToList();
                department_types.Remove("");
                returnData.Code = 200;
                returnData.Result = $"取得服務單位，共<{department_types.Count}>筆";
                returnData.Data = department_types;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 依單位取得伺服器連線資訊
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "ValueAry": ["門診"]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">ValueAry[0] = 單位名稱</param>
        /// <returns>符合單位條件的伺服器設定清單</returns>
        [Route("get_serversetting_by_department_type")]
        [HttpPost]
        public string POST_get_serversetting_by_department_type([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try { returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}"; } catch { }
            returnData.Method = "get_serversetting_by_department_type";
            try
            {
                this.CheckCreatTable();
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = "returnData.ValueAry 內容應為 [Type]";
                    return returnData.JsonSerializationt(true);
                }
                string department_type = returnData.ValueAry[0];
                List<sys_serverSettingClass> sys_serverSettingClasses = GetAllServerSetting();
                sys_serverSettingClasses = (from temp in sys_serverSettingClasses
                                            where temp.單位 == department_type
                                            where temp.內容 == "一般資料"
                                            select temp).ToList();
                sys_serverSettingClasses.Sort(new sys_serverSettingClass.ICP_By_dps_name());
                returnData.Code = 200;
                returnData.Result = $"取得連線資訊，共<{sys_serverSettingClasses.Count}>筆";
                returnData.Data = sys_serverSettingClasses;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 取得 VM 伺服器資訊（網頁類別 + VM端）
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// { "ValueAry": [] }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>符合條件的 VM 伺服器資訊</returns>
        [Route("get_VM_Server")]
        [HttpPost]
        public string POST_get_VM_Server([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try { returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}"; } catch { }
            returnData.Method = "get_VM_Server";
            try
            {
                this.CheckCreatTable();
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                List<sys_serverSettingClass> sys_serverSettingClasses = GetAllServerSetting();
                List<sys_serverSettingClass> sys_serverSettingClasses_VM = (from temp in sys_serverSettingClasses
                                                                            where temp.類別 == "網頁"
                                                                            where temp.內容 == "VM端"
                                                                            select temp).ToList();
                if (sys_serverSettingClasses_VM.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "查無資料";
                    return returnData.JsonSerializationt(true);
                }
                returnData.Code = 200;
                returnData.Result = $"取得 VM 伺服器資訊，共<{sys_serverSettingClasses_VM.Count}>筆";
                returnData.Data = sys_serverSettingClasses_VM[0];
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 依名稱、類別、內容取得伺服器資訊
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "ValueAry": ["ds01", "藥庫", "一般資料"]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">ValueAry[0] = 名稱, ValueAry[1] = 類別, ValueAry[2] = 內容</param>
        /// <returns>符合條件的伺服器資訊</returns>
        [Route("get_server")]
        [HttpPost]
        public string POST_get_server([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try { returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}"; } catch { }
            returnData.Method = "get_server";
            try
            {
                this.CheckCreatTable();
                if (returnData.ValueAry == null || returnData.ValueAry.Count != 3)
                {
                    returnData.Code = -200;
                    returnData.Result = "returnData.ValueAry 內容應為 [名稱],[類別],[內容]";
                    return returnData.JsonSerializationt(true);
                }
                string 類別 = returnData.ValueAry[1].ToUpper();
                string 設備名稱 = returnData.ValueAry[0].ToUpper();
                string 內容 = returnData.ValueAry[2].ToUpper();
                List<sys_serverSettingClass> sys_serverSettingClasses = GetAllServerSetting();
                List<sys_serverSettingClass> sys_serverSettingClasses_buf = (from temp in sys_serverSettingClasses
                                                                             where temp.類別.ToUpper() == 類別
                                                                             where temp.設備名稱.ToUpper() == 設備名稱
                                                                             where temp.內容.ToUpper() == 內容
                                                                             select temp).ToList();
                if (sys_serverSettingClasses_buf.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "查無資料";
                    return returnData.JsonSerializationt(true);
                }
                returnData.Code = 200;
                returnData.Result = $"取得伺服器資訊，共<{sys_serverSettingClasses_buf.Count}>筆";
                returnData.Data = sys_serverSettingClasses_buf[0];
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 取得指定網頁模組啟用狀態
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "ValueAry": ["盤點單管理模組不啟用", "條碼建置模組不啟用"]
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">ValueAry = 模組名稱清單</param>
        /// <returns>符合條件的模組設定清單</returns>
        [Route("get_web_peremeter")]
        [HttpPost]
        public string POST_get_web_peremeter([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try { returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}"; } catch { }
            returnData.Method = "get_web_peremeter";
            try
            {
                this.CheckCreatTable();
                if (returnData.ValueAry == null || returnData.ValueAry.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "returnData.ValueAry 無傳入資料或內容空白";
                    return returnData.JsonSerializationt(true);
                }
                List<sys_serverSettingClass> sys_serverSettingClasses = GetAllServerSetting();
                List<sys_serverSettingClass> sys_serverSettingClasses_temp = (from temp in sys_serverSettingClasses
                                                                              where temp.類別.ToUpper() == "網頁"
                                                                              where temp.程式類別.ToUpper() == "PEREMETER"
                                                                              select temp).ToList();
                List<sys_serverSettingClass> sys_serverSettingClasses_buf = sys_serverSettingClasses_temp
                    .Where(s => returnData.ValueAry.Contains(s.內容))
                    .ToList();
                if (sys_serverSettingClasses_buf.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "查無資料";
                    return returnData.JsonSerializationt(true);
                }
                returnData.Code = 200;
                returnData.Result = $"取得網頁模組設定，共<{sys_serverSettingClasses_buf.Count}>筆";
                returnData.Data = sys_serverSettingClasses_buf;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 取得所有網頁模組參數名稱
        /// </summary>
        /// <remarks>
        /// 範例 JSON：
        /// <code>
        /// {
        ///     "ValueAry": []
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>模組名稱字串清單</returns>
        [Route("get_web_peremeter_name")]
        [HttpPost]
        public string POST_get_web_peremeter_name([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try { returnData.RequestUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}"; } catch { }
            returnData.Method = "get_web_peremeter_name";
            try
            {
                this.CheckCreatTable();
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                List<sys_serverSettingClass> sys_serverSettingClasses = GetAllServerSetting();
                List<sys_serverSettingClass> sys_serverSettingClasses_temp = (from temp in sys_serverSettingClasses
                                                                              where temp.類別.ToUpper() == "網頁"
                                                                              where temp.程式類別.ToUpper() == "PEREMETER"
                                                                              select temp).ToList();
                if (sys_serverSettingClasses_temp.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "查無資料";
                    return returnData.JsonSerializationt(true);
                }
                List<string> strs = sys_serverSettingClasses_temp.Select(s => s.內容).ToList();
                returnData.Code = 200;
                returnData.Result = $"取得模組名稱，共<{strs.Count}>筆";
                returnData.Data = strs;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                returnData.TimeTaken = $"{myTimerBasic}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 檢查並建立資料表（使用預設連線參數）
        /// </summary>
        private string CheckCreatTable()
        {
            sys_serverSettingClass sys_serverSettingClass = new sys_serverSettingClass
            {
                Server = Server,
                Port = Port.ToString(),
                User = UserName,
                Password = Password,
                DBName = DB
            };
            return CheckCreatTable(sys_serverSettingClass);
        }

        /// <summary>
        /// 檢查並建立資料表（指定連線參數）
        /// </summary>
        private string CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            Table table = MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_sys_serverSetting());
            return table.JsonSerializationt(true);
        }

        /// <summary>
        /// 取得所有 Server 設定資料
        /// </summary>
        /// <returns>伺服器設定清單</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        static public List<sys_serverSettingClass> GetAllServerSetting()
        {
            SQLControl sQLControl = new SQLControl(Server, DB, "ServerSetting", UserName, Password, Port, SSLMode);
            List<object[]> list_value = sQLControl.GetAllRows(null);
            return list_value.SQLToClass<sys_serverSettingClass, enum_sys_serverSetting>();
        }
    }
}
