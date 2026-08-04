using CustomerService.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Commands.UploadProfilePicture
{
    public class UploadProfilePictureCommandHandler : IRequestHandler<UploadProfilePictureCommand, string>
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IBlobStorageService _blobStorageService;

        public UploadProfilePictureCommandHandler(
            ICustomerRepository customerRepository,
            IBlobStorageService blobStorageService)
        {
            _customerRepository = customerRepository;
            _blobStorageService = blobStorageService;
        }

        public async Task<string> Handle(UploadProfilePictureCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer is null)
                throw new Exception("Customer not found");

            var fileName = await _blobStorageService.UploadFileAsync(
                request.FileStream, request.FileName, request.ContentType, cancellationToken);

            customer.SetProfilePicture(fileName);
            await _customerRepository.UpdateAsync(customer);

            // Response గా, ఫ్రెష్ SAS URL client కి తిరిగి ఇవ్వొచ్చు (view చేయడానికి వెంటనే ఉపయోగపడేలా)
            return _blobStorageService.GenerateReadSasUrl(fileName);
        }
    }
}
