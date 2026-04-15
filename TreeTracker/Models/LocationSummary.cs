namespace TreeTracker.Models
{
    public class LocationSummary
    {
        public string Location { get; set; } = string.Empty;
        public int TreeCount { get; set; }
        public int ShopOrderCount { get; set; }
    }
}