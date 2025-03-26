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
        Task<bool> DeleteFileAsync(string key);
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
            try
            {
                // Generate a unique file name
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var key = $"{folder}/{fileName}";

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = memoryStream,
                    Key = key,
                    BucketName = _awsSettings.BucketName,
                    ContentType = file.ContentType
                };

                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(uploadRequest);

                // Return the public URL
                return $"https://{_awsSettings.BucketName}.s3.{_awsSettings.Region}.amazonaws.com/{key}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file to S3: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string key)
        {
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _awsSettings.BucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                throw;
            }
        }
    }
}
