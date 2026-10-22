using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NguyenHoangPhuoc_AttendanceAPI.Services;

public sealed class EmailOtpService
{
    private readonly IConfiguration _configuration;

    public EmailOtpService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendOtpAsync(string email, string otp)
    {
        var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var user = _configuration["Email:SmtpUser"];
        var password = _configuration["Email:SmtpPassword"];
        var from = _configuration["Email:From"] ?? user;

        if (string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException(
                "Chua cau hinh Email:SmtpUser, Email:SmtpPassword, Email:From.");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Ma OTP dang nhap";
        message.Body = new TextPart("plain")
        {
            Text = $"Ma OTP cua ban la: {otp}\nMa co hieu luc trong 5 phut. Khong chia se ma nay voi bat ky ai."
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, 587, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(user, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
