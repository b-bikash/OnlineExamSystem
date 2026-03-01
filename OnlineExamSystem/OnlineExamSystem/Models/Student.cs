using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        public int? CourseId { get; set; }

        // In SaaS architecture, every student MUST belong to a college
        [Required]
        public int CollegeId { get; set; }

        [MaxLength(50)]
        public string? RollNumber { get; set; }

        public bool IsProfileCompleted { get; set; } = false;
        

        public User User { get; set; }
        public Course Course { get; set; }
        public College College { get; set; }
        public ICollection<ExamAttempt> ExamAttempts { get; set; }
    }
}