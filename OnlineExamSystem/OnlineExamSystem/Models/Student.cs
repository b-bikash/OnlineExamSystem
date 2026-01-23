namespace OnlineExamSystem.Models
{
    public class Student
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; }

        public int? CourseId { get; set; }

        public int? CollegeId { get; set; }

        public string? RollNumber { get; set; }

        public bool IsProfileCompleted { get; set; } = false;

        public User User { get; set; }
        public Course Course { get; set; }
        public College College { get; set; }
    }
}
