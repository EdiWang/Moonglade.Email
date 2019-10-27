using System;
using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Notification.API.Authentication;
using Moonglade.Notification.API.Extensions;
using Moonglade.Notification.Core;

namespace Moonglade.Notification.API
{
    public class Startup
    {
        private ILogger<Startup> _logger;

        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMemoryCache();

            services.Configure<AppSettings>(Configuration.GetSection(nameof(AppSettings)));

            services.AddRateLimit(Configuration.GetSection("IpRateLimiting"));

            if (Environment.IsProduction())
            {
                services.AddApplicationInsightsTelemetry();
            }

            services.AddControllers();
            services.AddTransient<IMoongladeNotification, EmailHandler>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                    options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                })
                .AddApiKeySupport(options => { });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            _logger = logger;
            _logger.LogInformation($"Moonglade.Notification.API Version {Utils.AppVersion}\n" +
                                   $" Directory: {System.Environment.CurrentDirectory} \n" +
                                   $" x64Process: {System.Environment.Is64BitProcess} \n" +
                                   $" OSVersion: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} \n" +
                                   $" UserName: {System.Environment.UserName}");

            var baseDir = env.ContentRootPath;
            AppDomain.CurrentDomain.SetData(Constants.AppBaseDirectory, baseDir);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePages();
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseIpRateLimiting();

            app.MapWhen(context => context.Request.Path == "/", builder =>
            {
                builder.Run(async context =>
                {
                    await context.Response.WriteAsync("Moonglade.Notification.API Version: " + Utils.AppVersion, Encoding.UTF8);
                });
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
