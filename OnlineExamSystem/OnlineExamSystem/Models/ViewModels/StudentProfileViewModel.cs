using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models.ViewModels
{
    public class StudentProfileViewModel
    {
        public string Name { get; set; }

        public int? CourseId { get; set; }

        public int? CollegeId { get; set; }

        public string RollNumber { get; set; }
    }
}
