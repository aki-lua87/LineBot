using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Amazon.Lambda.Core;


using Newtonsoft.Json;
using System.IO;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LineToS3
{
    public class Function
    {
        // 環境変数にLINEkのChannel Access Tokenを指定
        private readonly string _accessToken = Environment.GetEnvironmentVariable("ChannelSecret");
        // バケ名
        private readonly string bucketName = Environment.GetEnvironmentVariable("bucketName");
        // S3認証情報
        private readonly string accessKey = Environment.GetEnvironmentVariable("accessKey");
        private readonly string accessSec = Environment.GetEnvironmentVariable("accessSec");

        // LINEからのメディア取得
        private readonly string urlLeft = "https://api.line.me/v2/bot/message/";
        private readonly string urlRight = "/content";

        private readonly string filePath = "line/";

        public async Task<string> FunctionHandler(LineReply input, ILambdaContext context)
        {
            var message = input.events[0];
            var response = "res:";
            var dateOffset = DateTimeOffset.Now;
            switch (message.message.type)
            {
                case "text":
                    response += PutJson(JsonConvert.SerializeObject(message),
                        $"{filePath}text/{dateOffset.Year}{dateOffset.Month}{dateOffset.Day}/{message.message.id}.json");
                    if (message.message.text == "test")
                    {
                        context.Logger.LogLine(message.replyToken);
                        context.Logger.LogLine(_accessToken);
                        PostToLine(message.replyToken, "test reply");
                    }
                    break;
                case "image":
                    response += PutJson(JsonConvert.SerializeObject(message),
                        $"{filePath}image/json/{dateOffset.Year}{dateOffset.Month}{dateOffset.Day}/{message.message.id}.json");
                    var img = GetImage(message.message.id).Result;
                    PutImage(img, $"{filePath}image/img/{dateOffset.Year}{dateOffset.Month}{dateOffset.Day}/{message.message.id}.jpg");
                    break;
            }
            return response;
        }

        private async Task<MemoryStream> GetImage(string messageId)
        {
            var url = $"{urlLeft}{messageId}{urlRight}";
            try
            {
                using (var client = new HttpClient())
                {
                    // リクエストデータを作成
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Method = HttpMethod.Get;

                    //　認証ヘッダーを追加
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
                    var res = await client.SendAsync(request);
                    var st = res.Content.ReadAsStreamAsync().Result;

                    byte[] buffer = new byte[65535];
                    MemoryStream ms = new MemoryStream();

                    while (true)
                    {
                        int rb = st.Read(buffer, 0, buffer.Length);
                        if (rb > 0)
                        {
                            ms.Write(buffer, 0, rb);
                        }
                        else
                        {
                            break;
                        }
                    }
                    byte[] wbuf = new byte[ms.Length];
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.Read(wbuf, 0, wbuf.Length);
                    return ms;
                }
            }
            catch (Exception e)
            {
                return null;
            }
            
        }

        private void PutImage(MemoryStream img,string filePath)
        {
            using (TransferUtility tUtility = new TransferUtility(accessKey, accessSec))
            {
                try
                {
                    TransferUtilityUploadRequest t = new TransferUtilityUploadRequest();
                    t.BucketName = bucketName;
                    t.InputStream = img;
                    t.Key = filePath;
                    tUtility.Upload(t);
                }
                catch (Exception e)
                {
                    // 何か書く
                    return;
                }
            }
        }

        private string PutJson(string json, string filePath)
        {
            try
            {
                var client = new AmazonS3Client(accessKey, accessSec);
                PutObjectRequest putRequest = new PutObjectRequest

                {
                    BucketName = bucketName,
                    Key = filePath,
                    ContentBody = json
                };
                var res = client.PutObjectAsync(putRequest);
                return res.Result.HttpStatusCode.ToString();
            }
            catch (Exception e)
            {
                return "err";
            }
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


    }
}
