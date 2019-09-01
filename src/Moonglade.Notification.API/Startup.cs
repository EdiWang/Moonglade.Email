using System;
using AspNetCoreRateLimit;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Notification.API.Authentication;
using Moonglade.Notification.Core;

namespace Moonglade.Notification.API
{
    public class Startup
    {
        private ILogger<Startup> _logger;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddMemoryCache();

            // Setup document: https://github.com/stefanprodan/AspNetCoreRateLimit/wiki/IpRateLimitMiddleware#setup
            //load general configuration from appsettings.json
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

            // inject counter and rules stores
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services.Configure<AppSettings>(Configuration.GetSection(nameof(AppSettings)));

            services.AddControllers();
            services.AddTransient<IMoongladeNotification, EmailNotification>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                    options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                })
                .AddApiKeySupport(options => { });

            // https://github.com/aspnet/Hosting/issues/793
            // the IHttpContextAccessor service is not registered by default.
            // the clientId/clientIp resolvers use it.
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // configuration (resolvers, counter key builders)
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            services.AddApplicationInsightsTelemetry();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            _logger = logger;
            _logger.LogInformation($"Moonglade.Notification.API Version {Utils.AppVersion}\n" +
                                   "--------------------------------------------------------\n" +
                                   $" Directory: {System.Environment.CurrentDirectory} \n" +
                                   $" x64Process: {System.Environment.Is64BitProcess} \n" +
                                   $" OSVersion: {System.Runtime.InteropServices.RuntimeInformation.OSDescription} \n" +
                                   $" UserName: {System.Environment.UserName} \n" +
                                   "--------------------------------------------------------");

            var baseDir = env.ContentRootPath;
            AppDomain.CurrentDomain.SetData(Constants.AppBaseDirectory, baseDir);

            if (env.IsDevelopment())
            {
                _logger.LogWarning("Moonglade.Notification.API is running in DEBUG.");

                TelemetryConfiguration.CreateDefault().DisableTelemetry = true;
                TelemetryDebugWriter.IsTracingDisabled = true;

                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePages();
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseIpRateLimiting();

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
