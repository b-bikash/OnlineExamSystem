using System;
using System.Collections.Generic;

namespace OnlineExamSystem.Models
{
    public class ExamAttemptViewModel
    {
        public int AttemptId { get; set; }

        public string ExamTitle { get; set; }

        public int DurationInMinutes { get; set; }

        public DateTime StartTime { get; set; }

        public int CurrentQuestionIndex { get; set; }

        public int TotalQuestions { get; set; }

        public Question Question { get; set; }

        public List<Question> Questions { get; set; }

        // NEW (STEP 9)
        public List<Option> Options { get; set; }

        public int? SelectedOptionId { get; set; }
    }
}
