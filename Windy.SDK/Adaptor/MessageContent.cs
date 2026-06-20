using Windy.SDK.Events;

namespace Windy.SDK.Adaptor
{
    public sealed class MessageContent
    {
        private readonly List<MessageSegment> segments = new();

        public IReadOnlyList<MessageSegment> Segments => segments;

        public static MessageContent Text(string text)
        {
            return new MessageContent().AddText(text);
        }

        public MessageContent AddText(string text)
        {
            segments.Add(new TextSegment(text));
            return this;
        }

        public MessageContent AddImage(byte[] data, string fileName = "upload.jpg", string contentType = "image/jpeg")
        {
            segments.Add(new ImageSegment(data, fileName, contentType));
            return this;
        }

        public MessageContent AddImageUrl(string url)
        {
            segments.Add(new ImageUrlSegment(url));
            return this;
        }

        public MessageContent AddMarkdown(string markdown)
        {
            segments.Add(new MarkdownSegment(markdown));
            return this;
        }

        public MessageContent AddMarkdownTemplate(string templateId, IDictionary<string, string> parameters)
        {
            segments.Add(new MarkdownTemplateSegment(templateId, parameters));
            return this;
        }

        public MessageContent AddButton(ButtonKeyboard keyboard)
        {
            segments.Add(new ButtonSegment(keyboard));
            return this;
        }

        public MessageContent AddAudio(string uri)
        {
            segments.Add(new AudioSegment(uri));
            return this;
        }

        public MessageContent AddVideo(string uri, string? thumbnailUri = null)
        {
            segments.Add(new VideoSegment(uri, thumbnailUri));
            return this;
        }

        public MessageContent AddFile(string uri, string fileName, string parentFolderId = "/")
        {
            segments.Add(new FileSegment(uri, fileName, parentFolderId));
            return this;
        }

        public MessageContent AddMention(string userId)
        {
            segments.Add(new MentionSegment(userId));
            return this;
        }

        public MessageContent AddMentionAll()
        {
            segments.Add(new MentionAllSegment());
            return this;
        }

        public MessageContent AddFace(string faceId, bool isLarge = false)
        {
            segments.Add(new FaceSegment(faceId, isLarge));
            return this;
        }

        public MessageContent AddReply(string messageSeq, string? senderId = null, string? senderName = null)
        {
            segments.Add(new ReplySegment(messageSeq, senderId, senderName));
            return this;
        }

        public MessageContent AddForward(IReadOnlyList<ForwardedMessageNode> messages, string? title = null, string? summary = null)
        {
            segments.Add(new ForwardSegment(messages, title, summary));
            return this;
        }

        public MessageContent AddLightApp(string jsonPayload, string? appName = null)
        {
            segments.Add(new LightAppSegment(jsonPayload, appName));
            return this;
        }

        public string PlainText => string.Concat(segments.OfType<TextSegment>().Select(segment => segment.Text));
    }

    public abstract record MessageSegment;

    public sealed record TextSegment(string Text) : MessageSegment;

    public sealed record ImageSegment(byte[] Data, string FileName, string ContentType) : MessageSegment;

    public sealed record ImageUrlSegment(string Url) : MessageSegment;

    public sealed record MarkdownSegment(string Markdown) : MessageSegment;

    public sealed record MarkdownTemplateSegment(string TemplateId, IDictionary<string, string> Parameters) : MessageSegment;

    public sealed record ButtonSegment(ButtonKeyboard Keyboard) : MessageSegment;

    public sealed record AudioSegment(string Uri) : MessageSegment;

    public sealed record VideoSegment(string Uri, string? ThumbnailUri = null) : MessageSegment;

    public sealed record FileSegment(string Uri, string FileName, string ParentFolderId = "/") : MessageSegment;

    public sealed record MentionSegment(string UserId) : MessageSegment;

    public sealed record MentionAllSegment : MessageSegment;

    public sealed record FaceSegment(string FaceId, bool IsLarge = false) : MessageSegment;

    public sealed record ReplySegment(string MessageSeq, string? SenderId = null, string? SenderName = null) : MessageSegment;

    public sealed record ForwardSegment(IReadOnlyList<ForwardedMessageNode> Messages, string? Title = null, string? Summary = null) : MessageSegment;

    public sealed record LightAppSegment(string JsonPayload, string? AppName = null) : MessageSegment;

    public sealed record MarketFaceSegment : MessageSegment
    {
        public string EmojiId { get; init; } = "";
        public long EmojiPackageId { get; init; }
        public string Key { get; init; } = "";
        public string Url { get; init; } = "";
        public string Summary { get; init; } = "";
    }

    public sealed record XmlSegment : MessageSegment
    {
        public int ServiceId { get; init; }
        public string XmlPayload { get; init; } = "";
    }

    public sealed class ForwardedMessageNode
    {
        public string UserId { get; set; } = "";
        public string SenderName { get; set; } = "";
        public MessageContent Message { get; set; } = new();
    }

    public sealed class ButtonKeyboard
    {
        public string? Id { get; set; }

        public List<ButtonRow> Rows { get; set; } = new();
    }

    public sealed class ButtonRow
    {
        public List<MessageButton> Buttons { get; set; } = new();
    }

    public sealed class MessageButton
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string RenderLabel { get; set; } = "";

        public string ActionData { get; set; } = "";

        public int ActionType { get; set; } = 2;

        public int PermissionType { get; set; } = 2;
    }

    public sealed class ContextMessageContent
    {
        private readonly MessageEventArgs _message;
        private readonly MessageContent _content = new();

        internal ContextMessageContent(MessageEventArgs message)
        {
            _message = message;
        }

        public ContextMessageContent AddReply()
        {
            _content.AddReply(_message.MessageId, _message.AuthorId, _message.AuthorName);
            return this;
        }

        public ContextMessageContent AddText(string text) { _content.AddText(text); return this; }

        public ContextMessageContent AddImage(byte[] data, string fileName = "upload.jpg", string contentType = "image/jpeg") { _content.AddImage(data, fileName, contentType); return this; }

        public ContextMessageContent AddImageUrl(string url) { _content.AddImageUrl(url); return this; }

        public ContextMessageContent AddMention(string userId) { _content.AddMention(userId); return this; }

        public ContextMessageContent AddMentionAll() { _content.AddMentionAll(); return this; }

        public ContextMessageContent AddFace(string faceId, bool isLarge = false) { _content.AddFace(faceId, isLarge); return this; }

        public ContextMessageContent AddAudio(string uri) { _content.AddAudio(uri); return this; }

        public ContextMessageContent AddVideo(string uri, string? thumbnailUri = null) { _content.AddVideo(uri, thumbnailUri); return this; }

        public ContextMessageContent AddFile(string uri, string fileName, string parentFolderId = "/") { _content.AddFile(uri, fileName, parentFolderId); return this; }

        public ContextMessageContent AddForward(IReadOnlyList<ForwardedMessageNode> messages, string? title = null, string? summary = null) { _content.AddForward(messages, title, summary); return this; }

        public ContextMessageContent AddLightApp(string jsonPayload, string? appName = null) { _content.AddLightApp(jsonPayload, appName); return this; }

        public ContextMessageContent AddMarkdown(string markdown) { _content.AddMarkdown(markdown); return this; }

        public ContextMessageContent AddButton(ButtonKeyboard keyboard) { _content.AddButton(keyboard); return this; }

        public static implicit operator MessageContent(ContextMessageContent builder) => builder._content;

        public MessageContent Build() => _content;
    }
}
