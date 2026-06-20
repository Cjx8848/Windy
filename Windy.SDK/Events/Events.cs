using Newtonsoft.Json.Linq;
using Windy.SDK.Adaptor;

namespace Windy.SDK.Events
{
    public enum MessageScene
    {
        Private,
        Group,
        GroupAt,
        Channel,
    }

    public sealed class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(Adaptor.Adaptor adaptor, MessageScene scene, string content, string messageId, string authorId, SendTarget replyTarget, string authorName = "")
        {
            Adaptor = new AdaptorMessageApi(adaptor, this);
            RawAdaptor = adaptor;
            Scene = scene;
            Content = content;
            MessageId = messageId;
            AuthorId = authorId;
            AuthorName = authorName;
            ReplyTarget = replyTarget;
        }

        public AdaptorMessageApi Adaptor { get; }

        public Adaptor.Adaptor RawAdaptor { get; }

        public MessageScene Scene { get; }

        public string Content { get; }

        public string MessageId { get; }

        public string? EventId { get; set; }

        public string AuthorId { get; }

        public string AuthorName { get; }

        public string? GroupId { get; set; }

        public SendTarget ReplyTarget { get; }

        public List<MessageAttachment> Attachments { get; } = new();

        public JObject? Raw { get; set; }

        public bool Handled { get; set; }
    }

    public sealed class MessageAttachment
    {
        public string ContentType { get; set; } = "";

        public string FileName { get; set; } = "";

        public string Url { get; set; } = "";
    }

    public sealed class AdaptorEventArgs : EventArgs
    {
        public AdaptorEventArgs(Adaptor.Adaptor adaptor, string type, JObject raw)
        {
            Adaptor = adaptor;
            Type = type;
            Raw = raw;
        }

        public Adaptor.Adaptor Adaptor { get; }

        public string Type { get; }
        public JObject Raw { get; }
        public bool Handled { get; set; }
    }
}
