using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;


namespace CustomerService.NotificationFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private readonly IConfiguration _configuration;

        public Function1(ILogger<Function1> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function(nameof(Function1))]
        public async Task Run(
            [ServiceBusTrigger("welcome-email-queue", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation($"Message Body: {message.Body}");

            var data = JsonSerializer.Deserialize<WelcomeEmailMessage>(message.Body.ToString());

            if (data is not null)
            {
                await SendEmailAsync(data.Email, data.CustomerName);
                _logger.LogInformation($"Welcome email sent successfully to {data.Email}");
            }
            else
            {
                _logger.LogWarning("Could not deserialize message body.");
            }

            await messageActions.CompleteMessageAsync(message);
        }

        private async Task SendEmailAsync(string toEmail, string customerName)
        {
            var smtpUsername = _configuration["SmtpUsername"];
            var smtpAppPassword = _configuration["SmtpAppPassword"];

            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpAppPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUsername!, "George Treats"),
                Subject = "Welcome to George Treats!",
                Body = $"Hi {customerName},\n\nWelcome to George Treats! You've successfully registered.\n\nThank you!",
                IsBodyHtml = false
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }

    public class WelcomeEmailMessage
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}