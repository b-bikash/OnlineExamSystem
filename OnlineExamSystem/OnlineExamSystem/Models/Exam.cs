using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public int DurationInMinutes { get; set; }

        [Required]
        public int TotalMarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Question> Questions { get; set; }
    }
}