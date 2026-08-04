using CustomerService.Application.Contracts;
using CustomerService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Queries.GetCustomerById
{
    public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IBlobStorageService _blobStorageService;

        public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository, IBlobStorageService lobStorageService)
        {
            _customerRepository = customerRepository;
            _blobStorageService = lobStorageService;
        }

        public async Task<CustomerDto?> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetByIdAsync(request.Id);

            if (customer == null)
                return null;

            string? profilePictureUrl = null;
            if (!string.IsNullOrEmpty(customer.ProfilePictureFileName))
            {
                profilePictureUrl = _blobStorageService.GenerateReadSasUrl(customer.ProfilePictureFileName);
            }

            return new CustomerDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Street = customer.Location.Street,
                City = customer.Location.City,
                State = customer.Location.State,
                ZipCode = customer.Location.ZipCode,
                Country = customer.Location.Country,
                IsActive = customer.IsActive,
                ProfilePictureUrl = profilePictureUrl
            };
        }
    }
}
