using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        // -------------------------------
        // NAVIGATION PROPERTIES
        // -------------------------------

        // Colleges offering this course
        public ICollection<CollegeCourse> CollegeCourses { get; set; }

        // Subjects included in this course
        public ICollection<CourseSubject> CourseSubjects { get; set; }
    }
}
