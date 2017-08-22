using System;
using System.Collections.Generic;
using System.Text;

namespace S3ToLine
{
    // LINEからの入力JSONの形式
    public class LineReply
    {
        public Event[] events { get; set; }
    }
    public class Event
    {
        public string timestamp { get; set; }
        public string replyToken { get; set; }
        public Message message { get; set; }
        public string type { get; set; }
        public Source source { get; set; }
    }

    public class Message
    {
        public string type { get; set; }
        public string id { get; set; }
        public string text { get; set; }
    }

    public class Source
    {
        public string userId { get; set; }
        public string type { get; set; }
    }


    // LINEに投げる形
    public class Response
    {
        public string replyToken { get; set; }
        public List<Messages> messages { get; set; }
    }
    public class Messages
    {
        public string type { get; set; }
        public string text { get; set; }
    }
}
