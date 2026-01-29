using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HEMS.Data;
using HEMS.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HEMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var exams = await _context.Exams
                .Where(e => e.ExamStatus == "Published")
                .ToListAsync();
            return View(exams);
        }

        public async Task<IActionResult> TakeExam(int examId, int index = 0)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return BadRequest("Student profile not found.");

            var exam = await _context.Exams
                .Include(e => e.Questions)
                .ThenInclude(q => q.Choices)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null) return NotFound();

            if (index >= exam.Questions.Count)
                return RedirectToAction("ViewResult", new { examId });

            var questionsList = exam.Questions.OrderBy(q => q.QuestionId).ToList();
            var currentQuestion = questionsList.ElementAt(index);

            // --- PERSISTENCE LOGIC ---
            // Fetch attempts to color the sidebar AND pre-select the student's previous choice
            var allAttempts = await _context.ExamAttempts
                .Where(a => a.StudentId == student.StudentId && a.ExamId == examId)
                .ToListAsync();

            // 1. Find the choice for the CURRENT question so it doesn't clear on refresh
            var existingAttempt = allAttempts.FirstOrDefault(a => a.QuestionId == currentQuestion.QuestionId);
            ViewBag.SelectedChoiceId = existingAttempt?.ChoiceId;

            // 2. Map indices for the Navigation Sidebar
            ViewBag.AnsweredIndices = allAttempts
                .Where(a => a.ChoiceId != 0)
                .Select(a => questionsList.FindIndex(q => q.QuestionId == a.QuestionId))
                .ToList();

            ViewBag.FlaggedIndices = allAttempts
                .Where(a => a.IsFlagged)
                .Select(a => questionsList.FindIndex(q => q.QuestionId == a.QuestionId))
                .ToList();

            ViewBag.Index = index;
            ViewBag.Total = questionsList.Count;
            ViewBag.ExamId = examId;

            return View(currentQuestion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAnswer(int qId, int choiceId, bool flagged, int examId, int nextIdx)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return BadRequest("Student profile not found.");

            var choice = await _context.Choices.FirstOrDefaultAsync(c => c.ChoiceId == choiceId);
            bool correct = (choice != null && choice.IsAnswer);

            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.StudentId == student.StudentId && a.QuestionId == qId);

            if (attempt == null)
            {
                attempt = new ExamAttempt
                {
                    StudentId = student.StudentId,
                    ExamId = examId,
                    QuestionId = qId,
                    ChoiceId = choiceId,
                    IsCorrect = correct,
                    IsFlagged = flagged,
                    UserId = userId
                };
                _context.ExamAttempts.Add(attempt);
            }
            else
            {
                attempt.ChoiceId = choiceId;
                attempt.IsCorrect = correct;
                attempt.IsFlagged = flagged;
                _context.Update(attempt);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("TakeExam", new { examId = examId, index = nextIdx });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFlag([FromBody] FlagUpdateModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return NotFound();

            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.StudentId == student.StudentId && a.QuestionId == model.QuestionId);

            if (attempt == null)
            {
                attempt = new ExamAttempt
                {
                    StudentId = student.StudentId,
                    QuestionId = model.QuestionId,
                    IsFlagged = model.IsFlagged,
                    UserId = userId,
                    ExamId = model.ExamId
                };
                _context.ExamAttempts.Add(attempt);
            }
            else
            {
                attempt.IsFlagged = model.IsFlagged;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> ViewResult(int examId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return NotFound();

            var exam = await _context.Exams.FindAsync(examId);
            var score = await _context.ExamAttempts
                .CountAsync(a => a.StudentId == student.StudentId && a.IsCorrect && a.ExamId == examId);

            ViewBag.Total = await _context.Questions.CountAsync(q => q.ExamId == examId);
            ViewBag.ExamTitle = exam?.ExamTitle;

            // --- TIMER CLEANUP ---
            // Pass ExamId so the View knows which localStorage key to delete
            ViewBag.ExamId = examId;

            return View(score);
        }
    }

    public class FlagUpdateModel
    {
        public int ExamId { get; set; }
        public int QuestionId { get; set; }
        public bool IsFlagged { get; set; }
    }
}