using System.Text;
using Windy.SDK;
using Windy.SDK.Adaptor;
using Windy.SDK.Adaptor.Milky;
using Windy.SDK.Command;
using Windy.SDK.Events;
using Windy.SDK.Plugin;
using Windy.SDK.Utils;

namespace ExamplePlugin.Milky
{
    public sealed class Plugin : WindyPlugin
    {
        public override string Name => "ExamplePlugin.Milky";

        public override string Version => "1.0.0";

        public override string Author => "Cjx";

        public override string Description => "Milky 适配器示例插件";

        public override AdaptorType RequiredAdaptor => AdaptorType.Milky;

        public override void Initialize()
        {
            JsonTool.Create<Config>(Path.Combine(AppContext.BaseDirectory, "Config", "ExampleMilky.json")).Read();
            Adaptor.OnMessage += OnMilkyMessageAsync;
            Adaptor.OnProCommand += OnMilkyProHandleAsync;

            MilkyAdaptor milky = (MilkyAdaptor)Adaptor;
            milky.OnBotOffline += OnBotOfflineAsync;
            milky.OnMessageRecall += OnMessageRecallAsync;
            milky.OnFriendRequest += OnFriendRequestAsync;
            milky.OnGroupJoinRequest += OnGroupJoinRequestAsync;
            milky.OnGroupInvitedJoinRequest += OnGroupInvitedJoinRequestAsync;
            milky.OnGroupInvitation += OnGroupInvitationAsync;
            milky.OnFriendNudge += OnFriendNudgeAsync;
            milky.OnFriendFileUpload += OnFileUploadAsync;
            milky.OnGroupAdminChange += OnGroupAdminChangeAsync;
            milky.OnGroupEssenceMessageChange += OnGroupEssenceMessageChangeAsync;
            milky.OnGroupMemberIncrease += OnGroupMemberIncreaseAsync;
            milky.OnGroupMemberDecrease += OnGroupMemberDecreaseAsync;
            milky.OnGroupNameChange += OnGroupNameChangeAsync;
            milky.OnGroupMessageReaction += OnGroupMessageReactionAsync;
            milky.OnGroupMute += OnGroupMuteAsync;
            milky.OnGroupWholeMute += OnGroupWholeMuteAsync;
            milky.OnGroupNudge += OnGroupNudgeAsync;
            milky.OnGroupFileUpload += OnFileUploadAsync;
        }

