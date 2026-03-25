using OnlineExamSystem.Models;
using OnlineExamSystem.Helpers;
using Microsoft.EntityFrameworkCore;

namespace OnlineExamSystem.Services.DemoData
{
    public interface IDemoDataSeederService
    {
        Task SeedAsync();
    }

    public class DemoDataSeederService : IDemoDataSeederService
    {
        private readonly ApplicationDbContext _context;

        public DemoDataSeederService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // جلوگیری از دوباره‌سازی داده
            if (_context.Colleges.Any())
                return;

            // =========================
            // 1. COLLEGES
            // =========================
            var college1 = new College { Name = "ABC Engineering College", IsActive = true };
            var college2 = new College { Name = "XYZ Institute of Technology", IsActive = true };

            _context.Colleges.AddRange(college1, college2);
            await _context.SaveChangesAsync();

            var colleges = new List<College> { college1, college2 };

            foreach (var college in colleges)
            {
                // =========================
                // 2. TEACHER ADMIN (1 PER COLLEGE)
                // =========================
                var teacherAdminUser = new User
                {
                    Username = $"ta_{college.Id}",
                    Email = $"ta{college.Id}@demo.com",
                    PasswordHash = PasswordHelper.HashPassword("a"),
                    Role = "TeacherAdmin",
                    CollegeId = college.Id,
                    IsActive = true
                };

                _context.Users.Add(teacherAdminUser);
                await _context.SaveChangesAsync();

                var teacherAdmin = new Teacher
                {
                    UserId = teacherAdminUser.Id,
                    Name = $"Admin {college.Name}",
                    CollegeId = college.Id
                };

                _context.Teachers.Add(teacherAdmin);

                // =========================
                // 3. TEACHERS
                // =========================
                var teacherNames = new[] { "Rahul Sharma", "Priya Das", "Amit Verma" };
                var teachers = new List<Teacher>();

                foreach (var name in teacherNames)
                {
                    var user = new User
                    {
                        Username = name.Replace(" ", "").ToLower(),
                        Email = $"{name.Replace(" ", "").ToLower()}@demo.com",
                        PasswordHash = PasswordHelper.HashPassword("a"),
                        Role = "Teacher",
                        CollegeId = college.Id,
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    var teacher = new Teacher
                    {
                        UserId = user.Id,
                        Name = name,
                        CollegeId = college.Id
                    };

                    teachers.Add(teacher);
                }

                _context.Teachers.AddRange(teachers);
                await _context.SaveChangesAsync();

                // =========================
                // 4. COURSES
                // =========================
                var course1 = new Course { Name = "BCA", CollegeId = college.Id };
                var course2 = new Course { Name = "MCA", CollegeId = college.Id };

                _context.Courses.AddRange(course1, course2);
                await _context.SaveChangesAsync();

                // =========================
                // 5. SUBJECTS
                // =========================
                var subjects = new List<Subject>
                {
                    new Subject { Name = "Data Structures", Code = $"DS{college.Id}", CollegeId = college.Id },
                    new Subject { Name = "DBMS", Code = $"DB{college.Id}", CollegeId = college.Id },
                    new Subject { Name = "Operating Systems", Code = $"OS{college.Id}", CollegeId = college.Id },
                    new Subject { Name = "C# Programming", Code = $"CS{college.Id}", CollegeId = college.Id }
                };

                _context.Subjects.AddRange(subjects);
                await _context.SaveChangesAsync();

                // =========================
                // 6. MAPPINGS
                // =========================

                foreach (var subject in subjects)
                {
                    _context.CourseSubjects.Add(new CourseSubject { CourseId = course1.Id, SubjectId = subject.Id });
                    _context.CourseSubjects.Add(new CourseSubject { CourseId = course2.Id, SubjectId = subject.Id });
                }

                foreach (var teacher in teachers)
                {
                    foreach (var subject in subjects)
                    {
                        _context.TeacherSubjects.Add(new TeacherSubject
                        {
                            TeacherId = teacher.Id,
                            SubjectId = subject.Id
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // =========================
                // 7. STUDENTS
                // =========================
                var studentNames = new[]
                {
                    "Amit Kumar", "Sneha Patel", "Rohit Singh", "Neha Gupta",
                    "Vikas Yadav", "Anjali Mehta", "Karan Shah", "Pooja Das"
                };

                foreach (var name in studentNames)
                {
                    var user = new User
                    {
                        Username = name.Replace(" ", "").ToLower(),
                        Email = $"{name.Replace(" ", "").ToLower()}{college.Id}@demo.com",
                        PasswordHash = PasswordHelper.HashPassword("a"),
                        Role = "Student",
                        CollegeId = college.Id,
                        IsActive = true
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _context.Students.Add(new Student
                    {
                        UserId = user.Id,
                        Name = name,
                        CollegeId = college.Id,
                        RollNumber = $"RN{college.Id}{user.Id}"
                    });
                }

                await _context.SaveChangesAsync();

                // =========================
                // 8. EXAMS
                // =========================
                foreach (var subject in subjects)
                {
                    foreach (var teacher in teachers)
                    {
                        var exam = new Exam
                        {
                            Title = $"{subject.Name} Quiz",
                            Description = $"Practice quiz for {subject.Name}",
                            DurationInMinutes = 30,
                            TotalMarks = 5,
                            CreatedByTeacherId = teacher.Id,
                            SubjectId = subject.Id,
                            CollegeId = college.Id
                        };

                        _context.Exams.Add(exam);
                        await _context.SaveChangesAsync();

                        // =========================
                        // QUESTIONS + OPTIONS
                        // =========================
                        var questionBank = new Dictionary<string, List<(string Question, string[] Options, int CorrectIndex)>>
                        {
                            ["Data Structures"] = new List<(string, string[], int)>
    {
        ("Which data structure uses FIFO?", new[] { "Queue", "Stack", "Tree", "Graph" }, 0),
        ("Which structure is used in recursion?", new[] { "Queue", "Stack", "Heap", "Array" }, 1),
        ("Which search is fastest for sorted array?", new[] { "Linear", "Binary", "DFS", "BFS" }, 1),
        ("Which DS is used for BFS?", new[] { "Stack", "Queue", "Tree", "Heap" }, 1),
        ("Which structure has LIFO?", new[] { "Queue", "Stack", "Graph", "Array" }, 1)
    },

                            ["DBMS"] = new List<(string, string[], int)>
    {
        ("What does SQL stand for?", new[] { "Structured Query Language", "Simple Query Language", "Standard Query List", "System Query Logic" }, 0),
        ("Which key uniquely identifies a record?", new[] { "Foreign Key", "Primary Key", "Candidate Key", "Composite Key" }, 1),
        ("Which normal form removes partial dependency?", new[] { "1NF", "2NF", "3NF", "BCNF" }, 1),
        ("Which command is used to fetch data?", new[] { "INSERT", "UPDATE", "SELECT", "DELETE" }, 2),
        ("Which is a NoSQL database?", new[] { "MySQL", "MongoDB", "Oracle", "SQL Server" }, 1)
    },

                            ["Operating Systems"] = new List<(string, string[], int)>
    {
        ("What is the brain of computer?", new[] { "CPU", "RAM", "OS", "Disk" }, 2),
        ("Which scheduling is non-preemptive?", new[] { "Round Robin", "FCFS", "Priority", "SJF" }, 1),
        ("What is deadlock?", new[] { "Infinite loop", "Process waiting forever", "Crash", "Interrupt" }, 1),
        ("Which is volatile memory?", new[] { "ROM", "Hard Disk", "RAM", "SSD" }, 2),
        ("Which manages hardware?", new[] { "Compiler", "OS", "Database", "Driver" }, 1)
    },

                            ["C# Programming"] = new List<(string, string[], int)>
    {
        ("C# is developed by?", new[] { "Microsoft", "Google", "Apple", "IBM" }, 0),
        ("Which keyword is used for inheritance?", new[] { "this", "base", "extends", "inherits" }, 1),
        ("Which type is value type?", new[] { "Class", "Object", "int", "string" }, 2),
        ("Which loop runs at least once?", new[] { "for", "while", "do-while", "foreach" }, 2),
        ("Which is used for exception handling?", new[] { "if-else", "try-catch", "loop", "switch" }, 1)
    }
                        };

                        // Get questions based on subject
                        var selectedQuestions = questionBank.ContainsKey(subject.Name)
                            ? questionBank[subject.Name]
                            : questionBank["Data Structures"]; // fallback

                        foreach (var q in selectedQuestions)
                        {
                            var question = new Question
                            {
                                Text = q.Question,
                                Marks = 1,
                                ExamId = exam.Id,
                                CollegeId = college.Id
                            };

                            _context.Questions.Add(question);
                            await _context.SaveChangesAsync();

                            for (int i = 0; i < q.Options.Length; i++)
                            {
                                _context.Options.Add(new Option
                                {
                                    QuestionId = question.Id,
                                    CollegeId = college.Id,
                                    Text = q.Options[i],
                                    IsCorrect = (i == q.CorrectIndex)
                                });
                            }

                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
        }
    }
}