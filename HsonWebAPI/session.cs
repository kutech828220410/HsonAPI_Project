using Basic;
using HsonAPILib; // 這裡引用你的 enum_products, productsClass
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace HsonAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class session : Controller
    {
        static private string API_Server = "http://127.0.0.1:4433/api/serversetting";
        static string API = Method.GetServerAPI("Main", "網頁", "API01");
        static private MySqlSslMode SSLMode = MySqlSslMode.None;
        /// <summary>
        /// 初始化資料庫
        /// </summary>
        /// <remarks>
        /// {
        ///     
        /// }
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [Route("init_login_data_index")]
        [Swashbuckle.AspNetCore.Annotations.SwaggerResponse(1, "", typeof(sessionClass))]
        [HttpPost]
        public string init_login_data_index([FromBody] returnData returnData)
        {
            try
            {
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                if (returnData.ServerName.StringIsEmpty() || returnData.ServerType.StringIsEmpty())
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
                }
                else
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind(returnData.ServerName, returnData.ServerType, "人員資料");
                }
                if (sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找無Server資料!";
                    return returnData.JsonSerializationt();
                }
                return CheckCreatTable(sys_serverSettingClasses[0]);

            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }
        }
        [HttpGet]
        public string Get(string level)
        {
            return GetPermissions(level.StringToInt32(), "", "").JsonSerializationt();
        }
        /// <summary>
        /// 使用者登入,取得session資訊,[ID,Pwd]、[UID]、[BARCODE]任一即可登入系統，若為admin則直接登入最高權限
        /// </summary>
        /// <remarks>
        ///  --------------------------------------------<br/> 
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///        "ID" : "",
        ///        "Password" : "",
        ///        "UID" : "",
        ///        "BARCODE" : ""
        ///     }
        ///   }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>[returnData.Data][sessionClass]</returns>
        [Route("login")]
        [HttpPost]
        public string POST_login([FromBody] returnData returnData)
        {
            try
            {
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                if (returnData.ServerName.StringIsEmpty() || returnData.ServerType.StringIsEmpty())
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
                }
                else
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind(returnData.ServerName, returnData.ServerType, "人員資料");
                }
                if (sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無資料庫參數!";
                    return returnData.JsonSerializationt();
                }
                string IP = sys_serverSettingClasses[0].Server;
                string DataBaseName = sys_serverSettingClasses[0].DBName;
                string UserName = sys_serverSettingClasses[0].User;
                string Password = sys_serverSettingClasses[0].Password;
                uint Port = (uint)sys_serverSettingClasses[0].Port.StringToInt32();
                bool flag_admin = false;

                SQLControl sQLControl_login_session = new SQLControl(IP, DataBaseName, "login_session", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_person_page = new SQLControl(IP, DataBaseName, "person_page", UserName, Password, Port, SSLMode);

                CheckCreatTable(sys_serverSettingClasses[0]);
                sessionClass data = returnData.Data.ObjToClass<sessionClass>();
                List<object[]> list_login_session = sQLControl_login_session.GetAllRows(null);
                List<object[]> list_person_page = sQLControl_person_page.GetAllRows(null);
                List<object[]> list_login_session_add = new List<object[]>();
                List<object[]> list_login_session_replace = new List<object[]>();
                sessionClass sessionClass = new sessionClass();
                if (data.ID.StringIsEmpty() == false && data.Password.StringIsEmpty() == false)
                {
                    if (data.ID.ToUpper() == "admin".ToUpper() && data.Password.ToUpper() == "66437068")
                    {
                        flag_admin = true;
                    }
                }

                if (flag_admin == false)
                {
                    if (data.UID.StringIsEmpty() == false)
                    {
                        list_person_page = list_person_page.GetRows((int)enum_personPage.卡號, data.UID);
                        if (list_person_page.Count == 0)
                        {
                            returnData.Code = -1;
                            returnData.Result = "此UID找無資料!";
                            return returnData.JsonSerializationt();
                        }
                        data.ID = list_person_page[0][(int)enum_personPage.ID].ObjectToString();
                        data.Password = list_person_page[0][(int)enum_personPage.密碼].ObjectToString();
                    }
                    else if (data.BARCODE.StringIsEmpty() == false)
                    {
                        list_person_page = list_person_page.GetRows((int)enum_personPage.一維條碼, data.BARCODE);
                        if (list_person_page.Count == 0)
                        {
                            returnData.Code = -1;
                            returnData.Result = "此一維條碼找無資料!";
                            return returnData.JsonSerializationt();
                        }
                        data.ID = list_person_page[0][(int)enum_personPage.ID].ObjectToString();
                        data.Password = list_person_page[0][(int)enum_personPage.密碼].ObjectToString();
                    }
                    list_person_page = list_person_page.GetRows((int)enum_personPage.ID, data.ID);
                    if (list_person_page.Count == 0)
                    {
                        returnData.Code = -1;
                        returnData.Result = "找無此帳號!";
                        return returnData.JsonSerializationt();
                    }
                    if (list_person_page[0][(int)enum_personPage.密碼].ObjectToString() != data.Password)
                    {
                        returnData.Code = -2;
                        returnData.Result = "密碼錯誤!";
                        return returnData.JsonSerializationt();
                    }
                    list_login_session = list_login_session.GetRows((int)enum_login_session.ID, data.ID);

                    object[] value = new object[new enum_login_session().GetLength()];
                    if (list_login_session.Count == 0)
                    {
                        value = new object[new enum_login_session().GetLength()];
                        value[(int)enum_login_session.GUID] = Guid.NewGuid().ToString();
                        value[(int)enum_login_session.ID] = list_person_page[0][(int)enum_personPage.ID].ObjectToString();
                        value[(int)enum_login_session.Name] = list_person_page[0][(int)enum_personPage.姓名].ObjectToString();
                        value[(int)enum_login_session.Employer] = list_person_page[0][(int)enum_personPage.單位].ObjectToString();
                        value[(int)enum_login_session.verifyTime] = DateTime.Now.ToDateTimeString();
                        value[(int)enum_login_session.loginTime] = DateTime.Now.ToDateTimeString();
                        list_login_session_add.Add(value);
                    }
                    else
                    {
                        value = list_login_session[0];
                        value[(int)enum_login_session.GUID] = Guid.NewGuid().ToString();
                        value[(int)enum_login_session.ID] = list_person_page[0][(int)enum_personPage.ID].ObjectToString();
                        value[(int)enum_login_session.Name] = list_person_page[0][(int)enum_personPage.姓名].ObjectToString();
                        value[(int)enum_login_session.Employer] = list_person_page[0][(int)enum_personPage.單位].ObjectToString();
                        value[(int)enum_login_session.verifyTime] = DateTime.Now.ToDateTimeString();
                        value[(int)enum_login_session.loginTime] = DateTime.Now.ToDateTimeString();
                        list_login_session_replace.Add(value);
                    }
                    if (list_login_session_add.Count > 0) sQLControl_login_session.AddRows(null, list_login_session_add);
                    if (list_login_session_replace.Count > 0) sQLControl_login_session.UpdateByDefulteExtra(null, list_login_session_replace);


                    sessionClass.GUID = value[(int)enum_login_session.GUID].ObjectToString();
                    sessionClass.ID = list_person_page[0][(int)enum_personPage.ID].ObjectToString();
                    sessionClass.Password = list_person_page[0][(int)enum_personPage.密碼].ObjectToString();
                    sessionClass.Name = list_person_page[0][(int)enum_personPage.姓名].ObjectToString();
                    sessionClass.Employer = list_person_page[0][(int)enum_personPage.單位].ObjectToString();
                    sessionClass.verifyTime = value[(int)enum_login_session.verifyTime].ObjectToString();
                    sessionClass.loginTime = value[(int)enum_login_session.loginTime].ObjectToString();
                    sessionClass.level = list_person_page[0][(int)enum_personPage.權限等級].ObjectToString();
                    sessionClass.Color = list_person_page[0][(int)enum_personPage.顏色].ObjectToString();
                    sessionClass.Permissions = GetPermissions(sessionClass.level.StringToInt32(), returnData.ServerName, returnData.ServerType);
                }
                else
                {
                    sessionClass.GUID = "";
                    sessionClass.ID = "admin";
                    sessionClass.Password = "66437068";
                    sessionClass.Name = "最高管理權限";
                    sessionClass.Employer = "";
                    sessionClass.verifyTime = DateTime.Now.ToDateTimeString();
                    sessionClass.loginTime = DateTime.Now.ToDateTimeString();
                    sessionClass.level = "-1";
                    sessionClass.Color = System.Drawing.Color.Red.ToColorString();
                    sessionClass.Permissions = GetPermissions(sessionClass.level.StringToInt32(), returnData.ServerName, returnData.ServerType);
                }





                returnData.Data = sessionClass;

                //if (list_login_session.Count > 0)
                //{
                //    returnData.Code = -3;
                //    returnData.Result = "已登入帳號,需登出!";
                //    return returnData.JsonSerializationt();
                //}



                returnData.Code = 200;
                returnData.Result = "登入成功!";
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }

        }

        [Route("logout")]
        [HttpPost]
        public string POST_logout([FromBody] returnData returnData)
        {
            try
            {
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                if (returnData.ServerName.StringIsEmpty() || returnData.ServerType.StringIsEmpty())
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
                }
                else
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind(returnData.ServerName, returnData.ServerType, "人員資料");
                }
                if (sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無資料庫參數!";
                    return returnData.JsonSerializationt();
                }
                string IP = sys_serverSettingClasses[0].Server;
                string DataBaseName = sys_serverSettingClasses[0].DBName;
                string UserName = sys_serverSettingClasses[0].User;
                string Password = sys_serverSettingClasses[0].Password;
                uint Port = (uint)sys_serverSettingClasses[0].Port.StringToInt32();

                SQLControl sQLControl_login_session = new SQLControl(IP, DataBaseName, "login_session", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_person_page = new SQLControl(IP, DataBaseName, "person_page", UserName, Password, Port, SSLMode);

                CheckCreatTable(sys_serverSettingClasses[0]);
                sessionClass sessionClass = returnData.Data.ObjToClass<sessionClass>();
                List<object[]> list_login_session = sQLControl_login_session.GetAllRows(null);
                list_login_session = list_login_session.GetRows((int)enum_login_session.ID, sessionClass.ID);
                if (list_login_session.Count > 0)
                {
                    sQLControl_login_session.DeleteExtra(null, list_login_session);
                    returnData.Code = 200;
                    returnData.Result = $"ID :{sessionClass.ID} ,清除session成功!";
                    return returnData.JsonSerializationt();
                }
                returnData.Code = 200;
                returnData.Result = $"ID :{sessionClass.ID} ,找無此session!";
                return returnData.JsonSerializationt();

            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }


        }

        [Route("check_session")]
        [HttpPost]
        public string POST_check([FromBody] returnData returnData)
        {
            try
            {
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                if (returnData.ServerName.StringIsEmpty() || returnData.ServerType.StringIsEmpty())
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
                }
                else
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind(returnData.ServerName, returnData.ServerType, "人員資料");
                }
                if (sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無資料庫參數!";
                    return returnData.JsonSerializationt();
                }
                string IP = sys_serverSettingClasses[0].Server;
                string DataBaseName = sys_serverSettingClasses[0].DBName;
                string UserName = sys_serverSettingClasses[0].User;
                string Password = sys_serverSettingClasses[0].Password;
                uint Port = (uint)sys_serverSettingClasses[0].Port.StringToInt32();

                SQLControl sQLControl_login_session = new SQLControl(IP, DataBaseName, "login_session", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_person_page = new SQLControl(IP, DataBaseName, "person_page", UserName, Password, Port, SSLMode);

                CheckCreatTable(sys_serverSettingClasses[0]);

                sessionClass sessionClass = returnData.Data.ObjToClass<sessionClass>();
                List<object[]> list_login_session = sQLControl_login_session.GetAllRows(null);
                list_login_session = list_login_session.GetRows((int)enum_login_session.ID, sessionClass.ID);
                if (list_login_session.Count == 0)
                {
                    returnData.Result = "其他裝置登入,即將登出!";
                    returnData.Code = -2;
                    return returnData.JsonSerializationt();
                }
                else
                {

                }
                DateTime loginTime = sessionClass.loginTime.StringToDateTime().StringToDateTime();
                if (sessionClass.loginTime.StringToDateTime().ToDateTimeString() != list_login_session[0][(int)enum_login_session.loginTime].StringToDateTime().ToDateTimeString())
                {
                    returnData.Result = "其他裝置登入,即將登出!";
                    returnData.Code = -2;
                    return returnData.JsonSerializationt();
                }
                DateTime verifyTime = list_login_session[0][(int)enum_login_session.verifyTime].StringToDateTime();


                TimeSpan timeDifference = DateTime.Now.Subtract(verifyTime);
                double secondsDifference = timeDifference.TotalSeconds;
                if (secondsDifference >= sessionClass.check_sec.StringToInt32())
                {
                    returnData.Result = "驗證逾時,即將登出!";
                    returnData.Code = -1;
                    return returnData.JsonSerializationt();
                }
                list_login_session[0][(int)enum_login_session.verifyTime] = DateTime.Now.ToDateTimeString();
                sQLControl_login_session.UpdateByDefulteExtra(null, list_login_session);

                returnData.Result = "驗證成功!";
                returnData.Code = 200;
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }

        }

        [Route("update_session")]
        [HttpPost]
        public string POST_update_session([FromBody] returnData returnData)
        {
            try
            {
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                if (returnData.ServerName.StringIsEmpty() || returnData.ServerType.StringIsEmpty())
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
                }
                else
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind(returnData.ServerName, returnData.ServerType, "人員資料");
                }
                if (sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無資料庫參數!";
                    return returnData.JsonSerializationt();
                }
                string IP = sys_serverSettingClasses[0].Server;
                string DataBaseName = sys_serverSettingClasses[0].DBName;
                string UserName = sys_serverSettingClasses[0].User;
                string Password = sys_serverSettingClasses[0].Password;
                uint Port = (uint)sys_serverSettingClasses[0].Port.StringToInt32();

                SQLControl sQLControl_login_session = new SQLControl(IP, DataBaseName, "login_session", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_person_page = new SQLControl(IP, DataBaseName, "person_page", UserName, Password, Port, SSLMode);

                CheckCreatTable(sys_serverSettingClasses[0]);

                sessionClass sessionClass = returnData.Data.ObjToClass<sessionClass>();
                List<object[]> list_login_session = sQLControl_login_session.GetAllRows(null);
                list_login_session = list_login_session.GetRows((int)enum_login_session.ID, sessionClass.ID);
                if (list_login_session.Count == 0)
                {
                    returnData.Result = "找無session!";
                    returnData.Code = -2;
                    return returnData.JsonSerializationt();
                }
                list_login_session[0][(int)enum_login_session.GUID] = sessionClass.GUID;
                returnData.Result = "更新session完成!";
                returnData.Code = 200;
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }

        }

        [Route("get_permissions")]
        [HttpPost]
        public string POST_get_permissions([FromBody] returnData returnData)
        {
            try
            {
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                if (returnData.ServerName.StringIsEmpty() || returnData.ServerType.StringIsEmpty())
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
                }
                else
                {
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind(returnData.ServerName, returnData.ServerType, "人員資料");
                }
                if (sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無資料庫參數!";
                    return returnData.JsonSerializationt();
                }
                string IP = sys_serverSettingClasses[0].Server;
                string DataBaseName = sys_serverSettingClasses[0].DBName;
                string UserName = sys_serverSettingClasses[0].User;
                string Password = sys_serverSettingClasses[0].Password;
                uint Port = (uint)sys_serverSettingClasses[0].Port.StringToInt32();

                SQLControl sQLControl_login_session = new SQLControl(IP, DataBaseName, "login_session", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_person_page = new SQLControl(IP, DataBaseName, "person_page", UserName, Password, Port, SSLMode);
                CheckCreatTable(sys_serverSettingClasses[0]);
                sessionClass data = returnData.Data.ObjToClass<sessionClass>();
                List<object[]> list_login_session = sQLControl_login_session.GetAllRows(null);
                List<object[]> list_person_page = sQLControl_person_page.GetAllRows(null);
                List<object[]> list_login_session_add = new List<object[]>();
                List<object[]> list_login_session_replace = new List<object[]>();

                list_person_page = list_person_page.GetRows((int)enum_personPage.ID, data.ID);
                if (list_person_page.Count == 0)
                {
                    returnData.Code = -1;
                    returnData.Result = "找無此帳號!";
                    return returnData.JsonSerializationt();
                }
                if (list_person_page[0][(int)enum_personPage.密碼].ObjectToString() != data.Password)
                {
                    returnData.Code = -2;
                    returnData.Result = "密碼錯誤!";
                    return returnData.JsonSerializationt();
                }
                sessionClass sessionClass = new sessionClass();
                sessionClass.ID = list_person_page[0][(int)enum_personPage.ID].ObjectToString();
                sessionClass.Password = list_person_page[0][(int)enum_personPage.密碼].ObjectToString();
                sessionClass.Name = list_person_page[0][(int)enum_personPage.姓名].ObjectToString();
                sessionClass.Employer = list_person_page[0][(int)enum_personPage.單位].ObjectToString();
                sessionClass.level = list_person_page[0][(int)enum_personPage.權限等級].ObjectToString();
                sessionClass.Color = list_person_page[0][(int)enum_personPage.顏色].ObjectToString();
                sessionClass.Permissions = GetPermissions(sessionClass.level.StringToInt32(), returnData.ServerName, returnData.ServerType);



                returnData.Data = sessionClass;

                returnData.Code = 200;
                returnData.Result = "取得權限成功!";
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }

        }
        /// <summary>
        /// 以權限等級取得資料
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///     },
        ///     "ValueAry":["權限等級"]
        ///   }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [Route("get_login_data_index")]
        [HttpPost]
        public string get_login_data_index([FromBody] returnData returnData)
        {
            try
            {

                MyTimerBasic myTimerBasic = new MyTimerBasic();
                returnData.Method = "get_login_data_index";
                string ServerName = returnData.ServerName;
                string ServerType = returnData.ServerType;
                string Content = "人員資料";
                if (ServerName.StringIsEmpty())
                {
                    ServerName = "Main";
                    ServerType = "網頁";
                    Content = "VM端";
                }
                (string Server, string DB, string UserName, string Password, uint Port) = Method.GetServerInfo(ServerName, ServerType, Content);
                SQLControl sQLControl_login_data_index = new SQLControl(Server, DB, "login_data_index", UserName, Password, Port, SSLMode);
                List<object[]> login_data_index = sQLControl_login_data_index.GetAllRows(null);
                List<loginDataIndexClass> loginDataIndexClasses = login_data_index.SQLToClass<loginDataIndexClass, enum_login_data_index>();
                loginDataIndexClasses.Sort(new loginDataIndexClass.ICP_By_index());
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = loginDataIndexClasses;
                returnData.Result = $"取得權限設定，共{loginDataIndexClasses.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 更新權限資料
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": [
        ///     {
        ///         "name":"交班對點頁面"
        ///         "index":0,
        ///         "type":"調劑台",
        ///         "state": true or false
        ///     }],
        ///     "ValueAry":["權限等級"]
        ///   }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [Route("update_login_data_index")]
        [HttpPost]
        public string update_login_data_index([FromBody] returnData returnData)
        {
            try
            {
                MyTimerBasic myTimerBasic = new MyTimerBasic();
                string ServerName = returnData.ServerName;
                string ServerType = returnData.ServerType;
                string Content = "人員資料";
                if (ServerName.StringIsEmpty())
                {
                    ServerName = "Main";
                    ServerType = "網頁";
                    Content = "VM端";
                }
                (string Server, string DB, string UserName, string Password, uint Port) = Method.GetServerInfo(ServerName, ServerType, Content);
                SQLControl sQLControl_login_data_index = new SQLControl(Server, DB, "login_data_index", UserName, Password, Port, SSLMode);
                List<loginDataIndexClass> input_loginDataIndex = returnData.Data.ObjToClass<List<loginDataIndexClass>>();

                List<object[]> loginDataIndex = sQLControl_login_data_index.GetAllRows(null);
                List<loginDataIndexClass> sql_loginDataIndex = loginDataIndex.SQLToClass<loginDataIndexClass, enum_login_data_index>();
                List<loginDataIndexClass> update_loginDataIndex = new List<loginDataIndexClass>();
                List<loginDataIndexClass> add_loginDataIndex = new List<loginDataIndexClass>();


                foreach (var item in input_loginDataIndex)
                {
                    string index = item.索引;
                    loginDataIndexClass loginDataIndexClass = sql_loginDataIndex.Where(temp => temp.索引 == index).FirstOrDefault();
                    if (loginDataIndexClass != null)
                    {
                        loginDataIndexClass.Name = item.Name;
                        loginDataIndexClass.Type = item.Type;
                        loginDataIndexClass.群組 = item.群組;
                        loginDataIndexClass.描述 = item.描述;

                        update_loginDataIndex.Add(loginDataIndexClass);
                    }
                    else
                    {
                        loginDataIndexClass.GUID = Guid.NewGuid().ToString();
                        loginDataIndexClass.Name = item.Name;
                        loginDataIndexClass.Type = item.Type;
                        loginDataIndexClass.群組 = item.群組;
                        loginDataIndexClass.描述 = item.描述;

                        add_loginDataIndex.Add(loginDataIndexClass);
                    }
                }


                List<object[]> update = update_loginDataIndex.ClassToSQL<loginDataIndexClass, enum_login_data_index>();
                List<object[]> add = add_loginDataIndex.ClassToSQL<loginDataIndexClass, enum_login_data_index>();

                if (update.Count > 0) sQLControl_login_data_index.UpdateByDefulteExtra(null, update);
                if (add.Count > 0) sQLControl_login_data_index.AddRows(null, add);
                add_loginDataIndex.AddRange(update_loginDataIndex);
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = add_loginDataIndex;
                returnData.Result = $"更新權限表，共{add_loginDataIndex.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 以權限等級和類別取得資料
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///     },
        ///     "ValueAry":["權限等級","類別(調劑台\藥庫\網頁)"]
        ///   }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [Route("get_setting_by_type")]
        [HttpPost]
        public string get_setting_by_type([FromBody] returnData returnData)
        {
            try
            {

                MyTimerBasic myTimerBasic = new MyTimerBasic();
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"ValueAry不得為空";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"ValueAry應為[\"權限等級\",\"調劑台or藥庫\"]";
                    return returnData.JsonSerializationt(true);
                }
                string ServerName = returnData.ServerName;
                string ServerType = returnData.ServerType;
                string Content = "人員資料";
                if (ServerName.StringIsEmpty())
                {
                    ServerName = "Main";
                    ServerType = "網頁";
                    Content = "VM端";
                }
                (string Server, string DB, string UserName, string Password, uint Port) = Method.GetServerInfo(ServerName, ServerType, Content);
                int level = returnData.ValueAry[0].StringToInt32();
                string 類別 = returnData.ValueAry[1];
                loadData();
                List<PermissionsClass> PermissionsClasses = GetPermissions(level, ServerName, ServerType);

                PermissionsClasses = PermissionsClasses.Where(item => item.類別 == 類別).ToList();

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = PermissionsClasses;
                returnData.Result = $"取得{類別}權限設定，共{PermissionsClasses.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 更新權限資料
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": [
        ///     {
        ///         "name":"交班對點頁面"
        ///         "index":0,
        ///         "type":"調劑台",
        ///         "state": true or false
        ///     }],
        ///     "ValueAry":["權限等級"]
        ///   }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [Route("update_setting")]
        [HttpPost]
        public string update_setting([FromBody] returnData returnData)
        {
            try
            {

                MyTimerBasic myTimerBasic = new MyTimerBasic();
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"ValueAry不得為空";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"ValueAry應為[\"權限等級\"]";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"Data不得為空";
                    return returnData.JsonSerializationt(true);
                }
                List<PermissionsClass> update_permiss = returnData.Data.ObjToClass<List<PermissionsClass>>();
                string ServerName = returnData.ServerName;
                string ServerType = returnData.ServerType;
                string Content = "人員資料";
                if (ServerName.StringIsEmpty())
                {
                    ServerName = "Main";
                    ServerType = "網頁";
                    Content = "VM端";
                }
                (string Server, string DB, string UserName, string Password, uint Port) = Method.GetServerInfo(ServerName, ServerType, Content);
                string level = returnData.ValueAry[0];
                List<PermissionsClass> PermissionsClasses = GetPermissions(level.StringToInt32(), ServerName, ServerType);

                foreach (var item in PermissionsClasses)
                {
                    PermissionsClass permissionsClasses = update_permiss.Where(temp => temp.索引 == item.索引).FirstOrDefault();
                    if (permissionsClasses != null)
                    {
                        item.狀態 = permissionsClasses.狀態;
                    }
                }
                List<loginDataClass> loginDataClasses = loginDataClass.get_permission_index(API);
                loginDataClass loginData = loginDataClasses.Where(item => item.權限等級 == level).FirstOrDefault();
                if (loginData == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"無{level}權限";
                    return returnData.JsonSerializationt(true);
                }
                List<loginDataClass> _loginDataClass = PackPermissionBitsToLongs(loginData, PermissionsClasses);
                List<object[]> update_login_data = _loginDataClass.ClassToSQL<loginDataClass, enum_login_data>();
                SQLControl sQLControl_login_data = new SQLControl(Server, DB, "login_data", UserName, Password, Port, SSLMode);
                sQLControl_login_data.UpdateByDefulteExtra(null, update_login_data);
                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = update_permiss;
                returnData.Result = $"更新權限設定，共{update_permiss.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 取得權限
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///   }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns>
        [Route("get_permission_index")]
        [HttpPost]
        public string get_permission_index([FromBody] returnData returnData)
        {
            try
            {

                MyTimerBasic myTimerBasic = new MyTimerBasic();

                string ServerName = returnData.ServerName;
                string ServerType = returnData.ServerType;
                string Content = "人員資料";
                if (ServerName.StringIsEmpty())
                {
                    ServerName = "Main";
                    ServerType = "網頁";
                    Content = "VM端";
                }
                (string Server, string DB, string UserName, string Password, uint Port) = Method.GetServerInfo(ServerName, ServerType, Content);
                SQLControl sQLControl_login_data = new SQLControl(Server, DB, "login_data", UserName, Password, Port, SSLMode);
                List<object[]> session_data = sQLControl_login_data.GetAllRows(null);
                List<loginDataClass> loginDataClasses = session_data.SQLToClass<loginDataClass, enum_login_data>();


                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = loginDataClasses;
                returnData.Result = $"取得權限表單，共{loginDataClasses.Count}筆";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
        }
        private List<PermissionsClass> GetPermissions(int level, string serverName, string serverType)
        {
            List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
            if (serverName.StringIsEmpty() || serverType.StringIsEmpty())
            {
                sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
            }
            else
            {
                sys_serverSettingClasses = sys_serverSettingClasses.MyFind(serverName, serverType, "人員資料");
            }
            if (sys_serverSettingClasses.Count == 0)
            {
                return new List<PermissionsClass>();
            }
            string IP = sys_serverSettingClasses[0].Server;
            string DataBaseName = sys_serverSettingClasses[0].DBName;
            string UserName = sys_serverSettingClasses[0].User;
            string Password = sys_serverSettingClasses[0].Password;
            uint Port = (uint)sys_serverSettingClasses[0].Port.StringToInt32();

            List<PermissionsClass> result = new List<PermissionsClass>();
            SQLControl sQLControl_login_data_index = new SQLControl(IP, DataBaseName, "login_data_index", UserName, Password, Port, SSLMode);
            SQLControl sQLControl_login_data = new SQLControl(IP, DataBaseName, "login_data", UserName, Password, Port, SSLMode);
            List<MySQL_Login.LoginDataWebAPI.Class_login_data> list_class_login_data = MySQL_Login.LoginDataWebAPI.Get_login_data(sQLControl_login_data);
            List<object[]> login_data_index = sQLControl_login_data_index.GetAllRows(null);
            List<object[]> login_data_index_buf = new List<object[]>();
            if (level == -1)
            {
                list_class_login_data = new List<MySQL_Login.LoginDataWebAPI.Class_login_data>();
                MySQL_Login.LoginDataWebAPI.Class_login_data class_Login_Data = new MySQL_Login.LoginDataWebAPI.Class_login_data();
                for (int i = 0; i < class_Login_Data.data.Count; i++)
                {
                    class_Login_Data.data[i] = true;
                }

                list_class_login_data.Add(class_Login_Data);
            }
            if (list_class_login_data.Count > 0)
            {
                list_class_login_data = (from value in list_class_login_data
                                         where value.level.StringToInt32().ToString() == level.StringToInt32().ToString()
                                         select value).ToList();
                if (list_class_login_data.Count == 0) return new List<PermissionsClass>();
                for (int i = 0; i < list_class_login_data[0].data.Count; i++)
                {
                    login_data_index_buf = login_data_index.GetRows((int)MySQL_Login.LoginDataWebAPI.enum_login_data_index.索引, i.ToString("00"));
                    if (list_class_login_data[0].data[i])
                    {

                        if (login_data_index_buf.Count > 0)
                        {
                            PermissionsClass permissionsClass = new PermissionsClass();
                            permissionsClass.名稱 = login_data_index_buf[0][(int)MySQL_Login.LoginDataWebAPI.enum_login_data_index.Name].ObjectToString();
                            permissionsClass.類別 = login_data_index_buf[0][(int)MySQL_Login.LoginDataWebAPI.enum_login_data_index.Type].ObjectToString();
                            permissionsClass.索引 = i;
                            permissionsClass.狀態 = true;
                            permissionsClass.群組 = login_data_index_buf[0][(int)enum_login_data_index.群組].ObjectToString();
                            permissionsClass.描述 = login_data_index_buf[0][(int)enum_login_data_index.描述].ObjectToString();

                            result.Add(permissionsClass);
                        }
                    }
                    else
                    {
                        if (login_data_index_buf.Count > 0)
                        {
                            PermissionsClass permissionsClass = new PermissionsClass();
                            permissionsClass.名稱 = login_data_index_buf[0][(int)MySQL_Login.LoginDataWebAPI.enum_login_data_index.Name].ObjectToString();
                            permissionsClass.類別 = login_data_index_buf[0][(int)MySQL_Login.LoginDataWebAPI.enum_login_data_index.Type].ObjectToString();
                            permissionsClass.索引 = i;
                            permissionsClass.狀態 = false;
                            permissionsClass.群組 = login_data_index_buf[0][(int)enum_login_data_index.群組].ObjectToString();
                            permissionsClass.描述 = login_data_index_buf[0][(int)enum_login_data_index.描述].ObjectToString();
                            result.Add(permissionsClass);
                        }
                    }

                }
            }
            return result;
        }
        private List<loginDataClass> PackPermissionBitsToLongs(loginDataClass loginDataClass, List<PermissionsClass> permissions)
        {
            bool[] data = new bool[256];

            foreach (var permission in permissions)
            {
                int index = permission.索引;
                if (index >= 0 && index < 256)
                {
                    data[index] = permission.狀態;
                }
            }

            List<long> packedLongs = new List<long>();

            for (int section = 0; section < 4; section++)
            {
                long value = 0;
                for (int bit = 0; bit < 64; bit++)
                {
                    if (data[section * 64 + bit])
                    {
                        value |= 1L << bit;
                    }
                }
                packedLongs.Add(value);
            }

            loginDataClass loginData = new loginDataClass
            {
                GUID = loginDataClass.GUID,
                權限等級 = loginDataClass.權限等級,
                Data01 = packedLongs[0].ToString(),
                Data02 = packedLongs[1].ToString(),
                Data03 = packedLongs[2].ToString(),
                Data04 = packedLongs[3].ToString()
            };

            return new List<loginDataClass> { loginData };
        }


        private string CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            string Server = sys_serverSettingClass.Server;
            string DB = sys_serverSettingClass.DBName;
            string UserName = sys_serverSettingClass.User;
            string Password = sys_serverSettingClass.Password;
            uint Port = (uint)sys_serverSettingClass.Port.StringToInt32();
            List<Table> tables = new List<Table>();
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_personPage()));         
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_login_data()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_login_session()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_login_data_index()));
            return tables.JsonSerializationt(true);
        }
        private void loadData()
        {
            init_login_data_index(new returnData());
            string data = Basic.MyFileStream.LoadFileAllText(@"./login_data_index.txt", "utf-8");
            //string loadText = Basic.MyFileStream.LoadFileAllText(@"./excel_emg_tradding.txt", "utf-8");

            List<loginDataIndexClass> loginDataIndexClasses = loginDataIndexClass.update_login_data_index(API, data);
        }

    }
}
