namespace TreeTracker.Models
{
    public class ManualLog
    {
        public int ID { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? ShopOrderNo { get; set; }
        public string? TreeName { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string UserID { get; set; } = string.Empty;
        public DateTime ActionAt { get; set; }
        public string? Notes { get; set; }
    }
}