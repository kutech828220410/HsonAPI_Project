using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models; // 导入 OpenApiInfo 和 OpenApiContact 类
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.AspNetCore.SignalR;
using H_Pannel_lib;

namespace HsonAPI
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder
                        .SetIsOriginAllowed(origin => true)
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });

            });
            services.AddControllers();
            services.AddSignalR();

            services.AddSwaggerGen(c =>
            {
                // API 服務簡介
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Hson.Co.Ltd Sysytem API",
                    Description = "Hson.Co.Ltd Sysytem API",

                });

                // 讀取 XML 檔案產生 API 說明
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                var xmlFile_HIS_DB_Lib = $"HsonAPI.xml";
                var xmlPath_HIS_DB_Lib = Path.Combine(AppContext.BaseDirectory, xmlFile_HIS_DB_Lib);

                c.IncludeXmlComments(xmlPath);
                c.IncludeXmlComments(xmlPath_HIS_DB_Lib);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors(builder =>
            {
                builder.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
            });
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors(); // 啟用CORS
            app.UseAuthorization();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHub<ChatHub>("/chatHub");
            });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }
        public class ChatHub : Hub
        {
            public async Task SendMessage(string user, string message)
            {
                await Clients.All.SendAsync("ReceiveMessage", user, message);
            }

        }
    }
}
