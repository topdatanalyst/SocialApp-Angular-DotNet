namespace backend.api.Models
{
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;   
        public string UserCollection { get; set; } = null!; 
    }
}