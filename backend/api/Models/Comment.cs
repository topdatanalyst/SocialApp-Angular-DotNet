using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.api.Models
{
    // 1. base comment model (what's stored in mongoDB)
    public class Comment {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}

        [BsonElement("postId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PostId {get; set;}

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId {get; set;}

        [BsonElement("value")]
        public string? Value {get; set;}

        [BsonElement("createdAt")]
        public DateTime? CreatedAt {get; set;} = DateTime.Now;
    }

    // 2 comment with user details (for api response after aggregation)

    public class CommentWithUser {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}

        [BsonElement("postId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PostId {get; set;}

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId {get; set;}

        [BsonElement("value")]
        public string? Value {get; set;}


        [BsonElement("createdAt")]
        public DateTime? CreatedAt {get; set;} = DateTime.Now;

        public UsserInfo? User {get; set;}
    }

    // 2. user information subset for comment response
    [BsonIgnoreExtraElements]
    public class UsserInfo {
        public string? Name {get; set;}
        public string? ImageUrl {get; set;}
    }

    public class CreateCommentInterface {
        public string? Value {get; set;}
    
    }
}

