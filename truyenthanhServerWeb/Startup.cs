using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using truyenthanhServerWeb.Models;
using truyenthanhServerWeb.Services;

namespace truyenthanhServerWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // requires using Microsoft.Extensions.Options
            //get setting from file appsettings.json

            services.Configure<TruyenthanhDatabaseSettings>(
                Configuration.GetSection(nameof(TruyenthanhDatabaseSettings)));

            services.AddSingleton<ITruyenthanhDatabaseSettings>(sp =>
                sp.GetRequiredService<IOptions<TruyenthanhDatabaseSettings>>().Value);

            services.AddServerSideBlazor();
            services.AddControllersWithViews();

            //DI for AccountService, DeviceService
            services.AddSingleton<AccountService>();
            services.AddSingleton<DeviceService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapBlazorHub();

            });
        }
    }
}
