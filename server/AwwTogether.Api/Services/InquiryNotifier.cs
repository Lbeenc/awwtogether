using AwwTogether.Api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AwwTogether.Api.Services;

/// <summary>Tells the duo about a new contact-form inquiry.</summary>
public interface IInquiryNotifier
{
    Task NotifyAsync(Inquiry inquiry, CancellationToken ct = default);
}

/// <summary>"Email" section of configuration. The password comes from the Email__Password secret.</summary>
public sealed class EmailOptions
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    /// <summary>The Gmail account that sends the notification (and signs in to SMTP).</summary>
    public string Username { get; set; } = "";
    /// <summary>A Gmail App Password — never the normal account password.</summary>
    public string Password { get; set; } = "";
    /// <summary>Where notifications are delivered.</summary>
    public string To { get; set; } = "";

    public bool IsConfigured => Username.Length > 0 && Password.Length > 0 && To.Length > 0;
}

/// <summary>
/// Emails each inquiry through Gmail SMTP. Reply-To is the person who filled in the form,
/// so hitting Reply in Gmail answers them directly. Does nothing until a password is configured.
/// </summary>
public sealed class SmtpInquiryNotifier(Microsoft.Extensions.Options.IOptions<EmailOptions> options, ILogger<SmtpInquiryNotifier> log)
    : IInquiryNotifier
{
    private readonly EmailOptions _opt = options.Value;

    public async Task NotifyAsync(Inquiry inquiry, CancellationToken ct = default)
    {
        if (!_opt.IsConfigured)
        {
            log.LogWarning("Email not configured (Email:Password missing); inquiry saved but no email sent");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("AwwTogether website", _opt.Username));
        message.To.Add(MailboxAddress.Parse(_opt.To));
        if (MailboxAddress.TryParse(inquiry.Email, out var visitor))
        {
            visitor.Name = inquiry.Name;
            message.ReplyTo.Add(visitor);
        }

        var who = string.IsNullOrEmpty(inquiry.Brand) ? inquiry.Name : $"{inquiry.Name} ({inquiry.Brand})";
        message.Subject = $"New inquiry from {OneLine(who)}";
        // Plain text only: whatever the visitor typed is shown as text, never as HTML.
        message.Body = new TextPart("plain")
        {
            Text =
                $"""
                New message from the contact form on awwtogether.com

                Name:   {inquiry.Name}
                Email:  {inquiry.Email}
                Brand:  {inquiry.Brand ?? "—"}
                Budget: {inquiry.Budget ?? "—"}
                Sent:   {inquiry.CreatedAt:dddd d MMMM yyyy, h:mm tt} UTC

                Message:
                {inquiry.Message}

                —
                Hit Reply to answer {inquiry.Name} directly.
                """,
        };

        using var smtp = new SmtpClient { Timeout = 15_000 };
        await smtp.ConnectAsync(_opt.SmtpHost, _opt.SmtpPort, SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(_opt.Username, _opt.Password, ct);
        await smtp.SendAsync(new MailKit.SendRequest(message), ct);
        await smtp.DisconnectAsync(true, ct);
        log.LogInformation("Inquiry email sent to {To}", _opt.To);
    }

    private static string OneLine(string s) => s.Replace('\r', ' ').Replace('\n', ' ');
}
