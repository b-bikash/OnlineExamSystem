using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;

namespace OnlineExamSystem.Services.AdminCleanup
{
    public class AdminCleanupService : IAdminCleanupService
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public AdminCleanupService(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task DeleteCollegeCompletelyAsync(int collegeId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ----------------------------------------
                // STEP 1: Get ALL core IDs ONCE
                // ----------------------------------------
                var teacherIds = await _context.Teachers
                    .Where(t => t.CollegeId == collegeId)
                    .Select(t => t.Id)
                    .ToListAsync();

                var examIds = await _context.Exams
                    .Where(e =>
                        e.CollegeId == collegeId ||
                        teacherIds.Contains(e.CreatedByTeacherId))
                    .Select(e => e.Id)
                    .ToListAsync();

                var studentIds = await _context.Students
                    .Where(s => s.CollegeId == collegeId)
                    .Select(s => s.Id)
                    .ToListAsync();

                var examAttempts = await _context.ExamAttempts
                    .Where(ea =>
                        ea.CollegeId == collegeId ||
                        examIds.Contains(ea.ExamId) ||
                        studentIds.Contains(ea.StudentId))
                    .ToListAsync();

                var examAttemptIds = examAttempts.Select(e => e.Id).ToList();

                // ----------------------------------------
                // STEP 2: Delete StudentAnswers
                // ----------------------------------------
                var studentAnswers = await _context.StudentAnswers
                    .Where(sa => examAttemptIds.Contains(sa.ExamAttemptId))
                    .ToListAsync();

                _context.StudentAnswers.RemoveRange(studentAnswers);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 3: Delete ExamProctorLogs & Physical Images
                // ----------------------------------------
                var proctorLogs = await _context.ExamProctorLogs
                    .Where(p => examAttemptIds.Contains(p.ExamAttemptId))
                    .ToListAsync();
                    
                foreach (var log in proctorLogs)
                {
                    if (!string.IsNullOrEmpty(log.ImagePath) && log.ImagePath != "EXPIRED")
                    {
                        var fileName = System.IO.Path.GetFileName(log.ImagePath);
                        var physicalPath = System.IO.Path.Combine(_env.WebRootPath, "proctoring", fileName);
                        if (System.IO.File.Exists(physicalPath))
                        {
                            try { System.IO.File.Delete(physicalPath); } catch { }
                        }
                    }
                }

                _context.ExamProctorLogs.RemoveRange(proctorLogs);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 4: Delete ExamBehaviorLogs
                // ----------------------------------------
                var behaviorLogs = await _context.ExamBehaviorLogs
                    .Where(b => examIds.Contains(b.ExamId))
                    .ToListAsync();

                _context.ExamBehaviorLogs.RemoveRange(behaviorLogs);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 5: Delete Options
                // ----------------------------------------
                var questionIds = await _context.Questions
                    .Where(q => examIds.Contains(q.ExamId))
                    .Select(q => q.Id)
                    .ToListAsync();

                var options = await _context.Options
                    .Where(o => questionIds.Contains(o.QuestionId))
                    .ToListAsync();

                _context.Options.RemoveRange(options);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 6: Delete Questions
                // ----------------------------------------
                var questions = await _context.Questions
                    .Where(q => examIds.Contains(q.ExamId))
                    .ToListAsync();

                _context.Questions.RemoveRange(questions);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 7: Delete ExamAttempts
                // ----------------------------------------
                _context.ExamAttempts.RemoveRange(examAttempts);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 8: Delete Exams (FORCE DELETE - avoids FK tracking issue)
                // ----------------------------------------
                await _context.Database.ExecuteSqlRawAsync(@"
                    DELETE FROM Exams
                    WHERE CollegeId = {0}
                    OR CreatedByTeacherId IN (
                    SELECT Id FROM Teachers WHERE CollegeId = {0}
                    )", collegeId);

                // ----------------------------------------
                // STEP 9: Delete TeacherSubjects
                // ----------------------------------------
                var teacherSubjects = await _context.TeacherSubjects.ToListAsync();
                _context.TeacherSubjects.RemoveRange(teacherSubjects);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 10: Delete CourseSubjects
                // ----------------------------------------
                var courseSubjects = await _context.CourseSubjects.ToListAsync();
                _context.CourseSubjects.RemoveRange(courseSubjects);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 11: Delete CollegeCourses
                // ----------------------------------------
                var collegeCourses = await _context.CollegeCourses
                    .Where(cc => cc.CollegeId == collegeId)
                    .ToListAsync();

                _context.CollegeCourses.RemoveRange(collegeCourses);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 12: Delete Students & Teachers
                // ----------------------------------------
                var students = await _context.Students
                    .Where(s => s.CollegeId == collegeId)
                    .ToListAsync();

                _context.Students.RemoveRange(students);

                var teachers = await _context.Teachers
                    .Where(t => t.CollegeId == collegeId)
                    .ToListAsync();

                _context.Teachers.RemoveRange(teachers);

                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 13: Delete Users
                // ----------------------------------------
                var users = await _context.Users
                    .Where(u => u.CollegeId == collegeId)
                    .ToListAsync();

                _context.Users.RemoveRange(users);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 14: Delete Subjects
                // ----------------------------------------
                var subjects = await _context.Subjects
                    .Where(s => s.CollegeId == collegeId)
                    .ToListAsync();

                _context.Subjects.RemoveRange(subjects);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 15: Delete Courses
                // ----------------------------------------
                var courses = await _context.Courses
                    .Where(c => c.CollegeId == collegeId)
                    .ToListAsync();

                _context.Courses.RemoveRange(courses);
                await _context.SaveChangesAsync();

                // ----------------------------------------
                // STEP 16: Delete College
                // ----------------------------------------
                var college = await _context.Colleges
                    .FirstOrDefaultAsync(c => c.Id == collegeId);

                if (college != null)
                {
                    _context.Colleges.Remove(college);
                    await _context.SaveChangesAsync();
                }

                // ----------------------------------------
                // COMMIT
                // ----------------------------------------
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}