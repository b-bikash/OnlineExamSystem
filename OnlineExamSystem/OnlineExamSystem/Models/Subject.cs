using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace OnlineExamSystem.Models
{
    public class Subject
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public ICollection<TeacherSubject> TeacherSubjects { get; set; }
    public ICollection<CourseSubject> CourseSubjects { get; set; }


}
}

