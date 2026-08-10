using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using CustomerService.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using CustomerService.Application.Contracts;
using CustomerService.Infrastructure.ServiceBus;


var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Key Vault configuration adding
var keyVaultUrl = new Uri("https://georgetreats-keyvault.vault.azure.net/");
var credential = builder.Environment.IsDevelopment()
    ? new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ExcludeManagedIdentityCredential = true
    })
    : new DefaultAzureCredential();
builder.Configuration.AddAzureKeyVault(keyVaultUrl, credential);
builder.Services.AddSingleton<IServiceBusSender, AzureServiceBusSender>();

// Why EnableRetryOnFailure: Azure SQL Serverless auto-pauses when idle to
// save cost, and takes a few seconds to "wake up" on the next request.
// Without retry, that wake-up window causes an immediate failure. This tells
// EF Core to automatically retry a few times instead of failing right away.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
