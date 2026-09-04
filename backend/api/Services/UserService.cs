using backend.api.Models;   
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace backend.api.Services
{
    public class UserService
    {
        private readonly IMongoCollection<Users> _usersCollection;

        // Constructor to initialize the UserService with MongoDB settings
        public UserService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _usersCollection = mongoDatabase.GetCollection<Users>(mongoDBSettings.Value.UserCollection);
        }

        // Add methods for CRUD operations here

        // Create a new user in the database    
        public async Task CreateUserAsync(Users user)
        {
            await _usersCollection.InsertOneAsync(user);
            return; 
        }

        public async Task<Users?> GetUserByEmailAsync(string email)
        {
            return await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync();
        }   

        public async Task<Users?> GetUserByIdAsync(string id)
        {
            return await _usersCollection.Find(user => user.Id == id).FirstOrDefaultAsync();
        }   

        public async Task<Users?> UpdateUserAsync(string id, Users updatedUser)
        {
            return await _usersCollection.FindOneAndReplaceAsync(user => user.Id == id, updatedUser);
        }  

        public async Task DeleteUserAsync(string id)
        {
            // Create a filter to find the user by ID and delete it from the collection
            FilterDefinition<Users> filter = Builders<Users>.Filter.Eq(user => user.Id, id);
            await _usersCollection.DeleteOneAsync(filter);
            return;
        }
    }
}

