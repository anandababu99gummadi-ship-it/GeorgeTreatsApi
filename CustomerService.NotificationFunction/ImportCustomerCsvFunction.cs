using CustomerService.Application.Contracts;
using CustomerService.Domain.Entities;
using CustomerService.Domain.ValueObjects;
using CustomerService.Persistence.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerService.NotificationFunction
{
    public class ImportCustomerCsvFunction
    {
        private readonly ILogger<ImportCustomerCsvFunction> _logger;
        private readonly AppDbContext _dbContext; //  DbContext 
        private readonly IServiceBusSender _serviceBusSender;
        private readonly ICacheService _cacheService;

        public ImportCustomerCsvFunction(ILogger<ImportCustomerCsvFunction> logger, AppDbContext dbContext, IServiceBusSender serviceBusSender, ICacheService cacheService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _serviceBusSender = serviceBusSender;
            _cacheService = cacheService;
        }

        // Why BlobTrigger: this function fires AUTOMATICALLY whenever a new file
        // lands in the "customer-imports" container — no manual call needed.
        // "%AzureBlobStorage:ConnectionString%" reads the connection string name
        // from app settings (same pattern as your Service Bus function).
        [Function("ImportCustomerCsvFunction")]
        public async Task Run(
            [BlobTrigger("customer-imports/{name}", Connection = "AzureBlobStorageConnection")] Stream blobStream,
            string name)
        {
            _logger.LogInformation("CSV import triggered for file: {FileName}", name);

            using var reader = new StreamReader(blobStream);

            // Why skip first line: it's the CSV header row (Name,Email,Phone,Street,City,State,Country,ZipCode),
            // not actual data — we don't want to try saving it as a customer.
            string? header = await reader.ReadLineAsync();

            int successCount = 0;
            int failCount = 0;
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split(',');

                if (columns.Length < 8)
                {
                    _logger.LogWarning("Skipping malformed row: {Line}", line);
                    failCount++;
                    continue;
                }

                var email = columns[1].Trim();

                // Why we check BEFORE inserting: this avoids throwing a SQL exception
                // for something we can predict in advance. Cheaper and cleaner than
                // catching the exception every time — especially useful since the
                // SAME file can get retried automatically by the Blob Trigger's retry
                // mechanism, so we WILL see the same rows again.
                bool alreadyExists = await _dbContext.Customers.AnyAsync(c => c.Email == email);
                if (alreadyExists)
                {
                    _logger.LogWarning("Skipping duplicate customer, email already exists: {Email}", email);
                    failCount++;
                    continue;
                }

                try
                {
                    var customer = new Customer(
                        name: columns[0].Trim(),
                        email: email,
                        phone: columns[2].Trim(),
                        location: new Address(
                            street: columns[3].Trim(),
                            city: columns[4].Trim(),
                            state: columns[5].Trim(),
                            country: columns[6].Trim(),
                            zipCode: columns[7].Trim()
                        )
                    );

                    _dbContext.Customers.Add(customer);

                    // Why SaveChangesAsync HERE, per-row, instead of once at the end:
                    // if we batch everything and ONE row fails (like a duplicate we
                    // didn't catch), EF Core throws and the ENTIRE batch is rolled
                    // back — including rows that were perfectly valid. Saving one at
                    // a time means only the bad row fails; good rows still get saved.
                    await _dbContext.SaveChangesAsync();
                    successCount++;


                    // Why we send this here too: CreateCustomerCommandHandler already does this
                    // for single customer creation (via the API). CSV bulk-upload creates
                    // customers through a DIFFERENT path (Blob Trigger, not the Command
                    // Handler), so without this, CSV-imported customers would silently
                    // NEVER get a welcome email. Same DTO shape, same queue, same pattern —
                    // just triggered from a different entry point.
                    var emailMessage = JsonSerializer.Serialize(new
                    {
                        CustomerId = customer.Id,
                        CustomerName = customer.Name,
                        Email = customer.Email
                    });

                    await _serviceBusSender.SendMessageAsync(emailMessage, cancellationToken: default);
                }
                catch (DbUpdateException ex)
                {
                    // Extra safety net: if a duplicate slips through the check above
                    // (e.g. two identical emails appear in the SAME csv file), this
                    // catches it here instead of crashing the whole function.
                    _logger.LogWarning(ex, "Failed to save row (likely duplicate): {Line}", line);
                    _dbContext.Entry(_dbContext.Customers.Local.Last()).State = EntityState.Detached;
                    failCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error importing row: {Line}", line);
                    failCount++;
                }
            }
            await _cacheService.RemoveAsync("all-customers", cancellationToken:default);

            _logger.LogInformation("CSV import complete. Success: {Success}, Failed: {Failed}", successCount, failCount);
        }
    }
}
