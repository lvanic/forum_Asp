using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace forum.Models
{
    public class CommentModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int CommentId { get; set; }
        public string Text { get; set; }
        public UserModel User { get; set; }
        public QuestionModel Question { get; set; }
        public DateTime Date { get; set; }
        public List<ReplyCommentModel> ReplyComments { get; set; } = new List<ReplyCommentModel>();
    }
}
