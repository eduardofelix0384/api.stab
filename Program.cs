using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace api.stab
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(@"
             _       _        _     
            (_)     | |      | |    
  __ _ _ __  _   ___| |_ __ _| |__  
 / _` | '_ \| | / __| __/ _` | '_ \ 
| (_| | |_) | |_\__ \ || (_| | |_) |
 \__,_| .__/|_(_)___/\__\__,_|_.__/ 
      | |                           
      |_|                           
");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseIISIntegration();
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:6000");
                });
    }
}
