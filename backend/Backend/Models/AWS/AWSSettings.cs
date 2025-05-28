namespace Backend.Models.AWS;

public class AWSSettings
{
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? BucketName { get; set; }
    public string? Region { get; set; }
    public string[] AllowedFileTypes { get; set; } = Array.Empty<string>();
    public int MaxFileSizeInMB { get; set; } = 10;
}
