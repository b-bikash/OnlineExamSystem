using Microsoft.EntityFrameworkCore;

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
        public DbSet<Option> Options { get; set; }
        public DbSet<ExamAttempt> ExamAttempts { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<College> Colleges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------------
            // STUDENT
            // -------------------------------

            // Unique RollNumber per College
            modelBuilder.Entity<Student>()
                .HasIndex(s => new { s.CollegeId, s.RollNumber })
                .IsUnique();

            // -------------------------------
            // STUDENT ANSWERS
            // -------------------------------

            // ONE answer per question per attempt
            modelBuilder.Entity<StudentAnswer>()
                .HasIndex(sa => new { sa.ExamAttemptId, sa.QuestionId })
                .IsUnique();

            // -------------------------------
            // OPTION → QUESTION
            // -------------------------------

            modelBuilder.Entity<Option>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------------------
            // STUDENT ANSWER → OPTION
            // -------------------------------

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.SelectedOption)
                .WithMany()
                .HasForeignKey(sa => sa.SelectedOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // STUDENT ANSWER → QUESTION (CRITICAL FIX)
            // -------------------------------

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Question)
                .WithMany()
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
