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
            private readonly ICacheService _cacheService;

            public GetAllCustomersQueryHandler(ICustomerRepository customerRepository, IBlobStorageService blobStorageService, ICacheService cacheService)
            {
                _customerRepository = customerRepository;
                _blobStorageService = blobStorageService;
                _cacheService = cacheService;

            }

            public async Task<List<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
            {
                const string cacheKey = "all-customers";

                var cachedCustomers = await _cacheService.GetAsync<List<CustomerDto>>(cacheKey, cancellationToken);
                if (cachedCustomers != null)
                {
                    return cachedCustomers;
                }

                var customers = await _customerRepository.GetAllAsync();

                var customerDtos = customers.Select(customer => new CustomerDto
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
                        : null
                }).ToList();

                // Why we cache AFTER mapping: we store the final DTO shape (not the
                // raw entity), so future cache hits return exactly what the API
                // should send back — no extra mapping work needed on a cache hit.
                await _cacheService.SetAsync(cacheKey, customerDtos, TimeSpan.FromMinutes(5), cancellationToken);

                return customerDtos;
            }
        }
    }
}
