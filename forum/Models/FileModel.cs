using forum.Utils;

namespace forum.Models
{
    public class FileModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public FileType Type { get; set; }
        public QuestionModel Question { get; set; }
    }
}
