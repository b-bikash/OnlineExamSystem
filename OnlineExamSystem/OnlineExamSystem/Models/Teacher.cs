namespace OnlineExamSystem.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; }

        public User User { get; set; }
    }
}
