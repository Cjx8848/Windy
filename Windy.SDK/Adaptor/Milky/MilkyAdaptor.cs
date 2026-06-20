namespace Windy.SDK.Adaptor.Milky
{
    public sealed class MilkyAdaptor : Adaptor
    {
        public MilkyAdaptor(AdaptorConfig config) : base("milky", AdaptorType.Milky, config)
        {
        }

        public override AdaptorCapabilities Capabilities => AdaptorCapabilities.Text |
            AdaptorCapabilities.Image |
            AdaptorCapabilities.DirectMessage |
            AdaptorCapabilities.GroupMessage;

        public override Task StartAsync(CancellationToken cancellationToken = default)
        {
            Message.Yellow("Milky 适配器结构已注册，协议连接实现待接入 Milky 文档.");
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override Task SendMessage(SendTarget target, MessageContent content, SendOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Milky 发送消息尚未实现.");
        }
    }
}
