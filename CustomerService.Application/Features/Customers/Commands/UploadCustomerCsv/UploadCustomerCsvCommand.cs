using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Commands.UploadCustomerCsv
{
    public class UploadCustomerCsvCommand : IRequest<string>
    {
        public Stream FileStream { get; set; }
        public string FileName { get; set; }

    }
}
