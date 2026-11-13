namespace SportsReservationAPI.Models.Announcement
{
    public class AnnouncementSendResult
    {
        public int Total { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public List<AnnouncementFailure> Failures { get; set; } = [];
    }

    public class AnnouncementFailure
    {
        public string Email { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
