using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.api.Models
{
    public class Message {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get;set;}
        public string Content {get; set; } = null!;
        public string Sender {get; set; } = null!;
        public string Recever {get; set; } = null!;
    
    }

}   