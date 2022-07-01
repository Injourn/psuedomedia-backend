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
            PostRating currentRating = await PostRatingService.GetOneByDefinition(x => x.UserId == userId && postId == x.PostId);
            await PostRatingService.DeleteByDefinitionAsync(x => x.UserId == userId && postId == x.PostId);
            if (currentRating == null || currentRating.Value != value) {
                await PostRatingService.CreateAsync(new PostRating() {
                    PostId = postId,
                    UserId = userId,
                    Value = value
                });
            }
        }

        public async Task<long> PostRatings(string PostId) {
            List<PostRating> postRatings = await PostRatingService.GetAllByDefinition(x => x.PostId == PostId);
            long rating = 0;
            postRatings.ForEach(x => {
                rating += x.Value;
            });
            return rating;
        }

        public async Task<int> UserRating(string PostId, string UserId) {
            PostRating postRating = await PostRatingService.GetOneByDefinition(x => x.PostId == PostId && x.UserId == UserId);
            return postRating?.Value ?? 0;
        }
    }
}
