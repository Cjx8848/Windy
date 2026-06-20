using Windy.SDK;
using Windy.SDK.Adaptor;
using Windy.SDK.Adaptor.QQOfficial;
using Windy.SDK.Command;
using Windy.SDK.Events;
using Windy.SDK.Plugin;
using Windy.SDK.Utils;

namespace ExamplePlugin
{
    public sealed class Plugin : WindyPlugin
    {
        public override string Name => "Example";

        public override string Version => "1.0.0";

        public override string Author => "Cjx";

        public override string Description => "示例插件";

        public override AdaptorType RequiredAdaptor => AdaptorType.QQOfficial;

        public override void Initialize()
        {
            JsonTool.Create<Config>(Path.Combine(AppContext.BaseDirectory, "Config", "Example.json")).Read();
            this.RegisterReadyHook(OnReadyAsync, priority: 100);
            this.RegisterResumedHook(OnResumedAsync, priority: 100);
            this.RegisterGroupAddRobotHook(OnGroupAddRobotAsync, priority: 100);
            this.RegisterGroupDelRobotHook(OnGroupDelRobotAsync, priority: 100);
        }
        private static Task OnReadyAsync(QQOfficialReadyEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] session={args.SessionId}");
            return Task.CompletedTask;
        }

        private static Task OnResumedAsync(QQOfficialSimpleEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] event={args.EventId}");
            return Task.CompletedTask;
        }

        private static Task OnGroupAddRobotAsync(QQOfficialGroupOperationEventArgs args)
        {
            Message.Yellow($"[Example][{args.Type}] group={args.GroupOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
            args.Handled = true;
            return Task.CompletedTask;
        }

        private static Task OnGroupDelRobotAsync(QQOfficialGroupOperationEventArgs args)
        {
            Message.Yellow($"[Example][{args.Type}] group={args.GroupOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        [Command("测试", "测试 Markdown 与按钮组合消息", MessageScene.Group)]
        [Command("测试", "测试 Markdown 与按钮组合消息", MessageScene.GroupAt)]
        public async Task TestMarkdownButtonAsync(CommandArgs args)
        {
            ButtonKeyboard keyboard = new()
            {
                Rows =
                [
                    new ButtonRow
                    {
                        Buttons =
                        [
                            new MessageButton
                            {
                                RenderLabel = "菜单",
                                ActionData = "/菜单",
                            },
                            new MessageButton
                            {
                                RenderLabel = "返回",
                                ActionData = "/返回",
                            },
                        ],
                    },
                ],
            };

            MessageContent content = new MessageContent()
                .AddMarkdown("# **黑体大标题**\n\n- 功能1\n- 功能2\n- 功能3")
                .AddButton(keyboard);

            await args.Adaptor.SendMessage(content);
        }

        [Command("ID", "测试", MessageScene.Group)]
        [Command("ID", "测试", MessageScene.GroupAt)]
        public async Task TestUID(CommandArgs args)
        {
            await args.Adaptor.SendMessage($"{args.Message.GroupId}\n{args.Message.AuthorId}\n{args.Message.AuthorName}");
        }
        [Command("主动", "测试", MessageScene.Group)]
        [Command("主动", "测试", MessageScene.GroupAt)]
        public void TestMessgae(CommandArgs args)
        {
            Adaptor.SendMessage(args.Message.GroupId!, $"这是一条主动消息测试!");
        }
    }
}
