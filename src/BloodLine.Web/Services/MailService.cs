using System.Net;
using System.Net.Mail;
namespace BloodLine.Web.Services;

public class MailService(IConfiguration config, IWebHostEnvironment env)
{
    public async Task Send(string recipient, string subject, string body)
    {
        var host = config["Mail:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            if (!env.IsDevelopment())
                throw new InvalidOperationException("Configure Mail:Host before sending account emails.");
            var dir = Path.Combine(env.ContentRootPath, "App_Data", "mail");
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, Guid.NewGuid() + ".txt"), $"To: {recipient}\nSubject: {subject}\n\n{body}");
            return;
        }
        using var client = new SmtpClient(host, config.GetValue<int>("Mail:Port", 587)) { EnableSsl = config.GetValue<bool>("Mail:EnableSsl", true), Credentials = new NetworkCredential(config["Mail:Username"], config["Mail:Password"]) };
        using var message = new MailMessage(config["Mail:From"] ?? throw new InvalidOperationException("Configure Mail:From."), recipient, subject, body);
        await client.SendMailAsync(message);
    }
}
