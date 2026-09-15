using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.api.Models
{
    public class UnReadedMessages {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get;set;}
        public string MainUserid {get; set; } = null!;
        public string OtherUserid {get; set; } = null!;
        public bool IsReaded {get; set; } = false;
        public int NumOfUnreadedMessages {get; set; } = 0;
    
    }
}