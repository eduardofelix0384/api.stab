using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.stab.Models
{
    public class RequestTraceItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }
        public string Url { get; set; }
        public string CleanUrl { get; set; }
        public string TargetUrl { get; set; }
        public string Method { get; set; }
        public int StatusCode { get; set; }
        public string RouterServer { get; set; }
        public string ApiServer { get; set; }
        public string ApiVersion { get; set; }
        public string RequestBody { get; set; }
        public string RequestHeaders { get; set; }
        public string ResponseHeaders { get; set; }
        public string ResponseBody { get; set; }
        public double Duration { get; set; }
        public string IP { get; set; }
        public string UserAgent { get; set; }
        public bool Cache { get; set; }
        public string CacheKey { get; set; }
        public string RouterException { get; set; }
        public bool Blacklist { get; set; }
        public Int64 RequestTimeStamp { get; set; }
    }
}