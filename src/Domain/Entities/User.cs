namespace ecovegetables_api.src.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; } = true;

        public string Avatar { get; set; } = "https://i.ibb.co/mDbnmNc/avatar-default.png";
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; }

        // MARK: Login GGOAuth
        public string GoogleId { get; set; }

        // MARK: Token
        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpires { get; set; }
    }
}
