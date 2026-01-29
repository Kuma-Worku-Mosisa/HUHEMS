using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    public class StudentExam
    {
        [Key]
        public int StudentToExamId { get; set; } // PK [cite: 113]

        public int StudentId { get; set; } // FK [cite: 117]
        public int ExamId { get; set; } // FK [cite: 119]

        public DateTime StartDateTime { get; set; } // [cite: 121]
        public DateTime? EndDateTime { get; set; } // [cite: 124]

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }
    }
}