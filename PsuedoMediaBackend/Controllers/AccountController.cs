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
            if(_authenticationService.ActiveUserId == null) {
                return new AccountResponseProtocolMessage() {
                    IsRelated = false
                };
            }

            AccountResponseProtocolMessage response = new AccountResponseProtocolMessage();

            FriendsFollowers? FromRelationship = (await _accountService.FriendsFollowersService.GetAllByDefinition(x => (x.UserAId == id && x.UserBId == _authenticationService.ActiveUserId))).FirstOrDefault();
            FriendsFollowers? ToRelationship = (await _accountService.FriendsFollowersService.GetAllByDefinition(x => (x.UserAId == _authenticationService.ActiveUserId && x.UserBId == id))).FirstOrDefault();

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

        [HttpPost("addFriend/{id:length(24)}"), PsuedoMediaAuthentication,]
        public async Task<ActionResult> AddFriend(string? id) {
            string? friendTypeId = (await _accountService.RelationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
            Users? otherUser = await _authenticationService.UserService.GetByIdAsync(id);
            if(otherUser == null) {
                return BadRequest("Other user does not exist");
            }
            FriendsFollowers friendsFollowers = new FriendsFollowers() {
                UserAId = _authenticationService.ActiveUserId,
                UserBId = otherUser.Id,
                RelationShipTypeId = friendTypeId
            };
            await _accountService.FriendsFollowersService.CreateAsync(friendsFollowers);
            return Ok();
        }

        [HttpPost("removeFriend/{id:length(24)}"), PsuedoMediaAuthentication]
        public async Task<ActionResult> RemoveFriend(string? id) {
            string? friendTypeId = (await _accountService.RelationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
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
            return Ok();
        }

        [HttpPost("follow/{id:length(24)}"), PsuedoMediaAuthentication]
        public async Task<ActionResult> Follow(string? id) {
            string? followTypeId = (await _accountService.RelationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
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
            return Ok();
        }

        [HttpPost("unfollow/{id:length(24)}"), PsuedoMediaAuthentication]
        public async Task<ActionResult> UnFollow(string? id) {
            string? followTypeId = (await _accountService.RelationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
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
            return Ok();
        }

        private async Task<RelationshipTypeEnum> RelationshipType(FriendsFollowers friendsFollowers) {
            string? friendTypeId = (await _accountService.RelationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FRIEND.ToString())).First().Id;
            string? followTypeId = (await _accountService.RelationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.FOLLOW.ToString())).First().Id;
            string? blockTypeId = (await _accountService.RelationshipTypeService.GetAllByDefinition(x => x.Code == RelationshipTypeEnum.BLOCK.ToString())).First().Id;

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
