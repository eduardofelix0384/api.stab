using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.stab.Tools;
using MongoDB.Driver;

namespace api.stab.Repository
{
    public class BlacklistRepository
    {
        const string CACHE_KEY = "ROUTER_BLACKLIST_CACHE";

        public static async Task<IList<Blacklist>> GetItems()
        {
            IList<Blacklist> items = GetItemsFromRedis();

            if (items == null || items.Count() == 0)
            {
                items = await GetItemsFromDB();

                if(CacheEnabled)
                    RedisCacheManager.SetItemObject(CACHE_KEY, items, new TimeSpan(0, 1, 0));
            }

            return items;
        }

        static IList<Blacklist> GetItemsFromRedis()
        {
            if (CacheEnabled && RedisCacheManager.HasAny(CACHE_KEY))
                return RedisCacheManager.GetItemObject<IList<Blacklist>>(CACHE_KEY);

            return null;
        }

        static async Task<IList<Blacklist>> GetItemsFromDB()
        {
            var client = new MongoClient(Config.DefaultConnectionString);
            var db = client.GetDatabase(Config.DefaultDatabaseName);
            var routes = db.GetCollection<Blacklist>("Blacklist");
            return await routes.Aggregate().ToListAsync();
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