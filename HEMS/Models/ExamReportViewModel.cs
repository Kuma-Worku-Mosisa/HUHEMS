namespace HEMS.Models
{
    public class ExamReportViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime DateTaken { get; set; }
        public double Percentage => TotalQuestions > 0 ? Math.Round(((double)Score / TotalQuestions) * 100, 2) : 0;
    }
}