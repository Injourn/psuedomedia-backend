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
                    string fileSystemName = postId + file.Name.Split(".").Last();
                    string fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), directory);
                    Attachment attachment = new Attachment {
                        AttachmentTypeId = type.Id,
                        PostId = postId,
                        FileName = file.FileName,
                        FileSystemFileName = fileSystemName
                    };
                    await FileAttachmentService.CreateAsync(attachment);
                    AddAttachment(file,Path.Combine(fileDirectory,fileSystemName));
                } else {
                    return false;
                }
            } catch(Exception e) {
                return false;
            }
            return true;
        }

        private void AddAttachment(IFormFile formFile,string fullPath) {
            using (var stream = new FileStream(fullPath, FileMode.Create)) {
                formFile.CopyTo(stream);
            }
        }
    }
}
