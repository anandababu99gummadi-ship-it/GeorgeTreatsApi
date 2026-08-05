using CustomerService.Application.Contracts;
using CustomerService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Queries.GetAllCustomers
{
    namespace CustomerService.Application.Features.Customers.Queries.GetAllCustomers
    {
        public class GetAllCustomersQueryHandler : IRequestHandler<GetAllCustomersQuery, List<CustomerDto>>
        {
            private readonly ICustomerRepository _customerRepository;
            private readonly IBlobStorageService _blobStorageService;

            public GetAllCustomersQueryHandler(ICustomerRepository customerRepository, IBlobStorageService blobStorageService)
            {
                _customerRepository = customerRepository;
                _blobStorageService = blobStorageService;
            }

            public async Task<List<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
            {
                var customers = await _customerRepository.GetAllAsync();

                return customers.Select(customer => new CustomerDto
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
                    ProfilePictureUrl = !string.IsNullOrEmpty(customer.ProfilePictureFileName)
                                        ? _blobStorageService.GenerateReadSasUrl(customer.ProfilePictureFileName)
                                        :null
                }).ToList();
            }
        }
    }
}
