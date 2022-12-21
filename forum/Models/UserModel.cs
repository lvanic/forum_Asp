using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace forum.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class UserModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Password 
        { 
            get => null; 
            set => _password = value; 
        }
        private string _password;
        public byte[] Salt { get; set; }
        public List<QuestionModel> Questions { get; set; } 
            = new List<QuestionModel>();
        public List<CommentModel> Comments { get; set; } 
            = new List<CommentModel>();
        public List<ReplyCommentModel> ReplyComments { get; set; } 
            = new List<ReplyCommentModel>();
        public UserModel(string name, string password, byte[] salt)
        {
            Salt = salt;
            Name = name;
            Password = password;
        }
        public string GetPassword()
        {
            string result = _password;
            return _password;
        }
    }
}
