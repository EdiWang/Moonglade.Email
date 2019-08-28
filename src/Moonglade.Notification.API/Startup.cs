using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
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
            services.Configure<AppSettings>(Configuration.GetSection(nameof(AppSettings)));

            services.AddControllers();
            services.AddTransient<IMoongladeNotification, EmailNotification>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                    options.DefaultChallengeScheme = ApiKeyAuthenticationOptions.DefaultScheme;
                })
                .AddApiKeySupport(options => { });
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
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePages();
                app.UseHsts();
                app.UseHttpsRedirection();
            }

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
