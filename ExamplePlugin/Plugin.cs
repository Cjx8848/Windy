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
            this.RegisterGroupMemberAddHook(OnGroupMemberAddAsync, priority: 100);
            this.RegisterGroupMemberRemoveHook(OnGroupMemberRemoveAsync, priority: 100);
            this.RegisterGroupMsgReceiveHook(OnGroupMsgReceiveAsync, priority: 100);
            this.RegisterGroupMsgRejectHook(OnGroupMsgRejectAsync, priority: 100);
            this.RegisterFriendAddHook(OnFriendAddAsync, priority: 100);
            this.RegisterFriendDelHook(OnFriendDelAsync, priority: 100);
            this.RegisterC2CMsgReceiveHook(OnC2CMsgReceiveAsync, priority: 100);
            this.RegisterC2CMsgRejectHook(OnC2CMsgRejectAsync, priority: 100);
            this.RegisterSubscribeMessageStatusHook(OnSubscribeMessageStatusAsync, priority: 100);
            this.RegisterInteractionCreateHook(OnInteractionCreateAsync, priority: 100);
            this.RegisterMessageAuditPassHook(OnMessageAuditPassAsync, priority: 100);
            this.RegisterMessageAuditRejectHook(OnMessageAuditRejectAsync, priority: 100);
            this.RegisterQQOfficialEventHook(QQOfficialEventType.AtMessageCreate, OnGenericOfficialEventAsync, priority: 100);
            this.RegisterQQOfficialEventHook(QQOfficialEventType.MessageCreate, OnGenericOfficialEventAsync, priority: 100);
            this.RegisterQQOfficialEventHook(QQOfficialEventType.DirectMessageCreate, OnGenericOfficialEventAsync, priority: 100);
        }
        private static Task OnReadyAsync(QQOfficialReadyEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] session={args.SessionId}, event={args.EventId}");
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

        private static Task OnGroupMemberAddAsync(QQOfficialGroupMemberEventArgs args)
        {
            Message.Yellow($"[Example][{args.Type}] group={args.GroupOpenId}, member={args.MemberOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnGroupMemberRemoveAsync(QQOfficialGroupMemberEventArgs args)
        {
            Message.Yellow($"[Example][{args.Type}] group={args.GroupOpenId}, member={args.MemberOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnGroupMsgReceiveAsync(QQOfficialGroupOperationEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] group={args.GroupOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnGroupMsgRejectAsync(QQOfficialGroupOperationEventArgs args)
        {
            Message.Red($"[Example][{args.Type}] group={args.GroupOpenId}, operator={args.OperatorMemberOpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnFriendAddAsync(QQOfficialUserOperationEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] user={args.OpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnFriendDelAsync(QQOfficialUserOperationEventArgs args)
        {
            Message.Yellow($"[Example][{args.Type}] user={args.OpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnC2CMsgReceiveAsync(QQOfficialUserOperationEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] user={args.OpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnC2CMsgRejectAsync(QQOfficialUserOperationEventArgs args)
        {
            Message.Red($"[Example][{args.Type}] user={args.OpenId}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnSubscribeMessageStatusAsync(QQOfficialSubscribeMessageStatusEventArgs args)
        {
            Message.Yellow($"[Example][{args.Type}] user={args.OpenId}, status={args.Status}, timestamp={args.Timestamp}");
            return Task.CompletedTask;
        }

        private static Task OnInteractionCreateAsync(QQOfficialInteractionEventArgs args)
        {
            Message.Blue($"[Example][{args.Type}] id={args.Id}, event={args.EventId}");
            return Task.CompletedTask;
        }

        private static Task OnMessageAuditPassAsync(QQOfficialMessageAuditEventArgs args)
        {
            Message.Green($"[Example][{args.Type}] audit={args.AuditId}, message={args.MessageId}, event={args.EventId}");
            return Task.CompletedTask;
        }

        private static Task OnMessageAuditRejectAsync(QQOfficialMessageAuditEventArgs args)
        {
            Message.Red($"[Example][{args.Type}] audit={args.AuditId}, message={args.MessageId}, event={args.EventId}");
            return Task.CompletedTask;
        }

        private static Task OnGenericOfficialEventAsync(QQOfficialEventArgs args)
        {
            Message.Blue($"[Example][{args.Type}] event={args.EventId}, raw={args.Raw}");
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
            await args.Adaptor.SendMessage($"{args.Message.GroupId}\n{args.Message.AuthorId}");
        }
    }
}
