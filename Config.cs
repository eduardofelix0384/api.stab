using System;
using dotenv.net;
using dotenv.net.Utilities;

namespace api.stab
{
    public class Config
    {
        public static string DefaultConnectionString => GetEnvVariable("API_STAB_DEFAULT_CONNECTIONSTRING");
        public static string DefaultDatabaseName => GetEnvVariable("API_STAB_DEFAULT_DATABASE_NAME");
        public static string TraceConnectionString => GetEnvVariable("API_STAB_TRACE_CONNECTIONSTRING");
        public static string TraceDatabaseName => GetEnvVariable("API_STAB_TRACE_DATABASE_NAME");
        public static string RedisHost => GetEnvVariable("API_STAB_REDIS_HOST");
        public static int RedisPort => GetEnvVariable<int>("API_STAB_REDIS_PORT");
        public static bool EnableLog => GetEnvVariable<bool>("API_STAB_ENABLE_LOG");
        public static bool EnableDataCache => GetEnvVariable<bool>("API_STAB_ENABLE_DATA_CACHE");
        public static bool EnableTrace => true;
        public static string BaseUrl => GetEnvVariable("API_STAB_BASE_URL");

        static T GetEnvVariable<T>(string key)
        {
            var value = GetEnvVariable(key);

            if(String.IsNullOrEmpty(value))
                return default(T);
            else
                return (T)Convert.ChangeType(value, typeof(T));
        }

        static string GetEnvVariable(string key)
        {
            DotEnv.AutoConfig();

            var envReader = new EnvReader();
            string value = null;
            
            if(!envReader.TryGetStringValue(key, out value))
            {
                value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Machine);

                if(String.IsNullOrEmpty(value))
                {
                    value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.User);

                    if(String.IsNullOrEmpty(value))
                        value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
                }
            }

            return String.IsNullOrEmpty(value) ? null : value;
        }
    }
}