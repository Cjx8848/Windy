namespace Windy.SDK.Adaptor
{
    public abstract class Adaptor : IAsyncDisposable
    {
        protected Adaptor(string name, AdaptorType type, AdaptorConfig config)
        {
            Name = name;
            Type = type;
            Config = config;
        }

        public string Name { get; }

        public AdaptorType Type { get; }

        public AdaptorConfig Config { get; }

        public bool IsActive { get; protected set; }

        public virtual AdaptorCapabilities Capabilities => AdaptorCapabilities.Text;

        public event EventHandler<Events.MessageEventArgs>? MessageReceived;

        public event EventHandler<Events.AdaptorEventArgs>? EventReceived;

        public abstract Task StartAsync(CancellationToken cancellationToken = default);

        public abstract Task StopAsync(CancellationToken cancellationToken = default);

        public abstract Task SendMessage(SendTarget target, MessageContent content, SendOptions? options = null, CancellationToken cancellationToken = default);

        protected bool EnsureActive(string action)
        {
            if (IsActive)
            {
                return true;
            }

            Message.Yellow($"适配器 {Name} 当前未连接，已阻止 {action}.");
            return false;
        }

        public virtual Task SendMessage(string id, MessageContent content, CancellationToken cancellationToken = default)
        {
            return SendMessage(SendTarget.Group(id), content, null, cancellationToken);
        }

        public virtual Task SendMessage(string id, string text, CancellationToken cancellationToken = default)
        {
            return SendMessage(id, MessageContent.Text(text), cancellationToken);
        }

        protected void PublishMessage(Events.MessageEventArgs args)
        {
            MessageReceived?.Invoke(this, args);
        }

        protected void PublishEvent(Events.AdaptorEventArgs args)
        {
            EventReceived?.Invoke(this, args);
        }

        public virtual async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }

    [Flags]
    public enum AdaptorCapabilities
    {
        None = 0,
        Text = 1,
        Image = 1 << 1,
        Markdown = 1 << 2,
        Button = 1 << 3,
        File = 1 << 4,
        DirectMessage = 1 << 5,
        GroupMessage = 1 << 6,
    }

    public enum SendTargetType
    {
        Group,
        User,
        Channel,
    }

    public enum AdaptorType
    {
        QQOfficial,
        Milky,
    }

    public sealed class SendTarget
    {
        private SendTarget(SendTargetType type, string id)
        {
            Type = type;
            Id = id;
        }

        public SendTargetType Type { get; }

        public string Id { get; }

        public static SendTarget Group(string id) => new(SendTargetType.Group, id);

        public static SendTarget User(string id) => new(SendTargetType.User, id);

        public static SendTarget Channel(string id) => new(SendTargetType.Channel, id);
    }

    public sealed class SendOptions
    {
        public string? MessageId { get; set; }

        public string? EventId { get; set; }

        public int Sequence { get; set; } = 1;

        public bool Passive { get; set; } = true;
    }

    public sealed class AdaptorMessageApi
    {
        private int sequence = 1;

        public AdaptorMessageApi(Adaptor adaptor, Events.MessageEventArgs source)
        {
            Adaptor = adaptor;
            Source = source;
        }

        public Adaptor Adaptor { get; }

        public Events.MessageEventArgs Source { get; }

        public Task SendMessage(MessageContent content, CancellationToken cancellationToken = default)
        {
            SendOptions options = new()
            {
                MessageId = Source.MessageId,
                EventId = Source.EventId,
                Sequence = sequence++,
                Passive = true,
            };

            return Adaptor.SendMessage(Source.ReplyTarget, content, options, cancellationToken);
        }

        public Task SendMessage(string text, CancellationToken cancellationToken = default)
        {
            return SendMessage(MessageContent.Text(text), cancellationToken);
        }

        public Task SendImage(byte[] data, string fileName = "upload.jpg", CancellationToken cancellationToken = default)
        {
            return SendMessage(new MessageContent().AddImage(data, fileName), cancellationToken);
        }

    }
}
