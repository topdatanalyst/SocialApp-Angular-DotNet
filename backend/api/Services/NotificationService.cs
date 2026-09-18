using backend.api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace backend.api.Services
{
    public class NotificationService
    {
        private readonly IMongoCollection<Notification> _notificationCollection;

        public NotificationService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _notificationCollection = mongoDatabase.GetCollection<Notification>(mongoDBSettings.Value.NotificationCollection);
        }

   
        public async Task CreateNotification(Notification notification){

            await _notificationCollection.InsertOneAsync(notification);
            // TODO CAll RealTime Notficiation grpc
            return; 
        }

        public async Task<List<Notification>> GetUserNotification(string uid){

            var filter = Builders<Notification>.Filter
                        .Regex("mainuid", new BsonRegularExpression(uid, "i"));
            
            var notifiactions = await _notificationCollection
                        .Find(filter)
                        .SortByDescending(p => p.CreatedAt)
                        .ToListAsync();

            return notifiactions;
        }

        public  async Task<bool> MarkNotificationsAsReaded(string uid){

            var filter = Builders<Notification>.Filter
                        .Regex("mainuid", new BsonRegularExpression(uid, "i"));
            var update = Builders<Notification>.Update
                    .Set(x => x.IsReaded, true);

            var result = await _notificationCollection.UpdateManyAsync(filter, update);

            if (result == null) {return false;} else {return true;} 
        }
    }

        
}
