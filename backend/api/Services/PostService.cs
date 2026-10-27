using backend.api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace backend.api.Services
{
    public class PostService
    {
        private readonly IMongoCollection<User> _userCollection;
        private readonly IMongoCollection<Post> _postCollection;

        private readonly IMongoCollection<Comment> _commentCollection;  

        // Constructor to initialize the PostService with MongoDB settings
        public PostService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _userCollection = mongoDatabase.GetCollection<User>(mongoDBSettings.Value.UserCollection);
            _postCollection = mongoDatabase.GetCollection<Post>(mongoDBSettings.Value.PostCollection);
            _commentCollection = mongoDatabase.GetCollection<Comment>(mongoDBSettings.Value.CommentCollection);
        }

        public async Task CreateOnePostAsync(Post post){
        await _postCollection.InsertOneAsync(post);
        return;
        }

        public async Task<Post?> UpdatePost(string id, Post newPost){
            return await _postCollection.FindOneAndReplaceAsync(x => x.Id == id, newPost);
        }

        public async Task<PostResponse?> GetPostByID(string id){
             var objectId =  ObjectId.Parse(id);
             // Create a filter to find the post by its ObjectId
             var filter = Builders<Post>.Filter.Eq("_id", objectId);
             // Build an aggregation pipeline to retrieve the post along with its comments and user information
             // Costruisci un pipeline di aggregazione per ottenere il post insieme ai commenti e alle informazioni dell'utente
             var pipeline = BuildPostWithCommentsAndUserPipeline(filter);
             // Execute the aggregation pipeline and retrieve the first matching post
             // Esegui il pipeline di aggregazione e recupera il primo post corrispondente
             var result = await _postCollection.Aggregate<PostResponse>(pipeline).FirstOrDefaultAsync();
             return result;
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

             /* // Create filter definitions for searching posts and users
            FilterDefinition<Post> FilterPost = new BsonDocument
            {
                // Search for posts where the title is not equal to the search query and the message contains the search query
                {"title", new BsonDocument("$ne", searchQuery)},
                {"message", new BsonDocument("$regex", searchQuery)}
            };

            FilterDefinition<User> FilterUser = new BsonDocument
            {
                // Search for users where the username is not equal to the search query and the email contains the search query
                {"username", new BsonDocument("$ne", searchQuery)},
                {"email", new BsonDocument("$regex", searchQuery)}
            };
            */
             
            // Definizione dei filtri per la ricerca di Post e Utenti
            var filterPost = Builders<Post>.Filter.Ne(p => p.Title, searchQuery) &
                 Builders<Post>.Filter.Regex(p => p.Message, new BsonRegularExpression(searchQuery, "i"));

            var filterUser = Builders<User>.Filter.Ne(u => u.Username, searchQuery) &
                 Builders<User>.Filter.Regex(u => u.Email, new BsonRegularExpression(searchQuery, "i"));

            // Execute the search queries and retrieve the results
            List<Post> posts = (await _postCollection.FindAsync(filterPost)).ToList();
            List<User> users = (await _userCollection.FindAsync(filterUser)).ToList();

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

        private List<BsonDocument> BuildPostWithCommentsAndUserPipeline(FilterDefinition<Post>? matchFilter = null)
      {
                var pipeline = new List<BsonDocument>();
            
                // 1 match stage (if filter provided)
                // 1. Fase di corrispondenza (se viene fornito un filtro)
                if (matchFilter != null)
                {
                    // Render the match filter to a BsonDocument and add it to the pipeline
                    // Renderizza il filtro di corrispondenza in un BsonDocument e aggiungilo al pipeline
                    // { "$match": { "author": "Mario", "likes": { "$gt": 10 } } }
                    pipeline.Add(new BsonDocument("$match", matchFilter.Render( new RenderArgs<Post>(BsonSerializer.LookupSerializer<Post>(), BsonSerializer.SerializerRegistry))));
                }

                // 2 lookup user who created the post
                // 2. Fase di lookup per ottenere l'utente che ha creato il post
                // Per ogni post, va a cercare nella collezione users l’utente che lo ha creato e lo “attacca” al documento del post.
                //{ "_id": 1, "title": "Ciao", "creator": ObjectId("abc123") }
                pipeline.Add(new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "users" },
                    { "localField", "creator" },
                    { "foreignField", "_id" },
                    { "as", "user" }
                }));

                // 3 unwind the user array.
                // 3. Fase di unwind per l'array degli utenti..
                // lo $unwind “srotola” l’array user prodotto dal $lookup, trasformandolo da array di un elemento a singolo oggetto. 
                // In più, grazie a preserveNullAndEmptyArrays: true, non scarta i post senza utente
                pipeline.Add(new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$user" },
                    { "preserveNullAndEmptyArrays", true }
                }));

                // 4 lookup comments (with nested user lookup )
                // 4. Fase di lookup per i commenti (con lookup annidato per gli utenti)
                // quarto stage: un $lookup più avanzato, in forma “con sub-pipeline”.
                //  Per ogni post recupera tutti i suoi commenti, li ordina dal più recente, e per ciascun commento aggiunge i dati essenziali dell’autore (nome e immagine). 
                // Il risultato finisce nel campo comments del post.
                pipeline.Add(new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "comments" },
                    { "let", new BsonDocument("postId", "$_id") },
                    { "pipeline", new BsonArray
                        {
                            // match comments for this post
                            new BsonDocument("$match", new BsonDocument("$expr", 
                            new BsonDocument("$eq", new BsonArray{"$postId", "$$postId"}))),

                            // Sort comments by date (newest first)
                            new BsonDocument("$sort", new BsonDocument("createdAt", -1)),

                            // lookup user for each comment
                            new BsonDocument("$lookup", new BsonDocument
                            {
                                { "from", "users" },
                                { "let", new BsonDocument("uid", "$userId") },
                                { "pipeline", new BsonArray
                                    {
                                        new BsonDocument("$match", new BsonDocument("$expr", 
                                        new BsonDocument("$eq", new BsonArray{"$_id", "$$uid"}))),
                                        new BsonDocument("$project", new BsonDocument
                                        {
                                            { "_id", 0 },
                                            { "name", 1 },
                                            { "imageUrl", 1 }
                                        })
                                    }
                                },
                                { "as", "user" }
                            }),
                            // reunwind user obj
                            new BsonDocument("$unwind", new BsonDocument
                                    {
                                        { "path", "$user" },
                                        { "preserveNullAndEmptyArrays", true }
                            }),
                            // Project comment fields 
                            new BsonDocument("$project", new BsonDocument
                            {
                                { "_id", 1 },
                                { "postId", 1 },
                                { "userId", 1 },
                                { "value", 1 },
                                { "createdAt", 1 },
                                { "user", 1}
                            })
                        }
                    },
                    { "as", "comments" }
                }));
            
            // 5. Final Projection  
            // 5. Proiezione finale
            pipeline.Add(new BsonDocument("$project", new BsonDocument
            {
                { "_id", 1 },
                { "creator", 1 },
                { "title", 1 },
                { "message", 1 },
                {"selectedFile", 1 },
                {"likes", 1 },
                { "createdAt", 1 },
                { "comments", 1 },
                { "user.name", 1 },
                { "user.imageUrl", 1 }
            }));


            return pipeline;
        }

    }
}