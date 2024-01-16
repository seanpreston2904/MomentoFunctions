using System.Data.Common;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Momento.UploadPhoto
{
    public class UploadPhoto
    {
        private readonly ILogger _logger;

        // Storage Connection Data
        private static readonly String _qrStorageAccountName = Environment.GetEnvironmentVariable("QrStorageAccountName")!;
        private static readonly String _qrStorageAccessKey = Environment.GetEnvironmentVariable("QrStorageAccessKey")!;
        private static readonly string _blobUri = "https://" + _qrStorageAccountName + ".blob.core.windows.net";

        public UploadPhoto(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadPhoto>();
        }

        [Function("UploadPhoto")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("UploadPhoto request received.");

            HttpResponseData response;
            List<string> uploadUrls = new List<string>();

            try{

                // Create storage credential
                StorageSharedKeyCredential qrCodeStorageKeyCredential =
                    new StorageSharedKeyCredential(_qrStorageAccountName, _qrStorageAccessKey);

                // Create blob service client and blob container clients
                BlobServiceClient blobServiceClient = 
                    new BlobServiceClient(new Uri(_blobUri), qrCodeStorageKeyCredential);

                BlobContainerClient blobContainerClient = 
                    blobServiceClient.GetBlobContainerClient("photodata");


                // Get QR Code GUID
                var guid = req.Query.Get("Guid");

                // Parse multiform data
                var parser = MultipartFormDataParser.Parse(req.Body);

                // Upload files to container
                foreach(var file in parser.Files)
                {

                    // Ensure file uploaded is an image
                    if(!file.ContentType.StartsWith("image")){

                        response = req.CreateResponse(HttpStatusCode.BadRequest);
                        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                        response.WriteString("Only image files are accepted.");

                        return response;

                    }

                    // Get bytes from file data
                    Stream data = file.Data;
                    byte[] photoBuffer = new byte[data.Length];

                    data.Read(photoBuffer);

                    // Attempt to upload file
                    string id = Guid.NewGuid().ToString();
                    string fileType = file.ContentType[(file.ContentType.LastIndexOf("/") + 1)..];

                    var uploadResponse = blobContainerClient.UploadBlob($"{guid}/{id}.{fileType}", BinaryData.FromBytes(photoBuffer));
                    uploadUrls.Add($"{blobContainerClient.Uri}/{guid}/{id}.{fileType}");

                }

            } catch (Exception ex){

                // On exception log error and return 500 status code
                _logger.LogError(ex.Message);

                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("An internal server error occurred.");

                return response;

            }

            // On success return JSON response containing QR code URL
            var responseContent = new {urls = uploadUrls.ToArray()};

            response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteAsJsonAsync(responseContent).GetAwaiter().GetResult();

            return response;
            
        }
        
        
    }
}
