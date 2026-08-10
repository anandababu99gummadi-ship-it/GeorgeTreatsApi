using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Application.Contracts
{
    public interface IBlobStorageService
    {
        // Uploads a file to the DEFAULT container (profile-pictures), which was
        // already fixed inside the constructor when this service was first built.
        // Use this whenever you always want the same, single container.
        Task<string> UploadFileAsync(Stream fileStream, string filename, string contentType, CancellationToken cancellationToken);

        // Uploads a file to ANY container you choose at call-time (e.g. "customer-imports").
        // Why: the original method above locks you into one container. CSV bulk-upload
        // needs a different container, so instead of hardcoding a second container
        // inside the service, we let the CALLER decide which container to use.
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName, string contentType, CancellationToken cancellationToken);

        // Creates a temporary, read-only link (SAS URL) to a file so it can be viewed
        // without making the whole container public. expiryDays controls how long
        // the link stays valid before it stops working.
        string GenerateReadSasUrl(string fileName, int expiryDays = 365);

    }
}
