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
        
    }
}
