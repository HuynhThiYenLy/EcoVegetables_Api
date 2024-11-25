using System;

namespace ecovegetables_api.src.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }

        public string Avatar { get; set; }
        public bool IsActive { get; set; } = true;
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; }

        // Đăng nhập qua Google OAuth
        public string GoogleId { get; set; }

        // Ghi đè phương thức ToString để log toàn bộ thông tin của đối tượng
        public override string ToString()
        {
            return $"Id: {Id}, Fullname: {Fullname}, Email: {Email}, Phone: {Phone}, Avatar: {Avatar ?? "N/A"}, Address: {Address ?? "N/A"}, IsActive: {IsActive}, CreatedAt: {CreatedAt}, UpdatedAt: {UpdatedAt}, GoogleId: {GoogleId ?? "N/A"}";
        }
    }
}
