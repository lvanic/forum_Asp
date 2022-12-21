using forum.Models;

namespace forum.Dto
{
    public struct ResponseQuestion
    {
        public string Title { get; set; }
        public object User { get; set; }
        public string Section { get; set; }
        public string Description { get; set; }
        public IEnumerable<byte[]> Files { get; set; }
        public IEnumerable<CommentModel> Comments { get; set; }
    }
}
