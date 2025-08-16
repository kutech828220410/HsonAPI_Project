using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.HttpOverrides;
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
using HsonAPILib;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.ReDoc;
using Swashbuckle.AspNetCore.Newtonsoft;
using Swashbuckle.AspNetCore;
namespace HsonWebAPI
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
            services.Configure<ForwardedHeadersOptions>(opts =>
            {
                opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                // 如有固定可信任 Proxy，可加入：
                // opts.KnownProxies.Add(IPAddress.Parse("172.18.0.2"));
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1",
                new OpenApiInfo
                 {
                     Title = "Hson.Co.Ltd. Sysytem API",
                     Version = "v1",
                     Description = "Hson.Co.Ltd. Sysytem API",
               
                });

                var xmlPath_HsonAPI = Path.Combine(AppContext.BaseDirectory, $"HsonWebAPI.xml");
                var xmlPath_HsonAPILib = Path.Combine(AppContext.BaseDirectory, $"HsonAPILib.xml");

                options.IncludeXmlComments(xmlPath_HsonAPILib , true);

                options.IncludeXmlComments(xmlPath_HsonAPI, true);
                options.OrderActionsBy(s => s.RelativePath);
            });

         
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
             
            }
            app.UseForwardedHeaders();
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
            });

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseReDoc(options =>
            {
                options.DocumentTitle = "Swagger Demo Documentation";
                options.SpecUrl = "/swagger/v1/swagger.json";
            });
        }
    }
}
