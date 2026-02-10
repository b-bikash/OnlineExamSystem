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
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<CollegeCourse> CollegeCourses { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }
        public DbSet<CourseSubject> CourseSubjects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------------
            // STUDENT
            // -------------------------------
            modelBuilder.Entity<Student>()
                .HasIndex(s => new { s.CollegeId, s.RollNumber })
                .IsUnique();

            // -------------------------------
            // STUDENT ANSWER (UNIQUE)
            // -------------------------------
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
            // STUDENT ANSWER → QUESTION
            // -------------------------------
            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Question)
                .WithMany()
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // TEACHER ↔ SUBJECT (MANY-TO-MANY)
            // -------------------------------
            modelBuilder.Entity<TeacherSubject>()
                .HasKey(ts => new { ts.TeacherId, ts.SubjectId });

            modelBuilder.Entity<TeacherSubject>()
                .HasOne(ts => ts.Teacher)
                .WithMany(t => t.TeacherSubjects)
                .HasForeignKey(ts => ts.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeacherSubject>()
                .HasOne(ts => ts.Subject)
                .WithMany(s => s.TeacherSubjects)
                .HasForeignKey(ts => ts.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // EXAM ATTEMPT
            // -------------------------------
            modelBuilder.Entity<ExamAttempt>()
                .HasOne(ea => ea.Student)
                .WithMany(s => s.ExamAttempts)
                .HasForeignKey(ea => ea.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamAttempt>()
                .HasOne(ea => ea.Exam)
                .WithMany(e => e.ExamAttempts)
                .HasForeignKey(ea => ea.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // COURSE ↔ SUBJECT (MANY-TO-MANY)
            // -------------------------------
            modelBuilder.Entity<CourseSubject>()
                .HasKey(cs => new { cs.CourseId, cs.SubjectId });

            modelBuilder.Entity<CourseSubject>()
                .HasOne(cs => cs.Course)
                .WithMany(c => c.CourseSubjects)
                .HasForeignKey(cs => cs.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseSubject>()
                .HasOne(cs => cs.Subject)
                .WithMany(s => s.CourseSubjects)
                .HasForeignKey(cs => cs.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // COLLEGE ↔ COURSE (MANY-TO-MANY)
            // -------------------------------
            modelBuilder.Entity<CollegeCourse>()
                .HasKey(cc => new { cc.CollegeId, cc.CourseId });

            modelBuilder.Entity<CollegeCourse>()
                .HasOne(cc => cc.College)
                .WithMany(c => c.CollegeCourses)
                .HasForeignKey(cc => cc.CollegeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CollegeCourse>()
                .HasOne(cc => cc.Course)
                .WithMany(c => c.CollegeCourses)
                .HasForeignKey(cc => cc.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // EXAM RELATIONSHIPS
            // -------------------------------
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Subject)
                .WithMany()
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.College)
                .WithMany()
                .HasForeignKey(e => e.CollegeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.CreatedByTeacher)
                .WithMany()
                .HasForeignKey(e => e.CreatedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
