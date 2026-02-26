using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        // In SaaS architecture, every teacher MUST belong to a college
        [Required]
        public int CollegeId { get; set; }

        public User User { get; set; }
        public College College { get; set; }

        public ICollection<TeacherSubject> TeacherSubjects { get; set; }
    }
}