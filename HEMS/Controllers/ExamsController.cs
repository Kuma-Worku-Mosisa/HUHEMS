using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HEMS.Data;
using HEMS.Models;
using HEMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace HEMS.Controllers
{
    [Authorize(Roles = "Coordinator")]
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly IIdObfuscator _idObfuscator;

        public ExamsController(ApplicationDbContext context, IIdObfuscator idObfuscator)
        {
            _context = context;
            _idObfuscator = idObfuscator;
            Account account = new Account(
                "di0eli4di",
                "113677216573493",
                "MuED4inIpYVYE0U8ItejDUO3as0"
            );

            _cloudinary = new Cloudinary(account);

            // FIX FOR CS1061: 
            // In many versions of the SDK, you set the timeout via the Api.Timeout property directly,
            // or by accessing the underlying CallTimeout.
            _cloudinary.Api.Timeout = (int)TimeSpan.FromSeconds(300).TotalMilliseconds;
        }

        // 1. List all exams
        public async Task<IActionResult> Index()
        {
            var exams = await _context.Exams
                .Include(e => e.Questions)
                .OrderByDescending(e => e.AcademicYear)
                .ToListAsync();
            return View(exams);
        }

        // 2. Create Exam (GET)
        public IActionResult Create()
        {
            var model = new Exam { AcademicYear = DateTime.Now.Year };
            return View(model);
        }

        // 3. Create Exam (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam exam)
        {
            ModelState.Remove("Questions");
            ModelState.Remove("StudentExams");

            if (ModelState.IsValid)
            {
                if (exam.ExamStatus == "Published")
                {
                    exam.ExamCode = GenerateRandomCode();
                }

                _context.Add(exam);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // 4. Edit Exam (GET)
        public async Task<IActionResult> Edit(string? id)
        {
            if (!_idObfuscator.TryDecode(id, out var examId)) return NotFound();
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();
            return View(exam);
        }

        // 5. Edit Exam (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Exam exam)
        {
            if (!_idObfuscator.TryDecode(id, out var examId) || examId != exam.ExamId) return NotFound();

            ModelState.Remove("Questions");
            ModelState.Remove("StudentExams");

            if (ModelState.IsValid)
            {
                try
                {
                    if (exam.ExamStatus == "Published" && string.IsNullOrEmpty(exam.ExamCode))
                    {
                        exam.ExamCode = GenerateRandomCode();
                    }

                    _context.Update(exam);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Exams.Any(e => e.ExamId == exam.ExamId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // 5b. Publish Toggle Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(string id)
        {
            if (!_idObfuscator.TryDecode(id, out var examId)) return NotFound();
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null) return NotFound();


            if (exam.ExamStatus != "Published")
            {
                exam.ExamStatus = "Published";
                if (string.IsNullOrEmpty(exam.ExamCode))
                {
                    exam.ExamCode = GenerateRandomCode();
                }

                _context.Update(exam);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Exam '{exam.ExamTitle}' published successfully! Code: {exam.ExamCode}";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. View Exam Details
        public async Task<IActionResult> Details(string? id)
        {
            if (!_idObfuscator.TryDecode(id, out var examId)) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.Questions!)
                .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(m => m.ExamId == examId);

            if (exam == null) return NotFound();
            return View(exam);
        }

        // 7. Reports
        public async Task<IActionResult> Reports(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (!_idObfuscator.TryDecode(id, out var examId)) return NotFound();
                var exam = await _context.Exams
                    .Include(e => e.Questions)
                    .FirstOrDefaultAsync(e => e.ExamId == examId);

                if (exam == null) return NotFound();

                int totalQuestionsCount = exam.Questions?.Count ?? 0;
                var allStudents = await _context.Students.OrderBy(s => s.FullName).ToListAsync();
                var attempts = await _context.ExamAttempts.Where(a => a.ExamId == examId).ToListAsync();

                var reportData = allStudents.Select(s =>
                {
                    var studentAttempts = attempts.Where(a => a.StudentId == s.StudentId).ToList();
                    int score = studentAttempts.Count(a => a.IsCorrect);
                    string status = !studentAttempts.Any() ? "Not Taken" : (totalQuestionsCount > 0 && score * 100.0 / totalQuestionsCount >= 50) ? "Passed" : "Failed";

                    return new ExamReportViewModel
                    {
                        StudentName = s.FullName,
                        Score = score,
                        TotalQuestions = totalQuestionsCount,
                        DateTaken = studentAttempts.Any() ? studentAttempts.Max(a => a.StartTime) : DateTime.MinValue,
                        Status = status
                    };
                }).ToList();

                ViewBag.ExamTitle = exam.ExamTitle;
                ViewBag.IsGeneralReport = false;
                return View(reportData);
            }
            else
            {
                var generalReport = await _context.Exams
                    .Select(e => new ExamReportViewModel
                    {
                        ExamId = e.ExamId,
                        ExamTitle = e.ExamTitle,
                        TotalQuestions = e.Questions.Count,
                        StudentCount = _context.ExamAttempts.Where(a => a.ExamId == e.ExamId).Select(a => a.StudentId).Distinct().Count(),
                        AverageScore = _context.ExamAttempts.Where(a => a.ExamId == e.ExamId && a.IsCorrect).Count()
                    }).ToListAsync();

                ViewBag.ExamTitle = "General Performance Report";
                ViewBag.IsGeneralReport = true;
                return View(generalReport);
            }
        }

        // 8. Delete Exam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!_idObfuscator.TryDecode(id, out var examId)) return NotFound();
            var exam = await _context.Exams.FindAsync(examId);
            if (exam != null)
            {
                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 9. Bulk Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(string id, IFormFile examZip)
        {
            if (!_idObfuscator.TryDecode(id, out var examId)) return NotFound();
            if (examZip == null || examZip.Length == 0) return RedirectToAction("Details", new { id = _idObfuscator.Encode(examId) });


            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            var existingInDb = await _context.Questions
                .Where(q => q.ExamId == examId)
                .Select(q => q.QuestionText.Trim().ToLower())
                .ToListAsync();

            var processedQuestions = new HashSet<string>(existingInDb);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                using (var stream = examZip.OpenReadStream())
                using (var archive = new ZipArchive(stream))
                {
                    archive.ExtractToDirectory(tempFolder);
                }

                string? manifestPath = Directory.GetFiles(tempFolder, "manifest.csv", SearchOption.AllDirectories).FirstOrDefault();
                if (string.IsNullOrEmpty(manifestPath)) throw new Exception("manifest.csv missing from ZIP");

                using (var reader = new StreamReader(manifestPath))
                using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<dynamic>().ToList();
                    int rowCount = 1;

                    foreach (var row in records)
                    {
                        rowCount++;
                        string cleanedText = (row.QuestionText?.ToString() ?? string.Empty).Trim();
                        string lookupKey = cleanedText.ToLower();

                        if (string.IsNullOrEmpty(cleanedText)) continue;

                        if (processedQuestions.Contains(lookupKey))
                            throw new Exception($"Duplicate found: '{cleanedText}' at row {rowCount}.");

                        var question = new Question { ExamId = examId, QuestionText = cleanedText, MarkWeight = 1.0m };

                        string? imageName = row.ImageName?.ToString();
                        if (!string.IsNullOrEmpty(imageName))
                        {
                            var imgPath = Directory.GetFiles(tempFolder, imageName, SearchOption.AllDirectories).FirstOrDefault();
                            if (imgPath != null)
                            {
                                var uploadParams = new ImageUploadParams()
                                {
                                    File = new FileDescription(imgPath),
                                    PublicId = $"hems_{Guid.NewGuid()}",
                                    Folder = "exam_questions"
                                };
                                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                                if (uploadResult.Error != null)
                                    throw new Exception($"Cloudinary: {uploadResult.Error.Message}");

                                question.ImagePath = uploadResult.SecureUrl?.ToString();
                            }
                        }

                        _context.Questions.Add(question);
                        await _context.SaveChangesAsync();
                        processedQuestions.Add(lookupKey);

                        string choicesRaw = row.Choices?.ToString() ?? string.Empty;
                        string[] choiceArray = choicesRaw.Split('|');
                        if (int.TryParse(row.CorrectChoiceIndex?.ToString(), out int correctIdx))
                        {
                            for (int i = 0; i < choiceArray.Length; i++)
                            {
                                _context.Choices.Add(new Choice
                                {
                                    QuestionId = question.QuestionId,
                                    ChoiceText = choiceArray[i].Trim(),
                                    IsAnswer = (i == correctIdx)
                                });
                            }
                        }
                    }
                }


                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = "Bulk upload successful!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Upload Failed: {ex.Message}";
            }
            finally { if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true); }

            return RedirectToAction("Details", new { id = _idObfuscator.Encode(examId) });
        }

        private string GenerateRandomCode() => new Random().Next(1000, 9999).ToString();
    }

    public class ExamReportViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateTaken { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int AverageScore { get; set; }
        public int TotalQuestions { get; set; }

        public double Percentage => TotalQuestions > 0 ? Math.Round(((double)Score / TotalQuestions) * 100, 2) : 0;
        public double AvgPercentage => (TotalQuestions == 0 || StudentCount == 0) ? 0 : Math.Round((AverageScore / (double)(TotalQuestions * StudentCount)) * 100, 2);
    }
}
