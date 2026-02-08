namespace OnlineExamSystem.Models
{
    public class CollegeCourse
    {
        public int CollegeId { get; set; }
        public College College { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }
    }
}
