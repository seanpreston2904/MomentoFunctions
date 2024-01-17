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
        private readonly IPhotoStorageService _photoStorageService;

        public UploadPhoto(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadPhoto>();
        }

        [Function("UploadPhoto")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("UploadPhoto request received.");

            HttpResponseData response;
            List<string> uploadUrls;

            try{

                // Get QR Code GUID
                var guid = req.Query.Get("Guid");

                // Upload photos
                uploadUrls = _photoStorageService.UploadPhotosWithGUID(req.Body, guid, "photodata");

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
