using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;

namespace Lambdas;


public class Parser
{
  private static readonly string bucket = Environment.GetEnvironmentVariable("BUCKET")
    ?? throw new Exception("No 'BUCKET' found ...");
  private static readonly string table = Environment.GetEnvironmentVariable("TABLE")
    ?? throw new Exception("No 'TABLE' found ...");

  public async Task Handler(S3Event input, ILambdaContext context)
  {
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(input));

    var bucket = input.Records[0].S3.Bucket.Name;
    var file = input.Records[0].S3.Object.Key;

    using var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.EUNorth1);
    var response = await s3Client.GetObjectAsync(bucket, file, CancellationToken.None);
    var reader = new StreamReader(response.ResponseStream);
    var contents = reader.ReadToEnd();

    var lines = contents
      .Split(Environment.NewLine)
      .Where(line => !string.IsNullOrWhiteSpace(line))
      .Select(line => line.Split(","));

    // skip header ...
    var games = lines.Skip(1).Select(Game.FromStringArray);

    using var ddbClient = new AmazonDynamoDBClient(Amazon.RegionEndpoint.EUNorth1);
    var config = new DynamoDBOperationConfig
    {
      OverrideTableName = table,
    };
    using var ddbContext = new DynamoDBContext(ddbClient, config);
    foreach (var game in games)
    {
      await ddbContext.SaveAsync(game, config, CancellationToken.None);
    }

  }
}
