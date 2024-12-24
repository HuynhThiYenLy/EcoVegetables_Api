namespace ecovegetables_api.src.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string email, string otp);
        Task<bool> SendLockNotificationEmailAsync(string email, string userName);
    }
}
