using System;
using System.Collections.Generic;

namespace OnlineExamSystem.Models
{
    public class ExamAttempt
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }

        public int ExamId { get; set; }
        public Exam Exam { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int Score { get; set; }

        // NEW: Answers given by student for this attempt
        public ICollection<StudentAnswer> StudentAnswers { get; set; }
    }
}
