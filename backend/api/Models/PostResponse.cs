using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace backend.api.Models
{
        public class PostResponse {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}
        [BsonElement("title")]
        public string? Title {get; set;}

        [BsonRepresentation(BsonType.ObjectId)]
        public string? Creator {get; set;}
        [BsonElement("message")]
        public string? Message {get; set;}
        public string? SelectedFile {get; set;}

        public List<string> Likes {get; set;} = new List<string>{};

        public List<CommentWithUser>? Comments {get; set;} 

        public DateTime? CreatedAt {get; set;} 

        public UsserInfo? User {get; set;}

    }


}


