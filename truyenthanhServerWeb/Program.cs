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
            Thread hostweb = new Thread(() =>
            {
                Thread.Sleep(5000);
                CreateHostBuilder(args).Build().Run();
            });
            hostweb.Start();

            var udpMp3Server = new UDPServer();
            udpMp3Server.Run();

            //CreateHostBuilder(args).Build().Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
