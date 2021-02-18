using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using StackExchange.Redis;

namespace api.stab.Tools
{
    public class RedisCacheManager
    {
        private static IDatabase Db { get; set; }
        private static IServer Server { get; set; }
        private static TimeSpan DefaultTimeout { get; set; }

        private static double DefaultTimeoutDaysValue = Convert.ToInt64(7);

        static RedisCacheManager()
        {
            Connect();
        }

        public static bool IsOffline
        {
            get { return Db == null && Server == null; }
        }

        private static void ResetValues()
        {
            Db = null;
            Server = null;
        }

        public static void Connect()
        {
            try
            {
                var timeoutConnection = Convert.ToInt32(60);

                var configurationOptions = new ConfigurationOptions()
                {
                    ConnectTimeout = timeoutConnection,
                    SyncTimeout = timeoutConnection,
                    EndPoints =
                {
                    { Config.RedisHost, Config.RedisPort }
                }
                };

                var Connection = ConnectionMultiplexer.Connect(configurationOptions);
                Db = Connection.GetDatabase();
                Server = Connection.GetServer(Connection.GetEndPoints().FirstOrDefault());
                DefaultTimeout = TimeSpan.FromDays(DefaultTimeoutDaysValue);
            }
            catch
            {
                ResetValues();
            }
        }

        public static bool SetItemString(string key, string value, TimeSpan limit)
        {
            try
            {
                return Db.StringSet(key, value, limit);
            }
            catch
            {
                ResetValues();
                return true;
            }
        }

        public static bool SetItemObject(string key, object value, TimeSpan limit)
        {
            try
            {
                var serializedObject = JsonSerializer.Serialize(value);
                return SetItemString(key, serializedObject, limit);
            }
            catch
            {
                ResetValues();
                return true;
            }
        }
        
        public static string GetItemString(string key)
        {
            try
            {
                return Db.StringGet(key);
            }
            catch
            {
                ResetValues();
                return null;
            }
        }

        public static T GetItemObject<T>(string key)
        {
            var s = GetItemString(key);
            if (string.IsNullOrEmpty(s))
                return default(T);            

            return JsonSerializer.Deserialize<T>(s);
        }

        public static bool HasAny(string key)
        {
            try
            {
                return Db.KeyExists(key);
            }
            catch
            {
                ResetValues();
                return false;
            }
        }

        private static List<RedisValue> GetObjects(IDatabase db, List<RedisKey> keys)
        {
            try
            {
                var lista = new List<RedisValue>();
                foreach (var key in keys)
                    lista.Add(db.StringGet(key));

                return lista;
            }
            catch
            {
                ResetValues();
                return null;
            }
        }

        private static List<RedisKey> GetKeysList(IServer server, string value)
        {
            try
            {
                return server.Keys(pattern: value).ToList();
            }
            catch
            {
                ResetValues();
                return null;
            }
        }

        public static List<RedisKey> GetAllKeys()
        {
            try
            {
                return GetKeysList(Server, "*");
            }
            catch
            {
                ResetValues();
                return null;
            }
        }
    }
}