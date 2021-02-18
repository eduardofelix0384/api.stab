using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.stab
{
    public class ApiRoute
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }
        public string Name { get; set; }
        public string Pattern { get; set; }
        public bool EnableCache{get;set;}        
        public int TTL { get; set; }
        public string BaseUrlKey {get;set;}        
        public bool Activated { get; set; }
        public int ItemOrder { get; set; }
        public bool? Cacheable { get; set; }
    }
}