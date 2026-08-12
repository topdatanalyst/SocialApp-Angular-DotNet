using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace backend.api.Models
{
    public class Users
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("username")]
        public string Username { get; set; } = null!;

        [BsonElement("email")]
        public string Email { get; set; } = null!;

        [BsonElement("password")]
        public string Password { get; set; } = null!;

        [BsonElement("imageUrl")    ]
        public string ImageUrl { get; set; } = null!;   

        [BsonElement("bio")]
        public string Bio { get; set; } = null!;    

        [BsonElement("followers")]
        public List<string> Followers { get; set; } = new List<string>();

        [BsonElement("following")] 
        public List<string> Following { get; set; } = new List<string>();   

        // Decrypts a Base64 encoded string
        internal string DecryptPasswordBase64(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        // Encrypts a string to Base64
        internal string EncryptPasswordBase64(string data)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(plainTextBytes);
        }

    }
}