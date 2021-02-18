using System;
using Microsoft.Extensions.Configuration;

namespace api.stab
{
    public class Config
    {
        static IConfigurationRoot CurrentConfiguration { get; set; }

        static Config() 
        {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            CurrentConfiguration = builder.Build();
        }

        public static string ApiRouterConnectionString => CurrentConfiguration.GetConnectionString("ApiRouter");
        public static string RequestTraceConnectionString => CurrentConfiguration.GetConnectionString("RequestTrace");

        public static string GetValue(string key) => CurrentConfiguration.GetValue<string>(key);
        public static T GetValue<T>(string key) => CurrentConfiguration.GetValue<T>(key);
    }
}