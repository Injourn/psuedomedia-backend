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
        public async Task<bool> AddAttachment(IFormFile file,string directory,string postId) {
            try {
                if (file.Length > 0) {
                    string fileSystemName = postId + "." + file.FileName.Split(".").Last();
                    string fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), directory);
                    string fullPath = Path.Combine(fileDirectory, fileSystemName);
                    string contentType;
                    new FileExtensionContentTypeProvider().TryGetContentType(file.FileName, out contentType);
                    AttachmentType attachmentType = await AttachmentTypeService.GetOneByDefinition(x => x.MimeType == contentType);
                    if (attachmentType == null) {
                        return false;
                    }
                    Attachment attachment = new Attachment {
                        AttachmentTypeId = attachmentType.Id,
                        PostId = postId,
                        FileName = file.FileName,
                        FileSystemFileName = fileSystemName
                    };
                    if (!Directory.Exists(fileDirectory)) {
                        Directory.CreateDirectory(fileDirectory);
                    }
                    using (var stream = new FileStream(fullPath, FileMode.Create)) {
                        file.CopyTo(stream);
                    }
                    await FileAttachmentService.CreateAsync(attachment);
                } else {
                    return false;
                }
            } catch(Exception e) {
                return false;
            }
            return true;
        }
    }
}
