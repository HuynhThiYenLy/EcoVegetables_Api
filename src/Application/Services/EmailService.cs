using System;
using System.Net;
using System.Net.Mail;
using ecovegetables_api.src.Application.Interfaces;

namespace ecovegetables_api.src.Application.Services
{
    public class EmailService : IEmailService
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
                    From = new MailAddress(_smtpFromAddress, _smtpFromDisplayName), // Tên hiển thị
                    Subject = "Your OTP Code",
                    IsBodyHtml = true, // Định dạng HTML cho nội dung email
                    Body = $@"
                    <!DOCTYPE html>
                    <html>
                        <head>
                            <style>
                                .container {{
                                    font-family: Arial, sans-serif;
                                    line-height: 1.6;
                                    text-align: center;
                                    max-width: 600px;
                                    margin: 20px auto;
                                    padding: 20px;
                                    border: 1px solid #ddd;
                                    border-radius: 10px;
                                }}
                                .otp {{
                                    font-size: 24px;
                                    font-weight: bold;
                                    color: #4CAF50;
                                    margin: 20px 0;
                                }}
                                .footer {{
                                    font-size: 12px;
                                    color: #aaa;
                                    margin-top: 20px;
                                    border-top: 1px solid #ddd;
                                    padding-top: 10px;
                                }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <h2>Your OTP Code</h2>
                                <p>Your OTP for registration is:</p>
                                <p class='otp'>{otp}</p>
                                <p>Please use this code within 10 minutes. Do not share it with anyone.</p>
                                <div class='footer'>
                                    &copy; {DateTime.UtcNow.Year} Ecovegetables. If you didn't request this, please ignore this email.
                                </div>
                            </div>
                        </body>
                    </html>"
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendLockNotificationEmailAsync(string email, string userName)
        {
            try
            {
                var smtpClient = new SmtpClient(_smtpServer)
                {
                    Port = 587,
                    Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                    EnableSsl = true,
                };

                var subject = "Your Account Has Been Locked";

                var body = $@"
        <!DOCTYPE html>
        <html>
            <head>
                <style>
                    .container {{
                        font-family: Arial, sans-serif;
                        line-height: 1.6;
                        max-width: 600px;
                        margin: 20px auto;
                        padding: 20px;
                        border: 1px solid #ddd;
                        border-radius: 10px;
                        text-align: center;
                        background-color: #f9f9f9;
                    }}
                    .header {{
                        font-size: 24px;
                        font-weight: bold;
                        color: red;
                        margin-bottom: 20px;
                    }}
                    .message {{
                        font-size: 16px;
                        color: #333;
                        margin-bottom: 20px;
                    }}
                    .footer {{
                        font-size: 12px;
                        color: #aaa;
                        margin-top: 20px;
                        border-top: 1px solid #ddd;
                        padding-top: 10px;
                    }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>Your Account Has Been Locked</div>
                    <div class='message'>
                        <p>Dear {userName},</p>
                        <p>Your account has been locked due to security reasons. If you believe this is a mistake, please contact our support team.</p>
                        <p>Thank you for your understanding.</p>
                    </div>
                    <div class='footer'>
                        &copy; {DateTime.UtcNow.Year} Ecovegetables. If you have any questions, please contact us.
                    </div>
                </div>
            </body>
        </html>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpFromAddress, _smtpFromDisplayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
