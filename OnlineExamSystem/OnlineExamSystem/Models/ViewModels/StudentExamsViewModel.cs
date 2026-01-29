using System.Collections.Generic;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Models.ViewModels
{
    public class StudentExamsViewModel
    {
        public List<Exam> LiveExams { get; set; } = new();
        public List<Exam> UpcomingExams { get; set; } = new();
        public List<Exam> AttemptedExams { get; set; } = new();
    }
}
