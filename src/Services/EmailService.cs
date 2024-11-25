using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ecovegetables_api.src.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly string _smtpFromAddress;
        private readonly string _smtpFromDisplayName;

        public EmailService(IConfiguration configuration)
        {
            // Lấy thông tin cấu hình từ appsettings.json hoặc biến môi trường
            _smtpServer = configuration["Email:SmtpServer"];
            _smtpUser = configuration["Email:SmtpUser"];
            _smtpPassword = configuration["Email:SmtpPassword"];
            _smtpFromAddress = configuration["Email:FromAddress"];
            _smtpFromDisplayName = configuration["Email:FromDisplayName"];
        }

        public async Task<bool> SendOtpEmailAsync(string email, string otp)
        {
            try
            {
                var smtpClient = new SmtpClient(_smtpServer)
                {
                    Port = 587,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpFromAddress, _smtpFromDisplayName),  // Tên hiển thị
                    Subject = "OTP for Registration",
                    Body = $"Your OTP for registration is: {otp}",
                    IsBodyHtml = false,
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết để dễ dàng chẩn đoán sự cố
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
