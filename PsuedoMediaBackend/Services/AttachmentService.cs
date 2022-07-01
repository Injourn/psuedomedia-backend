using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using PsuedoMediaBackend.Models;

namespace PsuedoMediaBackend.Services {
    public class AttachmentService {
        public MongoDbService<Attachment> FileAttachmentService { get; private set; }
        public MongoDbService<AttachmentType> AttachmentTypeService { get; private set; }

        public AttachmentService(IOptions<PsuedoMediaDatabaseSettings> psuedoMediaDatabaseSettings) {
            FileAttachmentService = new MongoDbService<Attachment>(psuedoMediaDatabaseSettings);
            AttachmentTypeService = new MongoDbService<AttachmentType>(psuedoMediaDatabaseSettings);           
        }
        public async Task<Attachment> AddAttachment(IFormFile file,string postId) {
            Attachment attachment;
            try {
                if (file.Length > 0) {
                    string fileSystemName = postId + "." + file.FileName.Split(".").Last();
                    string fileLocation = Path.Combine("Attachments",fileSystemName);
                    string fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Attachments");
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(),fileLocation);
                    string contentType;
                    new FileExtensionContentTypeProvider().TryGetContentType(file.FileName, out contentType);
                    AttachmentType attachmentType = await AttachmentTypeService.GetOneByDefinition(x => x.MimeType == contentType);
                    if (attachmentType == null || attachmentType.DisplayTag == "Video") {
                        return null;
                    }
                    attachment = new Attachment {
                        AttachmentTypeId = attachmentType.Id,
                        PostId = postId,
                        FileName = file.FileName,
                        FileSystemFileName = fileLocation
                    };
                    if (!Directory.Exists(fileDirectory)) {
                        Directory.CreateDirectory(fileDirectory);
                    }
                    using (var stream = new FileStream(fullPath, FileMode.Create)) {
                        file.CopyTo(stream);
                    }
                    await FileAttachmentService.CreateAsync(attachment);
                } else {
                    return null;
                }
            } catch(Exception e) {
                return null;
            }
            return attachment;
        }
    }
}
