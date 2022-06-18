using Microsoft.Extensions.Options;
using PsuedoMediaBackend.Models;

namespace PsuedoMediaBackend.Services {
    public class PostsService {

        public MongoDbService<Post> PostService { get; set; }
        public MongoDbService<PostType> PostTypeService { get; set; }

        public PostsService(IOptions<PsuedoMediaDatabaseSettings> psuedoMediaDatabaseSettings) {
            PostService = new MongoDbService<Post>(psuedoMediaDatabaseSettings);
            PostTypeService = new MongoDbService<PostType>(psuedoMediaDatabaseSettings);
        }
    }
}
