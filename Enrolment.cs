namespace RaceDay.Web.Models
{
    /// <summary>Flat enrolment shape returned by the API (my-enrolments and event enrolments).</summary>
    public class Enrolment
    {
        public int EnrolmentId { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int CategoryId { get; set; }
        public string Category { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string Event { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public bool HasResult { get; set; }
        public string? FinishTime { get; set; }
        public int? OverallPosition { get; set; }
        public int? CategoryPosition { get; set; }
    }
}
