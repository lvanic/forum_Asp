using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace forum.Models
{
    public class ReplyCommentModel : IModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int ReplyId { get; set; }
        public UserModel User { get; set; }
        public CommentModel RelatedComment { get; set; }
        public string Text { get; set; }

    }
}
