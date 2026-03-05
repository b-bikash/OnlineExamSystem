using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace OnlineExamSystem.Models
{
    public class Subject
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int CollegeId { get; set; }
    public College? College { get; set; }

    public ICollection<TeacherSubject>? TeacherSubjects { get; set; }
    public ICollection<CourseSubject>? CourseSubjects { get; set; }


}
}

