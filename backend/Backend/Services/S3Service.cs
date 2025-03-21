using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Backend.Models.AWS;
using Microsoft.Extensions.Options;

namespace Backend.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string fileUrl);
        Task<string> GetFileUrlAsync(string key);
    }

    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AWSSettings _awsSettings;

        public S3Service(IOptions<AWSSettings> awsSettings)
        {
            _awsSettings = awsSettings.Value;
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_awsSettings.Region)
            };
            _s3Client = new AmazonS3Client(_awsSettings.AccessKey, _awsSettings.SecretKey, config);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            // Validate file type
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_awsSettings.AllowedFileTypes.Contains(fileExtension))
                throw new ArgumentException($"File type {fileExtension} is not allowed");

            // Validate file size
            if (file.Length > _awsSettings.MaxFileSizeInMB * 1024 * 1024)
                throw new ArgumentException($"File size exceeds {_awsSettings.MaxFileSizeInMB}MB limit");

            // Generate unique file name
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var key = $"{folder}/{fileName}";

            using var fileStream = file.OpenReadStream();
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = key,
                BucketName = _awsSettings.BucketName,
                ContentType = file.ContentType
            };

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest);

            return await GetFileUrlAsync(key);
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                var key = GetKeyFromUrl(fileUrl);
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetFileUrlAsync(string key)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(1)
            };

            return await Task.FromResult(_s3Client.GetPreSignedURL(request));
        }

        private string GetKeyFromUrl(string fileUrl)
        {
            var uri = new Uri(fileUrl);
            return uri.LocalPath.TrimStart('/');
        }
    }
}