using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class College
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        // Single lifecycle flag
        public bool IsActive { get; set; } = true;

        // -------------------------------
        // NAVIGATION PROPERTIES
        // -------------------------------

        public ICollection<Student> Students { get; set; }
        public ICollection<Teacher> Teachers { get; set; }
        public ICollection<CollegeCourse> CollegeCourses { get; set; }
    }
}
