using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Assets;
using Amazon.CDK.AWS.Scheduler;
using Amazon.CDK.AWS.SES.Actions;
using Constructs;
using static Amazon.CDK.AWS.Scheduler.CfnSchedule;

namespace BetterCdkDotnet
{
  public class BetterCdkDotnetStack : Stack
  {
    internal BetterCdkDotnetStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
      var lambdaBuildOptions = new BundlingOptions
      {
        Image = Runtime.DOTNET_6.BundlingImage,
        User = "root",
        OutputType = BundlingOutput.ARCHIVED,
        Command = new string[] {
          "/bin/sh",
          "-c",
          " dotnet tool install -g Amazon.Lambda.Tools"+
          " && dotnet build"+
          " && dotnet lambda package --output-package /asset-output/function.zip"
        }
      };

      var table = new Table(this, "table", new TableProps
      {
        PartitionKey = new Attribute
        {
          Name = "Date",
          Type = AttributeType.STRING,
        },
        SortKey = new Attribute
        {
          Name = "Identifier",
          Type = AttributeType.STRING,
        },
        BillingMode = BillingMode.PAY_PER_REQUEST,
        RemovalPolicy = RemovalPolicy.DESTROY,
        TimeToLiveAttribute = "ExpiresAt",
      });

      var bucket = new Bucket(this, "bucket", new BucketProps
      {
        EventBridgeEnabled = true,
        RemovalPolicy = RemovalPolicy.DESTROY,
        AutoDeleteObjects = true,
      });

      var downloader = new Function(this, "downloader", new FunctionProps
      {
        Runtime = Runtime.DOTNET_6,
        Timeout = Duration.Seconds(30),
        Handler = "Lambdas::Lambdas.Downloader::Handler",
        Code = Code.FromAsset("./src/Lambdas", new Amazon.CDK.AWS.S3.Assets.AssetOptions
        {
          Bundling = lambdaBuildOptions
        }),
        Environment = new Dictionary<string, string> {
          { "BUCKET", bucket.BucketName },
        },
      });

      bucket.GrantReadWrite(downloader);

      var scheduleRole = new Role(this, "scheduleRole", new RoleProps
      {
        AssumedBy = new ServicePrincipal("scheduler.amazonaws.com"),
      });

      var schedulePolicy = new Policy(this, "schedulePolicy", new PolicyProps
      {
        Roles = new IRole[] { scheduleRole },
        Statements = new PolicyStatement[] {
          new PolicyStatement(new PolicyStatementProps {
            Effect = Effect.ALLOW,
            Actions = new [] { "lambda:InvokeFunction" },
            Resources = new [] { downloader.FunctionArn },
          }),
        }
      });

      var scheduleGroup = new CfnScheduleGroup(this, "scheduleGroup", new CfnScheduleGroupProps
      {
        Name = "schedule-group",
      });

      var schedule = new CfnSchedule(this, "schedule", new CfnScheduleProps
      {
        GroupName = scheduleGroup.Name,
        ScheduleExpression = "cron(0 0 * * ? *)",
        FlexibleTimeWindow = new FlexibleTimeWindowProperty
        {
          Mode = "OFF",
        },
        Target = new TargetProperty
        {
          Arn = downloader.FunctionArn,
          RoleArn = scheduleRole.RoleArn,
        }
      });

      var parser = new Function(this, "parser", new FunctionProps
      {
        Runtime = Runtime.DOTNET_6,
        Timeout = Duration.Seconds(30),
        Handler = "Lambdas::Lambdas.Parser::Handler",
        Code = Code.FromAsset("./src/Lambdas", new Amazon.CDK.AWS.S3.Assets.AssetOptions
        {
          Bundling = lambdaBuildOptions
        }),
        Environment = new Dictionary<string, string> {
          { "BUCKET", bucket.BucketName },
          { "TABLE", table.TableName },
        },
      });

      bucket.GrantRead(parser);
      table.GrantWriteData(parser);
      parser.AddEventSource(new S3EventSource(bucket, new S3EventSourceProps
      {
        Events = new EventType[] { EventType.OBJECT_CREATED },
      }));
    }
  }
}
