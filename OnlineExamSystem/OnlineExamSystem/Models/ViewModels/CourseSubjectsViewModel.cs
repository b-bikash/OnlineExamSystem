using System.Collections.Generic;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Models.ViewModels
{
    public class CourseSubjectsViewModel
    {
        public Course Course { get; set; }

        // All available subjects (checkbox list)
        public List<Subject> AllSubjects { get; set; }

        // SubjectIds already linked to this course
        public List<int> SelectedSubjectIds { get; set; }
    }
}
