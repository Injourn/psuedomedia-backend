using MongoDB.Driver;
using PsuedoMediaBackend.Models;

namespace PsuedoMediaBackend.Services {
    public class MongoDbService<T> where T : notnull {
        readonly IMongoCollection<T> _mongoCollection;
        public string? UserId { get; set; }

        public MongoDbService() {
            MongoClient mongoClient = new MongoClient("DataBaseConnectString");
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase("DataBaseName");

            _mongoCollection = mongoDatabase.GetCollection<T>(nameof(T).ToLower());
        }


        public async Task CreateAsync(T dataBaseItem) {
            if(dataBaseItem == null) {
                throw new ArgumentNullException(nameof(dataBaseItem));
            }
            if(dataBaseItem is CreatableEntity) {
                CreatableEntity creatableEntity = dataBaseItem as CreatableEntity;
                creatableEntity.DateCreated = DateTime.Now;
                creatableEntity.CreatedByUserId = UserId;
            }
            if(dataBaseItem is FadingEntity) {
                FadingEntity fadingEntity = dataBaseItem as FadingEntity;
                fadingEntity.RemoveDate = DateTime.Now.AddDays(7);
            }

            await _mongoCollection.InsertOneAsync(dataBaseItem);
        }
    }
}
