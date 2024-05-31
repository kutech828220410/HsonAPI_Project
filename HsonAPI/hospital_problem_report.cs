using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
    /// 問題回報單元
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class hospital_problem_report : ControllerBase
    {
      
        static private string Server = ConfigurationManager.AppSettings["server"];
        static private uint Port = (uint)ConfigurationManager.AppSettings["port"].StringToInt32();
        static private string UserName = ConfigurationManager.AppSettings["user"];
        static private string Password = ConfigurationManager.AppSettings["password"];
        static private string DB = "hs_db";
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

        /// <summary>
        /// 初始化醫院問題回報資料庫
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     }
        ///   }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns></returns> 
        [Route("init")]    
        [HttpPost]
        [SwaggerResponse(1, "", typeof(hospital_nameClass))]
        [SwaggerResponse(2, "", typeof(hostpital_reportClass))]
        [SwaggerResponse(3, "", typeof(hostpital_report_picture_Class))]
        public string POST_init(returnData returnData)
        {
            try
            {
                MyTimerBasic myTimerBasic = new MyTimerBasic();
                myTimerBasic.StartTickTime();
                returnData.Method = "POST_init";
         
                return CheckCreatTable();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
        /// <summary>
        /// 新增醫院名稱
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     },
        ///     "ValueAry" : 
        ///     [
        ///       "名稱",
        ///       "棟名",
        ///       "診別"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("add_hospital_name")]
        [HttpPost]
        public string POST_add_hospital_name(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "add_hospital_name";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
       
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if(returnData.ValueAry.Count != 3)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[名稱],[棟名],[診別]";
                    return returnData.JsonSerializationt(true);
                }
                string 名稱 = returnData.ValueAry[0];
                string 棟名 = returnData.ValueAry[1];
                string 診別 = returnData.ValueAry[2];
                if (名稱.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value [名稱]不得為空白";
                    return returnData.JsonSerializationt(true);
                }
                if (棟名.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value [棟名]不得為空白";
                    return returnData.JsonSerializationt(true);
                }
                if (診別.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value [診別]不得為空白";
                    return returnData.JsonSerializationt(true);
                }
                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetRowsByDefult(null, (int)enum_hospital_nameClass.名稱, 名稱);
                list_hostpital_name = list_hostpital_name.GetRows((int)enum_hospital_nameClass.棟名, 棟名);
                list_hostpital_name = list_hostpital_name.GetRows((int)enum_hospital_nameClass.診別, 診別);

                List<object[]> list_hostpital_name_add = new List<object[]>();
                List<object[]> list_hostpital_name_replace = new List<object[]>();

                if (list_hostpital_name.Count == 0)
                {
                    object[] value = new object[new enum_hospital_nameClass().GetLength()];
                    value[(int)enum_hospital_nameClass.GUID] = Guid.NewGuid().ToString();
                    value[(int)enum_hospital_nameClass.名稱] = 名稱;
                    value[(int)enum_hospital_nameClass.棟名] = 棟名;
                    value[(int)enum_hospital_nameClass.診別] = 診別;
                    list_hostpital_name_add.Add(value);
                }
                else
                {
                    object[] value = list_hostpital_name[0];
                    value[(int)enum_hospital_nameClass.名稱] = 名稱;
                    value[(int)enum_hospital_nameClass.棟名] = 棟名;
                    value[(int)enum_hospital_nameClass.診別] = 診別;
                    list_hostpital_name_replace.Add(value);
                }

                sQLControl_hostpital_name.AddRows(null, list_hostpital_name_add);
                sQLControl_hostpital_name.UpdateByDefulteExtra(null, list_hostpital_name_replace);
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功,共新增<{list_hostpital_name_add.Count}>筆資料,共修改<{list_hostpital_name_replace.Count}>筆資料";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;

                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {
       
                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 以GUID刪除醫院名稱
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     },
        ///     "ValueAry" : 
        ///     [ 
        ///       "GUID"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("delete_hospital_name_by_guid")]
        [HttpPost]
        public string POST_delete_hospital_name_by_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "delete_hospital_name_by_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[GUID]";
                    return returnData.JsonSerializationt(true);
                }
                string GUID = returnData.ValueAry[0];


                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetRowsByDefult(null, (int)enum_hospital_nameClass.GUID, GUID);
      
                sQLControl_hostpital_name.DeleteExtra(null, list_hostpital_name);
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功,共刪除<{list_hostpital_name.Count}>筆資料";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;

                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 取得所有醫院名稱
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     },
        ///     "Value" : ""
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("get_hospital_name")]
        [HttpPost]
        public string POST_get_hospital_name(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "get_hospital_name";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
     
        

                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetAllRows(null);
                List<hospital_nameClass> hospital_NameClasses = list_hostpital_name.SQLToClass<hospital_nameClass, enum_hospital_nameClass>();

                returnData.Data = hospital_NameClasses;
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 取得成功,共<{hospital_NameClasses.Count}>筆資料";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";

                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 以GUID取得所有醫院名稱
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     },
        ///     "ValueAry" : 
        ///     [ 
        ///       "GUID"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("get_hospital_name_by_guid")]
        [HttpPost]
        public string POST_get_hospital_name_by_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "get_hospital_name_by_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[GUID]";
                    return returnData.JsonSerializationt(true);
                }
                string GUID = returnData.ValueAry[0];
                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetRowsByDefult(null, (int)enum_hospital_nameClass.GUID, GUID);
                if(list_hostpital_name.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Data = null;
                    returnData.Result = $"找無任何資料";
                    return returnData.JsonSerializationt(true);
                }
                hospital_nameClass hospital_NameClass = list_hostpital_name[0].SQLToClass<hospital_nameClass, enum_hospital_nameClass>();

                returnData.Data = hospital_NameClass;
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 取得成功";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;

                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }


        /// <summary>
        /// 新增問題回報資料
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///         
        ///     },
        ///     "ValueAry" : 
        ///     [
        ///        "標題",
        ///        "內容",
        ///        "hospital_name_guid"
        ///        "回報人員"
        ///        "發生時間"
        ///     ]
        ///     
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("add_hospital_report")]
        [HttpPost]
        public string POST_add_hospital_report(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "add_hospital_report";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 5)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[標題],[內容],[hospital_name_guid],[回報人員],[發生時間]";
                    return returnData.JsonSerializationt(true);
                }
             
                string 標題 = returnData.ValueAry[0];
                string 內容 = returnData.ValueAry[1];
                string hospital_name_guid = returnData.ValueAry[2];
                string 回報人員 = returnData.ValueAry[3];
                string 發生時間 = returnData.ValueAry[4];
                if (發生時間.Check_Date_String() == false)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容[發噌時間]資料格式錯誤";
                    return returnData.JsonSerializationt(true);
                }
                if (標題.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value [標題]不得為空白";
                    return returnData.JsonSerializationt(true);
                }
                if (hospital_name_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value [hospital_name_guid]不得為空白";
                    return returnData.JsonSerializationt(true);
                }
                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetRowsByDefult(null, (int)enum_hospital_nameClass.GUID, hospital_name_guid);
                if (list_hostpital_name.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找無[醫院名稱]資料請檢查[hospital_name_guid]";
                    return returnData.JsonSerializationt(true);
                }

                List<object[]> list_hostpital_report = sQLControl_hostpital_report.GetRowsByDefult(null, (int)enum_hostpital_report.hospital_name_guid, hospital_name_guid);
                list_hostpital_report = list_hostpital_report.GetRows((int)enum_hostpital_report.標題, 標題);
                if (list_hostpital_report.Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"已有相同標題,請更換標題";
                    return returnData.JsonSerializationt(true);
                }
          
                hospital_nameClass hospital_NameClass = list_hostpital_name[0].SQLToClass<hospital_nameClass, enum_hospital_nameClass>();

                hostpital_reportClass hostpital_ReportClass = new hostpital_reportClass();
                hostpital_ReportClass.GUID = Guid.NewGuid().ToString();
                hostpital_ReportClass.標題 = 標題;
                hostpital_ReportClass.內容 = 內容;
                hostpital_ReportClass.hospital_NameClass = hospital_NameClass;
                hostpital_ReportClass.是否完成 = false.ToString();
                hostpital_ReportClass.是否審核 = false.ToString();
                hostpital_ReportClass.回報時間 = DateTime.Now.ToDateTimeString_6();
                hostpital_ReportClass.發生時間 = 發生時間;
                hostpital_ReportClass.完成時間 = DateTime.MinValue.ToDateTimeString();
                hostpital_ReportClass.審核時間 = DateTime.MinValue.ToDateTimeString();
                hostpital_ReportClass.回報人員 = 回報人員;

                object[] value = hostpital_ReportClass.ClassToSQL<hostpital_reportClass,enum_hostpital_report>();
                sQLControl_hostpital_report.AddRow(null, value);
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功新增<1>筆資料";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Data = hostpital_ReportClass;
                returnData.Code = 200;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 以GUID刪除問題回報資料
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     },
        ///     "ValueAry" : 
        ///     [ 
        ///       "GUID"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("delete_hospital_report_by_guid")]
        [HttpPost]
        public string POST_delete_hospital_report_by_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "delete_hospital_report_by_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[GUID]";
                    return returnData.JsonSerializationt(true);
                }
                string GUID = returnData.ValueAry[0];
                List<object[]> list_hostpital_report = sQLControl_hostpital_report.GetRowsByDefult(null, (int)enum_hostpital_report.GUID, GUID);
                if (list_hostpital_report.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value 傳入[GUID]查無資料";
                    return returnData.JsonSerializationt(true);
                }
                List<object[]> list_hostpital_report_picture = sQLControl_hostpital_report_picture.GetRowsByDefult(null, (int)enum_hostpital_report_picture.hostpital_report_guid, GUID);

              
                sQLControl_hostpital_report.DeleteExtra(null, list_hostpital_report);
                sQLControl_hostpital_report_picture.DeleteByDefult(null, (int)enum_hostpital_report_picture.hostpital_report_guid, GUID);
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功刪除資料";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;
                string json_out = returnData.JsonSerializationt(true);
                returnData.Data = "";
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ json_out}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 修改問題回報資料內容
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///         
        ///     },
        ///     "ValueAry" : 
        ///     [
        ///       "GUID",
        ///       "內容"
        ///     ]
        ///     
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("update_hospital_report_content_by_guid")]
        [HttpPost]
        public string POST_edit_hospital_report_content_by_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "update_hospital_report_content_by_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[名稱],[棟名],[診別]";
                    return returnData.JsonSerializationt(true);
                }
                string GUID = returnData.ValueAry[0];
                string 內容 = returnData.ValueAry[1];

                List<object[]> list_hostpital_report = sQLControl_hostpital_report.GetRowsByDefult(null, (int)enum_hostpital_report.GUID, GUID);
                if (list_hostpital_report.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"[GUID] 查無資料,請輸入正確的回報問題GUID";
                    return returnData.JsonSerializationt(true);
                }
                list_hostpital_report[0][(int)enum_hostpital_report.內容] = 內容;

           
                sQLControl_hostpital_report.UpdateByDefulteExtra(null, list_hostpital_report);
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功修改資料";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 取得問題回報資料(不含圖片)
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///         
        ///     }
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("get_all_hospital_report")]
        [HttpPost]
        public string POST_get_all_hospital_report(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "get_all_hospital_report";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);

                List<object[]> list_hostpital_report = sQLControl_hostpital_report.GetAllRows(null);
                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetAllRows(null);

                List<hostpital_reportClass> hostpital_ReportClasses = list_hostpital_report.SQLToClass<hostpital_reportClass, enum_hostpital_report>();
                List<hospital_nameClass> hospital_NameClasses = list_hostpital_name.SQLToClass<hospital_nameClass, enum_hospital_nameClass>();
                List<hospital_nameClass> hospital_NameClasses_buf = new List<hospital_nameClass>();
                string[] colnames = new string[] { enum_hostpital_report_picture.GUID.GetEnumName(), enum_hostpital_report_picture.hostpital_report_guid.GetEnumName(), enum_hostpital_report_picture.加入時間.GetEnumName() };
                List<object[]> list_hostpital_report_picture = sQLControl_hostpital_report_picture.GetColumnValues(null, colnames, false);
                List<hostpital_report_picture_Class> hostpital_Report_Picture_Classes = list_hostpital_report_picture.SQLToClass<hostpital_report_picture_Class, enum_hostpital_report_picture>();
                List<hostpital_report_picture_Class> hostpital_Report_Picture_Classes_buf = new List<hostpital_report_picture_Class>();

                for (int i = 0; i < hostpital_ReportClasses.Count; i++)
                {
                    string GUID = hostpital_ReportClasses[i].GUID;
                    string hospital_name_guid = hostpital_ReportClasses[i].hospital_name_guid;
                    hospital_NameClasses_buf = (from temp in hospital_NameClasses
                                                where temp.GUID == hospital_name_guid
                                                select temp).ToList();
                    if(hospital_NameClasses_buf.Count > 0)
                    {
                        hostpital_ReportClasses[i].hospital_NameClass = hospital_NameClasses_buf[0];
                    }

                    hostpital_Report_Picture_Classes_buf = (from temp in hostpital_Report_Picture_Classes
                                                            where temp.hostpital_report_guid == GUID
                                                            select temp).ToList();
                    if (hostpital_Report_Picture_Classes_buf.Count > 0)
                    {
                        hostpital_ReportClasses[i].pictures = hostpital_Report_Picture_Classes_buf;
                    }
                }


                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功取得資料";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Data = hostpital_ReportClasses;
                returnData.Code = 200;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 以GUID取得問題回報資料(包含圖片)
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     },
        ///     "ValueAry" : 
        ///     [ 
        ///       "GUID"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("get_hospital_report_by_guid")]
        [HttpPost]
        public string POST_get_hospital_report_by_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "get_hospital_report_by_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[GUID]";
                    return returnData.JsonSerializationt(true);
                }
                string GUID = returnData.ValueAry[0];
                List<object[]> list_hostpital_report = sQLControl_hostpital_report.GetRowsByDefult(null, (int)enum_hostpital_report.GUID, GUID);
                if(list_hostpital_report.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value 傳入[GUID]查無資料";
                    return returnData.JsonSerializationt(true);
                }
                List<object[]> list_hostpital_report_picture = sQLControl_hostpital_report_picture.GetRowsByDefult(null, (int)enum_hostpital_report_picture.hostpital_report_guid, GUID);
                List<hostpital_report_picture_Class> hostpital_Report_Picture_Classes = list_hostpital_report_picture.SQLToClass<hostpital_report_picture_Class , enum_hostpital_report_picture>();

                hostpital_reportClass hostpital_ReportClass = list_hostpital_report[0].SQLToClass<hostpital_reportClass, enum_hostpital_report>();



                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetAllRows(null);
                List<hospital_nameClass> hospital_NameClasses = list_hostpital_name.SQLToClass<hospital_nameClass, enum_hospital_nameClass>();
                List<hospital_nameClass> hospital_NameClasses_buf = new List<hospital_nameClass>();

                string hospital_name_guid = hostpital_ReportClass.hospital_name_guid;
                hospital_NameClasses_buf = (from temp in hospital_NameClasses
                                            where temp.GUID == hospital_name_guid
                                            select temp).ToList();
                if (hospital_NameClasses_buf.Count > 0)
                {
                    hostpital_ReportClass.hospital_NameClass = hospital_NameClasses_buf[0];
                }
                hostpital_ReportClass.pictures = hostpital_Report_Picture_Classes;

                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功取得資料";
                returnData.TimeTaken = myTimerBasic.ToString();             
                returnData.Code = 200;
                string json_out = returnData.JsonSerializationt(true);
                returnData.Data = hostpital_ReportClass;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ json_out}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 新增圖片到回報資料
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///         
        ///     },
        ///     "ValueAry" : 
        ///     [ 
        ///       "hospital_report_guid",
        ///       "base64"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("add_picture_by_hospital_report_guid")]
        [HttpPost]
        public string POST_add_picture_by_hospital_report_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "add_picture_by_hospital_report_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);

                if (returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[hospital_report_guid],[base64]";
                    return returnData.JsonSerializationt(true);
                }
                string hospital_report_guid = returnData.ValueAry[0];
                string base64 = returnData.ValueAry[1];
                List<object[]> list_hostpital_report = sQLControl_hostpital_report.GetRowsByDefult(null,(int)enum_hostpital_report.GUID, hospital_report_guid);
                if(list_hostpital_report.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"[hospital_report_guid] 查無資料,請輸入正確的回報問題GUID";
                    return returnData.JsonSerializationt(true);
                }
                hostpital_report_picture_Class hostpital_Report_Picture_Class = new hostpital_report_picture_Class();
                hostpital_Report_Picture_Class.GUID = Guid.NewGuid().ToString();
                hostpital_Report_Picture_Class.hostpital_report_guid = hospital_report_guid;
                hostpital_Report_Picture_Class.圖片 = base64;
                hostpital_Report_Picture_Class.加入時間 = DateTime.Now.ToDateTimeString_6();
                object[] value = hostpital_Report_Picture_Class.ClassToSQL<hostpital_report_picture_Class , enum_hostpital_report_picture>();

                sQLControl_hostpital_report_picture.AddRow(null, value);
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 新增成功";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 以GUID刪除圖片
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///         
        ///     },
        ///     "ValueAry" : 
        ///     [ 
        ///       "GUID",
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("delete_picture_by_guid")]
        [HttpPost]
        public string delete_picture_by_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "delete_picture_by_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);

                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[GUID]";
                    return returnData.JsonSerializationt(true);
                }
                string GUID = returnData.ValueAry[0];
                List<object[]> list_hostpital_report_picture = sQLControl_hostpital_report_picture.GetRowsByDefult(null, (int)enum_hostpital_report_picture.GUID, GUID);
                if (list_hostpital_report_picture.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"[hospital_report_guid] 查無資料,請輸入正確的回報問題GUID";
                    return returnData.JsonSerializationt(true);
                }


                sQLControl_hostpital_report_picture.DeleteExtra(null, list_hostpital_report_picture);
                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 刪除成功";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ returnData.JsonSerializationt(true)}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 以GUID設定問題回報資料完成
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     },
        ///     "ValueAry" : 
        ///     [ 
        ///       "GUID"
        ///       "完成人員"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("set_hospital_report_finished_by_guid")]
        [HttpPost]
        public string POST_set_hospital_report_finished_by_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "get_hospital_report_by_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[GUID],[完成人員]";
                    return returnData.JsonSerializationt(true);
                }
                string GUID = returnData.ValueAry[0];
                string 完成人員 = returnData.ValueAry[1];
                List<object[]> list_hostpital_report = sQLControl_hostpital_report.GetRowsByDefult(null, (int)enum_hostpital_report.GUID, GUID);
                if (list_hostpital_report.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value 傳入[GUID]查無資料";
                    return returnData.JsonSerializationt(true);
                }
                List<object[]> list_hostpital_report_picture = sQLControl_hostpital_report_picture.GetRowsByDefult(null, (int)enum_hostpital_report_picture.hostpital_report_guid, GUID);
                List<hostpital_report_picture_Class> hostpital_Report_Picture_Classes = list_hostpital_report_picture.SQLToClass<hostpital_report_picture_Class, enum_hostpital_report_picture>();

                hostpital_reportClass hostpital_ReportClass = list_hostpital_report[0].SQLToClass<hostpital_reportClass, enum_hostpital_report>();



                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetAllRows(null);
                List<hospital_nameClass> hospital_NameClasses = list_hostpital_name.SQLToClass<hospital_nameClass, enum_hospital_nameClass>();
                List<hospital_nameClass> hospital_NameClasses_buf = new List<hospital_nameClass>();

                string hospital_name_guid = hostpital_ReportClass.hospital_name_guid;
                hospital_NameClasses_buf = (from temp in hospital_NameClasses
                                            where temp.GUID == hospital_name_guid
                                            select temp).ToList();
                if (hospital_NameClasses_buf.Count > 0)
                {
                    hostpital_ReportClass.hospital_NameClass = hospital_NameClasses_buf[0];
                }
                hostpital_ReportClass.pictures = hostpital_Report_Picture_Classes;

                hostpital_ReportClass.是否完成 = true.ToString();
                hostpital_ReportClass.完成時間 = DateTime.Now.ToDateTimeString_6();
                hostpital_ReportClass.完成人員 = 完成人員;

                object[] value = hostpital_ReportClass.ClassToSQL<hostpital_reportClass , enum_hostpital_report>();
                sQLControl_hostpital_report.UpdateByDefulteExtra(null, value);

                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功設定資料完成";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;
                string json_out = returnData.JsonSerializationt(true);
                returnData.Data = hostpital_ReportClass;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ json_out}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }
        /// <summary>
        /// 以GUID設定問題回報資料審核完畢
        /// </summary>
        /// <remarks>
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///  
        ///     },
        ///     "ValueAry" : 
        ///     [ 
        ///       "GUID"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Route("set_hospital_report_reviewed_by_guid")]
        [HttpPost]
        public string POST_set_hospital_report_reviewed_by_guid(returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "set_hospital_report_reviewed_by_guid";
            try
            {
                POST_init(returnData);
                SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
                SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[GUID]";
                    return returnData.JsonSerializationt(true);
                }
                string GUID = returnData.ValueAry[0];
                List<object[]> list_hostpital_report = sQLControl_hostpital_report.GetRowsByDefult(null, (int)enum_hostpital_report.GUID, GUID);
                if (list_hostpital_report.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.Value 傳入[GUID]查無資料";
                    return returnData.JsonSerializationt(true);
                }
                List<object[]> list_hostpital_report_picture = sQLControl_hostpital_report_picture.GetRowsByDefult(null, (int)enum_hostpital_report_picture.hostpital_report_guid, GUID);
                List<hostpital_report_picture_Class> hostpital_Report_Picture_Classes = list_hostpital_report_picture.SQLToClass<hostpital_report_picture_Class, enum_hostpital_report_picture>();

                hostpital_reportClass hostpital_ReportClass = list_hostpital_report[0].SQLToClass<hostpital_reportClass, enum_hostpital_report>();



                List<object[]> list_hostpital_name = sQLControl_hostpital_name.GetAllRows(null);
                List<hospital_nameClass> hospital_NameClasses = list_hostpital_name.SQLToClass<hospital_nameClass, enum_hospital_nameClass>();
                List<hospital_nameClass> hospital_NameClasses_buf = new List<hospital_nameClass>();

                string hospital_name_guid = hostpital_ReportClass.hospital_name_guid;
                hospital_NameClasses_buf = (from temp in hospital_NameClasses
                                            where temp.GUID == hospital_name_guid
                                            select temp).ToList();
                if (hospital_NameClasses_buf.Count > 0)
                {
                    hostpital_ReportClass.hospital_NameClass = hospital_NameClasses_buf[0];
                }
                hostpital_ReportClass.pictures = hostpital_Report_Picture_Classes;

                hostpital_ReportClass.是否審核 = true.ToString();
                hostpital_ReportClass.審核時間 = DateTime.Now.ToDateTimeString_6();

                object[] value = hostpital_ReportClass.ClassToSQL<hostpital_reportClass, enum_hostpital_report>();
                sQLControl_hostpital_report.UpdateByDefulteExtra(null, value);

                returnData.Result = $"[{System.Reflection.MethodBase.GetCurrentMethod().Name}] 成功設定資料審核完成";
                returnData.TimeTaken = myTimerBasic.ToString();
                returnData.Code = 200;
                string json_out = returnData.JsonSerializationt(true);
                returnData.Data = hostpital_ReportClass;
                Logger.LogAddLine($"hospital_problem_report");
                Logger.Log($"hospital_problem_report", $"{ json_out}");
                Logger.LogAddLine($"hospital_problem_report");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception e)
            {

                returnData.Code = -200;
                returnData.Data = null;
                returnData.Result = $"{e.Message}";
                Logger.Log($"hospital_problem_report", $"[異常] { returnData.Result}");
                return returnData.JsonSerializationt(true);
            }
        }

        private string CheckCreatTable()
        {


            SQLControl sQLControl_hostpital_report = new SQLControl(Server, DB, "hostpital_report", UserName, Password, Port, SSLMode);
            SQLControl sQLControl_hostpital_report_picture = new SQLControl(Server, DB, "hostpital_report_picture", UserName, Password, Port, SSLMode);
            SQLControl sQLControl_hostpital_name = new SQLControl(Server, DB, "hostpital_name", UserName, Password, Port, SSLMode);


            List<Table> tables = new List<Table>();

            Table table_hostpital_report = new Table("hostpital_report");
            table_hostpital_report.Server = Server;
            table_hostpital_report.DBName = DB;
            table_hostpital_report.Username = UserName;
            table_hostpital_report.Password = Password;
            table_hostpital_report.Port = Port.ToString();
            table_hostpital_report.AddColumnList("GUID", Table.StringType.VARCHAR, 50, Table.IndexType.PRIMARY);
            table_hostpital_report.AddColumnList("hospital_name_guid", Table.StringType.VARCHAR, 50, Table.IndexType.INDEX);            
            table_hostpital_report.AddColumnList("標題", Table.StringType.VARCHAR, 500, Table.IndexType.None);
            table_hostpital_report.AddColumnList("內容", Table.StringType.VARCHAR, 2000, Table.IndexType.None);
            table_hostpital_report.AddColumnList("回報時間", Table.DateType.DATETIME, 500, Table.IndexType.INDEX);
            table_hostpital_report.AddColumnList("發生時間", Table.DateType.DATETIME, 500, Table.IndexType.INDEX);
            table_hostpital_report.AddColumnList("完成時間", Table.DateType.DATETIME, 500, Table.IndexType.INDEX);
            table_hostpital_report.AddColumnList("審核時間", Table.DateType.DATETIME, 500, Table.IndexType.INDEX);
            table_hostpital_report.AddColumnList("是否完成", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table_hostpital_report.AddColumnList("是否審核", Table.StringType.VARCHAR, 10, Table.IndexType.None);
            table_hostpital_report.AddColumnList("回報人員", Table.StringType.VARCHAR, 50, Table.IndexType.None);
            table_hostpital_report.AddColumnList("完成人員", Table.StringType.VARCHAR, 50, Table.IndexType.None);

            if (!sQLControl_hostpital_report.IsTableCreat()) sQLControl_hostpital_report.CreatTable(table_hostpital_report);
            else sQLControl_hostpital_report.CheckAllColumnName(table_hostpital_report, true);
            tables.Add(table_hostpital_report);


            Table table_hostpital_report_picture = new Table("hostpital_report_picture");
            table_hostpital_report_picture.Server = Server;
            table_hostpital_report_picture.DBName = DB;
            table_hostpital_report_picture.Username = UserName;
            table_hostpital_report_picture.Password = Password;
            table_hostpital_report_picture.Port = Port.ToString();
            table_hostpital_report_picture.AddColumnList("GUID", Table.StringType.VARCHAR, 50, Table.IndexType.PRIMARY);
            table_hostpital_report_picture.AddColumnList("hostpital_report_guid", Table.StringType.VARCHAR, 50, Table.IndexType.INDEX);
            table_hostpital_report_picture.AddColumnList("picture", Table.StringType.LONGTEXT, 500, Table.IndexType.None);
            table_hostpital_report_picture.AddColumnList("加入時間", Table.DateType.DATETIME, 500, Table.IndexType.None);
            if (!sQLControl_hostpital_report_picture.IsTableCreat()) sQLControl_hostpital_report_picture.CreatTable(table_hostpital_report_picture);
            else sQLControl_hostpital_report_picture.CheckAllColumnName(table_hostpital_report_picture, true);
            tables.Add(table_hostpital_report_picture);


            Table table_hostpital_name = new Table("hostpital_name");
            table_hostpital_name.Server = Server;
            table_hostpital_name.DBName = DB;
            table_hostpital_name.Username = UserName;
            table_hostpital_name.Password = Password;
            table_hostpital_name.Port = Port.ToString();
            table_hostpital_name.AddColumnList("GUID", Table.StringType.VARCHAR, 50, Table.IndexType.PRIMARY);
            table_hostpital_name.AddColumnList("名稱", Table.StringType.VARCHAR, 200, Table.IndexType.None);
            table_hostpital_name.AddColumnList("棟名", Table.StringType.VARCHAR, 200, Table.IndexType.None);
            table_hostpital_name.AddColumnList("診別", Table.StringType.VARCHAR, 200, Table.IndexType.None);

            if (!sQLControl_hostpital_name.IsTableCreat()) sQLControl_hostpital_name.CreatTable(table_hostpital_name);
            else sQLControl_hostpital_name.CheckAllColumnName(table_hostpital_name, true);
            tables.Add(table_hostpital_name);

            return tables.JsonSerializationt(true);
        }
        private void Test(hospital_nameClass hospital_NameClass)
        {

        }
    }
}
