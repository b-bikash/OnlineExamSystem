using System.Collections.Generic;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Models.ViewModels
{
    public class StudentExamsViewModel
    {
        public bool IsProfileCompleted { get; set; }
        public List<Exam> LiveExams { get; set; } = new();
        public List<Exam> UpcomingExams { get; set; } = new();

        // CHANGED: Attempted exams now carry attempt + teacher info
        public List<StudentAttemptedExamItem> AttemptedExams { get; set; } = new();
    }

    // Minimal wrapper – NO redesign
    public class StudentAttemptedExamItem
    {
        public Exam Exam { get; set; }
        public ExamAttempt Attempt { get; set; }
        public string TeacherName { get; set; }
    }
}
