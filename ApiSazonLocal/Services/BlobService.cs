using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using SazonLocalModels.Models;

namespace ApiSazonLocal.Services
{
    public class BlobService
    {
        private BlobServiceClient client;

        public BlobService(BlobServiceClient client)
        {
            this.client = client;
        }

        public async Task<Stream> GetBlobStreamAsync(string containerName, string blobName)
        {
            BlobContainerClient containerClient = this.client.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            BlobDownloadInfo response = await blobClient.DownloadAsync();
            return response.Content;
        }

        public string GetBlobSasUrl(string containerName, string blobName)
        {
            BlobContainerClient containerClient = this.client.GetBlobContainerClient(containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        public async Task UploadBlobAsync(string containerName, string blobName, Stream stream)
        {
            BlobContainerClient blobContainerClient = this.client.GetBlobContainerClient(containerName);
            await blobContainerClient.UploadBlobAsync(blobName, stream);
        }

        public async Task DeleteBlobAsync(string containerName, string blobName)
        {
            BlobContainerClient blobContainerClient = this.client.GetBlobContainerClient(containerName);
            await blobContainerClient.DeleteBlobAsync(blobName);
        }
    }
}
