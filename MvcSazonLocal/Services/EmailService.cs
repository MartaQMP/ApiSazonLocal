using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SazonLocalInterfaces.Services;
using SazonLocalModels.Models;

namespace MvcSazonLocal.Services
{
    public class EmailService: IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
            Console.WriteLine($"SMTP SERVER: {_settings.Server}");
        }

        #region EMAIL SIN ARCHIVOS
        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _settings.SenderName,
                _settings.SenderEmail));

            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            message.Body = new TextPart(isHtml ? "html" : "plain")
            {
                Text = body
            };

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _settings.Server,
                _settings.Port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        #endregion

        #region EMAIL CON ARCHIVOS
        public async Task SendEmailWithAttachmentBytesAsync(string toEmail, string subject, string body, byte[] pdfBytes, string fileName)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _settings.SenderName,
                _settings.SenderEmail));

            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };

            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                builder.Attachments.Add(fileName, pdfBytes, ContentType.Parse("application/pdf"));
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(_settings.Server, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        #endregion
    }
}
