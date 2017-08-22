using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace S3ToLine
{
    public class Function
    {
        // LINE bot 
        private readonly string _accessToken = Environment.GetEnvironmentVariable("ChannelSecret");

        IAmazonS3 S3Client { get; set; }

        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            context.Logger.LogLine("Start");
            var s3Event = evnt.Records?[0].S3;
            if(s3Event == null)
            {
                return null;
            }
            try
            {
                var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                if (response.Headers.ContentType == "application/octet-stream")
                {
                    context.Logger.LogLine("json yes");
                    var s3file = await S3Client.GetObjectAsync(s3Event.Bucket.Name, s3Event.Object.Key);
                    var json = new StreamReader(s3file.ResponseStream).ReadToEnd();
                    var lineMessage = JsonConvert.DeserializeObject<Event>(json);
                    if (lineMessage.message.text == "mtg")
                    {
                        context.Logger.LogLine("mtg yes");
                        var returnText = MtgCardPic().Result;
                        context.Logger.LogLine(returnText);
                        context.Logger.LogLine(lineMessage.replyToken);
                        context.Logger.LogLine(lineMessage.message.text);
                        var returnText2 = PostToLine(lineMessage.replyToken, returnText);
                        context.Logger.LogLine(returnText2.Result);
                    }
                    else if (lineMessage.message.text == "t")
                    {
                        var dateOffset1 = DateTimeOffset.Now;
                        var ut = dateOffset1.ToUnixTimeMilliseconds();
                        var returnText = $"post time:{lineMessage.timestamp}\r \ngets time:{ut}";
                        var returnText2 = PostToLine(lineMessage.replyToken, returnText);
                        context.Logger.LogLine(returnText2.Result);
                    }
                    else
                    {
                        context.Logger.LogLine("8-2");
                    }
                }
                
                return response.Headers.ContentType;
            }
            catch(Exception e)
            {
                return $"ÉGÉâÅ[{e.Message}";
            }
        }
        public async Task<string> MtgCardPic()
        {
            const string urlLeft = "http://gatherer.wizards.com/Handlers/Image.ashx?multiverseid=";
            const string urlRight = "&type=card";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://gatherer.wizards.com/Pages/Card/Details.aspx?action=random");

            var res = req.GetResponseAsync().Result;

            string[] stArrayData = res.ResponseUri.ToString().Split('=');
            var cardId = stArrayData[1];

            return $"{urlLeft}{cardId}{urlRight}";
        }

        private async Task<string> PostToLine(string token, string text)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    Response content = new Response();
                    Messages msg = new Messages();

                    content.replyToken = token;
                    content.messages = new List<Messages>();

                    msg.type = "text";
                    msg.text = text;
                    content.messages.Add(msg);

                    var reqData = JsonConvert.SerializeObject(content);
                    var repURL = "https://api.line.me/v2/bot/message/reply";

                    var request = new HttpRequestMessage(HttpMethod.Post, repURL);
                    request.Content = new StringContent(reqData, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                    var res = await client.SendAsync(request);

                    return res.StatusCode.ToString();
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        }

        private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static long FromDateTime(DateTime dateTime)
        {
            double nowTicks = (dateTime.ToUniversalTime() - UNIX_EPOCH).TotalSeconds;
            return (long)nowTicks;
        }
    }
}
