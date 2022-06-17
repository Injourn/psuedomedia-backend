using Microsoft.AspNetCore.Mvc;
using PsuedoMediaBackend.Filters;
using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Models.ProtocolMessages;
using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class FeedController : ControllerBase {

        readonly MongoDbService<Post> _postService;
        readonly MongoDbService<PostType> _postTypeService;
        readonly AuthenticationService _authenticationService;

        public FeedController(AuthenticationService authenticationService) {
            _postService = new MongoDbService<Post>();
            _postTypeService = new MongoDbService<PostType>();
            _authenticationService = authenticationService;
            _postService.UserId = _authenticationService.ActiveUserId;
        }

        [HttpGet]
        public async Task<List<PostProtocolMessage>> Get() {
            List<PostProtocolMessage> posts = (await _postService.GetAllAsync()).Select(x => PostToProtocolMessage(x).Result).ToList();
            return posts;
        }

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Post>> Get(string id) {
            var post = await _postService.GetByIdAsync(id);

            if (post is null) {
                return NotFound();
            }

            return post;
        }

        [HttpPost]
        [PsuedoMediaAuthentication]
        public async Task<IActionResult> Post(RequestPostProtocolMessage newPost) {
            Post post = new Post() {
                PostText = newPost.PostText,
                PostTypeId = (await _postTypeService.GetAllByDefinition(x => x.Code == newPost.PostTypeCode.ToString())).FirstOrDefault()?.Id,
                ParentPostId = newPost.ParentPostId,
            };
            await _postService.CreateAsync(post);

            return Accepted();
        }

        [HttpPut("{id:length(24)}")]
        [PsuedoMediaAuthentication]
        public async Task<IActionResult> Update(string id, RequestPostProtocolMessage updatedPost) {
            var post = await _postService.GetByIdAsync(id);
            Post newPost = new Post() {
                PostText = updatedPost.PostText,
                PostTypeId = (await _postTypeService.GetAllByDefinition(x => x.Code == updatedPost.PostTypeCode.ToString())).FirstOrDefault()?.Id,
                ParentPostId = updatedPost.ParentPostId,
            };

            if (post is null) {
                return NotFound();
            }

            newPost.Id = post.Id;

            await _postService.UpdateAsync(id, newPost);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        [PsuedoMediaAuthentication]
        public async Task<IActionResult> Delete(string id) {
            var book = await _postService.GetByIdAsync(id);

            if (book is null) {
                return NotFound();
            }

            await _postService.DeleteAsync(id);

            return NoContent();
        }

        private async Task<PostProtocolMessage> PostToProtocolMessage(Post post,bool replyMessages = true) {
            Users user = await _authenticationService.UserService.GetByIdAsync(post.CreatedByUserId);
            if(user == null) {
                user = (await _authenticationService.UserService.GetAllByDefinition(x => x.DisplayName == "UnknownUser")).First();
            }
            List<PostProtocolMessage> replies = new List<PostProtocolMessage>();
            if (replyMessages) {
                replies = (await _postService.GetAllByDefinition(x => x.ParentPostId == post.Id)).Select(x => PostToProtocolMessage(x, false).Result).ToList();
            }
            return new PostProtocolMessage(){
                Message = post.PostText,
                CreatedDate = post.DateCreated,
                UserCreatedName = user.DisplayName,
                Replies = replies,
                Id = post.Id
            };
        }
    }
}
