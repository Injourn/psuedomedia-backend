using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsuedoMediaBackend.Filters;
using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Models.ProtocolMessages;
using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase {

        readonly AuthenticationService _authenticationService;
        readonly MongoDbService<FriendsFollowers> _friendsFollowersService;
        readonly MongoDbService<RelationshipType> _relationshipTypeService;

        public AccountController(AuthenticationService authenticationService) {
            _authenticationService = authenticationService;
            _friendsFollowersService = new MongoDbService<FriendsFollowers>();
            _relationshipTypeService = new MongoDbService<RelationshipType>();
        }

        [HttpGet("{id:length(24)}"), PsuedoMediaAuthentication, AllowAnonymous]
        public async Task<AccountResponseProtocolMessage> GetAccountInfo(string? id) {
            if(_authenticationService.ActiveUserId == null) {
                return new AccountResponseProtocolMessage() {
                    IsRelated = false
                };
            }

            AccountResponseProtocolMessage response = new AccountResponseProtocolMessage();

            FriendsFollowers? FromRelationship = (await _friendsFollowersService.GetAllByDefinition(x => (x.UserAId == id && x.UserBId == _authenticationService.ActiveUserId))).FirstOrDefault();
            FriendsFollowers? ToRelationship = (await _friendsFollowersService.GetAllByDefinition(x => (x.UserAId == _authenticationService.ActiveUserId && x.UserBId == id))).FirstOrDefault();

            if (FromRelationship != null) {
                response.IsRelated = true;
                response.FromRelationshipType = RelationshipType(FromRelationship).ToString();
            }

            if (ToRelationship != null) {
                response.IsRelated = true;
                response.FromRelationshipType = RelationshipType(ToRelationship).ToString();
            }

            return response;

        }

        [HttpPost]
        public async Task<ActionResult> CreateAccount(AccountProtocolMessage postAccountProtocolMessage) {
            Users user = new Users() {
                Username = postAccountProtocolMessage.Username,
                Password = postAccountProtocolMessage.Password,
                DisplayName = postAccountProtocolMessage.Name
            };
            await _authenticationService.UserService.CreateAsync(user);
            return NoContent();
        }

        [HttpPut]
        [PsuedoMediaAuthentication]
        public async Task<ActionResult> EditAccount(AccountProtocolMessage postAccountProtocolMessage) {
            Users existingUser = await _authenticationService.UserService.GetByIdAsync(_authenticationService.ActiveUserId);
            Users user = new Users() {
                Username = existingUser.Username,
                Password = postAccountProtocolMessage.Password ?? existingUser.Password,
                DisplayName = postAccountProtocolMessage.Name ?? existingUser.DisplayName
            };
            await _authenticationService.UserService.UpdateAsync(existingUser.Id, user);
            return NoContent();
        }

        [HttpDelete]
        [PsuedoMediaAuthentication]
        public async Task<ActionResult> DeleteAccount() {
            await _authenticationService.UserService.DeleteAsync(_authenticationService.ActiveUserId);
            return Ok();
        }

        [HttpPost, PsuedoMediaAuthentication]
        public async Task<ActionResult> AddFriend(RelationshipProtocolMessage relationshipProtocolMessage) {
            string? friendTypeId = (await _relationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(relationshipProtocolMessage.RelatedUserId);
            if(otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            FriendsFollowers friendsFollowers = new FriendsFollowers() {
                UserAId = _authenticationService.ActiveUserId,
                UserBId = otherUser.Id,
                RelationShipTypeId = friendTypeId
            };
            await _friendsFollowersService.CreateAsync(friendsFollowers);
            return Ok();
        }

        [HttpDelete, PsuedoMediaAuthentication]
        public async Task<ActionResult> RemoveFriend(RelationshipProtocolMessage relationshipProtocolMessage) {
            string? friendTypeId = (await _relationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(relationshipProtocolMessage.RelatedUserId);
            if (otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            FriendsFollowers? friendsFollowers = await _friendsFollowersService.GetOneByDefinition(x => x.UserAId == _authenticationService.ActiveUserId 
                && x.UserBId == relationshipProtocolMessage.RelatedUserId 
                && x.RelationShipTypeId == friendTypeId);
            if(friendsFollowers == null) {
                return StatusCode(500);
            }
            _friendsFollowersService.DeleteAsync(friendsFollowers.Id);
            return Ok();
        }

        [HttpPost, PsuedoMediaAuthentication]
        public async Task<ActionResult> Follow(RelationshipProtocolMessage relationshipProtocolMessage) {
            string? followTypeId = (await _relationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(relationshipProtocolMessage.RelatedUserId);
            if (otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            FriendsFollowers friendsFollowers = new FriendsFollowers() {
                UserAId = _authenticationService.ActiveUserId,
                UserBId = otherUser.Id,
                RelationShipTypeId = followTypeId
            };
            await _friendsFollowersService.CreateAsync(friendsFollowers);
            return Ok();
        }

        [HttpDelete, PsuedoMediaAuthentication]
        public async Task<ActionResult> UnFollow(RelationshipProtocolMessage relationshipProtocolMessage) {
            string? followTypeId = (await _relationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(relationshipProtocolMessage.RelatedUserId);
            if (otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            FriendsFollowers? friendsFollowers = await _friendsFollowersService.GetOneByDefinition(x => x.UserAId == _authenticationService.ActiveUserId
                && x.UserBId == relationshipProtocolMessage.RelatedUserId
                && x.RelationShipTypeId == followTypeId);
            if (friendsFollowers == null) {
                return StatusCode(500);
            }
            _friendsFollowersService.DeleteAsync(friendsFollowers.Id);
            return Ok();
        }

        private async Task<RelationshipTypeEnum> RelationshipType(FriendsFollowers friendsFollowers) {
            string? friendTypeId = (await _relationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
            string? followTypeId = (await _relationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
            string? blockTypeId = (await _relationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.BLOCK.ToString())).First().Id;

            if(friendsFollowers.RelationShipTypeId == friendTypeId) {
                return RelationshipTypeEnum.FRIEND;
            } else if (friendsFollowers.RelationShipTypeId == followTypeId) {
                return RelationshipTypeEnum.FOLLOW;
            } else {
                return RelationshipTypeEnum.BLOCK;
            }
        }
    }
}
