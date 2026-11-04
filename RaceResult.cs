namespace RaceDay.Web.Models
{
    public class RaceResult
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string FinishTime { get; set; } = string.Empty;
        public int TotalSeconds { get; set; }
        public int OverallPosition { get; set; }
        public int CategoryPosition { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CaptureResultViewModel
    {
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public string FinishTime { get; set; } = string.Empty;
        public int OverallPosition { get; set; }
        public int CategoryPosition { get; set; }
    }
}
