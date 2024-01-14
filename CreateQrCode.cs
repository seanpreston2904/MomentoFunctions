using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using QRCoder;
using Azure.Storage;

namespace Momento.CreateQrCode
{
    public class CreateQrCode
    {

        private readonly QRCodeGenerator _qrCodeGenerator = new QRCodeGenerator();
        private readonly ILogger _logger;

        // Storage Connection Data
        private static readonly String _qrStorageAccountName = Environment.GetEnvironmentVariable("QrStorageAccountName")!;
        private static readonly String _qrStorageAccessKey = Environment.GetEnvironmentVariable("QrStorageAccessKey")!;

        private static readonly StorageSharedKeyCredential _qrCodeStorageKeyCredential =
            new StorageSharedKeyCredential(_qrStorageAccountName, _qrStorageAccessKey);
        
        private static readonly string _blobUri = "https://" + _qrStorageAccountName + ".blob.core.windows.net";

        private static readonly BlobServiceClient _blobServiceClient = new BlobServiceClient(new Uri(_blobUri), _qrCodeStorageKeyCredential);
        private static readonly BlobContainerClient _blobContainerClient = _blobServiceClient.GetBlobContainerClient("qrcodes");


        public CreateQrCode(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateQrCode>();
        }

        [Function("CreateQrCode")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {

            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Generate QR Code
            QRCodeData qrCodeData = _qrCodeGenerator.CreateQrCode("https://www.google.com", QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCodePng = new PngByteQRCode(qrCodeData);

            // Generate file from QR Code and upload to blob storage
            string filename = Guid.NewGuid().ToString() + ".png";
            _blobContainerClient.UploadBlob(filename, BinaryData.FromBytes(qrCodePng.GetGraphic(512)));

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("QR Code Uploaded");

            return response;
        }

    }
}
