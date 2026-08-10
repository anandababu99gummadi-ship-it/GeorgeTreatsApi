using Azure.Identity;
using CustomerService.Application.Behaviors;
using CustomerService.Application.Contracts;
using CustomerService.Application.Features.Customers.Commands.CreateCustomer;
using CustomerService.Infrastructure.BlobStorage;
using CustomerService.Infrastructure.Caching;
using CustomerService.Infrastructure.ServiceBus;
using CustomerService.Persistence.Data;
using CustomerService.Persistence.Data;
using CustomerService.Persistence.Repositories;
using CustomerService.Persistence.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Azure.Core;
using Microsoft.OpenApi.Models;
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

            

                 TokenCredential  credential= builder.Environment.IsDevelopment()
                ? new ChainedTokenCredential(
                    new AzureCliCredential(),
                    new VisualStudioCredential(),
                    new InteractiveBrowserCredential()) // fallback: browser popup if others fail
                : new DefaultAzureCredential();         // production: Managed Identity works fine


            builder.Configuration.AddAzureKeyVault(keyVaultUrl, credential);

            // Why we add this: this tells the app HOW to validate incoming JWT tokens —
            // it checks the token's signature, issuer, audience, and expiry against
            // Entra ID's public keys, using the AzureAd section from appsettings.json.
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

            builder.Services.AddAuthorization();


            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddHttpClient();


            builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null
                )
             ));

            // Redis distributed cache — connection string appsettings.json/Key Vault నుండి వస్తుంది
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration["Redis:ConnectionString"];
                options.InstanceName = "GeorgeTreats_";   // key prefix, multiple apps ఒకే Redis వాడితే collision రాకుండా
            });

            builder.Services.AddSingleton<ICacheService, RedisCacheService>();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

            builder.Services.AddValidatorsFromAssembly(typeof(CreateCustomerCommandValidator).Assembly);
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateCustomerCommand).Assembly));
            builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
            builder.Services.AddSingleton<IServiceBusSender, AzureServiceBusSender>();
            //builder.Services.AddSwaggerGen();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                                Name = "Authorization",
                                Type = SecuritySchemeType.Http,
                                Scheme = "Bearer",
                                BearerFormat = "JWT",
                                In = ParameterLocation.Header,
                                Description = "Enter 'Bearer' [space] and then your token. Example: \"Bearer eyJhbGci...\""
                            });

                            options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            var app = builder.Build();
            //Console.WriteLine($"SmtpUsername value: {app.Configuration["SmtpUsername"]}");
            //Console.WriteLine($"Connection String in use: {app.Configuration.GetConnectionString("DefaultConnection")}");

            app.UseSwagger();
            app.UseSwaggerUI();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {

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

            // Why order matters: Authentication (who are you?) must run BEFORE
            // Authorization (what are you allowed to do?). If reversed, every request
            // gets rejected before its identity is even checked.
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
