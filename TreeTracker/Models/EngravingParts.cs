namespace TreeTracker.Models
{
    public class EngravingPart
    {
        public string? Phase { get; set; }
        public string? SetID { get; set; }
        public string? SubPartID { get; set; }
        public bool IsChecked { get; set; } = false;
    }
}