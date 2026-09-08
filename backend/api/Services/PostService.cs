using backend.api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson; 

namespace backend.api.Services
{
    public class PostService
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Post> _postCollection;

        // Constructor to initialize the PostService with MongoDB settings
        public PostService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _userCollection = mongoDatabase.GetCollection<User>(mongoDBSettings.Value.UserCollection);
            _postCollection = mongoDatabase.GetCollection<Post>(mongoDBSettings.Value.PostCollection);
        }

        public async Task CreateOnePostAsync(Post post){
        await _postCollection.InsertOneAsync(post);
        return;
        }

        public async Task<Post?> UpdatePost(string id, Post newPost){
            return await _postCollection.FindOneAndReplaceAsync(x => x.Id == id, newPost);
        }

        public async Task<Post?> GetPostByID(string id){
            return await _postCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }    
        
        public async Task<User?> GetUsByid(string id){
            return await _userCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeletePostAsync(string id){
            FilterDefinition<Post> filter = Builders<Post>.Filter.Eq("Id", id);
            await _postCollection.DeleteOneAsync(filter);
            return;
        }

        public async Task<(List<Post>, List<User>)> Search(string searchQuery){

            // Create filter definitions for searching posts and users
            FilterDefinition<Post> FilterPost = new BsonDocument
            {
                {"title", new BsonDocument("$ne", searchQuery)},
                {"message", new BsonDocument("$regex", searchQuery)}
            };

            FilterDefinition<User> FilterUser = new BsonDocument
            {
                {"name", new BsonDocument("$ne", searchQuery)},
                {"email", new BsonDocument("$regex", searchQuery)}
            };

            // Execute the search queries and retrieve the results
            List<Post> posts = (await _postCollection.FindAsync(FilterPost)).ToList();
            List<User> users = (await _userCollection.FindAsync(FilterUser)).ToList();

            if(posts is null){
                posts = new List<Post>();
            } else if (users is null){
                users = new List<User>();
            }

            return (posts, users);
        }
        public Object Query(List<string> ides, int? queryPage)
        {
            // Create a filter to find posts where the creator is in the provided list of IDs
            var filter = Builders<Post>.Filter.In("creator", ides);

            // Sort the results in descending order based on the "Id" field
            var sort = Builders<Post>.Sort.Descending("Id");

            // Find the posts matching the filter and sort them
            var find = _postCollection.Find(filter).Sort(sort);

            // Determine the current page number, defaulting to 1 if not provided or if it's 0
            int currentPage = queryPage.GetValueOrDefault(1) == 0 ? 1 : queryPage.GetValueOrDefault(1);

            // Set the number of posts to display per page
            int perPage = 3;
            var numberOfPages = find.CountDocuments() / perPage;
            
            return new 
            {
                // Skip the appropriate number of posts based on the current page and limit the results to the specified number per page
                data = find.Skip((currentPage -1) * perPage).Limit(perPage).ToList(),
                numberOfPages,
                currentPage,
            };
        }

    }
}