        //提前处理消息
        private static Task OnMilkyMessageAsync(MessageEventArgs args)
        {
            Message.Blue($"[ExamplePlugin.Milky][Message] scene={args.Scene}, group={args.GroupId}, author={args.AuthorId}, name={args.AuthorName}, content={args.Content}");
            if (!args.Content.Trim().Equals("milky拦截", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            args.Handled = true;
            return args.Adaptor.SendMessage("这条 Milky 消息已被 Hook 拦截，不会继续进入命令系统.");
        }

        //提前处理指令
        private static Task OnMilkyProHandleAsync(CommandArgs args)
        {
            if (!args.CommandName.Equals("milkypro拦截", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            args.Handled = true;
            return args.Adaptor.SendMessage("Milky ProHandle 已拦截，命令方法不会执行.");
        }

        private static Task OnBotOfflineAsync(MilkyBotOfflineEventArgs args)
        {
            Message.Red($"[ExamplePlugin.Milky][{args.Type}] reason={args.Reason}");
            return Task.CompletedTask;
        }

        private static Task OnMessageRecallAsync(MilkyMessageRecallEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] scene={args.MessageScene}, peer={args.PeerId}, seq={args.MessageSeq}, sender={args.SenderId}, operator={args.OperatorId}");
            return Task.CompletedTask;
        }

        //好友申请
        private static Task OnFriendRequestAsync(MilkyFriendRequestEventArgs args)
        {
            Message.Green($"[ExamplePlugin.Milky][{args.Type}] initiator={args.InitiatorId}, uid={args.InitiatorUid}, comment={args.Comment}, via={args.Via}");
            if (args.Comment.Contains("同意test", StringComparison.OrdinalIgnoreCase))
            {
                return args.AcceptAsync();
            }

            if (args.Comment.Contains("拒绝test", StringComparison.OrdinalIgnoreCase))
            {
                return args.RejectAsync("ExamplePlugin.Milky 拒绝好友申请");
            }

            args.Ignore();
            return Task.CompletedTask;
        }
        //加群申请
        private static Task OnGroupJoinRequestAsync(MilkyGroupJoinRequestEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, initiator={args.InitiatorId}, seq={args.NotificationSeq}, comment={args.Comment}, filtered={args.IsFiltered}");
            if (args.Comment.Contains("同意test", StringComparison.OrdinalIgnoreCase))
            {
                return args.AcceptAsync();
            }

            if (args.Comment.Contains("拒绝test", StringComparison.OrdinalIgnoreCase))
            {
                return args.RejectAsync("ExamplePlugin.Milky 拒绝加群申请");
            }

            args.Ignore();
            return Task.CompletedTask;
        }

        private static Task OnGroupInvitedJoinRequestAsync(MilkyGroupInvitedJoinRequestEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, initiator={args.InitiatorId}, target={args.TargetUserId}, seq={args.NotificationSeq}");
            args.Ignore();
            return Task.CompletedTask;
        }

        //大概是邀请入群,但是我还没测试成功过
        private static Task OnGroupInvitationAsync(MilkyGroupInvitationEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, source={args.SourceGroupId}, initiator={args.InitiatorId}, seq={args.InvitationSeq}");
            args.Ignore();
            return Task.CompletedTask;
        }

        private static Task OnFriendNudgeAsync(MilkyFriendNudgeEventArgs args)
        {
            Message.Green($"[ExamplePlugin.Milky][{args.Type}] user={args.UserId}, action={args.DisplayAction}, suffix={args.DisplaySuffix}");
            return Task.CompletedTask;
        }

        private static Task OnFileUploadAsync(MilkyFileUploadEventArgs args)
        {
            Message.Green($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, user={args.UserId}, file={args.FileName}, size={args.FileSize}");
            return Task.CompletedTask;
        }

        private static Task OnGroupAdminChangeAsync(MilkyGroupAdminChangeEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, user={args.UserId}, operator={args.OperatorId}, set={args.IsSet}");
            return Task.CompletedTask;
        }

        private static Task OnGroupEssenceMessageChangeAsync(MilkyGroupEssenceMessageChangeEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, seq={args.MessageSeq}, operator={args.OperatorId}, set={args.IsSet}");
            return Task.CompletedTask;
        }

        //群成员增加 包括自己
        private static Task OnGroupMemberIncreaseAsync(MilkyGroupMemberChangeEventArgs args)
        {
            Message.Green($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, user={args.UserId}, operator={args.OperatorId}, invitor={args.InvitorId}");
            return Task.CompletedTask;
        }
        //群成员减少
        private static Task OnGroupMemberDecreaseAsync(MilkyGroupMemberChangeEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, user={args.UserId}, operator={args.OperatorId}");
            return Task.CompletedTask;
        }

        private static Task OnGroupNameChangeAsync(MilkyGroupNameChangeEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, name={args.NewGroupName}, operator={args.OperatorId}");
            return Task.CompletedTask;
        }

        private static Task OnGroupMessageReactionAsync(MilkyGroupMessageReactionEventArgs args)
        {
            Message.Green($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, user={args.UserId}, seq={args.MessageSeq}, face={args.FaceId}, add={args.IsAdd}");
            return Task.CompletedTask;
        }

        private static Task OnGroupMuteAsync(MilkyGroupMuteEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, user={args.UserId}, operator={args.OperatorId}, duration={args.Duration}");
            return Task.CompletedTask;
        }

        private static Task OnGroupWholeMuteAsync(MilkyGroupWholeMuteEventArgs args)
        {
            Message.Yellow($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, operator={args.OperatorId}, mute={args.IsMute}");
            return Task.CompletedTask;
        }

        private static Task OnGroupNudgeAsync(MilkyGroupNudgeEventArgs args)
        {
            Message.Green($"[ExamplePlugin.Milky][{args.Type}] group={args.GroupId}, sender={args.SenderId}, receiver={args.ReceiverId}, action={args.DisplayAction}");
            return Task.CompletedTask;
        }

        [Command("milkyid", "查看 Milky 消息上下文", MessageScene.Group)]
        [Command("milkyid", "查看 Milky 消息上下文", MessageScene.Private)]
        public async Task IdAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage($"Scene: {args.Message.Scene}\nGroup: {args.Message.GroupId}\nAuthor: {args.Message.AuthorId}\nName: {args.Message.AuthorName}\nRole: {args.Message.Role}");
        }

        [Command("milky文本", "发送 Milky 文本/提及示例", MessageScene.Group)]
        [Command("milky文本", "发送 Milky 文本/提及示例", MessageScene.Private)]
        public async Task TextAsync(CommandArgs args)
        {
            MessageContent content = new MessageContent()
                .AddText("Milky 文本消息示例 ")
                .AddMention(args.Message.AuthorId)
                .AddText(" hello");

            await args.Adaptor.SendMessage(content);
        }

        [Command("milky艾特", "Milky 文本里艾特发送人", MessageScene.Group)]
        public async Task MentionUserAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage(new MessageContent()
                .AddMention(args.Message.AuthorId)
                .AddText(" 这是 Milky mention 示例"));
        }

        [Command("milky全体", "发送 Milky @全体 示例", MessageScene.Group)]
        public async Task MentionAllAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage(new MessageContent().AddMentionAll().AddText(" Milky mention_all 示例"));
        }

        [Command("milky图片", "发送 Milky base64 图片示例", MessageScene.Group)]
        [Command("milky图片", "发送 Milky base64 图片示例", MessageScene.Private)]
        public async Task ImageAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage(new MessageContent()
                .AddText("Milky 图片示例：")
                .AddImage(Convert.FromBase64String(SamplePngBase64), "windy.png"));
        }

        [Command("milky文件", "上传 Milky 文件示例", MessageScene.Group)]
        [Command("milky文件", "上传 Milky 文件示例", MessageScene.Private)]
        public async Task FileAsync(CommandArgs args)
        {
            string fileText = Convert.ToBase64String(Encoding.UTF8.GetBytes("Windy Milky file upload test."));
            MessageContent content = new MessageContent()
                .AddFile("base64://" + fileText, "windy_milky_test.txt");

            await args.Adaptor.SendMessage(content);
        }

        [Command("milky主动", "Milky 主动群消息测试", MessageScene.Group)]
        public void ActiveGroupMessageAsync(CommandArgs args)
        {
            Adaptor.SendMessage(args.Message.GroupId!, "这是一条 Milky 主动群消息测试.");
        }

        [Command("milky群公告", "获取 Milky 群公告列表", MessageScene.Group)]
        public async Task GroupAnnouncementsAsync(CommandArgs args)
        {
            IReadOnlyList<MilkyGroupAnnouncementEntity> announcements = await ((MilkyAdaptor)Adaptor).GetGroupAnnouncementsAsync(args.Message.GroupId!);
            string text = announcements.Count == 0
                ? "当前群没有公告."
                : string.Join("\n", announcements.Select(item => $"{item.AnnouncementId}: {item.Content}"));
            await args.Adaptor.SendMessage(text);
        }

        [Command("milky群精华", "获取 Milky 群精华消息列表", MessageScene.Group)]
        public async Task GroupEssenceMessagesAsync(CommandArgs args)
        {
            MilkyGroupEssenceMessagesResult result = await ((MilkyAdaptor)Adaptor).GetGroupEssenceMessagesAsync(args.Message.GroupId!);
            string text = result.Messages.Count == 0
                ? "当前群没有精华消息."
                : string.Join("\n", result.Messages.Select(item => $"{item.MessageSeq}: {item.SenderName} ({item.Segments.Count} 段)"));
            await args.Adaptor.SendMessage(text);
        }

        [Command("milky群通知", "获取 Milky 群通知列表", MessageScene.Group)]
        public async Task GroupNotificationsAsync(CommandArgs args)
        {
            MilkyGroupNotificationsResult result = await ((MilkyAdaptor)Adaptor).GetGroupNotificationsAsync();
            string text = result.Notifications.Count == 0
                ? "当前没有群通知."
                : string.Join("\n", result.Notifications.Select(item => $"{item.NotificationSeq}: {item.Type} state={item.State}"));
            await args.Adaptor.SendMessage(text);
        }

        [Command("milkypro拦截", "演示 Milky ProHandle 拦截", MessageScene.Group)]
        [Command("milkypro拦截", "演示 Milky ProHandle 拦截", MessageScene.Private)]
        public async Task MilkyProHandleBlockedAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage("如果看到这条消息，说明 Milky ProHandle 没有拦截成功.");
        }

        [Command("milky表情", "Milky QQ 表情示例", MessageScene.Group)]
        [Command("milky表情", "Milky QQ 表情示例", MessageScene.Private)]
        public async Task FaceAsync(CommandArgs args)
        {
            await args.Adaptor.SendMessage(new MessageContent().AddFace("4").AddText(" Milky 表情示例"));
        }

    [Command("milky回复", "Milky 回复消息示例", MessageScene.Group)]
    [Command("milky回复", "Milky 回复消息示例", MessageScene.Private)]
    public async Task ReplyAsync(CommandArgs args)
    {
        await args.Adaptor.SendMessage(
            args.CreateMessageContent().AddReply().AddText(" Milky 回复消息示例"));
    }

        [Command("milky转发", "Milky 转发消息示例", MessageScene.Group)]
        [Command("milky转发", "Milky 转发消息示例", MessageScene.Private)]
        public async Task ForwardAsync(CommandArgs args)
        {
            MessageContent content = new MessageContent()
                .AddForward([
                    new ForwardedMessageNode
                    {
                        UserId = args.Message.AuthorId,
                        SenderName = args.Message.AuthorName,
                        Message = new MessageContent().AddText("这是一条 Milky 转发消息的内容示例"),
                    }
                ], "Milky 转发示例");
            await args.Adaptor.SendMessage(content);
        }
        private const string SamplePngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=";
    }
}
