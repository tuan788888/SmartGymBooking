namespace SmartGymBooking.Models
{
    public class UserModel
    {
        public string PK { get; set; } = string.Empty;

        public string SK { get; set; } = string.Empty;

        public string EntityType { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string StaffType { get; set; } = string.Empty;

        public string Specialization { get; set; } = string.Empty;

        public int Experience { get; set; }

        public string Status { get; set; } = "ACTIVE";

        public string CreatedAt { get; set; } = string.Empty;
    }
}