using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using libal.Domain;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace libal.Services
{
    public class S3Service : IS3Service
    {
        private AmazonS3Client GetS3Client() {
            var awsSecret = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");
            var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");

            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(awsAccessKey, awsSecret);
            var s3Client = new AmazonS3Client(awsCredentials, RegionEndpoint.EUWest1);

            return s3Client;
        }

        public async Task<byte[]> Get(string fileName)
        {
            var s3Client = GetS3Client();
            var bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");

            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = fileName
            };

            using (GetObjectResponse response = await s3Client.GetObjectAsync(request))
            using (Stream responseStream = response.ResponseStream)
            using (StreamReader reader = new StreamReader(responseStream))
            {
                var content = reader.ReadToEnd();
                var streamContent = Encoding.ASCII.GetBytes(content);

                return streamContent;
            }
        }

        public async Task<OperationResult> Create(string fileName, Stream stream)
        {
            var s3Client = GetS3Client();
            var bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");

            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.UploadAsync(stream, bucketName, fileName);

            var operationResult = new OperationResult();
            operationResult.fileName = fileName;

            return operationResult;
        }

    }

}
