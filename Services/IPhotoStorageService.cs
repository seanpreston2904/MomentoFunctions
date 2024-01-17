namespace Momento
{
    public interface IPhotoStorageService
    {
        
        List<string> UploadPhotos(Stream requestData, string guid);

    }
    
}