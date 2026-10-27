using MongoDB.Bson; 
using MongoDB.Bson.Serialization.Attributes;
using backend.api.Models;

namespace backend.api.Models
{ 
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}
        [BsonElement("title")]
        public string? Title {get; set;}
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Creator {get; set;}
        [BsonElement("message")]
        public string? Message {get; set;}
        [BsonElement("selectedFile")]
        public string? SelectedFile {get; set;}

        [BsonElement("likes")]
        public List<string> Likes {get; set;} = new List<string>{};
        [BsonIgnore] // don't store in mongoDB
        public List<CommentWithUser>? Comments {get; set;}

        [BsonElement("createdAt")]
        public DateTime? CreatedAt {get; set;} = DateTime.Now;
        [BsonIgnore]
        public UsserInfo? User {get; set;}


    }
}