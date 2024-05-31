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
using RDPSessionManager;
using MyEmail;
using System.Diagnostics;

namespace HsonAPI
{
    [Route("[controller]")]
    public class sendemail : ControllerBase
    {
        [HttpGet]
        public string Get(string name)
        {
            try
            {
                string batchFilePath = @"C:\batch\sendemail_batch\sendemail_batch.exe";
                string arguments = $"{name} 123";

                ProcessStartInfo processStartInfo = new ProcessStartInfo(batchFilePath, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                // 启动进程
                using (Process process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    // 读取输出
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        return $"Emails sent successfully.";
                    }
                    else if (process.ExitCode == -2)
                    {
                        return "使用者名稱錯誤";
                    }
                    else
                    {
                        return "Error sending emails";
                    }
                }
            }
            catch(Exception e)
            {
                return $"Exception : {e.Message}";
            }
            


        }


    }
}
