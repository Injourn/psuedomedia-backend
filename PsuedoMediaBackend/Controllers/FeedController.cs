using Microsoft.AspNetCore.Mvc;
using PsuedoMediaBackend.Filters;
using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Models.ProtocolMessages;
using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class FeedController : ControllerBase {

        readonly PostsService _postsService;
        readonly AuthenticationService _authenticationService;

        public FeedController(AuthenticationService authenticationService, PostsService postsService) {            
            _authenticationService = authenticationService;
            _postsService = postsService;
            _postsService.PostService.UserId = _authenticationService.ActiveUserId;
        }

        [HttpGet]
        public async Task<List<PostProtocolMessage>> Get() {
            List<PostProtocolMessage> posts = (await _postsService.PostService.GetAllAsync()).Select(x => PostToProtocolMessage(x).Result).ToList();
            return posts;
        }

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Post>> Get(string id) {
            var post = await _postsService.PostService.GetByIdAsync(id);

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
                PostTypeId = (await _postsService.PostTypeService.GetAllByDefinition(x => x.Code == newPost.PostTypeCode.ToString())).FirstOrDefault()?.Id,
                ParentPostId = newPost.ParentPostId,
            };
            await _postsService.PostService.CreateAsync(post);

            return NoContent();
        }

        [HttpPut("{id:length(24)}")]
        [PsuedoMediaAuthentication]
        public async Task<IActionResult> Update(string id, RequestPostProtocolMessage updatedPost) {
            var post = await _postsService.PostService.GetByIdAsync(id);
            Post newPost = new Post() {
                PostText = updatedPost.PostText,
                PostTypeId = (await _postsService.PostTypeService.GetAllByDefinition(x => x.Code == updatedPost.PostTypeCode.ToString())).FirstOrDefault()?.Id,
                ParentPostId = updatedPost.ParentPostId,
            };

            if (post is null) {
                return NotFound();
            }

            newPost.Id = post.Id;

            await _postsService.PostService.UpdateAsync(id, newPost);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        [PsuedoMediaAuthentication]
        public async Task<IActionResult> Delete(string id) {
            var book = await _postsService.PostService.GetByIdAsync(id);

            if (book is null) {
                return NotFound();
            }

            await _postsService.PostService.DeleteAsync(id);

            return NoContent();
        }

        private async Task<PostProtocolMessage> PostToProtocolMessage(Post post,bool replyMessages = true) {
            Users user = await _authenticationService.UserService.GetByIdAsync(post.CreatedByUserId);
            if(user == null) {
                user = (await _authenticationService.UserService.GetAllByDefinition(x => x.DisplayName == "UnknownUser")).First();
            }
            List<PostProtocolMessage> replies = new List<PostProtocolMessage>();
            if (replyMessages) {
                replies = (await _postsService.PostService.GetAllByDefinition(x => x.ParentPostId == post.Id)).Select(x => PostToProtocolMessage(x, false).Result).ToList();
            }
            return new PostProtocolMessage(){
                Message = post.PostText,
                CreatedDate = post.DateCreated,
                UserCreatedName = user.DisplayName,
                UserCreatedById = user.Id,
                Replies = replies,
                Id = post.Id
            };
        }
    }
}
