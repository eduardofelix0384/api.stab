using System;
using System.Collections.Generic;
using System.Linq;
using api.stab.Tools;
using MongoDB.Driver;

namespace api.stab.Repository
{
    public class RoutesRepository
    {
        const string CACHE_KEY = "API_ROUTES_CACHE";

        public static IList<ApiRoute> GetRoutes()
        {
            IList<ApiRoute> items = GetRoutesFromRedis();

            if (items == null || items.Count() == 0)
            {
                items = GetRoutesFromDB();

                if(CacheEnabled)
                    RedisCacheManager.SetItemObject(CACHE_KEY, items, new TimeSpan(0, 1, 0));
            }

            return items;
        }

        static IList<ApiRoute> GetRoutesFromRedis()
        {
            if (CacheEnabled && RedisCacheManager.HasAny(CACHE_KEY))
                return RedisCacheManager.GetItemObject<IList<ApiRoute>>(CACHE_KEY);

            return null;
        }

        static IList<ApiRoute> GetRoutesFromDB()
        {
            var client = new MongoClient(Config.DefaultConnectionString);
            var db = client.GetDatabase(Config.DefaultDatabaseName);
            var routes = db.GetCollection<ApiRoute>("Routes");
            return routes.Find(r => r.Activated).SortBy(r => r.ItemOrder).ToList();
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