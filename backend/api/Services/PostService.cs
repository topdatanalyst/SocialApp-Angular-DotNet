using backend.api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace backend.api.Services
{
    public class PostService : IPostService
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

        public virtual async Task CreateOnePostAsync(Post post){
        await _postCollection.InsertOneAsync(post);
        return;
        }

        public virtual async Task<Post?> UpdatePost(string id, Post newPost){
            return await _postCollection.FindOneAndReplaceAsync(x => x.Id == id, newPost);
        }

        public virtual async Task<PostResponse?> GetPostByID(string id){
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
        
        public virtual async Task<User?> GetUsByid(string id){
            return await _userCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public virtual async Task DeletePostAsync(string id){
            FilterDefinition<Post> filter = Builders<Post>.Filter.Eq("Id", id);
            await _postCollection.DeleteOneAsync(filter);
            return;
        }

        public virtual async Task<(List<Post>, List<User>)> Search(string searchQuery){

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

        /// <summary>
        /// Query posts created by users whose IDs are in the provided list, with pagination support.
        /// </summary>
        /// <param name="ides"></param>
        /// <param name="queryPage"></param>
        /// <returns></returns>
        public virtual async Task<Object> Query(List<string> ides, int? queryPage)
        {
            // Create a filter to match posts created by users whose IDs are in the provided list
            // Crea un filtro per corrispondere ai post creati dagli utenti i cui ID sono nella lista fornita
            var filterDef = new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("creator", new BsonDocument("$in", new BsonArray(ides.Select(id => ObjectId.Parse(id)).ToList()))),
                new BsonDocument("creator", new BsonDocument("$in", new BsonArray(ides)))
            });

            // Determine the current page for pagination, defaulting to 1 if not provided or if the provided value is 0
            // Determina la pagina corrente per la paginazione, impostando il valore predefinito a 1 se non fornito o se il valore fornito è 0
            int currentPage = queryPage.GetValueOrDefault(1) == 0 ? 1 : queryPage.GetValueOrDefault(1);
            
            // Set the number of posts to display per page for pagination
            // Imposta il numero di post da visualizzare per pagina per la paginazione
            int perPage = 3;
            var total = await _postCollection.CountDocumentsAsync(filterDef);
            var numberOfPages = (int)Math.Ceiling((double)total / perPage);
            
            // Build the aggregation pipeline to retrieve posts with comments and user information, applying pagination
            // Costruisci il pipeline di aggregazione per recuperare i post con commenti e informazioni sull'utente, applicando la paginazione
            var pipeline = new List<BsonDocument>();
            pipeline.Add(new BsonDocument("$match", filterDef));
            pipeline.Add(new BsonDocument("$sort", new BsonDocument("_id", -1)));
            pipeline.Add(new BsonDocument("$skip", (currentPage -1) * perPage));
            pipeline.Add(new BsonDocument("$limit", perPage));

            // Add the stages to retrieve posts with comments and user information
            // Aggiungi le fasi per recuperare i post con commenti e informazioni sull'utente
            pipeline.AddRange(BuildPostWithCommentsAndUserPipeline());

            // Execute the aggregation pipeline and retrieve the list of posts
            // Esegui il pipeline di aggregazione e recupera l'elenco dei post  
            var posts = await _postCollection.Aggregate<PostResponse>(pipeline).ToListAsync();

            return new 
            {
                data = posts,
                numberOfPages,
                currentPage,
            };

        }

        public virtual async Task<Comment> CreateComment(string postId, string userId, string value)
        {
            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                Value = value,
                CreatedAt = DateTime.Now
            };

            await _commentCollection.InsertOneAsync(comment);
            return comment;
        }

        public async Task<bool> DeleteComment(string commentId, string userId, string postCreatorId)
        {
            // Retrieve the comment from the database based on the provided commentId
            var comment = await _commentCollection 
                .Find(x => x.Id == commentId) 
                .FirstOrDefaultAsync(); 
            
            // If the comment is not found, return false
            if (comment == null) return false; 
            // check authorization : user must be coment anthor or psot creator 
            if (comment.UserId != userId && postCreatorId != userId)
            {
                throw new UnauthorizedAccessException("Not authorized to delte this comment");
            }

            var result = await _commentCollection.DeleteOneAsync(x => x.Id == commentId);
            return result.DeletedCount > 0; 
        }

         public async Task<PostResponse?> GetPostWithComments(string postId)
        {
            return await GetPostByID(postId); // uses aggregation
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
                            // lookup utente per ciascun commento
                            // Scopo di questo $lookup è arricchire ciascun commento con le informazioni essenziali dell’autore (nome e immagine).
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
                            // re unwind user obj
                            // "unwind" the user object to get a single user document instead of an array  
                            // "unwind" l'oggetto user per ottenere un singolo documento utente invece di un array
                            // Scopo di questo $unwind è trasformare l’array user prodotto dal $lookup in un singolo oggetto, così da poter accedere direttamente ai campi dell’autore del commento.
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