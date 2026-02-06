namespace OnlineExamSystem.Models
{
    public class Course
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public ICollection<Subject> Subjects { get; set; }
        public ICollection<CourseSubject> CourseSubjects { get; set; }


    }
}
