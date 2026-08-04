using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Commands.UploadProfilePicture
{
    public class UploadProfilePictureCommand : IRequest<string>
    {
        public Guid CustomerId { get; set; }
        public Stream FileStream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;


    }
}
