using Google.Apis.Auth.OAuth2;

namespace TaskLauncher.Common.Configuration;

public class StorageConfiguration
{
    public string GoogleCredentialFile
    {
        get
        {
            if (GoogleCredential is null)
                return string.Empty;
            return GoogleCredential.ToString();
        }
        set
        {
            GoogleCredential = GoogleCredential.FromFile(value);
        } 
    }

    public GoogleCredential GoogleCredential { get; private set; }
    
    public string BucketName { get; init; }
}
