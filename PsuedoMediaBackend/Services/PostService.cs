using Microsoft.Extensions.Options;
using PsuedoMediaBackend.Models;

namespace PsuedoMediaBackend.Services {
    public class PostsService {

        public MongoDbService<Post> PostService { get; set; }
        public MongoDbService<PostType> PostTypeService { get; set; }
        public MongoDbService<PostRating> PostRatingService { get; set; }

        public PostsService(IOptions<PsuedoMediaDatabaseSettings> psuedoMediaDatabaseSettings) {
            PostService = new MongoDbService<Post>(psuedoMediaDatabaseSettings);
            PostTypeService = new MongoDbService<PostType>(psuedoMediaDatabaseSettings);
            PostRatingService = new MongoDbService<PostRating>(psuedoMediaDatabaseSettings);
        }

        public async Task RatePost(string postId,string userId,int value) {
            await PostRatingService.DeleteByDefinitionAsync(x => x.UserId == userId && postId == x.PostId);
            await PostRatingService.CreateAsync(new PostRating() {
                PostId = postId,
                UserId = userId,
                Value = value
            });
        }

        public async Task<long> PostRatings(string PostId) {
            List<PostRating> postRatings = await PostRatingService.GetAllByDefinition(x => x.PostId == PostId);
            long rating = 0;
            postRatings.ForEach(x => {
                rating += x.Value;
            });
            return rating;
        }
    }
}
