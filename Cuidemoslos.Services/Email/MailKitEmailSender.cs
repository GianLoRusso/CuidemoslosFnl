using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Cuidemoslos.Services.Email;

public class MailKitEmailSender : IEmailSender
{
    public async Task SendAsync(string to, string subject, string html)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse("noreply@cuidemoslos.local"));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

        using var smtp = new SmtpClient();                       // <── usa el alias
        await smtp.ConnectAsync("localhost", 25, SecureSocketOptions.Auto);
        await smtp.SendAsync(msg);
        await smtp.DisconnectAsync(true);
    }
}

