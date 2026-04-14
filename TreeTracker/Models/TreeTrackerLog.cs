namespace TreeTracker.Models
{
    public class TreeTrackerLog
    {
        public int ID { get; set; }
        public Guid RunID { get; set; }
        public string? ShopOrderNo { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime LoggedAt { get; set; }
    }
}
