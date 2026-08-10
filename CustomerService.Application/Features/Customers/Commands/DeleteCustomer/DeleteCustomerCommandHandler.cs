using CustomerService.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Commands.DeleteCustomer
{
    public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICacheService _cacheService;

        public DeleteCustomerCommandHandler(ICustomerRepository customerRepository, ICacheService cacheService)
        {
            _customerRepository = customerRepository;
            _cacheService = cacheService;
        }

        public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetByIdAsync(request.Id);

            if (customer == null)
                return false;

            customer.Deactivate();

            await _customerRepository.UpdateAsync(customer);
            await _cacheService.RemoveAsync("all-customers", cancellationToken);

            return true;
        }
    }
}
