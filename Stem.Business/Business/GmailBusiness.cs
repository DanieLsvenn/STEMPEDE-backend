using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using MimeKit.Text;
using MimeKit;
using MailKit.Net.Smtp;


namespace Stem.Business.Business
{
    public interface IEmailService
    {
        Task SendEmail(string toMail, string subject, string body);
    }
    public class GmailBusiness : IEmailService
    {
        private readonly IConfiguration _configuration;
        public GmailBusiness(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmail(string toMail, string subject, string body)
        {
            // construct email data
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["Gmail:From"]));
            email.To.Add(MailboxAddress.Parse(toMail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            // send email
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration["Gmail:Smtp"], _configuration.GetValue<int>("Gmail:Port"), MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_configuration["Gmail:Username"], _configuration["Gmail:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
