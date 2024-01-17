using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using QRCoder;
using Azure.Storage;
using Microsoft.VisualBasic;
using Azure;
using System.Text.Json;

namespace Momento.CreateQrCode
{
    public class CreateQrCode
    {

        private readonly QRCodeGenerator _qrCodeGenerator = new QRCodeGenerator();
        private readonly ILogger _logger;

        // Storage Connection Data
        private static readonly String _qrStorageAccountName = Environment.GetEnvironmentVariable("QrStorageAccountName")!;
        private static readonly String _qrStorageAccessKey = Environment.GetEnvironmentVariable("QrStorageAccessKey")!;
        private static readonly string _blobUri = "https://" + _qrStorageAccountName + ".blob.core.windows.net";


        public CreateQrCode(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CreateQrCode>();
        }

        [Function("CreateQrCode")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {

            _logger.LogInformation("CreateQrCode request received.");

            HttpResponseData response;
            string uploadUrl;

            try{

                // Generate a unique GUID for QR Code and associated Bucket
                string id = Guid.NewGuid().ToString();

                // Create storage credential
                StorageSharedKeyCredential qrCodeStorageKeyCredential =
                    new StorageSharedKeyCredential(_qrStorageAccountName, _qrStorageAccessKey);

                // Create blob service client and blob container clients
                BlobServiceClient blobServiceClient = 
                    new BlobServiceClient(new Uri(_blobUri), qrCodeStorageKeyCredential);

                BlobContainerClient blobContainerClient = 
                    blobServiceClient.GetBlobContainerClient("qrcodes");

                // Generate QR Code and upload to blob storage with unique GUID
                QRCodeData qrCodeData = _qrCodeGenerator.CreateQrCode("https://www.google.com", QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCodePng = new PngByteQRCode(qrCodeData);

                var uploadResponse = blobContainerClient.UploadBlob($"{id}.png", BinaryData.FromBytes(qrCodePng.GetGraphic(512)));

                uploadUrl = $"{blobContainerClient.Uri}/{id}.png";

            } catch (Exception ex){

                // On exception log error and return 500 status code
                _logger.LogError(ex.Message);

                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("An internal server error occurred.");

                return response;

            }

            // On success return JSON response containing QR code URL
            var responseContent = new {url = uploadUrl};

            response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteAsJsonAsync(responseContent).GetAwaiter().GetResult();

            return response;

        }

    }
}
