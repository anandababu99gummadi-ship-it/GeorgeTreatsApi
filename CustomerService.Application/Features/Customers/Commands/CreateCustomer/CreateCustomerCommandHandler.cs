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

            public CreateCustomerCommandHandler(ICustomerRepository customerRepository, IServiceBusSender serviceBusSender)
            {
                _customerRepository = customerRepository;
                _serviceBusSender = serviceBusSender;
            }

            public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
            {
                var customer = new Customer(request.Name, request.Email, request.Phone, request.Location);
                await _customerRepository.AddAsync(customer);

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
