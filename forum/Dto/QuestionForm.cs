namespace forum.Forms
{
    public class QuestionForm
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Section { get; set; }
        public string? Tag { get; set; }
        public IFormFileCollection? FormFile { get; set; }
    }
}
