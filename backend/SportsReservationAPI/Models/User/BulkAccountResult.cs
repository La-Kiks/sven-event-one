namespace SportsReservationAPI.Models.User
{
    public class BulkAccountResult
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = "";
        public string Status { get; set; } = ""; // "sent" | "failed"
        public string? Error { get; set; }
    }
}
