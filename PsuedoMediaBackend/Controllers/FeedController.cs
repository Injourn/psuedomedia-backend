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

        public FeedController(AuthenticationService authenticationService, PostsService postsService, AccountService accountService,AttachmentService attachmentService) {            
            _authenticationService = authenticationService;
            _postsService = postsService;
            _postsService.PostService.UserId = _authenticationService.ActiveUserId;
            _accountService = accountService;            
            _attachmentService = attachmentService;
        }

        [HttpGet, PsuedoMediaAuthentication, AllowAnonymous]
        public async Task<PostProtocolResponseMessage> Get(int? page,int? limit) {
            PostType postType = await _postsService.PostTypeService.GetByCode(PostTypeEnum.POST.ToString());
            List<PostProtocolObject> posts = (await _postsService.PostService.GetSomeByDefinition(x => x.PostTypeId == postType.Id,x => x.DateCreated,(page ?? 0) * 10, Math.Min(limit ?? 10, 10)))
                .Select(x => PostToProtocolMessage(x).Result)
                .ToList();
            long count = _postsService.PostService.GetCountByDefinition(x => x.PostTypeId == postType.Id);
            return new PostProtocolResponseMessage() {
                Statuses = posts,
                Count = count
            };
        }

        [HttpGet("getMoreReplies")]
        public async Task<ActionResult<PostProtocolResponseMessage>> GetMoreReplies(string postId,int? page,int? limit) {
            List<PostProtocolObject> posts = (await _postsService.PostService.GetSomeByDefinition(x => x.ParentPostId == postId, (page ?? 0) * Math.Min(limit ?? 5, 5), Math.Min(limit ?? 10, 10)))
                .Select(x => PostToProtocolMessage(x).Result)
                .ToList();
            long count = _postsService.PostService.GetCountByDefinition(x => x.ParentPostId == postId);
            return Ok(new PostProtocolResponseMessage() {
                Statuses = posts,
                Count = count
            });
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
        public async Task<ActionResult> GetUserPosts(string id, int? page,int? limit) {
            PostType postType = await _postsService.PostTypeService.GetByCode(PostTypeEnum.POST.ToString());
            List<Post> posts = await _postsService.PostService.GetSomeByDefinition(x => 
                x.CreatedByUserId == id &&
                    x.PostTypeId == postType.Id,
                x => x.DateCreated,
                (page ?? 0) * 10, 
                Math.Min(limit ?? 10, 10));
            long count = _postsService.PostService.GetCountByDefinition(x => x.CreatedByUserId == id && x.PostTypeId == postType.Id);
            List<PostProtocolObject> response = posts.OrderByDescending(x => x.DateCreated).Select(x => PostToProtocolMessage(x).Result).ToList();

            return Ok(new PostProtocolResponseMessage() {
                Statuses = response,
                Count = count
            });
        }

        [HttpGet("getFriendsPosts"),PsuedoMediaAuthentication]
        public async Task<ActionResult> GetFriendsPosts(int? page, int? limit) {
            PostType postType = await _postsService.PostTypeService.GetByCode(PostTypeEnum.POST.ToString());
            List<FriendsFollowers> friendsFollowers = await _accountService.GetAllFriendsAndFollowing(_authenticationService.ActiveUserId);
            HashSet<string> userIds = new HashSet<string>();
            friendsFollowers.ForEach(x => {
                userIds.Add(x.UserBId);
            });
            List<Post> posts = await _postsService.PostService.GetSomeByDefinition(x => 
                userIds.Contains(x.CreatedByUserId) &&
                    x.PostTypeId == postType.Id,
                x => x.DateCreated,
                (page ?? 0) * 10, 
                Math.Min(limit ?? 10, 10));

            long count = _postsService.PostService.GetCountByDefinition(x => userIds.Contains(x.CreatedByUserId) && x.PostTypeId == postType.Id);
            List<PostProtocolObject> response = posts.OrderByDescending(x => x.DateCreated).Select(x => PostToProtocolMessage(x).Result).ToList();

            return Ok(new PostProtocolResponseMessage() {
                Statuses = response,
                Count = count
            });
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
            PostCreateProtocolResponseMessage response = new PostCreateProtocolResponseMessage {
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

        private async Task<PostProtocolObject> PostToProtocolMessage(Post post,bool replyMessages = true) {
            Users user = await _authenticationService.UserService.GetByIdAsync(post.CreatedByUserId);
            Attachment? attachment = await _attachmentService.FileAttachmentService.GetOneByDefinition(x => x.PostId == post.Id);
            string? attachmentTag = null;
            long replyCount = 0;
            if (attachment != null) {
                AttachmentType type = await _attachmentService.AttachmentTypeService.GetByIdAsync(attachment.AttachmentTypeId);
                attachmentTag = type.DisplayTag;
            }
            if(user == null) {
                user = await _authenticationService.UserService.GetOneByDefinition(x => x.DisplayName == "UnknownUser");
            }
            List<PostProtocolObject> replies = new List<PostProtocolObject>();
            if (replyMessages) {
                replies = (await _postsService.PostService.GetSomeByDefinition(x => x.ParentPostId == post.Id,limit:5)).Select(x => PostToProtocolMessage(x, false).Result).ToList();
                replyCount = _postsService.PostService.GetCountByDefinition(x => x.ParentPostId == post.Id);
            }
            long rating = await _postsService.PostRatings(post.Id);
            int userRating = 0;
            if (_authenticationService?.ActiveUserId != null) {
                userRating = await _postsService.UserRating(post.Id, _authenticationService.ActiveUserId);
            }
            return new PostProtocolObject() {
                Message = post.PostText,
                CreatedDate = post.DateCreated,
                UserCreatedName = user.DisplayName,
                UserCreatedById = user.Id,
                Replies = replies,
                ReplyCount = replyCount,
                Id = post.Id,
                Rating = rating,
                UserRating = userRating,
                AttachmentId = attachment?.Id,
                AttachmentTag = attachmentTag
            };
        }
    }
}
