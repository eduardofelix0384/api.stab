using System;
using System.Collections.Generic;
using System.Linq;
using api.stab.Tools;
using MongoDB.Driver;

namespace api.stab.Repository
{
    public class ApiUrlsRepository
    {
        const string CACHE_KEY = "API_ROUTER_URLS_CACHE";

        public static IList<ApiUrl> GetUrls()
        {
            IList<ApiUrl> items = GetUrlsFromRedis();

            if (items == null || items.Count() == 0)
            {
                items = GetUrlsFromDB();

                if(CacheEnabled)
                    RedisCacheManager.SetItemObject(CACHE_KEY, items, new TimeSpan(0, 1, 0));
            }

            return items;
        }

        static IList<ApiUrl> GetUrlsFromRedis()
        {
            if (CacheEnabled && RedisCacheManager.HasAny(CACHE_KEY))
                return RedisCacheManager.GetItemObject<IList<ApiUrl>>(CACHE_KEY);

            return null;
        }

        static IList<ApiUrl> GetUrlsFromDB()
        {
            var client = new MongoClient(Config.ApiRouterConnectionString);
            var db = client.GetDatabase(Config.GetValue("ApiRouterDatabaseName"));
            var routes = db.GetCollection<ApiUrl>("ApiUrls");
            return routes.Aggregate().ToList();
        }

        static bool CacheEnabled
        {
            get
            {
                return Config.GetValue<bool>("CacheEnabled");
            }
        }
    }
}