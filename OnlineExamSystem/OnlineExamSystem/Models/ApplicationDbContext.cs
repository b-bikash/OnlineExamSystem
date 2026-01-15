using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace OnlineExamSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<ExamBehaviorLog> ExamBehaviorLogs { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }

    }
}
