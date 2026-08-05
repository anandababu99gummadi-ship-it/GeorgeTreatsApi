using CustomerService.Application.Behaviors;
using CustomerService.Application.Contracts;
using CustomerService.Application.Features.Customers.Commands.CreateCustomer;
using CustomerService.Infrastructure.BlobStorage;
using CustomerService.Infrastructure.ServiceBus;
using CustomerService.Persistence.Data;
using CustomerService.Persistence.Data;
using CustomerService.Persistence.Repositories;
using CustomerService.Persistence.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CustomerService.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Key Vault  configuration  adding
            var keyVaultUrl = new Uri("https://georgetreats-keyvault.vault.azure.net/");

            var credential = builder.Environment.IsDevelopment()
                ? new Azure.Identity.DefaultAzureCredential(new Azure.Identity.DefaultAzureCredentialOptions
                {
                    ExcludeManagedIdentityCredential = true
                })
                : new Azure.Identity.DefaultAzureCredential();

            builder.Configuration.AddAzureKeyVault(keyVaultUrl, credential);


            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

            builder.Services.AddValidatorsFromAssembly(typeof(CreateCustomerCommandValidator).Assembly);
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateCustomerCommand).Assembly));
            builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
            builder.Services.AddSingleton<IServiceBusSender, AzureServiceBusSender>();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            //Console.WriteLine($"SmtpUsername value: {app.Configuration["SmtpUsername"]}");
            //Console.WriteLine($"Connection String in use: {app.Configuration.GetConnectionString("DefaultConnection")}");

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                        var exception = exceptionHandlerFeature?.Error;

                        if (exception is FluentValidation.ValidationException validationException)
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            context.Response.ContentType = "application/json";
                            var errors = validationException.Errors.Select(e => e.ErrorMessage);
                            await context.Response.WriteAsJsonAsync(new { errors });
                        }
                        else
                        {
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
                        }
                    });
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
