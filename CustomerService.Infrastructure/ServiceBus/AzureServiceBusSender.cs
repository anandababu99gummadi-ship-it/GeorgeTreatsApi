using Azure.Messaging.ServiceBus;
using CustomerService.Application.Contracts;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Infrastructure.ServiceBus
{
    public class AzureServiceBusSender : IServiceBusSender
    {
        private readonly string _connectionString;
        private readonly string _queueName;

        public AzureServiceBusSender(IConfiguration configuration)
        {
            _connectionString = configuration["AzureServiceBus:ConnectionString"]!;
            _queueName = configuration["AzureServiceBus:QueueName"]!;
        }

        public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            await using var client = new ServiceBusClient(_connectionString);
            ServiceBusSender sender = client.CreateSender(_queueName);

            var serviceBusMessage = new ServiceBusMessage(message);
            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
        }
    }
}