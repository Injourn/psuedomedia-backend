using Microsoft.AspNetCore.Mvc;
using PsuedoMediaBackend.Filters;
using PsuedoMediaBackend.Models;
using PsuedoMediaBackend.Models.ProtocolMessages;
using PsuedoMediaBackend.Services;

namespace PsuedoMediaBackend.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class AttachmentController : ControllerBase {
        readonly AttachmentService _attachmentService;
        readonly PostsService _postsService;
        readonly AuthenticationService _authenticationService;

        public AttachmentController(AttachmentService attachmentService, PostsService postsService, AuthenticationService authenticationService) {
            _attachmentService = attachmentService;
            _postsService = postsService;
            _authenticationService = authenticationService;
        }

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult> Get(string id) {
            Attachment? attachment = await _attachmentService.FileAttachmentService.GetByIdAsync(id);
            if (attachment != null) {
                AttachmentType? type = await _attachmentService.AttachmentTypeService.GetByIdAsync(attachment.AttachmentTypeId);
                if (type != null) {
                    string attachmentLocation = Path.Combine(Directory.GetCurrentDirectory(), attachment.FileSystemFileName);
                    Byte[] b = System.IO.File.ReadAllBytes(attachmentLocation);   // You can use your own method over here.         
                    return File(b, type.MimeType);
                }
                else {
                    return StatusCode(500, "Error retrieving file. Could not get file type");
                }
            }
            else {
                return BadRequest("File does not exist");
            }
        }

        [HttpPost, PsuedoMediaAuthentication]
        public async Task<ActionResult> Post(IFormFile file, string postId) {
            Post post = await _postsService.PostService.GetByIdAsync(postId);
            if (post == null || post.CreatedByUserId != _authenticationService.ActiveUserId) {
                return BadRequest();
            }
            Attachment attachment = await _attachmentService.AddAttachment(file, postId);
            if (attachment != null) {
                AttachmentResponseProtocolMessage response = new AttachmentResponseProtocolMessage() {
                    AttachmentId = attachment.Id,
                    FileName = attachment.FileName
                };
                return Ok(response);
            }
            else {
                return StatusCode(500, "Error: Could not upload file.");
            }

        }
    }
}
