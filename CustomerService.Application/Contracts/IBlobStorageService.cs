using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Contracts
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string filename, string contentType, CancellationToken cancellationToken);
        string GenerateReadSasUrl(string fileName, int expiryDays = 365);
    }
}
