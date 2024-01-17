namespace Momento
{
    public interface IPhotoStorageService
    {
        
        List<string> UploadPhotosWithGUID(Stream requestData, string location, string container);

    }
    
}