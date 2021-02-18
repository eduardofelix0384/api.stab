using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.stab.Models;
using MongoDB.Driver;

namespace api.stab.Repository
{
    public class RequestTraceRepository
    {
        public static async Task Insert(RequestTraceItem item)
        {
            var client = new MongoClient(Config.RequestTraceConnectionString);
            var db = client.GetDatabase(Config.GetValue("RequestTraceDatabaseName"));
            var collection = db.GetCollection<RequestTraceItem>($"Requests_{DateTime.Now.ToString("yyyyMMdd")}");
            var taskInsert = collection.InsertOneAsync(item);
            var taskDrop = db.DropCollectionAsync($"Requests_{DateTime.Now.AddDays(-7).ToString("yyyyMMdd")}");
            await Task.WhenAll(taskInsert, taskDrop);
        }
    }
}