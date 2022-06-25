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
        readonly AccountService _accountService;

        public AccountController(AuthenticationService authenticationService, AccountService accountService) {
            _authenticationService = authenticationService;
            _accountService = accountService;
        }

        [HttpGet("{id:length(24)}"), PsuedoMediaAuthentication, AllowAnonymous]
        public async Task<AccountResponseProtocolMessage> Get(string? id) {
            Users user = await _authenticationService.UserService.GetByIdAsync(id);
            if (_authenticationService.ActiveUserId == null) {
                return new AccountResponseProtocolMessage() {
                    IsRelated = false,
                    DisplayName = user.DisplayName
                };
            }

            AccountResponseProtocolMessage response = new AccountResponseProtocolMessage();

            FriendsFollowers? FromRelationship = await _accountService.FriendsFollowersService.GetOneByDefinition(x => (x.UserAId == id && x.UserBId == _authenticationService.ActiveUserId));
            FriendsFollowers? ToRelationship = await _accountService.FriendsFollowersService.GetOneByDefinition(x => (x.UserAId == _authenticationService.ActiveUserId && x.UserBId == id));

            if (FromRelationship != null) {
                response.IsRelated = true;
                response.FromRelationshipType = (await RelationshipType(FromRelationship)).ToString();
            }

            if (ToRelationship != null) {
                response.IsRelated = true;
                response.ToRelationshipType = (await RelationshipType(ToRelationship)).ToString();
            }


            response.DisplayName = user.DisplayName;

            return response;

        }

        [HttpGet("getUserAccount"),PsuedoMediaAuthentication]
        public async Task<ActionResult> GetUserAccount() {
            Users currentUser = await _authenticationService.UserService.GetByIdAsync(_authenticationService.ActiveUserId);
            AccountProtocolMessage accountProtocolMessage = new AccountProtocolMessage {
                Id = currentUser.Id,
                Username = currentUser.Username,
                Name = currentUser.DisplayName
            };

            return Ok(accountProtocolMessage);
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
                Id = existingUser.Id,
                Username = existingUser.Username,
                Password = string.IsNullOrEmpty(postAccountProtocolMessage.Password) ? existingUser.Password : postAccountProtocolMessage.Password,
                DisplayName = string.IsNullOrEmpty(postAccountProtocolMessage.Name) ? existingUser.DisplayName : postAccountProtocolMessage.Name,
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

        [HttpPost("addFriend/{id:length(24)}"), PsuedoMediaAuthentication,]
        public async Task<ActionResult> AddFriend(string? id) {
            string? friendTypeId = (await _accountService.RelationshipTypeService.GetSomeByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(id);            
            if(otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            await _accountService.FriendsFollowersService.DeleteByDefinitionAsync(x => x.UserAId == _authenticationService.ActiveUserId && x.UserBId == id);
            FriendsFollowers friendsFollowers = new FriendsFollowers() {
                UserAId = _authenticationService.ActiveUserId,
                UserBId = otherUser.Id,
                RelationShipTypeId = friendTypeId
            };
            await _accountService.FriendsFollowersService.CreateAsync(friendsFollowers);
            FriendsFollowers? FromRelationship = await _accountService.FriendsFollowersService.GetOneByDefinition(x => (x.UserAId == id && x.UserBId == _authenticationService.ActiveUserId));
            AccountResponseProtocolMessage response = new AccountResponseProtocolMessage() {
                IsRelated = true,
                ToRelationshipType = RelationshipTypeEnum.FRIEND.ToString(),
                FromRelationshipType = FromRelationship != null ? RelationshipType(FromRelationship).ToString() : null
            };
            return Ok(response);
        }

        [HttpPost("removeFriend/{id:length(24)}"), PsuedoMediaAuthentication]
        public async Task<ActionResult> RemoveFriend(string? id) {
            string? friendTypeId = (await _accountService.RelationshipTypeService.GetSomeByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(id);
            if (otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            FriendsFollowers? friendsFollowers = await _accountService.FriendsFollowersService.GetOneByDefinition(x => x.UserAId == _authenticationService.ActiveUserId 
                && x.UserBId == id
                && x.RelationShipTypeId == friendTypeId);
            if(friendsFollowers == null) {
                return StatusCode(500);
            }
            _accountService.FriendsFollowersService.DeleteAsync(friendsFollowers.Id);
            FriendsFollowers? FromRelationship = await _accountService.FriendsFollowersService.GetOneByDefinition(x => (x.UserAId == id && x.UserBId == _authenticationService.ActiveUserId));
            AccountResponseProtocolMessage response = new AccountResponseProtocolMessage() {
                IsRelated = FromRelationship != null,
                ToRelationshipType = null,
                FromRelationshipType = FromRelationship != null ? RelationshipType(FromRelationship).ToString() : null
            };
            return Ok(response);
        }

        [HttpPost("follow/{id:length(24)}"), PsuedoMediaAuthentication]
        public async Task<ActionResult> Follow(string? id) {
            string? followTypeId = (await _accountService.RelationshipTypeService.GetSomeByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(id);
            if (otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            FriendsFollowers friendsFollowers = new FriendsFollowers() {
                UserAId = _authenticationService.ActiveUserId,
                UserBId = otherUser.Id,
                RelationShipTypeId = followTypeId
            };
            await _accountService.FriendsFollowersService.CreateAsync(friendsFollowers);
            FriendsFollowers? FromRelationship = await _accountService.FriendsFollowersService.GetOneByDefinition(x => (x.UserAId == id && x.UserBId == _authenticationService.ActiveUserId));
            AccountResponseProtocolMessage response = new AccountResponseProtocolMessage() {
                IsRelated = true,
                ToRelationshipType = RelationshipTypeEnum.FOLLOW.ToString(),
                FromRelationshipType = FromRelationship != null ? RelationshipType(FromRelationship).ToString() : null
            };
            return Ok(response);
        }

        [HttpPost("unfollow/{id:length(24)}"), PsuedoMediaAuthentication]
        public async Task<ActionResult> UnFollow(string? id) {
            string? followTypeId = (await _accountService.RelationshipTypeService.GetSomeByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(id);
            if (otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            FriendsFollowers? friendsFollowers = await _accountService.FriendsFollowersService.GetOneByDefinition(x => x.UserAId == _authenticationService.ActiveUserId
                && x.UserBId == id
                && x.RelationShipTypeId == followTypeId);
            if (friendsFollowers == null) {
                return StatusCode(500);
            }
            _accountService.FriendsFollowersService.DeleteAsync(friendsFollowers.Id);
            FriendsFollowers? FromRelationship = await _accountService.FriendsFollowersService.GetOneByDefinition(x => (x.UserAId == id && x.UserBId == _authenticationService.ActiveUserId));
            AccountResponseProtocolMessage response = new AccountResponseProtocolMessage() {
                IsRelated = FromRelationship != null,
                ToRelationshipType = null,
                FromRelationshipType = FromRelationship != null ? RelationshipType(FromRelationship).ToString() : null
            };
            return Ok(response);
        }

        [HttpGet("getAllFriends"),PsuedoMediaAuthentication]
        public async Task<ActionResult> GetAllFriends() {
            List<FriendsFollowers> friendFollowers = await _accountService.GetAllFriendsAndFollowing(_authenticationService?.ActiveUserId);
            List<AccountFriendResponseProtocolMessage> response = friendFollowers.Select(x => {
                Users other = _authenticationService.UserService.GetByIdAsync(x.UserBId).Result;
                return new AccountFriendResponseProtocolMessage() {
                    UserId = other.Id,
                    DisplayName = other.DisplayName
                };
            }).ToList();
            return Ok(response);
        }

        private async Task<RelationshipTypeEnum> RelationshipType(FriendsFollowers friendsFollowers) {
            string? friendTypeId = (await _accountService.RelationshipTypeService.GetSomeByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
            string? followTypeId = (await _accountService.RelationshipTypeService.GetSomeByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
            string? blockTypeId = (await _accountService.RelationshipTypeService.GetSomeByDefinition(x => x.Code == RelationshipTypeEnum.BLOCK.ToString())).First().Id;

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
