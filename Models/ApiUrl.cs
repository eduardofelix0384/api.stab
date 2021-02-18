using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.stab
{
    public class ApiUrl
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ID { get; set; }
        public string Key { get; set; }
        public string Url { get; set; }
        public bool? IsDefault{get;set;}        
    }
}