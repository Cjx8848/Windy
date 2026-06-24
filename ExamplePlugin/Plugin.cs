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
            Adaptor.OnMessage += OnMessageAsync;
            Adaptor.OnProCommand += OnProHandleAsync;
            Adaptor.OnGroupAtNoCommand += OnGroupAtNoCommandAsync;

            QQOfficialAdaptor qq = (QQOfficialAdaptor)Adaptor;
            qq.OnReady += OnReadyAsync;
            qq.OnResumed += OnResumedAsync;
            qq.OnFriendAdd += OnFriendAddAsync;
            qq.OnGroupAddRobot += OnGroupAddRobotAsync;
            qq.OnGroupDelRobot += OnGroupDelRobotAsync;
            qq.OnGroupMemberAdd += OnGroupMemberAddAsync;
            qq.OnGroupMemberRemove += OnGroupMemberRemoveAsync;
        }

        //提前处理消息
        private static Task OnMessageAsync(MessageEventArgs args)
        {
            if (!args.Content.Trim().Equals("消息拦截", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            args.Handled = true;
            return args.Adaptor.SendMessage("QQ 官方 OnMessage Hook 已拦截，后续命令不会执行.");
        }

        //提前处理指令
        private static Task OnProHandleAsync(CommandArgs args)
        {
            if (!args.CommandName.Equals("pro拦截", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            args.Handled = true;
            return args.Adaptor.SendMessage("QQ 官方 ProHandle 已拦截，命令方法不会执行.");
        }

        //处理没有匹配指令 但是是at消息
        private static Task OnGroupAtNoCommandAsync(MessageEventArgs args)
        {
            args.Handled = true;
            return args.Adaptor.SendMessage($"收到没有匹配指令的 QQ 官方 GroupAt 消息：{args.Content}");
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

        private static Task OnFriendAddAsync(QQOfficialUserOperationEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] user={args.OpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnGroupDelRobotAsync(QQOfficialGroupOperationEventArgs args)
        {
            Message.Yellow($"[Example][{args.Type}] group={args.GroupOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnGroupMemberAddAsync(QQOfficialGroupMemberEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] group={args.GroupOpenId}, member={args.MemberOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnGroupMemberRemoveAsync(QQOfficialGroupMemberEventArgs args)
        {
            Message.Yellow($"[Example][{args.Type}] group={args.GroupOpenId}, member={args.MemberOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
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
                .AddMarkdown($"# **黑体大标题**\n\n- 功能1\n- 功能2\n- 功能3\n{QQOfficialLabel.CommandInput("/help")}")
                .AddButton(keyboard);

            await args.Adaptor.SendMessage(content);
        }

        [Command("ID", "测试", MessageScene.Group)]
        [Command("ID", "测试", MessageScene.GroupAt)]
        public async Task TestUID(CommandArgs args)
        {
            await args.Adaptor.SendMessage($"{args.Message.GroupId}\n{args.Message.AuthorId}\n{args.Message.AuthorName}");
        }

        [Command("艾特", "QQ 官方文本里艾特发送人", MessageScene.Group)]
        [Command("艾特", "QQ 官方文本里艾特发送人", MessageScene.GroupAt)]
        public async Task MentionUserAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage($"{QQOfficialLabel.At(args.Message.AuthorId)} 这是 QQ 官方文本 AT 示例");
        }

        [Command("全体", "QQ 官方文本里艾特全体示例", MessageScene.Group)]
        [Command("全体", "QQ 官方文本里艾特全体示例", MessageScene.GroupAt)]
        public async Task MentionAllAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage($"{QQOfficialLabel.AtEveryone()} 这是 QQ 官方 @全体 示例");
        }

        [Command("pro拦截", "演示 ProHandle 拦截", MessageScene.Group)]
        [Command("pro拦截", "演示 ProHandle 拦截", MessageScene.GroupAt)]
        public async Task ProHandleBlockedAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage("如果看到这条消息，说明 ProHandle 没有拦截成功.");
        }

        [Command("主动", "测试", MessageScene.Group)]
        [Command("主动", "测试", MessageScene.GroupAt)]
        public void TestMessgae(CommandArgs args)
        {
             Adaptor.SendMessage(args.Message.GroupId!, $"这是一条主动消息测试!");
        }

     }
}
