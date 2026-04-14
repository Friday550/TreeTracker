namespace TreeTracker.Models
{
    public class TreeTrackerItem
    {
        public int ID { get; set; }
        public string? ProjectID { get; set; }
        public string? ShopOrderNo { get; set; }
        public string? WorkOrderNo { get; set; }
        public string? TagNo { get; set; }
        public string? PartID { get; set; }
        public string? CurrentTree { get; set; }
        public DateTime? TimeAdded { get; set; }
        public string? TreeLocation { get; set; }
        public string? PreviousTree { get; set; }
    }
}