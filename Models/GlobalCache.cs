using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.stab
{
    public class GlobalCacheConfig
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }
        public bool Enabled { get; set; }
        public int TTL { get; set; }
    }
}