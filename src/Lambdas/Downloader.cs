using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using System;
using System.IO;
using System.Net.Http;
using Amazon.S3;
using Amazon.S3.Transfer;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]


namespace Lambdas;

public class Downloader
{
  private static readonly string bucket = Environment.GetEnvironmentVariable("BUCKET")
    ?? throw new Exception("No 'BUCKET' found ...");

  public async Task Handler(CloudWatchEvent<object> input, ILambdaContext context)
  {
    var fixtures = "https://www.football-data.co.uk/fixtures.csv";
    using var httpClient = new HttpClient();
    var content = await httpClient.GetByteArrayAsync(fixtures);

    using var stream = new MemoryStream(content);
    using var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.EUNorth1);
    using var transferUtility = new TransferUtility(s3Client);

    var request = new TransferUtilityUploadRequest
    {
      BucketName = bucket,
      InputStream = stream,
      Key = "fixtures.csv"
    };
    await transferUtility.UploadAsync(request, CancellationToken.None);
  }
}
