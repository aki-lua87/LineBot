using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Amazon.Lambda.S3Events;
using Amazon.S3;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace S3ToSlack
{
    public class Function
    {
        // 環境変数にSlackのインカミングウェブフックのURLを指定
        private readonly string webhookUrl = Environment.GetEnvironmentVariable("SlackWebhookUrl");

        IAmazonS3 S3Client { get; set; }

        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            context.Logger.LogLine("start");
            var s3Event = evnt.Records?[0].S3;
            var txt = "";
            if (s3Event == null)
            {
                txt = "null";
            }

            try
            {
                var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                txt = $"{response.Headers.ContentType}が保存されたっぽい";
            }
            catch (Exception e)
            {
               txt += e.Message;
            }

            using (var client = new HttpClient())
            {
                // Slackへ
                var payload = new
                {
                    channel = "#クソbot開発室",
                    username = "AWS Lambda Bot",
                    text = ":djp386:",
                    icon_emoji = ":lambda:",
                };
                var jsonString = JsonConvert.SerializeObject(payload);
                var slackRes = await client.PostAsync(webhookUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                context.Logger.LogLine(slackRes.ReasonPhrase);
            }
            return txt;
        }
    }
}
