namespace OnlineExamSystem.Models
{
    public class College
    {
        public int Id { get; set; }

        public string Name { get; set; }

        // Single lifecycle flag
        public bool IsActive { get; set; } = true;
    }
}
