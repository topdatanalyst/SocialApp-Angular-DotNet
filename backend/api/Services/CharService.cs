using backend.api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace backend.api.Services
{
    
    public class ChatService
    {
        private readonly IMongoCollection<UnReadedMessages> _unReadedmessageCollection;
        private readonly IMongoCollection<Message> _messageCollection;
        private readonly IMongoCollection<User> _userCollection;

        public ChatService(IOptions<MongoDBSettings> mongoDBSettings){
            MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionString);
            IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _unReadedmessageCollection = database.GetCollection<UnReadedMessages>(mongoDBSettings.Value.UnMessageCollection);
            _messageCollection = database.GetCollection<Message>(mongoDBSettings.Value.MessageCollection);
            _userCollection = database.GetCollection<User>(mongoDBSettings.Value.UserCollection);
        }

        public async void setUpdateUnreadedMessageBetweenUsers(string sender, string recever){
            // Create a filter to find the document with the specified MainUserid and OtherUserid
            var filter = Builders<UnReadedMessages>.Filter.And(
                Builders<UnReadedMessages>.Filter.Eq(x => x.MainUserid, recever),
                Builders<UnReadedMessages>.Filter.Eq(x => x.OtherUserid, sender)
            );
            
            // Create an update definition to set IsReaded to false and increment NumOfUnreadedMessages by 1
            var update = Builders<UnReadedMessages>.Update
                    .Set(x => x.IsReaded, false)
                    .Inc(x => x.NumOfUnreadedMessages, 1);

            // Specify options for the FindOneAndUpdate operation
            var options = new FindOneAndUpdateOptions<UnReadedMessages>{
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            // Perform the FindOneAndUpdate operation
            var result = await _unReadedmessageCollection.FindOneAndUpdateAsync(filter, update, options);

            // If the result is null, it means no document was found, so we create a new one
            if(result == null ){
                var newUnrededMsg = new UnReadedMessages{
                    MainUserid = recever,
                    OtherUserid = sender,
                    IsReaded = false,
                    NumOfUnreadedMessages = 1
                };
                
                await _unReadedmessageCollection.InsertOneAsync(newUnrededMsg);
            }
        }

    }
}