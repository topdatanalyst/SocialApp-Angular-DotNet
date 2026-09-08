using MongoDB.Bson; 
using MongoDB.Bson.Serialization.Attributes;

namespace backend.api.Models
{ 
    public class Post
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}
        [BsonElement("title")]
        public string? Title {get; set;}
        [BsonElement("creator")]
        public string? Creator {get; set;}
        [BsonElement("message")]
        public string? Message {get; set;}
        [BsonElement("selectedFile")]
        public string? SelectedFile {get; set;}

        [BsonElement("likes")]
        public List<string> Likes {get; set;} = new List<string>{};
        [BsonElement("comments")]
        public List<string> Comments {get; set;} = new List<string>{};

        [BsonElement("createdAt")]
        public DateTime? CreatedAt {get; set;} = DateTime.Now;

    }
}