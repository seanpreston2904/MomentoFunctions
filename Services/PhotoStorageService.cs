using Azure.Storage;
using Azure.Storage.Blobs;
using HttpMultipartParser;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Worker.Http;

namespace Momento
{
    public class PhotoStorageService : IPhotoStorageService
    {

        // Storage connection data
        private static readonly string _qrStorageAccountName = Environment.GetEnvironmentVariable("QrStorageAccountName")!;
        private static readonly string _qrStorageAccessKey = Environment.GetEnvironmentVariable("QrStorageAccessKey")!;
        private static readonly string _blobUri = "https://" + _qrStorageAccountName + ".blob.core.windows.net";

        // Storage connection clients
        private static StorageSharedKeyCredential qrCodeStorageKeyCredential =
            new StorageSharedKeyCredential(_qrStorageAccountName, _qrStorageAccessKey);

        private static BlobServiceClient blobServiceClient = 
            new BlobServiceClient(new Uri(_blobUri), qrCodeStorageKeyCredential);

        private static BlobContainerClient blobContainerClient = 
            blobServiceClient.GetBlobContainerClient("photodata");


        public List<string> UploadPhotos(Stream requestData, string guid)
        {

            var parser = MultipartFormDataParser.Parse(requestData);
            List<string> uploadUrls = new List<string>();

            // Upload files to container
            foreach(var file in parser.Files)
            {

                // Ensure file uploaded is an image
                if(!file.ContentType.StartsWith("image"))
                {
                    throw new ArgumentException($"Invalid file type, expected image, received {file.ContentType}");
                }

                // Get bytes from file data
                Stream data = file.Data;
                byte[] photoBuffer = new byte[data.Length];

                data.Read(photoBuffer);

                // Attempt to upload file
                string id = Guid.NewGuid().ToString();
                string fileType = file.ContentType[(file.ContentType.LastIndexOf("/") + 1)..];

                blobContainerClient.UploadBlob($"{guid}/{id}.{fileType}", BinaryData.FromBytes(photoBuffer));
                uploadUrls.Add($"{blobContainerClient.Uri}/{guid}/{id}.{fileType}");

            }

            return uploadUrls;

        }

    }

}