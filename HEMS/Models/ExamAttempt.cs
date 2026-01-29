using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    public class ExamAttempt
    {
        [Key]
        public int ExamAttemptId { get; set; }

        // 1. The Foreign Key Property
        public int StudentId { get; set; }

        // 2. The Navigation Property linked explicitly to the ID above
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        public int ExamId { get; set; }
        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }

        public int QuestionId { get; set; }
        public int ChoiceId { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsFlagged { get; set; }
        public double Score { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
        public string? UserId { get; set; }
    }
}