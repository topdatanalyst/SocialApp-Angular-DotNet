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


        // Method to send a message and update unread message count
        public async Task SendMessageAsync(Message msg, string sender, string recever){
            await _messageCollection.InsertOneAsync(msg);

            _ = SetUpdateUnreadedMessageBetweenUsers(sender, recever);
            return;
        }

        // Method to update unread message count between users
        public async Task SetUpdateUnreadedMessageBetweenUsers(string sender, string recever){
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

        // Method to get message count between users
        public async Task<List<Message>> GetMessageByNum(int from, string firstuid, string seconduid){

            // Create filters for messages sent from firstuid to seconduid
            var senderFilter = Builders<Message>.Filter.Eq("sender", firstuid);
            var receverFilter = Builders<Message>.Filter.Eq("recever", seconduid);
            // Create filters for messages sent from seconduid to firstuid
            var senderFilter1 = Builders<Message>.Filter.Eq("recever", firstuid);
            var receverFilter1 = Builders<Message>.Filter.Eq("sender", seconduid);
            
            var combinedFiter = Builders<Message>.Filter.Or(
                Builders<Message>.Filter.And(senderFilter, receverFilter),
                Builders<Message>.Filter.And(senderFilter1, receverFilter1)
            );
            
            // Sort messages in descending order by _id (assuming _id is a unique identifier for messages)
            var sort = Builders<Message>.Sort.Descending("_id");
            var numOfReturningMessages = 8;
            var messages = await _messageCollection
                .Find(combinedFiter)
                .Sort(sort)
                .Skip(from * numOfReturningMessages)
                .Limit(numOfReturningMessages)
                .ToListAsync();
            // Reverse the list of messages to return them in ascending order
            messages.Reverse();

            return messages;
        }

         // Method to mark messages as read between users
         public async Task<List<UnReadedMessages>> GetUserUnreadedmsgs(string userid)
        {
            //
            var filter1 = Builders<UnReadedMessages>.Filter.Eq("MainUserid", userid);
            var filter2 = Builders<UnReadedMessages>.Filter.Eq("IsReaded", false);

            // Combine the filters using the logical AND operator
            var combidedFilter = Builders<UnReadedMessages>.Filter.And(filter1, filter2);
            // Find the unread messages for the specified user and return them as a list
            var urms = await _unReadedmessageCollection.Find(combidedFilter).ToListAsync();

            return urms;
        }

        // Method to mark messages as read between users
         public async Task<bool> MarkMsgsAsReaded(string otheruid, string mainuid)
        {
            // Create a filter to find the document with the specified MainUserid and OtherUserid
            var filter = Builders<UnReadedMessages>.Filter.And(
                Builders<UnReadedMessages>.Filter.Eq(x => x.MainUserid , mainuid),
                Builders<UnReadedMessages>.Filter.Eq(x => x.OtherUserid , otheruid)
            );
           
           // Create an update definition to set IsReaded to true and NumOfUnreadedMessages to 0
            var update = Builders<UnReadedMessages>.Update
                .Set(x => x.IsReaded ,true)
                .Set(x=> x.NumOfUnreadedMessages, 0);
            
            // Specify options for the FindOneAndUpdate operation   
            var options = new FindOneAndUpdateOptions<UnReadedMessages>{
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var result = await _unReadedmessageCollection.FindOneAndUpdateAsync(filter, update, options); 

            if(result == null){
                return false;
            } else {
                return true;
            }

        }


    }
}