using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.api.Models
{
    public class Notification {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id {get;set;} = null!;
        public string Deatils {get;set;} = null!;
        public string Mainuid {get;set;} = null!;
        public string Targetid {get;set;} = null!;

        public bool IsReaded {get; set;} = false;
        public DateTime? CreatedAt {get; set;} = DateTime.Now;

        public UserIn user {get;set;} = null!;
    }

    public class UserIn {
         public string Name {get;set;} = null!;
        public string Avatar {get;set;} = null!;
    }
}