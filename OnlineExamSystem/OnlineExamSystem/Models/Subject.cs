namespace OnlineExamSystem.Models
{
    public class Subject
{
    public int Id { get; set; }

    public string Name { get; set; }   // DBMS, OS, etc.

    public int CourseId { get; set; }
    public Course Course { get; set; }

    public int Semester { get; set; }

    public ICollection<Exam> Exams { get; set; }

}
}

