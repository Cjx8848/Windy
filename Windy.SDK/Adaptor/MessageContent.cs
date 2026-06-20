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

        public string PlainText => string.Concat(segments.OfType<TextSegment>().Select(segment => segment.Text));
    }

    public abstract record MessageSegment;

    public sealed record TextSegment(string Text) : MessageSegment;

    public sealed record ImageSegment(byte[] Data, string FileName, string ContentType) : MessageSegment;

    public sealed record ImageUrlSegment(string Url) : MessageSegment;

    public sealed record MarkdownSegment(string Markdown) : MessageSegment;

    public sealed record MarkdownTemplateSegment(string TemplateId, IDictionary<string, string> Parameters) : MessageSegment;

    public sealed record ButtonSegment(ButtonKeyboard Keyboard) : MessageSegment;

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
}
