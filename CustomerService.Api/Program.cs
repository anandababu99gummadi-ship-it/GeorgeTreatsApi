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

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
