using System;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Capillary.ShellProxy.Service
{
    public interface IStorageService
    {
        Task<HttpStatusCode> AddToBucketAsync(string folderName ,string key, string content);
    }

    public class S3Service : IStorageService
    {
        private static IAmazonS3 _s3Client;
        private static string _bucketName;
        
        public S3Service(RegionEndpoint region, string bucketName)
        {
            _s3Client = new AmazonS3Client(region);
            _bucketName = bucketName;
        }

        public async Task<HttpStatusCode> AddToBucketAsync(string folderName ,string key, string content)
        {
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName =string.Format("{0}/{1}/{2}", _bucketName, folderName, DateTime.Now.ToString("yyyyMMdd")),
                    Key = key,
                    ContentBody = content
                };
                PutObjectResponse response = await _s3Client.PutObjectAsync(putRequest);
                return response.HttpStatusCode;
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("AmazonS3Exception encountered in S3Service.AddToBucket().Message:'{0}'", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception encountered in S3Service.AddToBucket().Message:'{0}'", e.Message);
            }

            return HttpStatusCode.InternalServerError;
        }
    }
}