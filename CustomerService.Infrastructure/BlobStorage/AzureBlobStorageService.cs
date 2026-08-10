using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CustomerService.Application.Contracts;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomerService.Infrastructure.BlobStorage
{
    public class AzureBlobStorageService : IBlobStorageService
    {
        // Default container client — points ONLY at the profile-pictures container
        // set up once in the constructor below. Used by the first UploadFileAsync overload.
        private readonly BlobContainerClient _containerClient;

        // Root client for the whole Azure Storage account. Why we need this separately:
        // _containerClient is locked to ONE container, but the CSV overload needs to
        // reach a DIFFERENT container ("customer-imports") on demand. BlobServiceClient
        // lets us call .GetBlobContainerClient(anyName) for that.
        // NOTE: type fixed from BlobContainerClient -> BlobServiceClient (this was the bug).
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureBlobStorage:ConnectionString"];
            var containerName = configuration["AzureBlobStorage:ContainerName"];

            var blobServiceClient = new BlobServiceClient(connectionString);

            // Save it to the field too — this line was MISSING before, which is why
            // _blobServiceClient was always null and the second overload couldn't work.
            _blobServiceClient = blobServiceClient;

            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            _containerClient.CreateIfNotExists(); // No public access – private container
        }

        // Uploads to the fixed, default container (profile-pictures).
        // Why simple: this is the original, single-purpose upload used by the
        // profile-picture feature — no need to pick a container, it's always the same one.
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(fileStream, new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = contentType
            }, cancellationToken: cancellationToken);

            // Here SAS not a URL, it's only returns the filename.
            return fileName;
        }

        // Uploads to WHICHEVER container the caller specifies (e.g. "customer-imports").
        // Why we rewrote this: the old version tried to rebuild a connection string from
        // _blobServiceClient.AccountName, which only gives the account name (not a full
        // connection string), so it could never have worked. Now we reuse the already-
        // authenticated _blobServiceClient directly — no need to reconnect.
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName, string contentType, CancellationToken cancellationToken)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = contentType
            }, cancellationToken: cancellationToken);

            // Here we DO return the full URL (not just filename), because CSV imports
            // will be picked up later by a Blob Trigger Function that needs the path.
            return blobClient.Uri.ToString();
        }



        public string GenerateReadSasUrl(string fileName, int expiryDays = 365)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = fileName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(expiryDays)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}
