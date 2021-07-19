using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using truyenthanhServerWeb.ServerMp3;
using truyenthanhServerWeb.Services;

namespace truyenthanhServerWeb
{
    public class CustomDipose
    {
        protected bool bLoopThread = true;
        internal void terminateThread()
        {
            bLoopThread = false;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            //get current OS
            // Save the OS info to an variable
            OperatingSystem os = Environment.OSVersion;
            // Print the OS info to the console
            Console.WriteLine($"platform:       {os.Platform}\n" +
                              $"version:        {os.Version}\n" +
                              $"version string: {os.VersionString}");

            //var udpMp3Server = new UDPServer();

            //Thread hostweb = new Thread(() =>
            //{
            //    //wait ultil UDPServer is done initialize
            //    while (!udpMp3Server.bIsInitalizeDone) ;
            //    CreateHostBuilder(args).Build().Run();
            //});
            //hostweb.Start();

            //udpMp3Server.Run();

            //CreateHostBuilder(args).Build().Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    //webBuilder.UseUrls("http://0.0.0.0:5005", "https://0.0.0.0:5002");
                    //webBuilder.UseUrls("http://0.0.0.0:5000");//, "https://0.0.0.0:5002");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
