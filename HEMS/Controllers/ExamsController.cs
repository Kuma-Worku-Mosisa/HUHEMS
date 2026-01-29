using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HEMS.Data;
using HEMS.Models;
using Microsoft.AspNetCore.Authorization;

namespace HEMS.Controllers
{
    [Authorize(Roles = "Coordinator")]
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamsController(ApplicationDbContext context)
        {
            _context = context;
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
                _context.Add(exam);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        // 4. Edit Exam (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            return View(exam);
        }

        // 5. Edit Exam (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam exam)
        {
            if (id != exam.ExamId) return NotFound();

            ModelState.Remove("Questions");
            ModelState.Remove("StudentExams");

            if (ModelState.IsValid)
            {
                try
                {
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

        // 6. View Exam Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var exam = await _context.Exams
                .Include(e => e.Questions!)
                .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(m => m.ExamId == id);

            if (exam == null) return NotFound();
            return View(exam);
        }

        // --- FIXED 7. Reports: Using ViewModel to prevent TotalQuestions error ---
        public async Task<IActionResult> Reports(int id)
        {
            // Get exam details first to get the total question count
            var exam = await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null) return NotFound();

            int totalQuestionsCount = exam.Questions?.Count ?? 0;

            // Group individual question attempts by student to get a summary
            var reportData = await _context.ExamAttempts
                .Where(a => a.ExamId == id)
                .Include(a => a.Student)
                .GroupBy(a => new { a.StudentId, a.Student.FullName })
                .Select(g => new ExamReportViewModel
                {
                    StudentName = g.Key.FullName,
                    Score = g.Count(x => x.IsCorrect),
                    TotalQuestions = totalQuestionsCount,
                    DateTaken = g.Max(x => x.StartTime)
                })
                .ToListAsync();

            ViewBag.ExamTitle = exam.ExamTitle;
            return View(reportData); // Now passing List<ExamReportViewModel>
        }

        // 8. Delete Exam
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam != null)
            {
                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }

    // ViewModel inside the namespace to support the Reports view
    public class ExamReportViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime DateTaken { get; set; }
        public double Percentage => TotalQuestions > 0 ? Math.Round(((double)Score / TotalQuestions) * 100, 2) : 0;
    }
}