using CustomerService.Application.Contracts;
using CustomerService.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Commands.CreateCustomer
{
    namespace CustomerService.Application.Features.Customers.Commands.CreateCustomer
    {
        public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
        {
            private readonly ICustomerRepository _customerRepository;
            private readonly IServiceBusSender _serviceBusSender;
            private readonly ICacheService _cacheService;   

            public CreateCustomerCommandHandler(ICustomerRepository customerRepository, IServiceBusSender serviceBusSender, ICacheService cacheService)
            {
                _customerRepository = customerRepository;
                _serviceBusSender = serviceBusSender;
                _cacheService = cacheService;
            }

            public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
            {
                var customer = new Customer(request.Name, request.Email, request.Phone, request.Location);
                await _customerRepository.AddAsync(customer);

                // Why we remove here: a new customer was just added, so the cached
                // "all customers" list is now stale/incomplete. Removing it forces
                // the NEXT GetAllCustomers call to fetch fresh data from DB and
                // re-cache it — rather than serving an outdated list.
                await _cacheService.RemoveAsync("all-customers", cancellationToken);

                var emailMessage = JsonSerializer.Serialize(new
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    Email = customer.Email
                });
                await _serviceBusSender.SendMessageAsync(emailMessage, cancellationToken);

                return customer.Id;
            }
        }
    }
}
