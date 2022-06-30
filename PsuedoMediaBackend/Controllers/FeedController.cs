using Microsoft.AspNetCore.Authorization;
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
        readonly AccountService _accountService;
        readonly AttachmentService _attachmentService;

        const string fileDirectory = "PostMedia";

        public FeedController(AuthenticationService authenticationService, PostsService postsService, AccountService accountService,AttachmentService attachmentService) {            
            _authenticationService = authenticationService;
            _postsService = postsService;
            _postsService.PostService.UserId = _authenticationService.ActiveUserId;
            _accountService = accountService;            
            _attachmentService = attachmentService;
        }

        [HttpGet, PsuedoMediaAuthentication, AllowAnonymous]
        public async Task<List<PostProtocolMessage>> Get() {
            PostType postType = await _postsService.PostTypeService.GetByCode(PostTypeEnum.POST.ToString());
            List<PostProtocolMessage> posts = (await _postsService.PostService.GetSomeByDefinition(x => x.PostTypeId == postType.Id)).Select(x => PostToProtocolMessage(x).Result).OrderByDescending(x => x.CreatedDate).ToList();
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
        [HttpGet("getUserPosts/{id:length(24)}")]
        public async Task<ActionResult> GetUserPosts(string id) {
            List<Post> posts = await _postsService.PostService.GetSomeByDefinition(x => x.CreatedByUserId == id);
            return Ok(posts.OrderByDescending(x => x.DateCreated).Select(x => PostToProtocolMessage(x).Result));
        }

        [HttpGet("getFriendsPosts"),PsuedoMediaAuthentication]
        public async Task<ActionResult> GetFriendsPosts() {
            List<FriendsFollowers> friendsFollowers = await _accountService.GetAllFriendsAndFollowing(_authenticationService.ActiveUserId);
            HashSet<string> userIds = new HashSet<string>();
            friendsFollowers.ForEach(x => {
                userIds.Add(x.UserBId);
            });
            List<Post> posts = await _postsService.PostService.GetSomeByDefinition(x => userIds.Contains(x.CreatedByUserId));
            return Ok(posts.OrderByDescending(x => x.DateCreated).Select(x => PostToProtocolMessage(x).Result));
        }

        [HttpPost]
        [PsuedoMediaAuthentication]
        public async Task<IActionResult> Post(RequestPostProtocolMessage body) {
            PostType postType = await _postsService.PostTypeService.GetByCode(PostTypeEnum.POST.ToString());
            PostType replyType = await _postsService.PostTypeService.GetByCode(PostTypeEnum.REPLY.ToString());
            Post post = new Post() {
                PostText = body.PostText,
                PostTypeId = body.ParentPostId == null ? postType.Id : replyType.Id,
                ParentPostId = body.ParentPostId,
            };
            await _postsService.PostService.CreateAsync(post);
            PostProtocolResponseMessage response = new PostProtocolResponseMessage {
                Id = post.Id,
                Message = body.PostText
            };

            return Ok(response);
        }

        [HttpPut("{id:length(24)}")]
        [PsuedoMediaAuthentication]
        public async Task<IActionResult> Update(string id, RequestPostProtocolMessage body) {
            var post = await _postsService.PostService.GetByIdAsync(id);
            post.PostText = body.PostText;

            if (post is null) {
                return NotFound();
            }

            await _postsService.PostService.UpdateAsync(id, post);

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

        [HttpPost("upvotePost"),PsuedoMediaAuthentication]
        public async Task<ActionResult> UpvotePost(string postId) {
            await _postsService.RatePost(postId, _authenticationService.ActiveUserId, 1);
            RatingPostResponseProtocolMessage response = new RatingPostResponseProtocolMessage {
                Rating = await _postsService.PostRatings(postId),
                UserRating = await _postsService.UserRating(postId, _authenticationService.ActiveUserId)
            };
            return Ok(response);
        }

        [HttpPost("downvotePost"), PsuedoMediaAuthentication]
        public async Task<ActionResult> DownvotePost(string postId) {
            await _postsService.RatePost(postId, _authenticationService.ActiveUserId, -1);
            RatingPostResponseProtocolMessage response = new RatingPostResponseProtocolMessage {
                Rating = await _postsService.PostRatings(postId),
                UserRating = await _postsService.UserRating(postId, _authenticationService.ActiveUserId)
            };
            return Ok(response);
        }

        private async Task<PostProtocolMessage> PostToProtocolMessage(Post post,bool replyMessages = true) {
            Users user = await _authenticationService.UserService.GetByIdAsync(post.CreatedByUserId);
            Attachment? attachment = await _attachmentService.FileAttachmentService.GetOneByDefinition(x => x.PostId == post.Id);
            string? attachmentTag = null;
            if (attachment != null) {
                AttachmentType type = await _attachmentService.AttachmentTypeService.GetByIdAsync(attachment.AttachmentTypeId);
                attachmentTag = type.DisplayTag;
            }
            if(user == null) {
                user = (await _authenticationService.UserService.GetSomeByDefinition(x => x.DisplayName == "UnknownUser")).First();
            }
            List<PostProtocolMessage> replies = new List<PostProtocolMessage>();
            if (replyMessages) {
                replies = (await _postsService.PostService.GetSomeByDefinition(x => x.ParentPostId == post.Id)).Select(x => PostToProtocolMessage(x, false).Result).ToList();
            }
            long rating = await _postsService.PostRatings(post.Id);
            int userRating = 0;
            if (_authenticationService?.ActiveUserId != null) {
                userRating = await _postsService.UserRating(post.Id, _authenticationService.ActiveUserId);
            }
            return new PostProtocolMessage() {
                Message = post.PostText,
                CreatedDate = post.DateCreated,
                UserCreatedName = user.DisplayName,
                UserCreatedById = user.Id,
                Replies = replies,
                Id = post.Id,
                Rating = rating,
                UserRating = userRating,
                AttachmentId = attachment?.Id,
                AttachmentTag = attachmentTag
            };
        }
    }
}
