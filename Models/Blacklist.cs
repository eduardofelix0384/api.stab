using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.stab
{
    public class Blacklist
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }
        public string IP { get; set; }
    }
}