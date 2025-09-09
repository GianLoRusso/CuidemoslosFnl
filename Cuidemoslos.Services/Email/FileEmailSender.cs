using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.Services.Email;

public class FileEmailSender : IEmailSender
{
    private readonly string _outboxPath;

    public FileEmailSender()
    {
        // Guarda los mails como .html en wwwroot/outbox del proyecto Web
        var wwwroot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        _outboxPath = Path.Combine(wwwroot, "outbox");
        Directory.CreateDirectory(_outboxPath);
    }

    public Task SendAsync(string to, string subject, string html)
    {
        var safeSubject = string.Join("_", subject.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeSubject}.html";
        var path = Path.Combine(_outboxPath, fileName);

        var content = new StringBuilder()
            .AppendLine("<!doctype html><meta charset='utf-8'>")
            .AppendLine($"<h3>To: {to}</h3>")
            .AppendLine($"<h3>Subject: {subject}</h3>")
            .AppendLine("<hr/>")
            .AppendLine(html)
            .ToString();

        File.WriteAllText(path, content, Encoding.UTF8);
        return Task.CompletedTask;
    }
}
