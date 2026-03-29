using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OnlineExamSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace OnlineExamSystem.Services.ImportExport
{
    public class QuestionImportService : IQuestionImportService
    {
        private readonly ApplicationDbContext _context;

        public QuestionImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionImportResultDto> ImportQuestionsAsync(IFormFile file, int examId, int collegeId)
        {
            var result = new QuestionImportResultDto();

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

            var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null)
            {
                result.Errors.Add("Invalid exam.");
                return result;
            }

            // 🔒 FAIRNESS CHECK
            var hasAttempts = _context.ExamAttempts.Any(ea => ea.ExamId == examId);
            var now = DateTime.Now;
            var isLive = exam.StartDateTime.HasValue && exam.EndDateTime.HasValue &&
                         now >= exam.StartDateTime.Value && now <= exam.EndDateTime.Value;

            if (hasAttempts || isLive)
            {
                result.Errors.Add("Questions cannot be imported after exam is live or attempted.");
                return result;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ================= CSV PARSING (FIXED) =================
                using var stream = file.OpenReadStream();
                using var parser = new TextFieldParser(stream);

                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                // 📌 Read Header
                if (parser.EndOfData)
                {
                    result.Errors.Add("CSV header missing.");
                    return result;
                }

                var headers = parser.ReadFields()
                    .Select(h => h.Trim().ToUpper().Replace(" ", ""))
                    .ToList();

                // 🔍 Flexible column detection
                int qIndex = headers.FindIndex(h => h == "QUESTIONTEXT" || h == "QUESTION");
                int marksIndex = headers.FindIndex(h => h == "QUESTIONMARKS" || h == "MARKS");
                int o1 = headers.FindIndex(h => h == "OPTION1");
                int o2 = headers.FindIndex(h => h == "OPTION2");
                int o3 = headers.FindIndex(h => h == "OPTION3");
                int o4 = headers.FindIndex(h => h == "OPTION4");
                int correctIndex = headers.FindIndex(h =>
                    h == "CORRECTOPTION" || h == "CORRECTANSWER" || h == "ANSWER"
                );

                // ❌ Invalid header check
                if (qIndex == -1 || marksIndex == -1 || o1 == -1 || o2 == -1 || correctIndex == -1)
                {
                    result.Errors.Add("Invalid CSV format. Required columns not found.");
                    return result;
                }

                // 📦 Load existing questions for duplicate check
                var existingQuestions = await _context.Questions
                    .Where(q => q.ExamId == examId)
                    .Select(q => q.Text.Trim().ToLower())
                    .ToListAsync();

                var currentBatch = new HashSet<string>();

                int rowNumber = 1;

                var questionsToAdd = new List<Question>();
                var optionsToAdd = new List<Option>();

                // ================= READ ROWS =================
                while (!parser.EndOfData)
                {
                    rowNumber++;

                    var cols = parser.ReadFields();

                    if (cols == null || cols.Length <= correctIndex)
                    {
                        result.InvalidRows++;
                        continue;
                    }

                    var text = cols[qIndex]?.Trim();
                    var marksStr = cols[marksIndex]?.Trim();

                    // 🔐 Basic validation
                    if (string.IsNullOrWhiteSpace(text) || !int.TryParse(marksStr, out int marks) || marks <= 0)
                    {
                        result.InvalidRows++;
                        continue;
                    }

                    var normalizedText = text.ToLower();

                    // 🔴 Duplicate check (DB + current batch)
                    if (existingQuestions.Contains(normalizedText) || currentBatch.Contains(normalizedText))
                    {
                        result.InvalidRows++;

                        if (result.Errors.Count < 10)
                            result.Errors.Add($"Row {rowNumber}: Duplicate question skipped.");

                        continue;
                    }

                    // 📌 Collect options (handle optional 3rd & 4th)
                    var rawOptions = new List<string?>
    {
        cols[o1]?.Trim(),
        cols[o2]?.Trim(),
        o3 >= 0 && o3 < cols.Length ? cols[o3]?.Trim() : null,
        o4 >= 0 && o4 < cols.Length ? cols[o4]?.Trim() : null
    };

                    var filteredOptions = rawOptions
                        .Where(o => !string.IsNullOrWhiteSpace(o))
                        .ToList();

                    // 🔐 Validate option count (2–4)
                    if (filteredOptions.Count < 2 || filteredOptions.Count > 4)
                    {
                        result.InvalidRows++;
                        continue;
                    }

                    // 🔐 Prevent duplicate options
                    var distinct = filteredOptions
                        .Select(o => o.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (distinct.Count != filteredOptions.Count)
                    {
                        result.InvalidRows++;
                        continue;
                    }

                    // 🔐 Validate correct option index
                    if (!int.TryParse(cols[correctIndex], out int correctOption) ||
                        correctOption < 1 || correctOption > filteredOptions.Count)
                    {
                        result.InvalidRows++;
                        continue;
                    }

                    // 🧠 Create Question
                    var question = new Question
                    {
                        Text = text,
                        Marks = marks,
                        ExamId = examId,
                        CollegeId = collegeId
                    };

                    questionsToAdd.Add(question);

                    // 🧠 Create Options
                    for (int i = 0; i < filteredOptions.Count; i++)
                    {
                        optionsToAdd.Add(new Option
                        {
                            Question = question,
                            CollegeId = collegeId,
                            Text = filteredOptions[i],
                            IsCorrect = (i + 1 == correctOption)
                        });
                    }

                    currentBatch.Add(normalizedText);
                    result.InsertedQuestions++;
                    result.InsertedOptions += filteredOptions.Count;
                }

                _context.Questions.AddRange(questionsToAdd);
                _context.Options.AddRange(optionsToAdd);

                await _context.SaveChangesAsync();

                // 🔄 Recalculate marks
                var totalMarks = _context.Questions
                    .Where(q => q.ExamId == examId)
                    .Sum(q => q.Marks);

                exam.TotalMarks = totalMarks;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                result.TotalRows = result.InsertedQuestions;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Errors.Add($"Unexpected error: {ex.Message}");
            }

            return result;
        }
    }
}