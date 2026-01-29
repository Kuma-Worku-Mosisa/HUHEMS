using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEMS.Models
{
    public class Choice
    {
        [Key]
        public int ChoiceId { get; set; }

        [Required]
        public string ChoiceText { get; set; } = string.Empty;

        public bool IsAnswer { get; set; } = false; // Add the '= false' for safety

        // FK Relationship to Question
        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question? Question { get; set; }
    }
}