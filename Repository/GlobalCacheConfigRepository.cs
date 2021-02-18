using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.stab.Tools;
using MongoDB.Driver;

namespace api.stab.Repository
{
    public class GlobalCacheConfigRepository
    {
        const string CACHE_KEY = "ROUTER_GLOBALCACHECONFIG_CACHE";

        public static async Task<GlobalCacheConfig> GetCurrent()
        {
            GlobalCacheConfig item = GetItemsFromRedis();

            if (item == null)
            {
                item = await GetItemsFromDB();

                if(CacheEnabled)
                    RedisCacheManager.SetItemObject(CACHE_KEY, item, new TimeSpan(0, 1, 0));
            }

            return item;
        }

        static GlobalCacheConfig GetItemsFromRedis()
        {
            if (CacheEnabled && RedisCacheManager.HasAny(CACHE_KEY))
                return RedisCacheManager.GetItemObject<GlobalCacheConfig>(CACHE_KEY);

            return null;
        }

        static async Task<GlobalCacheConfig> GetItemsFromDB()
        {
            var client = new MongoClient(Config.DefaultConnectionString);
            var db = client.GetDatabase(Config.DefaultDatabaseName);
            var routes = db.GetCollection<GlobalCacheConfig>("GlobalCacheConfig");
            return await routes.Aggregate().FirstOrDefaultAsync();
        }

        static bool CacheEnabled
        {
            get
            {
                return Config.EnableDataCache;
            }
        }
    }
}