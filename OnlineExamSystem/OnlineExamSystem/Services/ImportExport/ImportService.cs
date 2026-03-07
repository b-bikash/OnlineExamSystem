using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System.Threading.Tasks;

namespace OnlineExamSystem.Services.ImportExport
{
    public class ImportService : IImportService
    {
        private readonly ApplicationDbContext _context;

        public ImportService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<ImportResultDto> ImportCourseSubjectAsync(IFormFile file, int collegeId)
        {
            var result = new ImportResultDto();

            // 🔐 Basic validation
            if (file == null || file.Length == 0)
            {
                result.Errors.Add("File is empty.");
                return result;
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("Only CSV files are allowed.");
                return result;
            }

            try
            {
                using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);

            // 📌 Read Header
            var headerLine = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                result.Errors.Add("CSV header is missing or file is empty.");
                return result;
            }

            var headers = headerLine
                .Split(',')
                .Select(h => h.Trim().ToUpper())
                .ToList();

            int courseIndex = headers.FindIndex(h => h == "COURSE");
            int codeIndex = headers.FindIndex(h => h == "SUBJECT CODE" || h == "SUBJECTCODE");
            int nameIndex = headers.FindIndex(h => h == "SUBJECT NAME" || h == "SUBJECTNAME");

            if (codeIndex == -1 || nameIndex == -1)
            {
                result.Errors.Add("Required columns 'Subject Code' and 'Subject Name' not found.");
                return result;
            }

            if (courseIndex == -1)
            {
                result.Errors.Add("Column 'Course' not found.");
                return result;
            }

            // Header validated
            result.TotalRows = 0;

            // 📥 Read Rows
            var cleanedRows = new List<(string Course, string Code, string Name)>();

            string? line;
            int rowNumber = 1; // Header is row 1

            while ((line = await reader.ReadLineAsync()) != null)
            {
                rowNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = line.Split(',');

                if (columns.Length <= Math.Max(courseIndex, Math.Max(codeIndex, nameIndex)))
                {
                    result.InvalidRows++;
                    if (result.Errors.Count < 10)
                        result.Errors.Add($"Row {rowNumber}: Invalid column structure.");
                    continue;
                }

                var course = columns[courseIndex]?.Trim();
                var code = columns[codeIndex]?.Trim();
                var name = columns[nameIndex]?.Trim();

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    result.InvalidRows++;
                    if (result.Errors.Count < 10)
                        result.Errors.Add($"Row {rowNumber}: Subject Code and Subject Name are required.");
                    continue;
                }

                // Enforce max lengths to prevent DbUpdateException string truncation errors
                if (course != null && course.Length > 200) course = course.Substring(0, 200);
                if (code.Length > 30) code = code.Substring(0, 30);
                if (name.Length > 100) name = name.Substring(0, 100);

                cleanedRows.Add((
                    Course: course ?? string.Empty,
                    Code: code,
                    Name: name
                ));
            }

            result.TotalRows = cleanedRows.Count;

            // 🧠 Extract Unique Courses (ignore empty)
            var uniqueCourses = cleanedRows
                .Where(r => !string.IsNullOrWhiteSpace(r.Course))
                .Select(r => r.Course.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 🧠 Extract Unique Subjects (by Subject Code)
            var uniqueSubjects = cleanedRows
                .GroupBy(r => r.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            // 📦 Load Existing Courses (College-wise) with full entity
            var existingCourses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.CollegeId == collegeId)
                .ToListAsync();

            var existingCourseDict = existingCourses
                .GroupBy(c => c.Name?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase);

            // 📦 Load Existing Subjects (College-wise) with full entity
            var existingSubjects = await _context.Subjects
                .AsNoTracking()
                .Where(s => s.CollegeId == collegeId)
                .ToListAsync();

            var existingSubjectDict = existingSubjects
                .GroupBy(s => s.Code?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First(),
                    StringComparer.OrdinalIgnoreCase);

            // 🆕 Insert New Courses
            foreach (var courseName in uniqueCourses)
            {
                var trimmedName = courseName.Trim();

                if (existingCourseDict.TryGetValue(trimmedName, out var existingCourse))
                {
                    if (!existingCourse.IsActive)
                    {
                        existingCourse.IsActive = true;
                        result.InsertedCourses++; // counting as restored
                    }
                }
                else
                {
                    var newCourse = new Course
                    {
                        Name = trimmedName,
                        CollegeId = collegeId,
                        IsActive = true
                    };

                    _context.Courses.Add(newCourse);
                    result.InsertedCourses++;

                    existingCourseDict[trimmedName] = newCourse;
                }
            }

            // 🆕 Insert New Subjects
            foreach (var subject in uniqueSubjects)
            {
                var trimmedCode = subject.Code.Trim();
                var trimmedName = subject.Name.Trim();

                if (existingSubjectDict.TryGetValue(trimmedCode, out var existingSubject))
                {
                    if (!existingSubject.IsActive)
                    {
                        existingSubject.IsActive = true;
                        existingSubject.Name = trimmedName; // update name on reactivation
                        result.InsertedSubjects++; // counting as restored
                    }
                    else
                    {
                        result.SkippedDuplicates++;
                    }
                }
                else
                {
                    var newSubject = new Subject
                    {
                        Name = trimmedName,
                        Code = trimmedCode,
                        CollegeId = collegeId,
                        IsActive = true
                    };

                    _context.Subjects.Add(newSubject);
                    result.InsertedSubjects++;

                    existingSubjectDict[trimmedCode] = newSubject;
                }
            }
            // 💾 Save newly inserted Courses and Subjects
            await _context.SaveChangesAsync();

            // 📌 Load existing mappings for this college
            var existingMappings = await _context.CourseSubjects
    .Where(cs => _context.Courses
        .Where(c => c.CollegeId == collegeId)
        .Select(c => c.Id)
        .Contains(cs.CourseId))
    .Select(cs => new { cs.CourseId, cs.SubjectId })
    .ToListAsync();

            var mappingSet = new HashSet<(int, int)>(
                existingMappings.Select(m => (m.CourseId, m.SubjectId))
            );

            // 🔗 Create CourseSubject mappings
            foreach (var row in cleanedRows)
            {
                if (string.IsNullOrWhiteSpace(row.Course))
                    continue;

                if (!existingCourseDict.TryGetValue(row.Course.Trim(), out var courseObj))
                    continue;

                if (!existingSubjectDict.TryGetValue(row.Code.Trim(), out var subjectObj))
                    continue;

                var courseId = courseObj.Id;
                var subjectId = subjectObj.Id;

                var key = (courseId, subjectId);

                if (!mappingSet.Contains(key))
                {
                    _context.CourseSubjects.Add(new CourseSubject
                    {
                        CourseId = courseId,
                        SubjectId = subjectId
                    });

                    mappingSet.Add(key);
                    result.InsertedMappings++;
                }
            }

            // 💾 Save new mappings
            await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (result.Errors.Count < 10)
                {
                    result.Errors.Add($"Import stopped due to an unexpected error: {ex.Message}");
                }
            }

            return result;
        }
    }
}