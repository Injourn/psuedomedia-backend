using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PsuedoMediaBackend.Models;

namespace PsuedoMediaBackend.Services {
    public class MongoDbService<T> where T : BaseEntity{
        readonly IMongoCollection<T> _mongoCollection;
        public string? UserId { get; set; }

        public MongoDbService(IOptions<PsuedoMediaDatabaseSettings> psuedoMediaDatabaseSettings) {
            //Using local database for testing purposes
            MongoClient mongoClient = new MongoClient(psuedoMediaDatabaseSettings.Value.ConnectionString);
            IMongoDatabase mongoDatabase = mongoClient.GetDatabase(psuedoMediaDatabaseSettings.Value.DatabaseName);

            _mongoCollection = mongoDatabase.GetCollection<T>(typeof(T).Name.ToLower());
        }

        public async Task<List<T>> GetAllAsync() =>
            await _mongoCollection.Find(_ => true).ToListAsync();

        public async Task<T> GetByIdAsync(string id) =>
            await _mongoCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task<T> GetOneByDefinition(System.Linq.Expressions.Expression<Func<T, bool>> filter) =>
            await _mongoCollection.Find(filter).FirstOrDefaultAsync();

        public async Task<T> GetByCode(string code) {
            return await GetOneByDefinition(x => (x as DbEnumeration).Code == code);
        }

        public long GetCountByDefinition(System.Linq.Expressions.Expression<Func<T, bool>> filter) =>
            _mongoCollection.CountDocuments(filter);

        public async Task<List<T>> GetSomeByDefinition(System.Linq.Expressions.Expression<Func<T,bool>> filter,int offset = 0, int limit = 10) =>
            await _mongoCollection.Find(filter).Limit(limit).Skip(offset).ToListAsync();

        public async Task<List<T>> GetSomeByDefinition(System.Linq.Expressions.Expression<Func<T, bool>> filter, System.Linq.Expressions.Expression<Func<T, object>> sort, int offset = 0, int limit = 10) =>
            await _mongoCollection.Find(filter).SortByDescending(sort).Limit(limit).Skip(offset).ToListAsync();

        public async Task<List<T>> GetAllByDefinition(System.Linq.Expressions.Expression<Func<T, bool>> filter) =>
            await _mongoCollection.Find(filter).ToListAsync();

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

        public async Task UpdateAsync(string id, T databaseItem) =>
            await _mongoCollection.ReplaceOneAsync(x => x.Id == id, databaseItem);
        public async Task UpdateByDefinitionAsync(System.Linq.Expressions.Expression<Func<T, bool>> filter, T databaseItem) =>
            await _mongoCollection.ReplaceOneAsync(filter, databaseItem);

        public async Task DeleteAsync(string id) =>
            await _mongoCollection.DeleteOneAsync(x => x.Id == id);

        public async Task DeleteByDefinitionAsync(System.Linq.Expressions.Expression<Func<T, bool>> filter) =>
            await _mongoCollection.DeleteOneAsync(filter);
    }
}
