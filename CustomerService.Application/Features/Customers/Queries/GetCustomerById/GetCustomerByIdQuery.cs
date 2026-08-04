using CustomerService.Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Queries.GetCustomerById
{

        public class GetCustomerByIdQuery : IRequest<CustomerDto?>
        {
            public Guid Id { get; set; }

            public GetCustomerByIdQuery(Guid id)
            {
                Id = id;
            }
        }
    }

