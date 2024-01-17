using System.Net;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Momento.DeletePhoto
{
    public class DeletePhoto
    {
        private readonly ILogger _logger;

        private static readonly String _qrStorageAccountName = Environment.GetEnvironmentVariable("QrStorageAccountName")!;
        private static readonly String _qrStorageAccessKey = Environment.GetEnvironmentVariable("QrStorageAccessKey")!;
        private static readonly string _blobUri = "https://" + _qrStorageAccountName + ".blob.core.windows.net";

        public DeletePhoto(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DeletePhoto>();
        }

        [Function("DeletePhoto")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequestData req)
        {
            
            HttpResponseData response;
            _logger.LogInformation("DeletePhoto request received.");

            // Storage Connection Data
            

            try
            {

                throw new NotImplementedException();

            }
            catch (Exception ex)
            {

                // On exception log error and return 500 status code
                _logger.LogError(ex.Message);

                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString("An internal server error occurred.");

                return response;

            }

            response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            return response;

        }
    }
}

