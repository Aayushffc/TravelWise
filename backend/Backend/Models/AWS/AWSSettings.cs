namespace Backend.Models.AWS
{
    public class AWSSettings
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string[] AllowedFileTypes { get; set; } = Array.Empty<string>();
        public int MaxFileSizeInMB { get; set; }
    }
}