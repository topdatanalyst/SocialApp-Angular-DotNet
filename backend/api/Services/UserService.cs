using backend.api.Models;   
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace backend.api.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _userCollection;

        // Constructor to initialize the UserService with MongoDB settings
        public UserService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _userCollection = mongoDatabase.GetCollection<User>(mongoDBSettings.Value.UserCollection);
        }

        // Add methods for CRUD operations here

        // Create a new user in the database    
        public async Task CreateUserAsync(User user)
        {
            await _userCollection.InsertOneAsync(user);
            return; 
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userCollection.Find(user => user.Email == email).FirstOrDefaultAsync();
        }   

        public async Task<User?> GetUserByIdAsync(string id)
        {
            return await _userCollection.Find(user => user.Id == id).FirstOrDefaultAsync();
        }   

        public async Task<User?> UpdateUserAsync(string id, User updatedUser)
        {
            return await _userCollection.FindOneAndReplaceAsync(user => user.Id == id, updatedUser);
        }  

        public async Task DeleteUserAsync(string id)
        {
            // Create a filter to find the user by ID and delete it from the collection
            FilterDefinition<User> filter = Builders<User>.Filter.Eq(user => user.Id, id);
            await _userCollection.DeleteOneAsync(filter);
            return;
        }
    }
}

