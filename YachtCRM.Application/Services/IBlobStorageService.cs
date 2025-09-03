namespace YachtCRM.Web.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadAsync(string path, Stream content, string contentType);
    }
}

