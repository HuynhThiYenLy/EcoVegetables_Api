using ecovegetables_api.src.Domain.Entities;

namespace ecovegetables_api.src.Application.DTOs.User.Response
{
    public class LoginResponse
    {
        public UserResponse UserResponse { get; set; }  
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
