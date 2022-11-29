using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace forum.Models
{
    public class QuestionModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int QuestionId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public UserModel User { get; set; }
        public string Section { get; set; }
        public string Tag { get; set; }
        public List<CommentModel> Comments { get; set; } = new List<CommentModel>();
        public List<FileModel> Files { get; set; } = new List<FileModel>();
        public IEnumerable<byte[]> GetFilesBytes(IWebHostEnvironment appEnvironment)
        {
            return Enumerable.Range(0, Files.Count()).Select(index =>
            {
                var path = $"{Files[index].Path}";
                using FileStream fileStream = new FileStream(appEnvironment.WebRootPath + path, FileMode.Open);
                byte[] buf = new byte[fileStream.Length];
                fileStream.Read(buf);
                return buf;
            });
        }
    }
}
