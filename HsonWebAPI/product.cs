using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

using MySql.Data.MySqlClient;
using SQLUI;
using Basic;
using HsonAPILib; // 這裡引用你的 enum_products, productsClass

namespace HsonAPI
{
    [Route("api/[controller]")]
    [ApiController]
    public class Products : ControllerBase
    {
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

        /// <summary>
        /// 初始化 dbvm.products 資料表
        /// </summary>
        /// <remarks>
        /// 呼叫此 API 會檢查並建立 `products` 資料表  
        /// **以下為 JSON 範例**
        /// <code>
        /// {
        ///     "ServerName": "Main",
        ///     "ServerType": "網頁",
        ///     "ValueAry": []
        /// }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>回傳 Table 結構資訊</returns>
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                // 取得系統所有 Server 設定
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();

                // 如果沒傳 ServerName / ServerType，就用預設 VM端
                if (returnData.ServerType.StringIsEmpty() || returnData.ServerName.StringIsEmpty())
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
                else
                    sys_serverSettingClasses = sys_serverSettingClasses.MyFind(returnData.ServerName, returnData.ServerType, "VM_DB");

                if (sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無 Server 資料!";
                    return returnData.JsonSerializationt();
                }

                // 檢查並建立 Table
                List<Table> tables = CheckCreatTable(sys_serverSettingClasses[0]);

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Data = tables;
                returnData.Result = $"初始化 products 資料表完成";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception : {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 新增產品資料
        /// </summary>
        /// <param name="returnData">
        /// 共用傳遞資料結構  
        /// Data 需傳入 <see cref="productsClass"/> JSON 物件
        /// </param>
        /// <returns>處理結果 JSON</returns>
        [Route("add_product")]
        [HttpPost]
        public string add_product([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "add_product";
            try
            {
                // 驗證 Data
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 反序列化為 productsClass
                productsClass newProduct = returnData.Data.ObjToClass<productsClass>();

                if (newProduct == null || newProduct.產品代碼.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "缺少必要欄位 product_code";
                    return returnData.JsonSerializationt();
                }

                // 取得 Server 設定
                List<sys_serverSettingClass> sys_serverSettingClasses = serverSetting.GetAllServerSetting();
                List<sys_serverSettingClass> _sys_serverSettingClasses;

                if (returnData.ServerType.StringIsEmpty() || returnData.ServerName.StringIsEmpty())
                    _sys_serverSettingClasses = sys_serverSettingClasses.MyFind("Main", "網頁", "VM端");
                else
                    _sys_serverSettingClasses = sys_serverSettingClasses.MyFind(returnData.ServerName, returnData.ServerType, "VM_DB");

                if (_sys_serverSettingClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找無 Server 資料";
                    return returnData.JsonSerializationt();
                }

                string Server = _sys_serverSettingClasses[0].Server;
                string DB = _sys_serverSettingClasses[0].DBName;
                string UserName = _sys_serverSettingClasses[0].User;
                string Password = _sys_serverSettingClasses[0].Password;
                uint Port = (uint)_sys_serverSettingClasses[0].Port.StringToInt32();
                string TableName = new enum_products().GetEnumDescription();
                SQLControl sQLControl_products = new SQLControl(Server, DB, TableName, UserName, Password, Port, SSLMode);

                // 檢查 product_code 是否重複
                string productCodeCol = enum_products.產品代碼.GetEnumName();
                List<object[]> existRows = sQLControl_products.GetRowsByDefult(null, (int)enum_products.產品代碼, newProduct.產品代碼);
                if (existRows.Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"產品代碼 {newProduct.產品代碼} 已存在";
                    return returnData.JsonSerializationt();
                }

                // 設定 GUID 與時間
                newProduct.GUID = Guid.NewGuid().ToString();
                newProduct.建立時間 = DateTime.Now.ToDateTimeString();
                newProduct.更新時間 = DateTime.Now.ToDateTimeString();

                // 寫入 DB
                object[] insertValue = newProduct.ClassToSQL<productsClass, enum_products>();
                sQLControl_products.AddRow(null, insertValue);

                returnData.Code = 200;
                returnData.TimeTaken = $"{myTimerBasic}";
                returnData.Result = "新增產品成功";
                returnData.Data = newProduct;

                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }
        }




        private List<Table> CheckCreatTable(sys_serverSettingClass sys_serverSettingClass)
        {
            string Server = sys_serverSettingClass.Server;
            string DB = sys_serverSettingClass.DBName;
            string UserName = sys_serverSettingClass.User;
            string Password = sys_serverSettingClass.Password;
            uint Port = (uint)sys_serverSettingClass.Port.StringToInt32();
            List<Table> tables = new List<Table>();
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_products()));
            tables.Add(MethodClass.CheckCreatTable(sys_serverSettingClass, new enum_product_components()));
            return tables;
        }
    }
}
