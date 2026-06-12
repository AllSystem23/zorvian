using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Zorvian.Application.Interfaces;

namespace Zorvian.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config["EmailSettings:From"]));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        var builder = new BodyBuilder();
        if (isHtml) builder.HtmlBody = body;
        else builder.TextBody = body;

        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(
            _config["EmailSettings:Host"],
            int.Parse(_config["EmailSettings:Port"] ?? "587"),
            MailKit.Security.SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(_config["EmailSettings:Username"], _config["EmailSettings:Password"]);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    public async Task SendWelcomeEmailAsync(string to, string name)
    {
        string subject = "¡Bienvenido a la familia Zorvian!";
        string body = $@"
            <div style='font-family: sans-serif; color: #333;'>
                <h1 style='color: #2563eb;'>¡Hola {name}!</h1>
                <p>Gracias por contactarnos. Hemos recibido tu solicitud de información y uno de nuestros asesores comerciales te contactará muy pronto.</p>
                <p>En Zorvian ERP estamos comprometidos con llevar tu empresa al siguiente nivel.</p>
                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                <small style='color: #666;'>Este es un mensaje automático, por favor no respondas a este correo.</small>
            </div>";

        await SendEmailAsync(to, subject, body);
    }
}
