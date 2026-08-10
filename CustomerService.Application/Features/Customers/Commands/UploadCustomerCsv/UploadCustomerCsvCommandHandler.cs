using CustomerService.Application.Contracts;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Features.Customers.Commands.UploadCustomerCsv
{
    public class UploadCustomerCsvCommandHandler : IRequestHandler<UploadCustomerCsvCommand, string>
    {
        private readonly IBlobStorageService _blobStorageService;

        public UploadCustomerCsvCommandHandler(IBlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public async Task<string> Handle(UploadCustomerCsvCommand request, CancellationToken cancellationToken)
        {
            // We read FileStream and FileName from "request" — MediatR hands us
            // the whole command object, and the command already carries the
            // file data that the controller packaged into it.
            var blobUrl = await _blobStorageService.UploadFileAsync(
                fileStream: request.FileStream,
                fileName: request.FileName,
                containerName: "customer-imports",   // other container passing here only
                contentType: "text/csv",
                cancellationToken: cancellationToken
            );

            // Return the blob URL — Task<string> requires every path to
            // return a string, otherwise the compiler throws CS0161.
            return blobUrl;
        }
    }
}